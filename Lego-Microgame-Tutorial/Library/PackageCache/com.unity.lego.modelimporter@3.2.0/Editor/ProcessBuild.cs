using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LEGOModelImporter
{
    class PreProcessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private bool brickBuildingWasOn = false;
        private string activeScenePath;
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            brickBuildingWasOn = SceneBrickBuilder.GetToggleBrickBuildingStatus();
            if(brickBuildingWasOn)
            {
                SceneBrickBuilder.ToggleBrickBuilding();
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene != null && activeScene.path != "")
            {
                activeScenePath = activeScene.path;
            }

            ProcessHelper.ProcessAssets((go) => {
                Brick[] bricks = go.GetComponentsInChildren<Brick>(true);
                bool hasComponents = false;

                // Remove all of them
                foreach (var brick in bricks)
                {
                    if(brick.colliding)
                    {
                        hasComponents = true;
                        brick.SetMaterial(false, false);                        
                    }
                }

                // Flag as dirty
                if (hasComponents)
                {
                    EditorUtility.SetDirty(go);
                    return true;
                }

                return false;
            });            
        }        

        public void OnPostprocessBuild(BuildReport report)
        {
            if (brickBuildingWasOn)
            {
                ProcessHelper.ProcessAssets((go) =>
                {
                    Brick[] bricks = go.GetComponentsInChildren<Brick>(true);
                    bool hasComponents = false;

                    // Remove all of them
                    foreach (var brick in bricks)
                    {
                        if (brick.colliding)
                        {
                            hasComponents = true;
                            brick.SetMaterial(brick.colliding, false);
                        }
                    }

                    // Flag as dirty
                    if (hasComponents)
                    {
                        EditorUtility.SetDirty(go);
                        return true;
                    }

                    return false;
                });
                SceneBrickBuilder.ToggleBrickBuilding();
            }

            if(activeScenePath != null)
            {
                EditorApplication.delayCall += OpenActiveScene;
            }
        }

        private void OpenActiveScene()
        {
            EditorSceneManager.OpenScene(activeScenePath);
        }
    }


    static class ProcessHelper
    {
        public static void ProcessAssets(Func<GameObject, bool> processAction)
        {
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            allAssetPaths = allAssetPaths.OrderBy(x => x).ToArray();
            List<string> scenes = new List<string>();

            foreach (var path in allAssetPaths)
            {
                int pos = path.IndexOf("/", StringComparison.Ordinal);
                if (pos < 0 || path.Substring(0, pos) == "Packages")
                    continue;   // Skip assets in packages

                pos = path.LastIndexOf(".", StringComparison.Ordinal) + 1;
                string type = path.Substring(pos, path.Length - pos);
                switch (type)
                {
                    case "prefab":
                        var prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                        if (processAction(prefab))
                        {
                            AssetDatabase.SaveAssets(); // Because of issues with nested prefabs, this is called for each asset
                        }
                        break;
                }
            }
            
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(buildScene.path);

                GameObject[] gameObjectsInScene = GameObject.FindObjectsOfType<GameObject>();
                foreach (var gameObject in gameObjectsInScene)
                {
                    if (processAction(gameObject))
                    {
                        AssetDatabase.SaveAssets();
                    }
                }
                EditorSceneManager.SaveScene(scene);                
            }
        }
    }
}
