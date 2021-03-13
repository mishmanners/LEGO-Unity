// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LEGOModelImporter
{
    public class ConnectivityVersionChecker
    {
        public static readonly int currentVersion = 1;
        static readonly string connectivityVersionFile = "Version.asset";
        static readonly string dontAskUpdateToThisVersionPrefsKey = "com.unity.lego.modelimporter.dontAskUpdateForVersion";
        private static bool updating = false;

        public static event System.Action updateStarted;
        public static event System.Action updateFinished;

        internal static void CheckForUpdates()
        {
            if (CheckVersion())
            {
                UpdateAssets();
            }
        }

        static bool UpdateDialog()
        {
            return EditorUtility.DisplayDialog("LEGO® connectivity data outdated",
            "You need to update to continue using Brick Building and connectivity.\n\nUpdating the connectivity data is non-reversible and will be done across all scenes and prefabs.\n\nThe update may take a long time depending on the number of prefabs.\n\nYou can always force an update at a later time in the LEGO Tools menu.\n\nAll unsaved changes will be lost.\n\nUpdate connectivity data?", "Yes", "No");
        }

        static void UpToDateDialog()
        {
            EditorUtility.DisplayDialog("LEGO® connectivity data up to date", $"The connectivity data is up to date on version {currentVersion}.", "Ok");
        }

        [MenuItem("LEGO Tools/Check Connectivity Version", priority = 100)]
        static void CheckConnectivityVersion()
        {
            EditorApplication.delayCall += () => CheckAndUpdate(true, true);
        }

        //[MenuItem("LEGO Tools/Dev/Force Update")]
        static void ForceUpdate()
        {
            UpdateAssets();
        }

        [InitializeOnLoadMethod]
        private static void InitializeVersionChecker()
        {
            EditorApplication.delayCall += () => CheckAndUpdate();
        }

        private static void CheckAndUpdate(bool forceCheck = false, bool notifyUpToDate = false)
        {
            if(updating)
            {
                return;
            }

            if(CheckVersion(forceCheck, notifyUpToDate))
            {
                UpdateAssets();
            }            
        }

        private static void UpdateAssets()
        {
            updateStarted?.Invoke();

            EditorUtility.DisplayProgressBar("Updating connectivity", "Getting ready", 0.0f);
            updating = true;

            var activeScene = SceneManager.GetActiveScene();
            var activeScenePath = activeScene.path;

            CheckConnectivityFeatureLayer.CreateReceptorLayer();
            CheckConnectivityFeatureLayer.CreateConnectorLayer();

            // 1. Update connectivity prefabs.
            string[] guids = AssetDatabase.FindAssets("", new string[] { PartUtility.connectivityPath });

            for (int i = 0; i < guids.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Updating connectivity", "Updating connectivity prefabs", 0.25f * i / guids.Length);

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Connectivity connectivity = AssetDatabase.LoadAssetAtPath<Connectivity>(assetPath);
                if (connectivity)
                {
                    if (connectivity.version == 0 && currentVersion == 1)
                    {
                        ConvertVersion_0_To_1(connectivity);
                    }
                }
            }

            // 2. Run through all prefabs and determine dependency graph based on nesting. Also collect all scene paths for later processing.
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            List<string> scenes = new List<string>();
            var index = 0;

            var dependencyGraph = new DirectedGraph<string>();
            var existingDependencyNodes = new Dictionary<string, DirectedGraph<string>.Node>();

            foreach (var path in allAssetPaths)
            {
                EditorUtility.DisplayProgressBar("Updating connectivity", "Detecting prefab dependencies", 0.25f + 0.25f * index++ / allAssetPaths.Length);

                int pos = path.IndexOf("/", StringComparison.Ordinal);
                if (pos < 0 || path.Substring(0, pos) == "Packages")
                    continue;   // Skip assets in packages.

                if (path.StartsWith(PartUtility.connectivityPath))
                    continue;   // Skip connectivity assets.

                if (path.StartsWith(PartUtility.collidersPath))
                    continue;   // Skip collider assets.

                if (path.StartsWith(PartUtility.geometryPath))
                    continue;   // Skip geometry assets.

                pos = path.LastIndexOf(".", StringComparison.Ordinal) + 1;
                string type = path.Substring(pos, path.Length - pos);

                switch (type)
                {
                    case "prefab":
                        {
                            var contents = PrefabUtility.LoadPrefabContents(path);

                            var bricks = contents.GetComponentsInChildren<Brick>(true);

                            if (bricks.Length > 0)
                            {
                                FindDependencies(path, bricks, existingDependencyNodes, dependencyGraph);
                            }

                            PrefabUtility.UnloadPrefabContents(contents);
                        }
                        break;
                    case "unity":
                        {
                            scenes.Add(path);
                        }
                        break;
                }
            }

            // 3. Run through dependency sorted prefabs and update+connect connectivity on non-prefab instances.
            var sortedNodes = dependencyGraph.TopologicalSort();
            index = 0;
            foreach (var node in sortedNodes)
            {
                EditorUtility.DisplayProgressBar("Updating connectivity", "Updating prefabs", 0.5f + 0.25f * index++ / sortedNodes.Count);

                var contents = PrefabUtility.LoadPrefabContents(node.data);

                var bricks = contents.GetComponentsInChildren<Brick>(true);

                if (bricks.Length > 0)
                {
                    if (UpdateConnections(bricks))
                    {
                        Debug.Log(node.data + " Updated");
                    }
                    FixConnections(bricks);

                    PrefabUtility.SaveAsPrefabAsset(contents, node.data);
                }

                PrefabUtility.UnloadPrefabContents(contents);
            }

            // 4. Run through all scenes and update+connect connectivity on non-prefab instances.
            index = 0;
            foreach (var path in scenes)
            {
                EditorUtility.DisplayProgressBar("Updating connectivity", "Updating scenes", 0.75f + 0.25f * index++ / scenes.Count);

                Scene scene = EditorSceneManager.OpenScene(path);

                var gameObjectsInScene = scene.GetRootGameObjects();

                foreach (var gameObject in gameObjectsInScene)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                    {
                        continue;
                    }

                    var bricks = gameObject.GetComponentsInChildren<Brick>(true);

                    if (bricks.Length > 0)
                    {
                        if (UpdateConnections(bricks))
                        {
                            Debug.Log($" -------- {gameObject.name} Updated");
                        }
                        FixConnections(bricks);
                    }
                }
                EditorSceneManager.SaveScene(scene);
            }

            if (activeScenePath.Length != 0)
            {
                EditorSceneManager.OpenScene(activeScenePath);
            }

            updating = false;
            EditorUtility.ClearProgressBar();

            updateFinished?.Invoke();
        }

        static bool CheckVersion(bool forceCheck = false, bool notifyUpToDate = false)
        {
            var dontAskVersion = SessionState.GetInt(dontAskUpdateToThisVersionPrefsKey, 0);
            if(!forceCheck && currentVersion == dontAskVersion)
            {
                return false;
            }
            
            var updateFrom = currentVersion;
            var versionFilePath = Path.Combine(PartUtility.connectivityPath, connectivityVersionFile);
            if (File.Exists(versionFilePath))
            {
                TextAsset versionAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(versionFilePath); 
                if(int.TryParse(versionAsset.text, out int version))
                {
                    if(currentVersion > version)
                    {
                        if(UpdateDialog())
                        {
                            AssetDatabase.CreateAsset(new TextAsset(currentVersion.ToString()), versionFilePath);
                            updateFrom = version;
                        }
                        else
                        {
                            SessionState.SetInt(dontAskUpdateToThisVersionPrefsKey, currentVersion);
                        }
                    }
                    else
                    {
                        if (notifyUpToDate)
                        {

                            UpToDateDialog();
                        }
                    }
                }
            }
            else
            {
                if(!Directory.Exists(PartUtility.connectivityPath))
                {
                    Directory.CreateDirectory(PartUtility.connectivityPath);

                    AssetDatabase.CreateAsset(new TextAsset(currentVersion.ToString()), versionFilePath);
                    if (notifyUpToDate)
                    {
                        UpToDateDialog();
                    }
                }
                else
                {
                    if (UpdateDialog())
                    {
                        AssetDatabase.CreateAsset(new TextAsset(currentVersion.ToString()), versionFilePath);
                        updateFrom = 0;
                    }
                    else
                    {
                        SessionState.SetInt(dontAskUpdateToThisVersionPrefsKey, currentVersion);
                    }
                }
            }
            return updateFrom < currentVersion;
        }

        private static void FixConnections(Brick[] bricks)
        {
            Physics.SyncTransforms();
            foreach(var brick in bricks)
            {

                foreach (var part in brick.parts)
                {
                    var connectivity = part.connectivity;
                    if (!connectivity)
                    {
                        // Unsupported or legacy.
                        continue;
                    }

                    foreach (var field in connectivity.connectionFields)
                    {

                        var connections = field.QueryConnections(out _);
                        foreach (var (connection, otherConnection) in connections)
                        {
                            if (field.HasConnection(connection) || otherConnection.field.HasConnection(otherConnection))
                            {
                                continue;
                            }

                            if (Connection.ConnectionValid(connection, otherConnection, out Connection.ConnectionMatch match))
                            {
                                if (match == Connection.ConnectionMatch.ignore)
                                {
                                    continue;
                                }
                                connection.field.Connect(connection, otherConnection);

                                Connection.RegisterPrefabChanges(connection.field);
                                Connection.RegisterPrefabChanges(otherConnection.field);
                            }
                        }
                    }
                }
            }
        }

        private static bool UpdateConnections(Brick[] bricks)
        {
            var updated = false;

            foreach(var brick in bricks)
            {
                if(PrefabUtility.IsPartOfPrefabInstance(brick))
                {
                    continue;
                }

                foreach(var part in brick.parts)
                {
                    var designID = part.designID.ToString();
                    if (!PartUtility.CheckIfConnectivityForPartIsUnpacked(designID))
                    {
                        PartUtility.UnpackConnectivityForPart(designID);
                    }

                    var connectivity = part.connectivity;
                    if(!connectivity)
                    {
                        // Unsupported or legacy.
                        continue;
                    }

                    if (connectivity.version == currentVersion)
                    {
                        // Already up to date.
                        continue;
                    }

                    var connectivityToInstantiate = PartUtility.LoadConnectivityPrefab(designID);
                    if (connectivityToInstantiate)
                    {
                        GameObject.DestroyImmediate(connectivity.gameObject, true);
                        var connectivityGO = UnityEngine.Object.Instantiate(connectivityToInstantiate);
                        connectivityGO.name = "Connectivity";
                        connectivityGO.transform.SetParent(part.transform, false);
                        var connectivityComp = connectivityGO.GetComponent<Connectivity>();
                        part.connectivity = connectivityComp;
                        part.brick.totalBounds.Encapsulate(connectivityComp.extents);
                        connectivityComp.part = part;

                        updated = true;

                        foreach(var tube in part.tubes)
                        {
                            tube.connections.Clear();
                            tube.field = null;
                        }

                        foreach(var knob in part.knobs)
                        {
                            knob.field = null;
                            knob.connectionIndex = -1;
                        }

                        foreach (var field in connectivityComp.connectionFields)
                        {
                            foreach (var connection in field.connections)
                            {
                                ModelImporter.MatchConnectionWithKnob(connection, part.knobs);
                                ModelImporter.MatchConnectionWithTubes(connection, part.tubes);
                            }
                        }
                    }
                }
            }           
            return updated;
        }

        private static void FindDependencies(string assetPath, Brick[] bricks, Dictionary<string, DirectedGraph<string>.Node> existingNodes, DirectedGraph<string> dependencyGraph)
        {
            DirectedGraph<string>.Node node;
            if (!existingNodes.ContainsKey(assetPath))
            {
                node = new DirectedGraph<string>.Node() { data = assetPath };
                existingNodes.Add(assetPath, node);
                dependencyGraph.AddNode(node);
            }
            else
            {
                node = existingNodes[assetPath];
            }

            foreach (var brick in bricks)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(brick))
                {
                    var dependencyAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(brick);

                    DirectedGraph<string>.Node dependencyNode;
                    if (!existingNodes.ContainsKey(dependencyAssetPath))
                    {
                        dependencyNode = new DirectedGraph<string>.Node() { data = dependencyAssetPath };
                        existingNodes.Add(dependencyAssetPath, dependencyNode);
                        dependencyGraph.AddNode(dependencyNode);
                    }
                    else
                    {
                        dependencyNode = existingNodes[dependencyAssetPath];
                    }

                    dependencyGraph.AddEdge(dependencyNode, node);
                }
            }
        }

        public static void ConvertVersion_0_To_1(Connectivity connectivity)
        {
            Debug.Log($"Updating Connectivity 0 -> 1 on {connectivity.name}");
            PartUtility.UnpackConnectivityForPart(connectivity.name, true);
        }
    }
}
