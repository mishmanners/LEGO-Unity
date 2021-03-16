// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace LEGOModelImporter
{

    [CustomEditor(typeof(ModelGroup))]
    public class ModelGroupEditor : Editor
    {
        ModelGroup modelGroup;
        SerializedProperty processedProp;
        SerializedProperty optimizationsProp;
        SerializedProperty randomizeNormalsProp;
        //SerializedProperty imperfectionsProp;
        SerializedProperty viewsProp;

        private static bool showOtherGroups = false;
        private static bool showLights = false;
        private static bool showCameras = false;

        private static Dictionary<ModelGroup, List<CullingCameraConfig>> otherGroupViews = new Dictionary<ModelGroup, List<CullingCameraConfig>>();
        private static Dictionary<Light, CullingCameraConfig> lightViews = new Dictionary<Light, CullingCameraConfig>();
        private static Dictionary<Camera, CullingCameraConfig> cameraViews = new Dictionary<Camera, CullingCameraConfig>();

        private static readonly string[] lodOptions = { "LOD 0", "LOD 1", "LOD 2" };

        protected virtual void OnEnable()
        {
            modelGroup = (ModelGroup)target;
            processedProp = serializedObject.FindProperty("processed");
            optimizationsProp = serializedObject.FindProperty("optimizations");
            randomizeNormalsProp = serializedObject.FindProperty("randomizeNormals");
            //imperfectionsProp = serializedObject.FindProperty("imperfections");
            viewsProp = serializedObject.FindProperty("views");

            // Collect views from other model groups.
            otherGroupViews.Clear();
            var groups = GameObject.FindObjectsOfType<ModelGroup>();
            foreach(var otherGroup in groups)
            {
                if (otherGroup != modelGroup)
                {
                    foreach(var view in otherGroup.views)
                    {
                        if (!otherGroupViews.ContainsKey(otherGroup))
                        {
                            otherGroupViews.Add(otherGroup, new List<CullingCameraConfig>());
                        }
                        otherGroupViews[otherGroup].Add(view);
                    }
                }
            }
            // Collect views from light sources.
            lightViews.Clear();
            var lights = GameObject.FindObjectsOfType<Light>();
            Bounds groupBounds = new Bounds();
            bool hasComputedGroupBounds = false;
            foreach(var light in lights)
            {
                switch(light.type)
                {
                    case LightType.Spot:
                        {
                            CullingCameraConfig view = new CullingCameraConfig()
                            {
                                name = light.name,
                                perspective = true,
                                position = light.transform.position,
                                rotation = light.transform.rotation,
                                fov = light.spotAngle,
                                minRange = light.shadowNearPlane,
                                maxRange = light.range,
                                aspect = 1.0f
                            };
                            lightViews.Add(light, view);
                            break;
                        }
                    case LightType.Directional:
                        {
                            // Need to compute group bounds.
                            if (!hasComputedGroupBounds)
                            {
                                var renderers = modelGroup.transform.GetComponentsInChildren<MeshRenderer>();
                                if (renderers.Length > 0)
                                {
                                    groupBounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);

                                    foreach (var renderer in renderers)
                                    {
                                        groupBounds.Encapsulate(renderer.bounds);
                                    }
                                }

                                hasComputedGroupBounds = true;
                            }

                            // Find bounds' corners in light space.
                            var corners = new List<Vector3>()
                            {
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3(-1, -1, -1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3(-1, -1,  1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3(-1,  1, -1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3(-1,  1,  1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3( 1, -1, -1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3( 1, -1,  1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3( 1,  1, -1), groupBounds.extents)),
                                light.transform.InverseTransformPoint(groupBounds.center + Vector3.Scale(new Vector3( 1,  1,  1), groupBounds.extents))
                            };

                            // Find min and max range for corners.
                            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                            foreach(var corner in corners)
                            {
                                min = Vector3.Min(min, corner);
                                max = Vector3.Max(max, corner);
                            }

                            // Get size.
                            Vector2 size = new Vector2(Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x)), Mathf.Max(Mathf.Abs(min.y), Mathf.Abs(max.y)));

                            CullingCameraConfig view = new CullingCameraConfig()
                            {
                                name = light.name,
                                perspective = false,
                                position = light.transform.position,
                                rotation = light.transform.rotation,
                                size = size.y,
                                minRange = min.z,
                                maxRange = max.z,
                                aspect = size.x / size.y
                            };
                            lightViews.Add(light, view);
                            break;
                        }
                }
            }
            // Collect view from cameras.
            cameraViews.Clear();
            var cameras = GameObject.FindObjectsOfType<Camera>();
            foreach(var camera in cameras)
            {
                CullingCameraConfig view = new CullingCameraConfig()
                {
                    name = camera.name,
                    perspective = !camera.orthographic,
                    position = camera.transform.position,
                    rotation = camera.transform.rotation,
                    size = camera.orthographicSize,
                    fov = camera.fieldOfView,
                    aspect = camera.aspect,
                    minRange = camera.nearClipPlane,
                    maxRange = camera.farClipPlane
                };
                cameraViews.Add(camera, view);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var doReimport = false;

            GUILayout.Label("Import", EditorStyles.boldLabel);
            if (modelGroup.autoGenerated)
            {
                EditorGUILayout.HelpBox("This group was changed by brick building since import. You can only reimport the entire model.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Number", modelGroup.number.ToString());
                EditorGUILayout.LabelField("Name", modelGroup.groupName);
                EditorGUILayout.LabelField("Colliders", modelGroup.importSettings.colliders.ToString());
                EditorGUILayout.LabelField("Connectivity", modelGroup.importSettings.connectivity.ToString());
                EditorGUILayout.LabelField("Static", modelGroup.importSettings.isStatic.ToString());
                EditorGUILayout.LabelField("Lightmapped", modelGroup.importSettings.lightmapped.ToString());
                EditorGUILayout.LabelField("Randomized Rotations", modelGroup.importSettings.randomizeRotation.ToString());
                EditorGUILayout.LabelField("Legacy Preference", modelGroup.importSettings.preferLegacy.ToString());
                EditorGUILayout.LabelField("LOD", modelGroup.importSettings.lod.ToString());

                GUILayout.Space(16);

                // Check if part of prefab instance and prevent reimport.
                if (PrefabUtility.IsPartOfAnyPrefab(target))
                {
                    EditorGUILayout.HelpBox("You cannot reimport a prefab instance. Please perform reimporting on the prefab itself.", MessageType.Warning);
                }
                else
                {
                    if (File.Exists(modelGroup.relativeFilePath) || File.Exists(modelGroup.absoluteFilePath))
                    {
                        doReimport = GUILayout.Button("Reimport");
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Could not find original file. Select model and reimport from a new file.", MessageType.Warning);
                    }
                }
            }

            var doProcessing = false;

            GUILayout.Space(16);
            GUILayout.Label("Processing", EditorStyles.boldLabel);
            if (processedProp.boolValue)
            {
                EditorGUILayout.HelpBox("Already Processed", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(optimizationsProp);

                // Backface culling and geometry removal UI, and sorting UI.
                if (((ModelGroup.Optimizations)optimizationsProp.intValue & (ModelGroup.CameraBasedGeometryRemovalOptimizations | ModelGroup.Optimizations.SortFrontToBack)) != 0)
                {
                    var geometryRemoval = ((ModelGroup.Optimizations)optimizationsProp.intValue & ModelGroup.CameraBasedGeometryRemovalOptimizations) != 0;
                    var sorting = ((ModelGroup.Optimizations)optimizationsProp.intValue & ModelGroup.Optimizations.SortFrontToBack) != 0;

                    var message = "";
                    if (geometryRemoval)
                    {
                        message += "Backface culling and geometry removal are based on all the specified views.\n";
                    }
                    if (sorting)
                    {
                        message += "Front-to-back geometry sorting is based on the first specified view.";
                    }
                    EditorGUILayout.HelpBox(message, MessageType.Info);

                    if (viewsProp.arraySize == 0 && Camera.main)
                    {
                        var warning = "No views have been specified.\n";
                        if (geometryRemoval)
                        {
                            warning += "View from current main camera will be used when doing backface culling and geometry removal.\n";
                        }
                        if (sorting)
                        {
                            warning += "View from current main camera will be used when sorting geometry.";
                        }
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                    else if (viewsProp.arraySize == 0)
                    {
                        if (geometryRemoval || sorting)
                        {
                            var error = "No views have been specified.\n";
                            if (geometryRemoval)
                            {
                                error += "Could not find main camera to use when doing backface culling and geometry removal.\n";

                            }
                            if (sorting)
                            {
                                error += "Could not find main camera to use when sorting geometry.";
                            }
                            EditorGUILayout.HelpBox(error, MessageType.Error);
                        }
                    }

                    if (otherGroupViews.Count > 0)
                    {
                        showOtherGroups = EditorGUILayout.Foldout(showOtherGroups, "Views From Other Groups", true);
                        if (showOtherGroups)
                        {
                            foreach(var group in otherGroupViews.Keys)
                            {
                                if (GUILayout.Button("Add Views From " + group.parentName + " " + group.groupName))
                                {
                                    foreach(var groupView in otherGroupViews[group])
                                    {
                                        AddView(groupView);
                                    }
                                }
                            }
                        }
                    }

                    if (lightViews.Count > 0)
                    {
                        showLights = EditorGUILayout.Foldout(showLights, "Views From Lights", true);
                        if (showLights)
                        {
                            foreach (var light in lightViews.Keys)
                            {
                                if (GUILayout.Button("Add View From " + light.name))
                                {
                                    AddView(lightViews[light]);
                                }
                            }
                        }
                    }

                    if (cameraViews.Count > 0)
                    {
                        showCameras = EditorGUILayout.Foldout(showCameras, "Views From Cameras", true);
                        if (showCameras)
                        {
                            foreach (var camera in cameraViews.Keys)
                            {
                                if (GUILayout.Button("Add View From " + camera.name))
                                {
                                    AddView(cameraViews[camera]);
                                }
                            }
                        }
                    }

                    if (GUILayout.Button("Add Current Scene Views"))
                    {
                        foreach (var sceneView in SceneView.sceneViews)
                        {
                            var sceneViewCamera = ((SceneView)sceneView).camera;

                            viewsProp.arraySize++;
                            var newEntry = viewsProp.GetArrayElementAtIndex(viewsProp.arraySize - 1);
                            newEntry.FindPropertyRelative("name").stringValue = sceneViewCamera.name;
                            newEntry.FindPropertyRelative("perspective").boolValue = !sceneViewCamera.orthographic;
                            newEntry.FindPropertyRelative("position").vector3Value = sceneViewCamera.transform.position;
                            newEntry.FindPropertyRelative("rotation").quaternionValue = sceneViewCamera.transform.rotation;
                            newEntry.FindPropertyRelative("size").floatValue = sceneViewCamera.orthographicSize;
                            newEntry.FindPropertyRelative("fov").floatValue = sceneViewCamera.fieldOfView;
                            newEntry.FindPropertyRelative("aspect").floatValue = sceneViewCamera.aspect;
                            newEntry.FindPropertyRelative("minRange").floatValue = sceneViewCamera.nearClipPlane;
                            newEntry.FindPropertyRelative("maxRange").floatValue = sceneViewCamera.farClipPlane;
                        }
                    }

                    if (viewsProp.arraySize > 0)
                    {
                        EditorList.Show(viewsProp, EditorListOption.All);//, new GUIContent[] { new GUIContent("Z", "View From") }, new System.Action<SerializedProperty>[] { (p) => ViewFrom(p) });
                    }
                }

                GUILayout.Space(16);
                EditorGUILayout.PropertyField(randomizeNormalsProp, new GUIContent("Add Noise To Normals", "A small amount of noise adds visual detail."));


                /*				GUILayout.Space(16);
                                EditorGUILayout.PropertyField(imperfectionsProp);

                                // Imperfections UI.
                                if (((ModelGroup.Imperfections)imperfectionsProp.intValue & ModelGroup.Imperfections.UVDegradation) == ModelGroup.Imperfections.UVDegradation)
                                {
                                    EditorGUILayout.HelpBox("UV degradation has not been implemented yet.", MessageType.Warning);
                                }
                                if (((ModelGroup.Imperfections)imperfectionsProp.intValue & ModelGroup.Imperfections.Scratches) == ModelGroup.Imperfections.Scratches)
                                {
                                    EditorGUILayout.HelpBox("Scratches have not been implemented yet.", MessageType.Warning);
                                }*/

                GUILayout.Space(16);
                if (PrefabUtility.IsPartOfAnyPrefab(target))
                {
                    EditorGUILayout.HelpBox("You cannot process a prefab instance. Please perform processing on the prefab itself.", MessageType.Warning);
                }
                else
                {
                    // Process button.
                    doProcessing = GUILayout.Button("Process");
                    if (doProcessing)
                    {
                        Undo.RegisterFullObjectHierarchyUndo(modelGroup.gameObject, "Process");
                        processedProp.boolValue = true;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (doReimport)
            {
                // FIXME Issue with finding the correct group when multiple groups have same name and group number has changed!

                ImportModel.ReimportModelGroup(modelGroup);
            }

            if (doProcessing)
            {
                Vector2Int vertCount = Vector2Int.zero;
                Vector2Int triCount = Vector2Int.zero;
                Vector2Int meshCount = Vector2Int.zero;
                Vector2Int boxColliderCount = Vector2Int.zero;
                ModelProcessor.ProcessModelGroup(modelGroup, ref vertCount, ref triCount, ref meshCount, ref boxColliderCount);

                Debug.Log($"Process result (before/after):\nVerts {vertCount.x}/{vertCount.y}, tris {triCount.x}/{triCount.y}, meshes {meshCount.x}/{meshCount.y}, box colliders {boxColliderCount.x}/{boxColliderCount.y}");

                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }
            }
        }

        private void AddView(CullingCameraConfig view)
        {
            viewsProp.arraySize++;
            var newEntry = viewsProp.GetArrayElementAtIndex(viewsProp.arraySize - 1);
            newEntry.FindPropertyRelative("name").stringValue = view.name;
            newEntry.FindPropertyRelative("perspective").boolValue = view.perspective;
            newEntry.FindPropertyRelative("position").vector3Value = view.position;
            newEntry.FindPropertyRelative("rotation").quaternionValue = view.rotation;
            newEntry.FindPropertyRelative("size").floatValue = view.size;
            newEntry.FindPropertyRelative("fov").floatValue = view.fov;
            newEntry.FindPropertyRelative("aspect").floatValue = view.aspect;
            newEntry.FindPropertyRelative("minRange").floatValue = view.minRange;
            newEntry.FindPropertyRelative("maxRange").floatValue = view.maxRange;
        }

        /*static SceneView sceneView;

        private void ViewFrom(SerializedProperty viewProp)
        {
            if (sceneView == null)
            {
                sceneView = EditorWindow.CreateWindow<SceneView>();
            } else
            {
                sceneView.Show();
            }

            var sceneRect = sceneView.position;
            sceneRect.width = sceneRect.height * viewProp.FindPropertyRelative("aspect").floatValue;
            sceneRect.height += EditorStyles.toolbar.fixedHeight; // Add toolbar height to window height.
            sceneView.position = sceneRect;

            sceneView.orthographic = !viewProp.FindPropertyRelative("perspective").boolValue;
            sceneView.pivot = viewProp.FindPropertyRelative("position").vector3Value + viewProp.FindPropertyRelative("rotation").quaternionValue * Vector3.forward * sceneView.size;
            sceneView.rotation = viewProp.FindPropertyRelative("rotation").quaternionValue;

            // There is currently an issue with Scene View camera's FOV not matching a given camera's FOV.
            // https://issuetracker.unity3d.com/issues/scene-camera-viewport-does-not-match-game-camera-even-with-the-same-settings

            var cameraSettings = new SceneView.CameraSettings();
            cameraSettings.dynamicClip = false;
            cameraSettings.nearClip = viewProp.FindPropertyRelative("minRange").floatValue;
            cameraSettings.farClip = viewProp.FindPropertyRelative("maxRange").floatValue;
            if (sceneView.orthographic)
            {
                sceneView.size = ConvertOrthographicSizeToSceneViewSize(viewProp.FindPropertyRelative("size").floatValue, viewProp.FindPropertyRelative("aspect").floatValue);
            }
            else
            {
                cameraSettings.fieldOfView = viewProp.FindPropertyRelative("fov").floatValue;
            }
            sceneView.cameraSettings = cameraSettings;
        }

        private float ConvertOrthographicSizeToSceneViewSize(float orthographicSize, float aspect)
        {
            var height = orthographicSize;
            var width = height * aspect;

            return Mathf.Sqrt(height * height + width * width) * 0.9825f;
        }*/
    }

}

