// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited


#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LEGOModelImporter
{
    [InitializeOnLoad]
    public class SceneBrickBuilder
    {
        #region Editor Prefs
        private static bool buildWithBricks = false;
        private static readonly float stickySnapDistanceDefault = 20.0f;
        private static float stickySnapDistance = stickySnapDistanceDefault;
        public static float StickySnapDistance {
            get { return stickySnapDistance;}
            set { EditorPrefs.SetFloat(stickySnapDistancePrefsKey, value);}
        }
        private static int maxTriesPerBrick = BrickBuildingUtility.defaultMaxBricksToConsiderWhenFindingConnections;
        public static int MaxTriesPerBrick {
            get { return maxTriesPerBrick;}
            set { EditorPrefs.SetInt(maxTriesPerBrickPrefsKey, value);}
        }
        private static bool autoUpdateHierarchyDefault = false;
        private static bool autoUpdateHierarchy = autoUpdateHierarchyDefault;
        private static bool rotateGizmoOnDefault = true;
        private static bool rotateGizmoOn = rotateGizmoOnDefault;
        static readonly string rotateGizmoOnPrefsKey = "com.unity.lego.modelimporter.rotateGizmoOn";
        private const string rotateGizmoOnMenuPath = "LEGO Tools/Show Rotate Gizmo";
        private static bool disablePrefabAutoSaveDefault = true;
        private static bool disablePrefabAutoSave = disablePrefabAutoSaveDefault;
        static readonly string brickBuildingPrefsKey = "com.unity.lego.modelimporter.editorBrickBuilding";
        static readonly string stickySnapDistancePrefsKey = "com.unity.lego.modelimporter.stickySnapDistance";
        static readonly string maxTriesPerBrickPrefsKey = "com.unity.lego.modelimporter.maxTriesPerBrick";
        static readonly string selectConnectedPrefsKey = "com.unity.lego.modelimporter.selectConnected";
        static readonly string disablePrefabAutoSavePrefsKey = "com.unity.lego.modelimporter.disablePrefabAutoSave";

#if !PUBLIC
        static readonly string autoUpdateHierarchyPrefsKey = "com.unity.lego.modelimporter.autoUpdateHierarchy";
        private const string autoUpdateHierarchyMenuPath = "LEGO Tools/Auto Update Hierarchy";
#endif

        private const string brickBuildingShortcut = "%&n";
        public const string brickBuildingMenuPath = "LEGO Tools/Brick Building";

        private const string expandSelectionShortcut = "%&e";
        public const string expandSelectionMenuPath = "LEGO Tools/Selection/Expand To Connected Bricks";

        public const string rotateUpMenuPath = "LEGO Tools/Rotate/Rotate Brick Up";
        public const string rotateLeftMenuPath = "LEGO Tools/Rotate/Rotate Brick Left";
        public const string rotateDownMenuPath = "LEGO Tools/Rotate/Rotate Brick Down";
        public const string rotateRightMenuPath = "LEGO Tools/Rotate/Rotate Brick Right";
        public const string nudgeUpMenuPath = "LEGO Tools/Rotate/Nudge Brick Up";
        public const string nudgeLeftMenuPath = "LEGO Tools/Rotate/Nudge Brick Left";
        public const string nudgeDownMenuPath = "LEGO Tools/Rotate/Nudge Brick Down";
        public const string nudgeRightMenuPath = "LEGO Tools/Rotate/Nudge Brick Right";

        private static bool selectConnectedDefault = true;
        private static bool selectConnected = selectConnectedDefault;
        
        public const string editorSelectConnectedMenuPath = "LEGO Tools/Select Connected Bricks";
        private const string editorSelectConnectedShortcut = "%&k";

        private const string disablePrefabAutoSaveMenuPath = "LEGO Tools/Disable Prefab Auto Save";

        #endregion

        // Used to keep track of which brick is under the mouse between frames
        // Is only reset if the current event is a valid picking event
        private static Brick hitBrick = null;

        private static List<GameObject> queuedSelection = null;
        private static UnityEngine.Object[] lastSelection = new UnityEngine.Object[] { };
        private static UnityEngine.Object lastActiveObject = null;
        private static Event currentEvent = null;

        private static HashSet<Brick> selectedBricks = new HashSet<Brick>();
        private static HashSet<Brick> bricksRelatedToMovingBricks = new HashSet<Brick>();
        private static HashSet<Brick> bricksRelatedToDeletedBricks = new HashSet<Brick>();

        private static Bounds selectionBounds = new Bounds();

        // Focus brick always represents the most recently clicked or dragged brick
        private static Brick focusBrick = null;
        public static Brick FocusBrick {
            get {return focusBrick;}
        }

        // Used for caching of brick rotations and offsets relative to focusBrick
        private static List<(Quaternion, Vector3)> rotationOffsets = new List<(Quaternion, Vector3)>();
        private static (Connection, Connection) currentConnection = (null, null);
        private static Vector3 mousePosition = Vector3.zero;
        private static Vector3 pickupOffset = Vector3.zero;
        private static Brick[] bricks = null;
        private static bool sceneChanged = false;
        private static Brick draggedBrick = null;
        private static Vector3 placeOffset = Vector3.zero;                

        public enum SelectionState
        {
            noSelection,
            dragging,
            selected,
            moving
        }

        enum InteractionKey
        {
            none,
            left,
            right,
            up,
            down
        }

        private static InteractionKey rotateDirection = InteractionKey.none;
        private static InteractionKey nudgeDirection = InteractionKey.none;
        private static SelectionState currentSelectionState = SelectionState.noSelection;
        public static SelectionState CurrentSelectionState {
            get { return currentSelectionState; }
            private set 
            {
                rotateDirection = InteractionKey.none;
                nudgeDirection = InteractionKey.none;
                currentSelectionState = value;
            } 
        }
        
        private static RaycastHit currentHitPoint = new RaycastHit();
        private static bool aboutToPlace = false;
        private static float currentMouseDelta = 0.0f;
        private static Ray currentRay = new Ray();
        private static Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
        private static bool undoQueued = false;
        private static bool undoRedo = false;
        private static bool duplicateQueued = false;
        private static bool dragAndDropQueued = false;
        private static bool deleteQueued = false;
        private static int currentUndoGroupIndex;
        private static HashSet<Model> modelsToRecomputePivot = new HashSet<Model>();
        private static bool sceneViewCurrentlyInFocus = false;
        private static HashSet<ConnectionField> dirtyFields = new HashSet<ConnectionField>();
        private static bool collapseUndo = false;
        private static int framesSinceSelectionChanged = 0;

        private enum BrickBuildingState
        {
            off,
            on,
            playMode
        }

        #region scene Handles
        static Texture2D rotationArrowUpNormal;
        static Texture2D rotationArrowRightNormal;
        static Texture2D rotationArrowDownNormal;
        static Texture2D rotationArrowLeftNormal;
        static Texture2D rotationArrowUpActive;
        static Texture2D rotationArrowRightActive;
        static Texture2D rotationArrowDownActive;
        static Texture2D rotationArrowLeftActive;
        static Texture2D rotationArrowUpHover;
        static Texture2D rotationArrowRightHover;
        static Texture2D rotationArrowDownHover;
        static Texture2D rotationArrowLeftHover;
        #endregion

        static void LoadTextures()
        {
            rotationArrowUpNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Forwards Normal@2x.png");
            rotationArrowRightNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Right Normal@2x.png");
            rotationArrowDownNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Backwards Normal@2x.png");
            rotationArrowLeftNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Left Normal@2x.png");

            rotationArrowUpActive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Forwards Active@2x.png");
            rotationArrowRightActive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Right Active@2x.png");
            rotationArrowDownActive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Backwards Active@2x.png");
            rotationArrowLeftActive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Left Active@2x.png");

            rotationArrowUpHover = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Forwards Hover@2x.png");
            rotationArrowRightHover = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Right Hover@2x.png");
            rotationArrowDownHover = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Backwards Hover@2x.png");
            rotationArrowLeftHover = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Rotate Left Hover@2x.png");
        }

        static BrickBuildingState GetBrickBuildingState()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode ? BrickBuildingState.playMode : (buildWithBricks ? BrickBuildingState.on : BrickBuildingState.off);
        }

        static void ConnectivityUpdateStarted()
        {
            DisableConnectivityEvents();
        }

        static void ConnectivityUpdateFinished()
        {
            SetupConnectivityEvents();
        }

        static SceneBrickBuilder()
        {            
            buildWithBricks = EditorPrefs.GetBool(brickBuildingPrefsKey, false);
            stickySnapDistance = EditorPrefs.GetFloat(stickySnapDistancePrefsKey, stickySnapDistanceDefault);
            selectConnected = EditorPrefs.GetBool(selectConnectedPrefsKey, selectConnectedDefault);
            disablePrefabAutoSave = EditorPrefs.GetBool(disablePrefabAutoSavePrefsKey, disablePrefabAutoSaveDefault);
            #if PUBLIC
                autoUpdateHierarchy = true;
            #else
                autoUpdateHierarchy = EditorPrefs.GetBool(autoUpdateHierarchyPrefsKey, autoUpdateHierarchyDefault);
            #endif
            if (disablePrefabAutoSave)
            {
                EditorPrefs.SetBool("PrefabEditing.AutoSave", false);
            }

            rotateGizmoOn = EditorPrefs.GetBool(rotateGizmoOnPrefsKey, rotateGizmoOnDefault);

            // Events we always need to know about
            ConnectivityVersionChecker.updateStarted -= ConnectivityUpdateStarted;
            ConnectivityVersionChecker.updateStarted += ConnectivityUpdateStarted;
            ConnectivityVersionChecker.updateFinished -= ConnectivityUpdateFinished;
            ConnectivityVersionChecker.updateFinished += ConnectivityUpdateFinished;

            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

            SetupBrickBuilding(GetBrickBuildingState());
            LoadTextures();            
        }

        public static void MarkSceneDirty()
        {
            sceneChanged = true;
        }

        private static void DisableConnectivityEvents()
        {
            SceneView.duringSceneGui -= OnSceneGUIDefault;
            SceneView.duringSceneGui -= OnSceneGUIBuilding;

            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.update -= EditorUpdate;

            Selection.selectionChanged -= OnSelectionChanged;

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            ConnectionField.dirtied -= OnConnectionFieldsDirtied;

            Brick.destroyed -= OnBrickDestroyed;

            PrefabStage.prefabStageClosing -= PrefabStageClosing;
            PrefabStage.prefabStageOpened -= PrefabStageOpened;

            PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdated;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode -= ActiveSceneChanged;
        }

        private static void SetupConnectivityEvents()
        {
            // Good form to remove events before adding them
            DisableConnectivityEvents();
            var state = GetBrickBuildingState();
            switch(state)
            {
                case BrickBuildingState.on:
                {
                    EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

                    EditorApplication.update += EditorUpdate;

                    SceneView.duringSceneGui += OnSceneGUIBuilding;
                    
                    Selection.selectionChanged += OnSelectionChanged;

                    Undo.undoRedoPerformed += OnUndoRedoPerformed;

                    PrefabStage.prefabStageClosing += PrefabStageClosing;
                    PrefabStage.prefabStageOpened += PrefabStageOpened;

                    EditorSceneManager.sceneOpened += OnSceneOpened;
                    EditorSceneManager.activeSceneChangedInEditMode += ActiveSceneChanged;

                    ConnectionField.dirtied += OnConnectionFieldsDirtied;

                    PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;

                    Brick.destroyed += OnBrickDestroyed;
                }
                break;
                case BrickBuildingState.off:
                {
                    EditorApplication.update += EditorUpdate;

                    SceneView.duringSceneGui += OnSceneGUIDefault;

                    Selection.selectionChanged += OnSelectionChanged;

                    Undo.undoRedoPerformed += OnUndoRedoPerformed;

                    ConnectionField.dirtied += OnConnectionFieldsDirtied;
                    EditorSceneManager.sceneOpened += OnSceneOpened;
                    EditorSceneManager.activeSceneChangedInEditMode += ActiveSceneChanged;

                    PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;

                    Brick.destroyed += OnBrickDestroyed;
                }
                break;
                default:
                break;
            }
        }

        private static void SetupBrickBuilding(BrickBuildingState newState)
        {
            SetupConnectivityEvents();

            rotateDirection = InteractionKey.none;
            nudgeDirection = InteractionKey.none;
            undoRedo = false;
            duplicateQueued = false;
            dragAndDropQueued = false;
            
            sceneChanged = true;
            SyncSceneBricks();

            switch(newState)
            {
                case BrickBuildingState.on:
                {                    
                    ConnectivityUIManager.ShowTools(ConnectivityUIManager.ShouldShowTools());
                    
                    if (Selection.transforms.Length > 0)
                    {
                        SetFromSelection();
                        CurrentSelectionState = SelectionState.selected;
                    }

                    foreach(var brick in bricks)
                    {
                        if(brick.colliding)
                        {
                            brick.SetMaterial(true, false);
                        }
                    }
                }
                break;
                case BrickBuildingState.off:
                {
                    ConnectivityUIManager.ShowTools(ConnectivityUIManager.ShouldShowTools());
                    PlaceBrick();
                    SetFocusBrick(null);

                    OnSelectionChanged();
                    foreach(var brick in bricks)
                    {
                        if(brick.colliding)
                        {
                            brick.SetMaterial(false, false);
                        }
                    }
                }
                break;
                case BrickBuildingState.playMode:
                {
                    ConnectivityUIManager.ShowTools(false, false);
                    PlaceBrick();
                    SetFocusBrick(null);
                    foreach(var brick in bricks)
                    {
                        if(brick.colliding)
                        {
                            brick.SetMaterial(false, false);
                        }
                    }
                }
                break;
            }
        }        

        private static HashSet<Brick> UpdateBrickCollision(IEnumerable<Brick> bricksToCheck)
        {
            HashSet<Brick> previouslyColliding = new HashSet<Brick>();
            Physics.SyncTransforms();
            foreach(var brick in bricksToCheck)
            {
                // Only check colliding bricks
                if(brick.colliding)
                {
                    if(!brick.IsColliding(out int hits))
                    {
                        previouslyColliding.Add(brick);
                        brick.UpdateColliding(false);
                        var connection = FindConnection(brick);
                        if(connection == (null, null))
                        {
                            continue;
                        }

                        var pivot = connection.Item1.field.connectivity.part.brick.transform.position;
                        if(ConnectionField.IsConnectionValid(connection.Item1, connection.Item2, pivot))
                        {
                            ConnectionField.Connect(connection.Item1, connection.Item2, pivot);
                        }
                    }
                    else
                    {
                        var collidingWithSelectedBrick = false;
                        for(var i = 0; i < hits; i++)
                        {
                            var collider = BrickBuildingUtility.colliderBuffer[i];
                            if(!collider)
                            {
                                continue;
                            }

                            var collidingBrick = collider.GetComponentInParent<Brick>();
                            if(collidingBrick && collidingBrick != brick)
                            {
                                var rotating = rotateDirection != InteractionKey.none;
                                var brickInSelected = selectedBricks.Contains(brick);
                                var collidingBrickInSelected = selectedBricks.Contains(collidingBrick);

                                // If both are in selected bricks, do nothing
                                // If one is in selected bricks and the other isn't, then only do nothing if we are nudging or rotating

                                if((collidingBrickInSelected == brickInSelected) || (CurrentSelectionState == SelectionState.selected && collidingBrickInSelected != brickInSelected && rotating))
                                {
                                    collidingWithSelectedBrick = true;
                                    break;
                                }
                            }
                        }

                        if(collidingWithSelectedBrick)
                        {
                            continue;
                        }
                        
                        brick.UpdateColliding(false);
                    }                    
                }
            }
            return previouslyColliding;
        }        

        [MenuItem(rotateGizmoOnMenuPath, priority = 40)]
        private static void ToggleRotateGizmoOn()
        {
            rotateGizmoOn = !rotateGizmoOn;
            EditorPrefs.SetBool(rotateGizmoOnPrefsKey, rotateGizmoOn);
        }

        [MenuItem(rotateGizmoOnMenuPath, validate = true)]
        private static bool ValidateRotateGizmoOn()
        {
            Menu.SetChecked(rotateGizmoOnMenuPath, rotateGizmoOn);
            return buildWithBricks;
        }

        public static bool GetToggleBrickBuildingStatus()
        {
            return EditorPrefs.GetBool(brickBuildingPrefsKey, false);
        }

        [MenuItem(editorSelectConnectedMenuPath + " " + editorSelectConnectedShortcut, priority = 35)]
        public static void ToggleSelectConnectedBricks()
        {
            selectConnected = !selectConnected;
            EditorPrefs.SetBool(selectConnectedPrefsKey, selectConnected);
            SceneView.RepaintAll();
        }

        [MenuItem(editorSelectConnectedMenuPath + " " + editorSelectConnectedShortcut, validate = true)]
        public static bool ValidateSelectConnectedBricks()
        {
            Menu.SetChecked(editorSelectConnectedMenuPath, selectConnected);
            return buildWithBricks;
        }

        public static bool GetAutoUpdateHierarchy()
        {
            #if PUBLIC
            return true;
            #else
            return EditorPrefs.GetBool(autoUpdateHierarchyPrefsKey, autoUpdateHierarchyDefault);
            #endif
        }

#if !PUBLIC
        [MenuItem(autoUpdateHierarchyMenuPath, priority = 40)]
        public static void ToggleAutoUpdateHierarchy()
        {
            SetAutoUpdateHierarchy(!autoUpdateHierarchy);
        }

        internal static void SetAutoUpdateHierarchy(bool value)
        {
            autoUpdateHierarchy = value;
            EditorPrefs.SetBool(autoUpdateHierarchyPrefsKey, autoUpdateHierarchy);
        }

        [MenuItem(autoUpdateHierarchyMenuPath, validate = true)]
        private static bool ValidateAutoUpdateHierarchy()
        {
            Menu.SetChecked(autoUpdateHierarchyMenuPath, autoUpdateHierarchy);
            return true;
        }
#endif

        public static bool GetSelectConnectedBricks()
        {
            return EditorPrefs.GetBool(selectConnectedPrefsKey, selectConnectedDefault);
        }

        [MenuItem(brickBuildingMenuPath + " " + brickBuildingShortcut, priority = 30)]
        public static void ToggleBrickBuilding()
        {
            buildWithBricks = !buildWithBricks;
            EditorPrefs.SetBool(brickBuildingPrefsKey, buildWithBricks);
            SetupBrickBuilding(GetBrickBuildingState());
        }

        [MenuItem(brickBuildingMenuPath + " " + brickBuildingShortcut, true)]
        private static bool ValidateBrickBuilding()
        {
            Menu.SetChecked(brickBuildingMenuPath, buildWithBricks);
            return !EditorApplication.isPlaying;
        }

        [MenuItem(disablePrefabAutoSaveMenuPath, priority = 45)]
        private static void ToggleDisablePrefabAutoSave()
        {
            disablePrefabAutoSave = !disablePrefabAutoSave;
            EditorPrefs.SetBool(disablePrefabAutoSavePrefsKey, disablePrefabAutoSave);
        }

        [MenuItem(disablePrefabAutoSaveMenuPath, true)]
        private static bool ValidateDisablePrefabAutoSave()
        {
            Menu.SetChecked(disablePrefabAutoSaveMenuPath, disablePrefabAutoSave);
            return true;
        }

        private static void RemoveInvalidConnections(Brick brick)
        {
            // Remove connections and make sure it is undoable
            foreach (var part in brick.parts)
            {
                if (part.connectivity == null)
                {
                    continue;
                }

                foreach (var field in part.connectivity.connectionFields)
                {
                    field.DisconnectAllInvalid();
                }
            }
        }

        private static bool CheckConnectedBricksShareGroup(Brick brick, out HashSet<Brick> connectedBricks)
        {
            connectedBricks = new HashSet<Brick>();
            if(CurrentSelectionState == SelectionState.moving && !autoUpdateHierarchy)
            {                
                return true;
            }

            var modelGroup = brick.GetComponentInParent<ModelGroup>();
            if(!modelGroup)
            {
                return true;
            }

            connectedBricks = brick.GetConnectedBricks();
            connectedBricks.Add(brick);
            foreach(var b in connectedBricks)
            {
                if(b.transform.parent != brick.transform.parent)
                {
                    return false;
                }
            }

            // Check if all bricks in the group are connected
            var bricksInGroup = modelGroup.GetComponentsInChildren<Brick>();
            if(connectedBricks.Intersect(bricksInGroup).Count() != bricksInGroup.Length)
            {
                return false;
            }
            return true;
        }

        private static bool IsBrickValidInHierarchy(Brick brick)
        {
            // We don't care while we're moving
            if(CurrentSelectionState == SelectionState.moving)
            {
                return true;
            }

            // Hierarchy has no rules in case we don't auto update
            if(!autoUpdateHierarchy)
            {
                return true;
            }

            // Unparented bricks always need to be wrapped
            if(brick.transform.parent == null)
            {
                return false;
            }

            // Don't nest bricks
            var bricksInParent = brick.GetComponentsInParent<Brick>();
            if(bricksInParent.Length > 1)
            {
                return false;
            }
            
            // If no group, the brick is invalid
            var modelGroup = brick.GetComponentInParent<ModelGroup>();
            if(!modelGroup)
            {
                return false;
            }

            // Don't nest groups
            var groupsInParent = modelGroup.GetComponentsInParent<ModelGroup>();
            if(groupsInParent.Length > 1)
            {
                return false;
            }

            // If no model, the brick is invalid
            var model = brick.GetComponentInParent<Model>();
            if(!model)
            {
                return false;
            }

            // Don't nest models
            var modelsInParent = model.GetComponentsInParent<Model>();
            if(modelsInParent.Length > 1)
            {
                return false;
            }
            
            return true;
        }

        private static void CheckBrickForHierarchyChange(Brick brick, ref HashSet<Brick> checkedBricks, ref HashSet<Brick> bricksToUpdate)
        {
            brick.transform.hasChanged = false;
            RemoveInvalidConnections(brick);

            if(CurrentSelectionState == SelectionState.moving)
            {
                return;
            }

            if(bricksToUpdate.Contains(brick) || checkedBricks.Contains(brick))
            {
                return;
            }

            if(!CheckConnectedBricksShareGroup(brick, out HashSet<Brick> connectedBricks))
            {
                bricksToUpdate.UnionWith(connectedBricks);
            }

            checkedBricks.UnionWith(connectedBricks);

            if(!IsBrickValidInHierarchy(brick))
            {
                bricksToUpdate.Add(brick);
            }
        }

        private static void CheckChangedTransforms()
        {
            var selection = Selection.activeTransform;
            if (selection != null)
            {
                // Remove any invalid connections on this transform if it has changed
                if (selection.hasChanged)
                {
                    var checkedBricks = new HashSet<Brick>();
                    var bricksToUpdate = new HashSet<Brick>();

                    foreach(var transform in Selection.transforms)
                    {
                        var brick = transform.GetComponentInParent<Brick>();
                        if (brick)
                        {
                            CheckBrickForHierarchyChange(brick, ref checkedBricks, ref bricksToUpdate);
                        }

                        var bricks = transform.GetComponentsInChildren<Brick>();
                        foreach(var b in bricks)
                        {
                            CheckBrickForHierarchyChange(b, ref checkedBricks, ref bricksToUpdate);
                        }

                        var group = transform.GetComponent<ModelGroup>();
                        if(group)
                        {
                            var model = transform.GetComponentInParent<Model>();                                                        
                            if(model && model.pivot != Model.Pivot.Original)
                            {
                                modelsToRecomputePivot.Add(model);
                            }

                            var bricksInGroup = group.GetComponentsInChildren<Brick>();

                            foreach(var b in bricksInGroup)
                            {
                                CheckBrickForHierarchyChange(b, ref checkedBricks, ref bricksToUpdate);
                            }
                            selection.hasChanged = false;
                        }
                    }

                    if(bricksToUpdate.Count > 0 && autoUpdateHierarchy)
                    {
                        ModelGroupUtility.RecomputeHierarchy(bricksToUpdate, true, ModelGroupUtility.UndoBehavior.withUndo);
                    }
                }
            }
        }

        private static List<Transform> PrepareForMove(HashSet<Brick> bricksToMove, bool disconnect = true)
        {
            var transforms = new List<Transform>();            

            // Remove all connections when we start to move the brick. Undo is handled within the ConnectionFields
            // Don't remove connections that are connected to bricks in the selection
            foreach (var brick in bricksToMove)
            {
                transforms.Add(brick.transform);

                if(disconnect)
                {
                    foreach(var part in brick.parts)
                    {
                        if(!part.connectivity)
                        {
                            continue;
                        }

                        foreach(var field in part.connectivity.connectionFields)
                        {
                            field.DisconnectInverse(bricksToMove);
                        }
                    }
                }
            }

            if(!CheckPlaying())
            {
                Undo.RegisterCompleteObjectUndo(transforms.ToArray(), "Register brick state before selection");
            }
            return transforms;
        }

        private static HashSet<Brick> GetConnectedBricks(Brick[] bricks)
        {
            var connectedBricks = new HashSet<Brick>();
            foreach(var brick in bricks)
            {
                var connected = brick.GetConnectedBricks();   
                connectedBricks.Add(brick);
                connectedBricks.UnionWith(connected);
            }
            return connectedBricks;
        }

        private static void BeginUndoCollapse()
        {
            currentUndoGroupIndex = Undo.GetCurrentGroup();
        }

        private static void EndUndoCollapse(bool defer = true)
        {
            if(defer)
            {
                collapseUndo = true;
            }
            else
            {
                Undo.CollapseUndoOperations(currentUndoGroupIndex);
            }
        }

        private static HashSet<Brick> GetRelatedBricks(HashSet<Brick> toCheck, bool includeSelf = true)
        {
            var relatedBricks = new HashSet<Brick>();
            foreach(var b in toCheck)
            {
                relatedBricks.UnionWith(b.GetConnectedBricks(false));
                if(includeSelf)
                {
                    relatedBricks.Add(b);    
                }
            }
            return relatedBricks;
        }

        private static void StartMovingBrick(Brick brick)
        {
            BeginUndoCollapse();
            currentMouseDelta = 0.0f;
            draggedBrick = null;       
            bricksRelatedToMovingBricks = GetRelatedBricks(selectedBricks);

            PrepareForMove(selectedBricks);
            SyncSceneBricks();
            UpdateBrickCollision(bricks);
            SyncBounds();

            if (CurrentSelectionState == SelectionState.dragging)
            {
                pickupOffset = currentHitPoint.point - focusBrick.transform.position;
            }
            else if(CurrentSelectionState == SelectionState.selected)
            {
                pickupOffset = selectionBounds.center - focusBrick.transform.position;
            }
            CurrentSelectionState = SelectionState.moving;
        }

        private static void EvaluateSelectionState(Event current, Camera camera, SceneView sceneView)
        {
            switch(CurrentSelectionState)
            {
                case SelectionState.noSelection:
                    {
                        if(current.type == EventType.MouseDown && current.button == 0 && !current.alt)
                        {
                            var brick = BrickUnderRay(current, out currentHitPoint);
                            if(brick != null)
                            {                                
                                draggedBrick = brick;
                                CurrentSelectionState = SelectionState.dragging;
                            }
                        }
                        currentMouseDelta = 0.0f;
                    }
                    break;
                case SelectionState.dragging:
                    {
                        if(current.type == EventType.MouseUp)
                        {
                            // We select on mouse up and in case we haven't yet started a drag
                            currentMouseDelta = 0.0f;
                            CurrentSelectionState = SelectionState.selected;

                            if (current.control || current.command || current.shift)
                            {
                                if(selectedBricks.Contains(draggedBrick))
                                {
                                    if(selectConnected)
                                    {
                                        var connected = GetConnectedBricks(new Brick[]{draggedBrick});
                                        foreach(var brick in connected)
                                        {
                                            selectedBricks.Remove(brick);
                                        }
                                    }
                                    else
                                    {
                                        selectedBricks.Remove(draggedBrick);
                                    }

                                    if(draggedBrick == focusBrick)
                                    {
                                        if(selectedBricks.Count > 0)
                                        {
                                            SetFocusBrick(selectedBricks.First());
                                        }
                                    }
                                }
                                else
                                {
                                    SetFocusBrick(draggedBrick);
                                    selectedBricks.Add(draggedBrick);
                                }
                            }
                            else
                            {
                                selectedBricks = new HashSet<Brick> { draggedBrick };
                                SetFocusBrick(draggedBrick);
                            }

                            if(selectConnected)
                            {
                                selectedBricks.UnionWith(GetConnectedBricks(selectedBricks.ToArray()));
                            }
                            QueueSelection(selectedBricks);
                        }
                        else if (currentMouseDelta > stickySnapDistance && current.type == EventType.MouseDrag)
                        {
                            if (Selection.transforms.Length == 0 || !selectedBricks.Contains(draggedBrick))
                            {
                                if(selectConnected)
                                {
                                    selectedBricks.Clear();
                                    selectedBricks.UnionWith(GetConnectedBricks(new Brick[1]{draggedBrick}));
                                    QueueSelection(selectedBricks);
                                }
                                else
                                {
                                    queuedSelection = new List<GameObject> { draggedBrick.gameObject };
                                    selectedBricks = new HashSet<Brick> { draggedBrick };
                                }
                            }
                            else if(selectConnected)
                            {
                                selectedBricks.UnionWith(GetConnectedBricks(selectedBricks.ToArray()));
                                QueueSelection(selectedBricks);
                            }

                            SetFocusBrick(draggedBrick);
                            StartMovingBrick(draggedBrick);
                        }
                    }
                    break;
                case SelectionState.selected:
                    {                        
                        currentMouseDelta = 0.0f;
                        if(current.type == EventType.MouseDown && current.button == 0 && !current.alt)
                        {
                            // Cache the hit point here for use in the dragging state
                            var brick = BrickUnderRay(current, out currentHitPoint);
                            if(brick != null)
                            {
                                draggedBrick = brick;
                                CurrentSelectionState = SelectionState.dragging;
                                currentMouseDelta = 0.0f;
                            }
                        }

                        if(rotateDirection != InteractionKey.none)
                        {
                            RotateBricks(camera, rotateDirection);
                            rotateDirection = InteractionKey.none;
                        }
                        else if(nudgeDirection != InteractionKey.none)
                        {
                            NudgeBricks(camera, nudgeDirection);
                            nudgeDirection = InteractionKey.none;
                        }
                    }
                    break;
                case SelectionState.moving:
                    {                      
                        if(!IsOverSceneView())
                        {
                            return;
                        }                        

                        if (aboutToPlace && current.type == EventType.MouseUp)
                        {
                            PlaceBrick();
                            aboutToPlace = false;

                            // Force a repaint to reflect connection has been made.
                            sceneView.Repaint();
                            return;
                        }

                        if (current.type == EventType.MouseDown)
                        {
                            aboutToPlace = true;
                        }

                        // Check for a small delta to make sure we place even though the user moves the mouse ever so slightly.
                        // In that case, the event will actually be a drag, but it will seem weird to the user since their intention
                        // was to do a place.
                        if (currentMouseDelta > 20.0f && current.type == EventType.MouseDrag)
                        {
                            aboutToPlace = false;
                        }

                        // Cancel a selection
                        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
                        {
                            // Cannot do Undo.PerformUndo in OnSceneGUI as it causes null ref inside Unity GUI code.
                            undoQueued = true;
                        }

                        if(rotateDirection != InteractionKey.none)
                        {
                            RotateBricks(camera, rotateDirection);
                            rotateDirection = InteractionKey.none;
                        }

                        if (currentMouseDelta > 15.0f)
                        {
                            foreach(var brick in bricks)
                            {
                                if(brick.colliding)
                                {
                                    brick.UpdateColliding(brick.IsColliding(out _));
                                }
                            }

                            currentMouseDelta = 0.0f;
                            ComputeNewConnection(camera, currentRay);
                        }
                    }
                    break;
            }
        }

        private static ShortcutBinding currentShortcutDown;
        private static void DoRotate(string path, InteractionKey key, bool lockRotationForKey = true)
        {
            if(lockRotationForKey)
            {
                var shortcut = ShortcutManager.instance.GetShortcutBinding(path);
                if(currentShortcutDown.Equals(shortcut))
                {
                    return;
                }
                currentShortcutDown = shortcut;
            }            
            rotateDirection = key;
        }

        [Shortcut(rotateLeftMenuPath, KeyCode.LeftArrow, ShortcutModifiers.Action)]
        private static void RotateLeft()
        {
            DoRotate(rotateLeftMenuPath, InteractionKey.left);
        }

        [Shortcut(rotateRightMenuPath, KeyCode.RightArrow, ShortcutModifiers.Action)]
        private static void RotateRight()
        {
            DoRotate(rotateRightMenuPath, InteractionKey.right);
        }

        [Shortcut(rotateUpMenuPath, KeyCode.UpArrow, ShortcutModifiers.Action)]
        private static void RotateUp()
        {   
            DoRotate(rotateUpMenuPath, InteractionKey.up);
        }

        [Shortcut(rotateDownMenuPath, KeyCode.DownArrow, ShortcutModifiers.Action)]
        private static void RotateDown()
        {
            DoRotate(rotateDownMenuPath, InteractionKey.down);
        }

        [Shortcut(nudgeLeftMenuPath, KeyCode.LeftArrow, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        private static void NudgeLeft()
        {
            nudgeDirection = InteractionKey.left;
        }

        [Shortcut(nudgeRightMenuPath, KeyCode.RightArrow, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        private static void NudgeRight()
        {
            nudgeDirection = InteractionKey.right;
        }

        [Shortcut(nudgeUpMenuPath, KeyCode.UpArrow, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        private static void NudgeUp()
        {
            nudgeDirection = InteractionKey.up;            
        }

        [Shortcut(nudgeDownMenuPath, KeyCode.DownArrow, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        private static void NudgeDown()
        {
            nudgeDirection = InteractionKey.down;
        }

        private static void NudgeBricks(Camera camera, InteractionKey key)
        {
            if(CurrentSelectionState != SelectionState.selected)
            {
                return;
            }

            if(focusBrick == null)
            {
                return;
            }

            PrepareForMove(selectedBricks, false);

            switch(key)
            {
                case InteractionKey.left:
                {
                    var right = MathUtils.SnapMajorAxis(camera.transform.right, true).normalized;
                    foreach(var brick in selectedBricks)
                    {
                        brick.transform.position -= right * BrickBuildingUtility.LU_10;
                    }
                }
                break;
                case InteractionKey.right:
                {
                    var right = MathUtils.SnapMajorAxis(camera.transform.right, true).normalized;
                    foreach(var brick in selectedBricks)
                    {
                        brick.transform.position += right * BrickBuildingUtility.LU_10;
                    }
                }
                break;
                case InteractionKey.up:
                {
                    var up = MathUtils.SnapMajorAxis(camera.transform.up, true).normalized;
                    var upUnsigned = MathUtils.SnapMajorAxis(camera.transform.up, false).normalized;
                    var increment = BrickBuildingUtility.LU_1 * 4;
                    var angleUpUnsigned = Vector3.Angle(Vector3.up, upUnsigned);
                    if(angleUpUnsigned > Vector3.Angle(Vector3.forward, upUnsigned) ||
                      angleUpUnsigned > Vector3.Angle(Vector3.right, upUnsigned))
                    {
                        increment = BrickBuildingUtility.LU_10;
                    }

                    foreach(var brick in selectedBricks)
                    {
                        brick.transform.position += up * increment;
                    }
                }
                break;
                case InteractionKey.down:
                {
                    var up = MathUtils.SnapMajorAxis(camera.transform.up, true).normalized;
                    var upUnsigned = MathUtils.SnapMajorAxis(camera.transform.up, false).normalized;
                    var increment = BrickBuildingUtility.LU_1 * 4;
                    var angleUpUnsigned = Vector3.Angle(Vector3.up, upUnsigned);
                    if(angleUpUnsigned > Vector3.Angle(Vector3.forward, upUnsigned) ||
                      angleUpUnsigned > Vector3.Angle(Vector3.right, upUnsigned))
                    {
                        increment = BrickBuildingUtility.LU_10;
                    }

                    foreach(var brick in selectedBricks)
                    {
                        brick.transform.position -= up * increment;
                    }
                }
                break;
            }
            
            SyncSceneBricks();
            var previouslyColliding = UpdateBrickCollision(bricks);
            SyncBounds();
            previouslyColliding.UnionWith(RecomputeConnectionsAfterNudgeRotate(previouslyColliding));
            
            if(autoUpdateHierarchy)
            {
                ModelGroupUtility.RecomputeHierarchy(previouslyColliding.Union(selectedBricks), false);
            }
        }

        private static (Connection, Connection) FindConnection(Brick brick)
        {
            if(brick.colliding)
            {
                return (null, null);
            }

            foreach(var part in brick.parts)
            {
                 if(!part.connectivity)
                {
                    continue;
                }

                foreach(var field in part.connectivity.connectionFields)
                {                   
                    var connections = field.QueryConnections(out _);
                    foreach(var connection in connections)
                    {
                        if(field.HasConnection(connection.Item1) || connection.Item2.field.HasConnection(connection.Item2))
                        {
                            continue;
                        }

                        if(connection.Item2.field.connectivity.part.brick.colliding)
                        {
                            continue;
                        }
                        return connection;
                    }                    
                }
            }

            return (null, null);
        }
        
        private static void RotateBricks(Camera camera, InteractionKey key)
        {    
            if(focusBrick == null)
            {
                return;
            }
            
            SyncBounds();
            Vector3 rotationPivot = Vector3.zero;
            var localPickupOffset = focusBrick.transform.InverseTransformDirection(pickupOffset);
            if(CurrentSelectionState == SelectionState.moving)
            {                
                rotationPivot = focusBrick.transform.position + pickupOffset;
            }
            else
            {
                PrepareForMove(selectedBricks, true);
                rotationPivot = selectionBounds.center;
            }

            switch (key)
            {
                case InteractionKey.left:
                    {
                        foreach(var selected in selectedBricks)
                        {
                            selected.transform.RotateAround(rotationPivot, Vector3.up, -90.0f);
                        }
                        break;
                    }
                case InteractionKey.right:
                    {
                        foreach (var selected in selectedBricks)
                        {
                            selected.transform.RotateAround(rotationPivot, Vector3.up, 90.0f);
                        }
                        break;
                    }
                case InteractionKey.up:
                    {
                        var right = MathUtils.SnapMajorAxis(camera.transform.right, true);
                        foreach (var selected in selectedBricks)
                        {
                            selected.transform.RotateAround(rotationPivot, right, 90.0f);
                        }
                        break;
                    }
                case InteractionKey.down:
                    {
                        var right = MathUtils.SnapMajorAxis(camera.transform.right, true);
                        foreach (var selected in selectedBricks)
                        {
                            selected.transform.RotateAround(rotationPivot, right, -90.0f);
                        }
                        break;
                    }
            }

            SyncSceneBricks();
            var previouslyColliding = UpdateBrickCollision(bricks);
            SyncBounds();

            if(CurrentSelectionState == SelectionState.moving)
            {
                pickupOffset = focusBrick.transform.TransformDirection(localPickupOffset);
                ComputeNewConnection(camera, currentRay);
            }
            else
            {
                previouslyColliding.UnionWith(RecomputeConnectionsAfterNudgeRotate(previouslyColliding));
            }

            if(autoUpdateHierarchy)
            {
                ModelGroupUtility.RecomputeHierarchy(previouslyColliding.Union(selectedBricks), false);
            }
        }

        private static HashSet<Brick> RecomputeConnectionsAfterNudgeRotate(IEnumerable<Brick> previouslyColliding)
        {            
            if(focusBrick == null)
            {
                return new HashSet<Brick>();
            }

            var collidingBricks = new HashSet<Brick>();
            
            if(CollideAtTransformation(selectedBricks))
            {
                foreach(var brick in selectedBricks)
                {
                    if(brick.IsColliding(out int hits, selectedBricks))
                    {
                        collidingBricks.Add(brick);
                        brick.UpdateColliding(true);
                        brick.DisconnectAll();
                        for(var i = 0; i < hits; i++)
                        {
                            var collider = BrickBuildingUtility.colliderBuffer[i];
                            if(!collider)
                            {
                                continue;
                            }

                            var collidingBrick = collider.GetComponentInParent<Brick>();
                            if(collidingBrick)
                            {
                                collidingBrick.UpdateColliding(true);
                                collidingBrick.DisconnectAll();
                                collidingBricks.Add(collidingBrick);
                            }
                        }
                    }
                    else
                    {
                        brick.UpdateColliding(false);
                    }
                }
            }
            else
            {
                Vector3 axis = Vector3.zero;
                float angle = 0.0f;
                var pivot = focusBrick.transform.position + pickupOffset;
                (Connection, Connection) foundConnection = (null, null);
                Quaternion rot = Quaternion.identity;
                foreach(var brick in selectedBricks)
                {
                    if(brick.colliding)
                    {
                        continue;
                    }
                    
                    foreach(var part in brick.parts)
                    {
                        if(!part.connectivity)
                        {
                            continue;
                        }

                        foreach(var field in part.connectivity.connectionFields)
                        {
                            field.DisconnectInverse(selectedBricks);

                            if(foundConnection == (null, null))
                            {
                                if(!field.HasAvailableConnections())
                                {
                                    continue;
                                }
                                
                                var connections = field.QueryConnections(out _);
                                foreach(var connection in connections)
                                {
                                    if(field.HasConnection(connection.Item1) || connection.Item2.field.HasConnection(connection.Item2))
                                    {
                                        continue;
                                    }

                                    if(connection.Item2.field.connectivity.part.brick.colliding)
                                    {
                                        continue;
                                    }

                                    foundConnection = connection;
                                    ConnectionField.GetConnectedTransformation(connection.Item1, connection.Item1, pivot, out _, out angle, out axis);
                                    break;
                                }
                            }
                        }
                    }
                }

                if(foundConnection != (null, null) && ConnectionField.IsConnectionValid(foundConnection.Item1, foundConnection.Item2, pivot))
                {
                    foundConnection.Item1.field.connectivity.part.brick.transform.RotateAround(pivot, axis, angle);
                    ConnectWithSelection(foundConnection.Item1, foundConnection.Item2);
                }                
            }

            foreach(var brick in previouslyColliding)
            {
                if(brick.colliding)
                {
                    continue;
                }

                var connection = FindConnection(brick);
                if(connection != (null, null) && ConnectionField.IsConnectionValid(connection.Item1, connection.Item2, brick.transform.position))
                {
                    ConnectionField.Connect(connection.Item1, connection.Item2, brick.transform.position);
                }
            }

            return collidingBricks;
        }

        private static void ConnectWithSelection(Connection src, Connection dst)
        {
            var pivot = focusBrick.transform.position + pickupOffset;
            ConnectionField.Connect(src, dst, pivot, null, selectedBricks);

            foreach(var brick in selectedBricks)
            {
                if(brick == src.field.connectivity.part.brick)
                {
                    continue;
                }

                foreach(var part in brick.parts)
                {
                    foreach(var field in part.connectivity.connectionFields)
                    {
                        var possibleConnections = field.QueryConnections(out _);
                        foreach(var connection in possibleConnections)
                        {
                            if(field.HasConnection(connection.Item1) || connection.Item2.field.HasConnection(connection.Item2))
                            {
                                continue;    
                            }

                            if(ConnectionField.IsConnectionValid(connection.Item1, connection.Item2, pivot))
                            {
                                ConnectionField.Connect(connection.Item1, connection.Item2, pivot);
                                break;
                            }
                        }
                    }
                }
            }            
        }

        private static void PlaceBrick()
        {
            if (focusBrick == null)
            {
                return;
            }

            // If we had selected a brick and a connection should be made, do the connection here.
            if (currentConnection.Item1 != null && currentConnection.Item2 != null)
            {
                // Get rid of place offset. Need to sync transforms afterwards.
                foreach(var brick in selectedBricks)
                {
                    brick.transform.position -= placeOffset;
                }
                placeOffset = Vector3.zero;
                Physics.SyncTransforms();

                ConnectWithSelection(currentConnection.Item1, currentConnection.Item2);
                currentConnection = (null, null);
            }
            else if(CurrentSelectionState == SelectionState.moving)
            {
                foreach(var brick in selectedBricks)
                {
                    var connection = FindConnection(brick);
                    var pivot = focusBrick.transform.position + pickupOffset;
                    if(connection != (null, null) && ConnectionField.IsConnectionValid(connection.Item1, connection.Item2, pivot))
                    {
                        ConnectionField.GetConnectedTransformation(connection.Item1, connection.Item2, pivot, out _, out float angle, out Vector3 axis);
                        connection.Item1.field.connectivity.part.brick.transform.RotateAround(pivot, axis, angle);
                        ConnectWithSelection(connection.Item1, connection.Item2);
                        break;
                    }   
                }
                
                SyncSceneBricks();
                foreach(var brick in bricks)
                {
                    if(selectedBricks.Contains(brick))
                    {
                        continue;
                    }

                    var connection = FindConnection(brick);
                    if(connection != (null, null) && ConnectionField.IsConnectionValid(connection.Item1, connection.Item2, Vector3.zero))
                    {
                        ConnectionField.Connect(connection.Item1, connection.Item2, Vector3.zero);
                    }   
                }
            }

            if(CurrentSelectionState == SelectionState.moving)
            {
                if(autoUpdateHierarchy)
                {
                    bricksRelatedToMovingBricks.UnionWith(GetRelatedBricks(selectedBricks));
                    SyncSceneBricks();
                    ModelGroupUtility.RecomputeHierarchy(bricksRelatedToMovingBricks);
                    bricksRelatedToMovingBricks.Clear();                
                }
                EndUndoCollapse(false);                
            }
            
            pickupOffset = Vector3.zero;
            CurrentSelectionState = SelectionState.selected;
        }
        
        private static bool IsPickingEvent(Event current)
        {
            return current.type != EventType.Repaint &&
                current.type != EventType.Layout &&
                current.type != EventType.ExecuteCommand &&
                current.type != EventType.ValidateCommand;
        }

        private static bool IsDuplicateEvent(Event current)
        {
            return current != null 
                    && ((current.commandName == "Duplicate" && selectedBricks.Count > 0) || current.commandName == "Paste") 
                    && current.type == EventType.ExecuteCommand;
        }

        private static bool AreShortcutsInEvent(Event current, ICollection<IEnumerable<KeyCombination>> sequences)
        {
            if(current == null)
            {
                return false;
            }

            foreach(var sequence in sequences)
            {
                foreach(var keyCombination in sequence)
                {
                    var performed = true;

                    if(keyCombination.action)
                    {
                        performed &= current.modifiers.HasFlag(EventModifiers.Control) || current.modifiers.HasFlag(EventModifiers.Command);
                    }

                    if(keyCombination.shift)
                    {
                        performed &= current.modifiers.HasFlag(EventModifiers.Shift);
                    }
                    
                    if(keyCombination.alt)
                    {
                        performed &= current.modifiers.HasFlag(EventModifiers.Alt);
                    }

                    performed &= current.keyCode == keyCombination.keyCode;
                    if(performed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool AreShortcutsInEvent(Event current, string[] bindingPaths)
        {
            var sequences = new List<IEnumerable<KeyCombination>>();
            foreach(var bindingPath in bindingPaths)
            {
                var binding = ShortcutManager.instance.GetShortcutBinding(bindingPath);
                sequences.Add(binding.keyCombinationSequence);
            }

            return AreShortcutsInEvent(current, sequences);            
        }

        private static bool IsUndoRedoEvent(Event current)
        {
            return current.type == EventType.KeyDown && AreShortcutsInEvent(current, new string[]{"Main Menu/Edit/Undo", "Main Menu/Edit/Redo"});
        }

        private static bool IsOverSceneView()
        {
            if(SceneView.mouseOverWindow == null)
            {
                return false;
            }
            System.Type windowOver = SceneView.mouseOverWindow.GetType();
            System.Type sceneView = typeof(SceneView);
            return windowOver.Equals(sceneView);
        }

        private static Brick BrickUnderRay(Event current, out RaycastHit hit)
        {
            hit = new RaycastHit();
            
            PhysicsScene physicsScene;
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                physicsScene = PrefabStageUtility.GetCurrentPrefabStage().scene.GetPhysicsScene();
            }
            else
            {
                physicsScene = PhysicsSceneExtensions.GetPhysicsScene(EditorSceneManager.GetActiveScene());
            }
            
            if (physicsScene.Raycast(currentRay.origin, currentRay.direction, out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var brick = hit.collider.gameObject.GetComponentInParent<Brick>();
                if(brick)
                {
                    foreach(var part in brick.parts)
                    {
                        if(part.connectivity)
                        {
                            return brick;
                        }
                    }
                }
            }            
            return null;
        }

        private static void UpdateMouse(Vector3 newMousePosition)
        {
            var mouseDelta = Vector3.Distance(newMousePosition, mousePosition);
            currentMouseDelta += mouseDelta;
            mousePosition = newMousePosition;
            currentRay = HandleUtility.GUIPointToWorldRay(mousePosition);
        }

        private static bool AdjustSelectionIfNeeded(GameObject go, object[] lastSelection, List<GameObject> newSelection, bool checkParent, out bool isNotBrick)
        {
            var brick = FindBrick(go, checkParent);
            if(brick)
            {                
                isNotBrick = false;
                if (Array.IndexOf(lastSelection, go) < 0)
                {
                    if (Array.IndexOf(lastSelection, brick) < 0)
                    {
                        newSelection.Add(brick.gameObject);
                        return true;
                    }
                }
            }
            else
            {
                isNotBrick = true;
            }

            newSelection.Add(go);
            return false;
        }        

        private static HashSet<Brick> GetBrickSelection()
        {
            var selection = new HashSet<Brick>();            
            foreach(var obj in Selection.objects)
            {
                if(!GetValidGameObject(obj, out GameObject go))
                {
                    continue;
                }

                var brick = go.GetComponent<Brick>();
                if(brick != null)
                {
                    selection.Add(brick);
                }
                else
                {
                    var bricks = go.GetComponentsInChildren<Brick>();
                    foreach(var b in bricks)
                    {
                        selection.Add(b);
                    }
                }
            }
            return selection;
        }

        private static Brick FindBrick(GameObject go, bool checkParent)
        {
            if(checkParent)
            {
                return go.GetComponentInParent<Brick>();
            }
            return go.GetComponent<Brick>();
        }
        
        private static void QueueSelection(HashSet<Brick> selection)
        {
            queuedSelection = new List<GameObject>();
            foreach (var brick in selection)
            {
                queuedSelection.Add(brick.gameObject);
            }
        }

        [MenuItem(expandSelectionMenuPath + " " + expandSelectionShortcut, priority = 40)]
        public static void ExpandSelection()
        {
            var connectedBricks = new HashSet<Brick>();
            if(buildWithBricks)
            {
                foreach(var brick in selectedBricks)
                {
                    var connected = brick.GetConnectedBricks();
                    connectedBricks.UnionWith(connected);
                }
                selectedBricks.UnionWith(connectedBricks);
                QueueSelection(selectedBricks);
            }         
        }

        [MenuItem(expandSelectionMenuPath + " " + expandSelectionShortcut, true)]
        private static bool ValidateExpandSelection()
        {
            var enabled = ExpandSelectionEnabled();            
            return enabled;
        }

        public static bool ExpandSelectionEnabled()
        {
            if(buildWithBricks)
            {
                return CurrentSelectionState == SelectionState.selected;
            }
            return false;
        }

        private static void UpdateRemovedBricks()
        {
            var toRemove = new List<Brick>();
            foreach(var brick in selectedBricks)
            {
                if(brick == null)
                {
                    toRemove.Add(brick);
                }
            }

            foreach(var brick in toRemove)
            {
                if(brick == focusBrick)
                {
                    SetFocusBrick(null);
                }
                selectedBricks.Remove(brick);
            }
        }        

        private static void HandleDeletedBricks()
        {
            deleteQueued = false;
            sceneChanged = true;
            SyncSceneBricks();
            UpdateBrickCollision(bricks);
            if(autoUpdateHierarchy)
            {
                var stillLivingBricks = new HashSet<Brick>();
                foreach(var brick in bricksRelatedToDeletedBricks)
                {
                    if(!brick)
                    {
                        continue;
                    }
                    stillLivingBricks.Add(brick);
                }
                ModelGroupUtility.RecomputeHierarchy(stillLivingBricks);
                bricksRelatedToDeletedBricks.Clear();
            }
        }

        private static Vector2 ClampPosition(Vector2 input, float buttonSize)
        {
            var distanceFromBorders = 5;
            var distanceFromTop = distanceFromBorders;
            var sceneViewTopBarHeight = 21;
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                distanceFromTop += distanceFromBorders + sceneViewTopBarHeight;
            }
            var doubleButtonSize = buttonSize * 1.5f;
            Rect sceneViewArea = SceneView.lastActiveSceneView.position;
            Vector2 pos = input;

            pos.x = Mathf.Clamp(pos.x, distanceFromBorders, sceneViewArea.width - doubleButtonSize - distanceFromBorders);
            pos.y = Mathf.Clamp(pos.y, distanceFromTop + sceneViewTopBarHeight, sceneViewArea.height - doubleButtonSize - distanceFromTop);
            
            return pos;
        }

        private static void HandleRotateGizmo(Camera camera)
        {
            if(!rotateGizmoOn)
            {
                return;
            }

            if(selectedBricks.Count == 0)
            {
                return;
            }

            if(CurrentSelectionState != SelectionState.selected && CurrentSelectionState != SelectionState.dragging)
            {
                return;
            }

            if(Tools.current == Tool.View)
            {
                return;
            }

            SyncBounds();
            var bounds = selectionBounds;
            var center = bounds.center;
            var radius = bounds.extents.magnitude + 0.5f;

            var cameraPlane = new Plane(camera.transform.forward, camera.transform.position);
            if(!cameraPlane.GetSide(center) || Vector3.Distance(center, camera.transform.position) < 1.0f)
            {
                return;
            }
            
            var buttonStyle = new GUIStyle(GUIStyle.none);
            var buttonSize = 36;
            var buttonMargin = 4;
            buttonStyle.fixedWidth = buttonSize;
            buttonStyle.fixedHeight = buttonSize;
            if(rotationArrowDownNormal == null)
            {
                LoadTextures();
            }

            Handles.BeginGUI();
            var adjustment = new Vector2(buttonStyle.fixedWidth * 0.5f, buttonStyle.fixedWidth * 0.5f);
            var pos = HandleUtility.WorldToGUIPoint(center) - adjustment;
            var upFromCenter = center + (radius * camera.transform.up);
            var downFromCenter = center - (radius * camera.transform.up);
            var rightFromCenter = center + (radius * camera.transform.right);
            var leftFromCenter = center - (radius * camera.transform.right);
            var up = HandleUtility.WorldToGUIPoint(upFromCenter) - adjustment;
            var down = HandleUtility.WorldToGUIPoint(downFromCenter) - adjustment;
            var right = HandleUtility.WorldToGUIPoint(rightFromCenter) - adjustment;
            var left = HandleUtility.WorldToGUIPoint(leftFromCenter) - adjustment;
            var upToDown = Vector2.Distance(up, down);
            var minDistance = buttonSize * 2 + buttonMargin;
            if(upToDown < minDistance)
            {
                var distanceToAdd = (minDistance - upToDown) * 0.5f;
                up -= new Vector2(0.0f, distanceToAdd);
                down += new Vector2(0.0f, distanceToAdd);
                left -= new Vector2(distanceToAdd, 0.0f);
                right += new Vector2(distanceToAdd, 0.0f);
            }

            var distanceFromBorders = 5;
            var distanceFromTop = distanceFromBorders;
            var sceneViewTopBarHeight = 21;
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                distanceFromTop += distanceFromBorders + sceneViewTopBarHeight;
            }
            var doubleButtonSize = buttonSize * 1.5f;
            Rect sceneViewArea = SceneView.lastActiveSceneView.position;

            up.y = Mathf.Clamp(up.y, distanceFromTop + sceneViewTopBarHeight, up.y);
            down.y = Mathf.Clamp(down.y, down.y, sceneViewArea.height - doubleButtonSize - distanceFromTop);
            left.x = Mathf.Clamp(left.x, distanceFromBorders, left.x);
            right.x = Mathf.Clamp(right.x, right.x, sceneViewArea.width - doubleButtonSize - distanceFromBorders);

            right.y = pos.y;
            left.y = pos.y;
            up.x = pos.x;
            down.x = pos.x;

            if(up.y > pos.y - buttonSize)
            {
                up.y = pos.y - buttonSize;
            }

            if(down.y < pos.y + buttonSize)
            {
                down.y = pos.y + buttonSize;
            }

            if(left.x > pos.x - buttonSize)
            {
                left.x = pos.x - buttonSize;
            }

            if(right.x < pos.x + buttonSize)
            {
                right.x = pos.x + buttonSize;
            }                

            buttonStyle.normal.background = rotationArrowUpNormal;
            buttonStyle.active.background = rotationArrowUpActive;
            buttonStyle.hover.background = rotationArrowUpHover;
            string upShortcut = ShortcutManager.instance.GetShortcutBinding(rotateUpMenuPath).ToString();
            GUIContent upArrowContent = new GUIContent("", "Rotate Up 90 degrees " + upShortcut);                
            if(GUI.Button(new Rect(up.x, up.y, buttonSize, buttonSize), upArrowContent, buttonStyle))
            {
                DoRotate(rotateUpMenuPath, InteractionKey.up, false);
            }

            string downShortcut = ShortcutManager.instance.GetShortcutBinding(rotateDownMenuPath).ToString();
            GUIContent downArrowContent = new GUIContent("", "Rotate Down 90 degrees " + downShortcut);
            buttonStyle.normal.background = rotationArrowDownNormal;
            buttonStyle.active.background = rotationArrowDownActive;
            buttonStyle.hover.background = rotationArrowDownHover;
            if(GUI.Button(new Rect(down.x, down.y, buttonSize, buttonSize), downArrowContent, buttonStyle))
            {
                DoRotate(rotateDownMenuPath, InteractionKey.down, false);
            }

            string leftShortcut = ShortcutManager.instance.GetShortcutBinding(rotateLeftMenuPath).ToString();
            GUIContent leftArrowContent = new GUIContent("", "Rotate Left 90 degrees " + leftShortcut);
            buttonStyle.normal.background = rotationArrowLeftNormal;
            buttonStyle.active.background = rotationArrowLeftActive;
            buttonStyle.hover.background = rotationArrowLeftHover;
            if(GUI.Button(new Rect(left.x, left.y, buttonSize, buttonSize), leftArrowContent, buttonStyle))
            {
                DoRotate(rotateLeftMenuPath, InteractionKey.left, false);
            }

            string rightShortcut = ShortcutManager.instance.GetShortcutBinding(rotateRightMenuPath).ToString();
            GUIContent rightArrowContent = new GUIContent("", "Rotate Right 90 degrees " + rightShortcut);
            buttonStyle.normal.background = rotationArrowRightNormal;
            buttonStyle.active.background = rotationArrowRightActive;
            buttonStyle.hover.background = rotationArrowRightHover;
            if(GUI.Button(new Rect(right.x, right.y, buttonSize, buttonSize), rightArrowContent, buttonStyle))
            {
                DoRotate(rotateRightMenuPath, InteractionKey.right, false);
            }
            
            Handles.EndGUI();           
        }

#region event handling
        private static void OnSceneGUIBuilding(SceneView sceneView)
        {
            currentEvent = Event.current;
            undoRedo = IsUndoRedoEvent(currentEvent);

            if(currentEvent.type == EventType.KeyUp 
            || (currentEvent.type == EventType.KeyDown && !AreShortcutsInEvent(currentEvent, new IEnumerable<KeyCombination>[]{currentShortcutDown.keyCombinationSequence})))
            {
                currentShortcutDown = new ShortcutBinding();
            }

            if (!dragAndDropQueued && Event.current.type == EventType.DragUpdated)
            {
                var objects = DragAndDrop.objectReferences;
                if (objects.Length > 0)
                {
                    var validObject = false;
                    foreach(var obj in objects)
                    {
                        var go = obj as GameObject;
                        if(go)
                        {
                            validObject = true;
                            break;
                        }
                    }

                    if(validObject)
                    {
                        PlaceBrick();
                        SetFocusBrick(null);
                        selectedBricks.Clear();
                        dragAndDropQueued = true;
                        BeginUndoCollapse();
                    }
                }
            }

            CheckChangedTransforms();
            UpdateRemovedBricks();

            HandleRotateGizmo(sceneView.camera);

            if (IsPickingEvent(Event.current))
            {
                UpdateMouse(Event.current.mousePosition);
            }
            // Make sure that when we are starting to drag a brick in the scene that we are allowed to.
            // Allowed to means:
            // - The mouse is over the scene view. There is no reason to check for a brick if we are not mousing over the scene view
            // - If hot control is zero, we are sure that we are not interacting with any handle/tool (move tool, rotation tool etc.)
            if (GUIUtility.hotControl != 0)
            {
                // Dragging a gizmo over a brick starts the dragging state
                if (CurrentSelectionState == SelectionState.dragging)
                {
                    CurrentSelectionState = SelectionState.noSelection;
                }
                currentMouseDelta = 0.0f;
            }
            else
            {
                if (Event.current.button == 0 && Tool.View != Tools.current && IsPickingEvent(Event.current) && IsOverSceneView())
                {
                    hitBrick = BrickUnderRay(Event.current, out _);
                }

                if (hitBrick != null || (focusBrick != null && CurrentSelectionState == SelectionState.moving))
                {
                    HandleUtility.AddDefaultControl(0);
                }

                if (CurrentSelectionState != SelectionState.dragging)
                {
                    if (IsDuplicateEvent(Event.current))
                    {
                        PlaceBrick();
                        duplicateQueued = true;
                    }
                }

                if(Tools.current == Tool.View)
                {
                    PlaceBrick();
                }

                EvaluateSelectionState(Event.current, sceneView.camera, sceneView);
            }            
        }

        private static void ApplyConnectivityLayer()
        {
            var currentStage = StageUtility.GetCurrentStageHandle();
            if(currentStage != null && currentStage.IsValid())
            {
                var fields = currentStage.FindComponentsOfType<ConnectionField>();
                foreach(var field in fields)
                {
                    if(!CheckPlaying())
                    {
                        var newLayer = LayerMask.NameToLayer(ConnectionField.GetLayer(field.kind));
                        if(newLayer == -1)
                        {
                            return;
                        }
                        field.gameObject.layer = newLayer;
                    }
                }
            }
        }

        private static void SyncBounds()
        {            
            selectionBounds = BrickBuildingUtility.ComputeBounds(selectedBricks);
        }

        private static void SyncSceneBricks()
        {
            if(sceneChanged)
            {
                sceneChanged = false;
                bricks = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Brick>();
            }
        }

        private static void SetFocusBrick(Brick brick)
        {
            focusBrick = brick;
            if (focusBrick != null)
            {
                Tools.hidden = true;
                rotationOffsets.Clear();
                foreach(var selected in selectedBricks)
                {
                    var offset = focusBrick.transform.position - selected.transform.position;
                    var localOffset = focusBrick.transform.InverseTransformVector(offset);
                    rotationOffsets.Add((selected.transform.rotation, localOffset));
                }
            }
            else
            {
                Tools.hidden = false;
                rotationOffsets.Clear();
                CurrentSelectionState = SelectionState.noSelection;
            }
        }

        private static bool CanConnect((Connection, Connection) chosenConnection, RaycastHit collidingHit, out Vector3 pivot, out Vector3 connectedOffset, out float angle, out Vector3 axis)
        {                
            if(chosenConnection != (null, null))
            {
                // Compute the pivot for the rotation
                pivot = chosenConnection.Item1.field.connectivity.part.brick.transform.position + pickupOffset;

                // Get the connected transformation to compute a snapping position
                ConnectionField.GetConnectedTransformation(chosenConnection.Item1, chosenConnection.Item2, pivot, out connectedOffset, out angle, out axis);

                // Check if the chosen connection will be underneath the hitpoint in local space of the hit plane.
                var hitNormal = collidingHit.normal;
                var hitPoint = collidingHit.point;
                var transformation = Matrix4x4.TRS(hitPoint, Quaternion.FromToRotation(Vector3.up, hitNormal), Vector3.one).inverse;
                var localConnection = transformation.MultiplyPoint(chosenConnection.Item2.field.GetPosition(chosenConnection.Item2));
                
                return localConnection.y >= 0.0f || !collidingHit.transform;
            }

            pivot = Vector3.zero;
            connectedOffset = Vector3.zero;
            angle = 0.0f;
            axis = Vector3.zero;

            return false;
        }

        private static void ComputeNewConnection(Camera camera, Ray ray)
        {
            if (focusBrick == null)
            {
                return;
            }

            // We only need to cache the position of the focus brick, since every other brick
            // can be repositioned relative to the focus brick (see: rotationOffsets)
            var oldFocusPosition = focusBrick.transform.position;

            // catch-all rotation caching if any collision happens 
            var oldRotations = new List<Quaternion>();

            foreach(var brick in selectedBricks)
            {
                oldRotations.Add(brick.transform.rotation);
            }
            
            var pivot = focusBrick.transform.position + pickupOffset;

            var fallbackPlane = worldPlane;
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                // Position plane to always be near prefab in prefab stage
                var bounds = BrickBuildingUtility.ComputeBounds(bricks, Matrix4x4.identity);
                fallbackPlane = new Plane(worldPlane.normal, bounds.min);
            }

            // Align the bricks to intersecting geometry or a fallback plane (worldPlane)
            BrickBuildingUtility.AlignBricks(focusBrick, selectedBricks, selectionBounds, pivot, pickupOffset, ray, fallbackPlane, BrickBuildingUtility.maxRayDistance, out Vector3 offset, out Vector3 alignedOffset, out Quaternion rotation, out RaycastHit collidingHit);

            // We only rotate if we are not in the middle of a connection, since then we already know where we will place the brick(s).
            // This makes it easier to connect when already in a connection.
            if(currentConnection == (null, null))
            {
                // Before we align, cache the pickup offset so we don't get weird placements after rotating.
                // This is especially important for larger selections.
                
                var localOffset = focusBrick.transform.InverseTransformDirection(pickupOffset);
                rotation.ToAngleAxis(out float alignedAngle, out Vector3 alignedAxis);
                foreach(var brick in selectedBricks)
                {
                    brick.transform.RotateAround(pivot, alignedAxis, alignedAngle);
                }

                // Transform pickup offset back to world space for later use
                pickupOffset = focusBrick.transform.TransformDirection(localOffset);
                
                // Cache new rotations for later
                var k = 0;
                foreach(var brick in selectedBricks)
                {
                    rotationOffsets[k] = (brick.transform.rotation, rotationOffsets[k].Item2);
                    k++;
                }
            }

            // Apply the un-aligned offset to place the brick in the world
            focusBrick.transform.position += offset;
            ResetPositions();
            SyncBounds();
            // Find the best connection after the brick has been moved
            var chosenConnection = BrickBuildingUtility.FindBestConnection(pickupOffset, selectedBricks, ray, camera, bricks, maxTriesPerBrick);
            
            Quaternion rot = Quaternion.identity;

            if (CanConnect(chosenConnection, collidingHit, out pivot, out Vector3 connectedOffset, out float angle, out Vector3 axis))
            {
                // Cache the local pickup offset to recompute it after connection
                var localOffset = focusBrick.transform.InverseTransformDirection(pickupOffset);
                currentConnection = chosenConnection;  

                BrickBuildingUtility.AlignTransformations(selectedBricks, pivot, axis, angle, connectedOffset);

                // Compute place offset:
                // 1. Find all connections at current position.
                // 2. If a connection is to a non-selected brick, get the preconnect offset.
                // 3. If all preconnect offsets are similar, use them as place offset.
                // 4. Move the selected bricks with the place offset.
                Physics.SyncTransforms();

                HashSet<(Connection, Connection)> currentConnections = new HashSet<(Connection, Connection)>();
                foreach (var brick in selectedBricks)
                {
                    foreach (var part in brick.parts)
                    {
                        foreach (var connectionField in part.connectivity.connectionFields)
                        {
                            currentConnections.UnionWith(connectionField.QueryConnections(out _, false));
                        }
                    }
                }

                var potentialPlaceOffset = Vector3.zero;
                var firstPlaceOffset = true;
                foreach (var connection in currentConnections)
                {
                    if (!selectedBricks.Contains(connection.Item2.field.connectivity.part.brick))
                    {                        
                        var preconnectOffset = Connection.GetPreconnectOffset(connection.Item2);
                        if (firstPlaceOffset)
                        {
                            potentialPlaceOffset = preconnectOffset;
                            firstPlaceOffset = false;
                        }
                        else
                        {
                            if ((preconnectOffset - potentialPlaceOffset).sqrMagnitude > 0.01f)
                            {
                                potentialPlaceOffset = Vector3.zero;
                                break;
                            }
                        }
                    }
                }

                placeOffset = potentialPlaceOffset;
                foreach (var brick in selectedBricks)
                {
                    brick.transform.position += placeOffset;
                }
                pickupOffset = focusBrick.transform.TransformDirection(localOffset);
            }
            else
            {
                // Revert the offset and apply the aligned offset
                focusBrick.transform.position -= offset;
                focusBrick.transform.position += alignedOffset;
                ResetPositions();

                var localPickupOffset = focusBrick.transform.InverseTransformDirection(pickupOffset);
                // If we collide at this position, simply reset to before we moved the bricks.
                // This could mean back to a possible connection or simply on intersecting geometry.
                if (CollideAtTransformation(selectedBricks))
                {    
                    var k = 0;
                    foreach (var brick in selectedBricks)
                    {
                        brick.transform.rotation = oldRotations[k++];
                    }
                    focusBrick.transform.position = oldFocusPosition;
                    ResetPositions();
                }
                else if(currentConnection != (null, null))
                {
                    // We don't have a collision at the moved position
                    // In case we don't have a collision at this position, we need to reset the selection to their
                    // original rotations from previously. 
                    // Once we reset the original rotations, we need to find a position in the scene to re-align them to
                    // any intersecting geometry they might hit now.
                    // Doing this reset (both rotation and re-aligning), might result in a collision again.
                    // In case we collide again, we have to go back to before we tried to reposition the selection
                    // since it is invariant that the bricks never collide after this function.

                    var i = 0;
                    var previousRotations = new List<Quaternion>();

                    foreach(var brick in selectedBricks)
                    {
                        previousRotations.Add(brick.transform.rotation);
                        brick.transform.rotation = rotationOffsets[i++].Item1;
                    }
                    SyncBounds();

                    // Realignment needs a correct pickupOffset
                    pickupOffset = focusBrick.transform.TransformDirection(localPickupOffset);

                    pivot = focusBrick.transform.position + pickupOffset;
                    BrickBuildingUtility.AlignBricks(focusBrick, selectedBricks, selectionBounds, pivot, pickupOffset, ray, worldPlane, BrickBuildingUtility.maxRayDistance, out _, out alignedOffset, out _, out _);

                    focusBrick.transform.position += alignedOffset;
                    ResetPositions();
                    Physics.SyncTransforms();

                    if(CollideAtTransformation(selectedBricks))
                    {
                        focusBrick.transform.position = oldFocusPosition;
                        i = 0;
                        foreach(var brick in selectedBricks)
                        {
                            brick.transform.rotation = previousRotations[i++];
                        }
                        ResetPositions();
                    }
                    else
                    {
                        currentConnection = (null, null);
                    }
                }
                pickupOffset = focusBrick.transform.TransformDirection(localPickupOffset);
            }
            SyncBounds();
        }

        private static bool CollideAtTransformation(ICollection<Brick> bricks)
        {
            foreach (var brick in bricks)
            {
                if (BrickBuildingUtility.IsCollidingAtTransformation(brick, brick.transform.position, brick.transform.rotation, bricks))
                {
                    return true;
                }
            }
            return false;
        }

        private static HashSet<Brick> GetCollidingAtTransformation(HashSet<Brick> bricks)
        {
            HashSet<Brick> collidingBricks = new HashSet<Brick>();
            foreach (var brick in bricks)
            {
                if (BrickBuildingUtility.IsCollidingAtTransformation(brick, brick.transform.position, brick.transform.rotation, bricks))
                {
                    collidingBricks.Add(brick);
                }
            }
            return collidingBricks;
        }

        private static void ResetPositions()
        {
            if(rotationOffsets.Count == 0)
            {
                return;
            }                      

            var j = 0;            
            foreach (var selected in selectedBricks)
            {
                var (_, localOffset) = rotationOffsets[j++];
                if(selected == focusBrick)
                {
                    continue;
                }
                var offset = focusBrick.transform.TransformVector(localOffset);
                selected.transform.position = focusBrick.transform.position - offset;
            }
        }

        private static bool CheckPlaying()
        {
            return EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void EditorUpdate()
        {
            if(GUIUtility.hotControl == 0 && modelsToRecomputePivot.Count > 0)
            {
                foreach(var model in modelsToRecomputePivot)
                {
                    if(!model)
                    {
                        continue;
                    }
                    ModelGroupUtility.RecomputePivot(model);
                }
                modelsToRecomputePivot.Clear();
            }

            framesSinceSelectionChanged++;
            sceneViewCurrentlyInFocus = EditorWindow.focusedWindow == SceneView.lastActiveSceneView;
            if (queuedSelection != null)
            {
                Selection.objects = queuedSelection.ToArray();
                queuedSelection = null;

                if(buildWithBricks)
                {
                    if (CurrentSelectionState != SelectionState.moving)
                    {
                        if (selectedBricks.Count > 0 && Selection.objects.Length == selectedBricks.Count)
                        {
                            Tools.hidden = true;
                            CurrentSelectionState = SelectionState.selected;
                        }
                        else
                        {
                            Tools.hidden = false;
                            CurrentSelectionState = SelectionState.noSelection;
                            selectionBounds = new Bounds();
                        }
                    }
                }
            }

            if(collapseUndo)
            {
                collapseUndo = false;
                if(!CheckPlaying())
                {
                    Undo.CollapseUndoOperations(currentUndoGroupIndex);
                }
            }

            if(deleteQueued && !CheckPlaying())
            {                
                HandleDeletedBricks();
            }

            if (undoQueued)
            {
                undoQueued = false;
                Undo.PerformUndo();
            }

            if (dirtyFields.Count > 0)
            {
                if(!CheckPlaying())
                {
                    foreach(var field in dirtyFields)
                    {                                            
                        if(field)
                        {
                            foreach(var connection in field.connections)
                            {
                                if (connection != null)
                                {
                                    if (connection.knob)
                                    {
                                        Undo.RegisterCompleteObjectUndo(connection.knob.gameObject, "Updating connection");
                                    }
                                    foreach (var tube in connection.tubes)
                                    {
                                        if (tube)
                                        {
                                            Undo.RegisterCompleteObjectUndo(tube.gameObject, "Updating connection");
                                        }
                                    }
                                    connection.UpdateKnobsAndTubes();
                                }
                            }
                        }   
                    }
                }
                dirtyFields.Clear();
            }
        }

        private static void OnUndoRedoPerformed()
        {
            undoRedo = false;
            deleteQueued = false;
            sceneChanged = true;
            if(buildWithBricks)
            {
                CurrentSelectionState = SelectionState.selected;
                SetFromSelection(false);
            }
        } 

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if(buildWithBricks)
            {
                if(IsDuplicateEvent(Event.current))
                {
                    PlaceBrick();
                    duplicateQueued = true;
                }
            }
        }

        private static void PrefabInstanceUpdated(GameObject instance)
        {
            var rootObjects = instance.scene.GetRootGameObjects();
            var bricks = new HashSet<Brick>();
            foreach(var obj in rootObjects)
            {
                bricks.UnionWith(obj.GetComponentsInChildren<Brick>());
            }

            SyncAndUpdateBrickCollision(bricks);
        }

        private static void PrefabStageClosing(PrefabStage stage)
        {
            sceneChanged = true;
            selectedBricks.Clear();
        }

        private static void PrefabStageOpened(PrefabStage stage)
        {
            selectedBricks.Clear();
            SyncAndUpdateBrickCollision();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode openMode)
        {
            if(!scene.IsValid())
            {
                return;
            }

            var rootObjects = scene.GetRootGameObjects();
            var bricks = new HashSet<Brick>();
            foreach (var rootObject in rootObjects)
            {
                bricks.UnionWith(rootObject.GetComponentsInChildren<Brick>());
            }
            SyncAndUpdateBrickCollision(bricks);
        }

        private static void SyncAndUpdateBrickCollision(ICollection<Brick> bricksToSync)
        {
            var previouslyColliding = new HashSet<Brick>();
            foreach (var brick in bricksToSync)
            {
                var didBrickCollide = brick.colliding;
                brick.UpdateColliding(brick.IsColliding(out _), buildWithBricks, false);
                
                if (brick.colliding)
                {                    
                    brick.DisconnectAll();
                }
                else if(didBrickCollide)
                {
                    previouslyColliding.Add(brick);
                }

                if (!buildWithBricks)
                {
                    brick.SetMaterial(false, false);
                }
            }

            foreach(var brick in previouslyColliding)
            {
                var connection = FindConnection(brick);
                if(connection == (null, null))
                {
                    continue;
                }

                var pivot = connection.Item1.field.connectivity.part.brick.transform.position;
                if(ConnectionField.IsConnectionValid(connection.Item1, connection.Item2, pivot))
                {
                    ConnectionField.Connect(connection.Item1, connection.Item2, pivot);
                }
            }
        }

        public static void SyncAndUpdateBrickCollision(bool forceSync = true)
        {
            sceneChanged = forceSync;
            SyncSceneBricks();
            SyncAndUpdateBrickCollision(bricks);
        }

        private static void ActiveSceneChanged(Scene previous, Scene active)
        {
            ApplyConnectivityLayer();
            sceneChanged = true;
            SyncSceneBricks();
            var rootObjects = active.GetRootGameObjects();
            var bricks = new HashSet<Brick>();
            foreach (var rootObject in rootObjects)
            {
                bricks.UnionWith(rootObject.GetComponentsInChildren<Brick>());
            }
            SyncAndUpdateBrickCollision(bricks);
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.ExitingEditMode)
            {
                SetupBrickBuilding(BrickBuildingState.playMode);
            }
            else if(state == PlayModeStateChange.EnteredEditMode)
            {
                SetupBrickBuilding(buildWithBricks ? BrickBuildingState.on : BrickBuildingState.off);
            }
        }

        private static void OnConnectionFieldsDirtied(ICollection<ConnectionField> newDirtyFields)
        {
            dirtyFields.UnionWith(newDirtyFields);
        }

        private static void OnBrickDestroyed(Brick destroyedBrick)
        {            
            deleteQueued = true;

            if (autoUpdateHierarchy)
            {
                bricksRelatedToDeletedBricks.UnionWith(GetRelatedBricks(new HashSet<Brick>{destroyedBrick}, false));
            }
        }

        private static void SetFromSelection(bool updateSelection = true)
        {
            selectedBricks = GetBrickSelection();
            SetFocusBrick(selectedBricks.FirstOrDefault());

            if(updateSelection)
            {
                QueueSelection(selectedBricks);
            }
        } 

        private static void OnSceneGUIDefault(SceneView sceneView)
        {
            currentEvent = Event.current;
            undoRedo = IsUndoRedoEvent(currentEvent);

            CheckChangedTransforms();

            if(lastActiveObject != Selection.activeObject)
            {
                OnSelectionChanged();
            }
            lastActiveObject = Selection.activeObject;
        }

        private static void OnSelectionChanged()
        {
            if(buildWithBricks)
            {
                OnSelectionChangedBuilding();
            }
            else
            {
                OnSelectionChangedDefault();
            }
        }

        private static void OnSelectionChangedDefault()
        {            
            if(framesSinceSelectionChanged == 0)
            {
                return;
            }
            framesSinceSelectionChanged = 0;
            
            if(undoRedo)
            {
                return;
            }

            if(Selection.objects.Length == 0)
            {
                return;
            }

            if((IsOverSceneView() && !sceneViewCurrentlyInFocus) || !IsOverSceneView())
            {
                return;
            }

            var controlDown = currentEvent.control || currentEvent.command;
            
            // Check if anything has changed in selection
            var lastSelectionCount = lastSelection != null ? lastSelection.Length : 0;
            bool changed = Selection.objects.Length != lastSelectionCount;
            var newSelection = new List<GameObject>();
            foreach(var obj in Selection.objects)
            {
                if(!GetValidGameObject(obj, out GameObject go))
                {
                    continue;
                }
                changed |= AdjustSelectionIfNeeded(go, lastSelection, newSelection, true, out _);
            }

            var toRemove = new List<GameObject>();
            HashSet<GameObject> modelGroups = new HashSet<GameObject>();

            foreach(var sel in newSelection)
            {
                var brick = sel.GetComponent<Brick>();
                if(brick)
                {
                    var group = brick.GetComponentInParent<ModelGroup>();
                    if(group)
                    {
                        toRemove.Add(brick.gameObject);
                        if (Array.IndexOf(lastSelection, group) < 0 && !newSelection.Contains(group.gameObject))
                        {
                            modelGroups.Add(group.gameObject);
                        }
                    }
                }
            }

            newSelection.RemoveAll(x => toRemove.Contains(x));
            newSelection.AddRange(modelGroups);

            var nonBricks = new List<GameObject>();

            if(changed || newSelection.Count > 0)
            {
                // Find all model groups and add to selection                
                foreach(var obj in Selection.objects)
                {
                    if(!GetValidGameObject(obj, out GameObject go))
                    {
                        continue;
                    }
                    
                    var group = go.GetComponentInParent<ModelGroup>();
                    if(group != null || (controlDown && group != null && group.gameObject == go))
                    {
                        modelGroups.Add(group.gameObject);
                    }
                    else
                    {
                        nonBricks.Add(go);
                    }
                }
                
                // In case we control clicked, the will be an extra in the selection.
                // This is fine if it was a new brick, but if the brick was already selected
                // we need to remove it from the selection.
                if(controlDown && lastSelection.Length + 1 == Selection.objects.Length)
                {
                    foreach(var obj in Selection.objects)
                    {
                        if(!GetValidGameObject(obj, out GameObject go))
                        {
                            continue;
                        }
                        
                        if(Array.IndexOf(lastSelection, go) < 0)
                        {
                            var group = go.GetComponentInParent<ModelGroup>();
                            if(group && Array.IndexOf(lastSelection, group.gameObject) >= 0)
                            {
                                modelGroups.Remove(group.gameObject);
                            }
                        }
                    }
                }
            }

            var selection = new List<GameObject>();
            foreach(var group in modelGroups)
            {
                selection.Add(group.gameObject);
            }

            foreach(var go in nonBricks)
            {
                selection.Add(go);
            }

            queuedSelection = selection;
            lastActiveObject = Selection.activeObject;
            lastSelection = Selection.objects;
        }

        private static bool GetValidGameObject(UnityEngine.Object obj, out GameObject go)
        {
            go = obj as GameObject;
            return go && go.scene.IsValid();
        }

        private static void OnSelectionChangedBuilding()
        {            
            if(undoRedo)
            {
                selectedBricks = GetBrickSelection();
                return;
            }            

            bool checkParent = true;
            if((IsOverSceneView() && !sceneViewCurrentlyInFocus) || !IsOverSceneView())
            {
                var earlyOut = true;
                foreach(var obj in Selection.objects)
                {
                    if(!GetValidGameObject(obj, out GameObject go))
                    {
                        continue;
                    }
                    var brick = FindBrick(go, false);
                    if(brick)
                    {
                        checkParent = false;
                        earlyOut = false;
                        break;
                    }
                }

                dragAndDropQueued = false;

                if(earlyOut)
                {
                    selectedBricks.Clear();
                    SetFocusBrick(null);
                    return;
                }
            }
  
            // Cache previous selected bricks, so we can check if it has changed
            // If selection has changed, but selected bricks hasn't, then we are still
            // in a drag-and-drop situation.
            var oldSelectedBricks = new HashSet<Brick>(selectedBricks);

            selectedBricks.Clear();
            var lastSelectionCount = lastSelection != null ? lastSelection.Length : 0;
            var newSelection = new List<GameObject>();
            bool changed = Selection.objects.Length != lastSelectionCount;
            var containsNonBricks = false;
            foreach(var obj in Selection.objects)
            {
                if(!GetValidGameObject(obj, out GameObject go))
                {
                    continue;
                }

                changed |= AdjustSelectionIfNeeded(go, lastSelection, newSelection, checkParent, out bool isNotBrick);
                if(!isNotBrick)
                {
                    selectedBricks.Add(FindBrick(go, checkParent));
                }
                else if(sceneViewCurrentlyInFocus)
                {
                    var bricks = go.GetComponentsInChildren<Brick>();
                    foreach(var brick in bricks)
                    {
                        newSelection.Add(brick.gameObject);
                        selectedBricks.Add(brick);
                    }

                    changed = bricks.Length > 0;

                    // In case there is an exact intersection, then we are still in a drag-and-drop situation
                    // and we can queue a collapse undo, since we don't want to be able to undo the intermediate selections
                    if(selectedBricks.Count > 0 && oldSelectedBricks.Intersect(selectedBricks).ToList().Count == oldSelectedBricks.Count)
                    {
                        EndUndoCollapse();
                    }

                    if(changed)
                    {
                        isNotBrick = false;
                        newSelection.Remove(go);
                    }
                }
                containsNonBricks |= isNotBrick;
            }

            if(!changed)
            {
                newSelection = null;
            }

            if(selectedBricks.Count == 0)
            {
                SetFocusBrick(null);
            }

            if(selectedBricks.Count > 0 && focusBrick == null)
            {
                SetFocusBrick(selectedBricks.First());
            }

            queuedSelection = newSelection;
            lastSelection = Selection.objects;
            lastActiveObject = Selection.activeObject;

            if(dragAndDropQueued)
            {
                if(selectedBricks.Count > 0)
                {
                    sceneChanged = true;
                    SetFocusBrick(selectedBricks.First());
                    StartMovingBrick(focusBrick);
                }          
                dragAndDropQueued = false;
            }

            if (duplicateQueued)
            {
                sceneChanged = true;
                duplicateQueued = false;
                if(focusBrick != null)
                {
                    Brick newFocusBrick = null;
                    foreach(var brick in selectedBricks)
                    {
                        if(newFocusBrick == null)
                        {
                            newFocusBrick = brick;
                        }

                        if (brick.transform.position == focusBrick.transform.position)
                        {
                            newFocusBrick = brick;
                        }
                    }

                    SetFocusBrick(newFocusBrick);
                }
                else if(selectedBricks.Count > 0) // In case there was no previous selection, there will be no focus brick to relate to
                {
                    SetFocusBrick(selectedBricks.First());
                }

                // In case we have a duplicate queued, we want to make sure of a few things:
                // 1. All duplicated bricks now possibly reference a one-way connection to the old 
                //    selection. In this case, ONLY set the reference on this side to null. Using Connect(null), would
                //    result in the original bricks losing their connections. Remember to record prefab changes.

                // 2. Add these pairs of connection and old connection to a list, so that we can check them later
                //    when we want to re-establish connections in the new selection. This really only applies for brick
                //    selections larger than 1.

                var connectionPairs = new List<(Connection, Connection)>();
                
                // In our new selection check all bricks
                foreach(var brick in selectedBricks)
                {
                    brick.UpdateColliding(false);
                    foreach (var part in brick.parts)
                    {
                        foreach (var field in part.connectivity.connectionFields)
                        {
                            foreach (var connection in field.connections)
                            {
                                // For each connection, remove the one-way reference to the previous connection 
                                if(field.HasConnection(connection))
                                {
                                    var connectedTo = field.GetConnection(connection);
                                    connection.field.connectedTo[connection.index] = null;
                                    connection.field.OnConnectionChanged(connection);
                                    connectionPairs.Add((connection, connectedTo));
                                }
                            }
                        }
                    }
                }

                foreach(var (connection, connectedTo) in connectionPairs)
                {
                    var brick = connection.field.connectivity.part.brick;

                    // Now check all other bricks for a connection equivalent to the old one
                    foreach(var otherBrick in selectedBricks)
                    {
                        if(otherBrick == brick)
                        {
                            continue;
                        }

                        if(otherBrick.transform.position != connectedTo.field.connectivity.part.brick.transform.position)
                        {
                            continue;
                        }

                        foreach(var otherPart in otherBrick.parts)
                        {
                            if(otherPart.transform.position != connectedTo.field.connectivity.part.transform.position)
                            {
                                continue;
                            }

                            foreach(var otherField in otherPart.connectivity.connectionFields)
                            {
                                if(otherField.transform.position != connectedTo.field.transform.position)
                                {
                                    continue;
                                }

                                foreach(var otherConnection in otherField.connections)
                                {
                                    if(otherConnection.field.HasConnection(otherConnection))
                                    {
                                        continue;
                                    }

                                    var toPosition = connectedTo.field.GetPosition(connectedTo);
                                    var otherPosition = otherConnection.field.GetPosition(otherConnection);

                                    if(toPosition == otherPosition)
                                    {
                                        connection.field.Connect(connection, otherConnection);
                                    }
                                }
                            }
                        }
                    }
                    connection.UpdateKnobsAndTubes();
                }                

                if(focusBrick != null)
                {
                    StartMovingBrick(focusBrick);
                    currentMouseDelta = stickySnapDistance;
                }
            }
        }

#endregion
    }
}
#endif