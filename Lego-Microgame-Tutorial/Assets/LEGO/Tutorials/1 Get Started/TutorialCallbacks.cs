using Unity.LEGO.Minifig;
using UnityEngine;
using LEGOModelImporter;
using Unity.LEGO.Behaviours.Triggers;
using UnityEditor;
using Unity.InteractiveTutorials;

namespace Unity.LEGO.Tutorials
{
    class TutorialCallbacks : ScriptableObject
    {
        [Tooltip("The name by which the Player Minifig GameObject is searched")]
        public string PlayerMinifigName = default;

        [Tooltip("The desired value for MinifigController.MaxForwardSpeed")]
        [Range(5, 30)] // Range same as in MinifigController
        public float MaxForwardSpeed = 6;

        public DeletionCriteria DeletionCriteria = default;
        public FutureObjectReference futurePlatformInstance = default;
        public SceneViewCameraSettings SceneViewCameraSettingsForPlayground = default;

        // As the default speed (should be 12) is good (fast enough), we want to lower the speed a bit
        // for the very first run of the project in order to make it easier for newcomers to handle.
        public void SetupPlayerForTutorial()
        {
            var minifig = GameObject.Find(PlayerMinifigName);
            if (!minifig)
            {
                Debug.LogError($"Could not find GameObject by name '{PlayerMinifigName}'");
                return;
            }

            var controller = minifig.GetComponent<MinifigController>();
            if (!controller)
            {
                Debug.LogError($"'{PlayerMinifigName}' does not have a MinifigController component");
                return;
            }

            controller.maxForwardSpeed = MaxForwardSpeed;
            controller.transform.position = new Vector3(247.92f, -0.06020164f, -42.6f);
        }

        public void MuteOrUnmuteEditorAudio(bool mute)
        {
            EditorUtility.audioMasterMute = mute;
        }

        public void SelectTouchTriggerInScene()
        {
            TouchTrigger touchTrigger = DeletionCriteria.TouchTrigger;
            if (!touchTrigger)
            {
                return;
            }

            Selection.activeObject = touchTrigger.gameObject;
            (SceneView.sceneViews[0] as SceneView).Focus();
        }

        public void EnableBrickBuildingTool()
        {
            bool brickBuildingActive = SceneBrickBuilder.GetToggleBrickBuildingStatus();
            if (brickBuildingActive) { return; }
            SceneBrickBuilder.ToggleBrickBuilding();
        }

        public void DisableBrickBuildingTool()
        {
            bool brickBuildingActive = SceneBrickBuilder.GetToggleBrickBuildingStatus();
            if (!brickBuildingActive) { return; }
            SceneBrickBuilder.ToggleBrickBuilding();
        }

        public void EnableSingleBrickSelectionTool()
        {
            bool singleBrickSelectionIsEnabled = !SceneBrickBuilder.GetSelectConnectedBricks();
            if (singleBrickSelectionIsEnabled) { return; }
            SceneBrickBuilder.ToggleSelectConnectedBricks();
        }

        public void PingFolderOrAsset(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) { return; }
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            EditorGUIUtility.PingObject(obj);
        }

        public void PingFolderOrFirstAsset(string folderPath)
        {
            string path = GetFirstAssetPathInFolder(folderPath, true);
            if (string.IsNullOrEmpty(path))
            {
                path = folderPath;
            }
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            EditorGUIUtility.PingObject(obj);
        }

        /// <summary>
        /// Use this method when you want to force a tutorial page completion when the condition
        /// is "compelete when Any criteria is met". Particularly useful on empty pages that use
        /// KeepPlatformSelected() alike methods.
        /// </summary>
        /// <returns>True</returns>
        public bool ReturnTrue() { return true; }

        /// <summary>
        /// Keeps the platform selected during a tutorial. 
        /// </summary>
        /// <returns>
        /// False in order to not let the tutorial proceed just because of this. Do not use this method
        /// if you want to let the user proceed when "all" criterias are met
        /// </returns>
        public void KeepPlatformSelected()
        {
            SelectGameObject(futurePlatformInstance);
        }

        /// <summary>
        /// Selects a GameObject in the scene, marking it as the active object for selection
        /// </summary>
        /// <param name="futureObjectReference"></param>
        public void SelectGameObject(FutureObjectReference futureObjectReference)
        {
            if (futureObjectReference.sceneObjectReference == null) { return; }
            Selection.activeObject = futureObjectReference.sceneObjectReference.ReferencedObjectAsGameObject;
        }

        public void LoadPlaygroundScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnPlaygroundSceneLoaded;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/LEGO/Scenes/Playground.unity");
        }

        void OnPlaygroundSceneLoaded(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnPlaygroundSceneLoaded;
            SceneViewCameraSettingsForPlayground.Apply();
        }

        public void SelectMoveTool()
        {
            Tools.current = Tool.Move;
        }

        public void SelectRotateTool()
        {
            Tools.current = Tool.Rotate;
        }

        #region Utils (needed by real callbacks)
        string GetFirstAssetPathInFolder(string folder, bool includeFolders)
        {
            if (includeFolders)
            {
                string path = GetFirstValidAssetPath(System.IO.Directory.GetDirectories(folder));
                if (path != null)
                {
                    return path;
                }
            }
            return GetFirstValidAssetPath(System.IO.Directory.GetFiles(folder));
        }

        string GetAssetPathInFolder(string folder, string fileName)
        {
            return GetPathOfAssetWithName(System.IO.Directory.GetFiles(folder), fileName);
        }

        string GetPathOfAssetWithName(string[] paths, string fileName)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(paths[i]))
                && paths[i].Contains(fileName))
                {
                    return paths[i];
                }
            }
            return null;
        }

        string GetFirstValidAssetPath(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(paths[i])))
                {
                    return paths[i];
                }
            }
            return null;
        }
        #endregion
    }
}
