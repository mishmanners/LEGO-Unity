// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace LEGOModelImporter
{
    [InitializeOnLoad]
    public class ConnectivityUIManager
    {
        const int WindowID = 42;
        const int SceneViewTopBarHeight = 21;
        const int DistanceFromBorders = 5;
        const int ButtonSize = 40;
        const int ButtonMargin = 4;

        static bool show;
        static bool Initialised;

        const string showBrickBuildingToolsMenuPath = "LEGO Tools/Show LEGO Tools #&%b";
        static readonly string showBrickBuildingToolsPrefsKey = "com.unity.lego.modelimporter.showBrickBuildingTools";

        static GUIStyle brickBuildingStyle;
        static GUIStyle selectConnectedStyle;

        static Texture2D brickBuildingOnImage;
        static Texture2D brickBuildingOffImage;
        static Texture2D selectConnectedOffImage;
        static Texture2D selectConnectedOnImage;

        static Rect toolSettingsWindow = new Rect(10f, SceneViewTopBarHeight + 10, 80, 40);

        static ConnectivityUIManager()
        {
            show = EditorPrefs.GetBool(showBrickBuildingToolsPrefsKey, true);
            SceneView.duringSceneGui += CustomOnSceneGUI;
            ShowTools(show);
        }

        public static void ShowTools(bool value, bool keep = true)
        {
            show = value;
            if(keep)
            {
                EditorPrefs.SetBool(showBrickBuildingToolsPrefsKey, show);
            }
            
            SceneView.RepaintAll();
        }

        [MenuItem(showBrickBuildingToolsMenuPath, priority = 25)]
        static void ShowBrickBuildingTools()
        {
            ShowTools(!show);
        }

        public static bool ShouldShowTools()
        {
            return EditorPrefs.GetBool(showBrickBuildingToolsPrefsKey, true);
        }

        [MenuItem(showBrickBuildingToolsMenuPath, true)]
        static bool ValidateShowBrickBuildingTools()
        {
            Menu.SetChecked(showBrickBuildingToolsMenuPath, show);
            return !EditorApplication.isPlaying;
        }

        static void CustomOnSceneGUI(SceneView sceneview)
        {
            if (show)
            {
                Init();
                toolSettingsWindow = ClampRectToSceneView(GUILayout.Window(WindowID, toolSettingsWindow, ConnectivityUIWindow, new GUIContent("LEGO® Tools")));
            }
        }

        static void Init()
        {
            if (!Initialised)
            {
                brickBuildingOnImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Brick Building On@2x.png");
                brickBuildingOffImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Brick Building Off@2x.png");
                selectConnectedOnImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Select Connected On@2x.png");
                selectConnectedOffImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Select Connected Off@2x.png");

                brickBuildingStyle = new GUIStyle(GUIStyle.none);
                brickBuildingStyle.fixedWidth = ButtonSize;
                brickBuildingStyle.fixedHeight = ButtonSize;
                brickBuildingStyle.margin = new RectOffset(ButtonMargin, ButtonMargin, ButtonMargin, ButtonMargin);

                selectConnectedStyle = new GUIStyle(brickBuildingStyle);
                selectConnectedStyle.normal.background = selectConnectedOffImage;
                selectConnectedStyle.active.background = selectConnectedOnImage;

                Initialised = true;
            }
        }

        static void ConnectivityUIWindow(int windowId)
        {
            bool brickBuildingActive = SceneBrickBuilder.GetToggleBrickBuildingStatus();
            bool selectConnectedActive = SceneBrickBuilder.GetSelectConnectedBricks();

            GUILayout.BeginHorizontal();

            // Toggle brick building
            string toggleBrickBuildingShortcut = ShortcutManager.instance.GetShortcutBinding("Main Menu/" + SceneBrickBuilder.brickBuildingMenuPath).ToString();
            GUIContent toggleBrickBuildingContent = new GUIContent("", "Brick Building " + toggleBrickBuildingShortcut);
            brickBuildingStyle.normal.background = brickBuildingActive ? brickBuildingOnImage : brickBuildingOffImage;
            brickBuildingStyle.active.background = brickBuildingActive ? brickBuildingOffImage : brickBuildingOnImage;
            if (GUILayout.Button(toggleBrickBuildingContent, brickBuildingStyle))
            {
                SceneBrickBuilder.ToggleBrickBuilding();
            }

            // Toggle select connected
            string toggleSelectConnectedShortcut = ShortcutManager.instance.GetShortcutBinding("Main Menu/" + SceneBrickBuilder.editorSelectConnectedMenuPath).ToString();
            GUIContent toggleSelectConnectedContent = selectConnectedActive ? new GUIContent("", "Connected Brick Selection " + toggleSelectConnectedShortcut)
                                                                            : new GUIContent("", "Single Brick Selection " + toggleSelectConnectedShortcut);
            selectConnectedStyle.normal.background = selectConnectedActive ? selectConnectedOnImage : selectConnectedOffImage;
            selectConnectedStyle.active.background = selectConnectedActive ? selectConnectedOffImage : selectConnectedOnImage;
            GUI.enabled = SceneBrickBuilder.ValidateSelectConnectedBricks();
            if (GUILayout.Button(toggleSelectConnectedContent, selectConnectedStyle))
            {
                SceneBrickBuilder.ToggleSelectConnectedBricks();
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUI.DragWindow();
        }

        static Rect ClampRectToSceneView(Rect rect)
        {
            var distanceFromTop = DistanceFromBorders;
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                distanceFromTop += DistanceFromBorders + SceneViewTopBarHeight;
            }
            Rect sceneViewArea = SceneView.lastActiveSceneView.position;
            rect.x = Mathf.Clamp(rect.x, DistanceFromBorders, sceneViewArea.width - rect.width - DistanceFromBorders);
            rect.y = Mathf.Clamp(rect.y, distanceFromTop + SceneViewTopBarHeight, sceneViewArea.height - rect.height - distanceFromTop);
            return rect;
        }
    }
}
