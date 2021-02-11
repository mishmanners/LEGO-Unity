// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;

namespace LEGOModelImporter
{
    public class ImportModel : EditorWindow
    {
        static Model model;
        static ModelGroup group;

        static Texture logoTexture;

        static LXFMLDoc lxfml;
        static Model.Pivot pivot;
        static string filePath;
        static Vector2 scrollPosition;

        static readonly int leftMargin = 20;
        static readonly string[] lodOptions = { "LOD 0", "LOD 1", "LOD 2" };

        static DictionaryIntToModelGroupImportSettings importSettings = new DictionaryIntToModelGroupImportSettings();

        static Dictionary<string, List<string>> trackedErrors;

        [MenuItem("LEGO Tools/Import Model &%l", priority = 0)]
        public static void FindModelFile()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select model file", "Packages/com.unity.lego.modelimporter/Models", new string[] {"All model files", "ldr,io,lxfml,lxf", "LDraw files", "ldr", "Studio files", "io", "LXFML files", "lxfml", "LXF files", "lxf" });
            if (path.Length != 0)
            {
                lxfml = ReadFileLogic(path);
                if (lxfml != null)
                {
                    pivot = Model.Pivot.BottomCenter;
                    filePath = path;

                    importSettings.Clear();
                    foreach (var group in lxfml.groups)
                    {
                        importSettings.Add(group.number, new ModelGroupImportSettings());
                    }

                    model = null;
                    group = null;

                    GetWindow<ImportModel>(true, "LEGO Model Importer");
                } else
                {
                    ShowReadError();
                }
            }
        }

        public static void ReimportModel(Model model, string newReimportPath = null)
        {
            var absoluteFilePath = model.absoluteFilePath;
            var relativeFilePath = model.relativeFilePath;
            // Update the file path 
            if (newReimportPath != null) {
                absoluteFilePath = newReimportPath;
                relativeFilePath = PathUtils.GetRelativePath(Directory.GetCurrentDirectory(), newReimportPath);
            }

            lxfml = ReadFileLogic(relativeFilePath);
            if (lxfml == null)
            {
                lxfml = ReadFileLogic(absoluteFilePath);
            }
            if (lxfml != null)
            {
                pivot = model.pivot;

                // Store the new reimport path so it can be applied to the model if reimport is successful.
                filePath = newReimportPath;

                importSettings.Clear();

                // Make sure we are compatible with models from before ModelGroupImportSettings by adding default import settings.
                if (model.importSettings.Keys.Count == 0)
                {
                    foreach (var group in lxfml.groups)
                    {
                        importSettings.Add(group.number, new ModelGroupImportSettings());
                    }
                }
                else
                {
                    foreach (var entry in model.importSettings)
                    {
                        importSettings.Add(entry.Key, entry.Value);
                    }
                }

                foreach(var group in lxfml.groups)
                {
                    if (!importSettings.ContainsKey(group.number))
                    {
                        importSettings.Add(group.number, new ModelGroupImportSettings());
                    }
                }

                // Check if groups match up with the file.
                // FIXME Next version could include option to match groups up manually.
                // FIXME Check on group name? We do not have direct access to the groups from the model.
                var groupsMatch = true;
                foreach(var entry in importSettings)
                {
                    if (entry.Key >= lxfml.groups.Length)
                    {
                        Debug.LogWarning("Group " + entry.Key + " does not match up with file.");
                        groupsMatch = false;
                    }
                }

                if (!groupsMatch)
                {
                    EditorUtility.DisplayDialog("Reimport failed", "Model groups do not match up with groups in file. Check log for details", "Ok");
                    return;
                }

                ImportModel.model = model;
                group = null;

                GetWindow<ImportModel>(true, "LEGO Model Importer");
            }
            else
            {
                ShowReadError();
            }
        }

        public static void ReimportModelGroup(ModelGroup group)
        {
            lxfml = ReadFileLogic(group.relativeFilePath);
            if (lxfml == null)
            {
                lxfml = ReadFileLogic(group.absoluteFilePath);
            }
            if (lxfml != null)
            {
                filePath = null;

                importSettings.Clear();
                importSettings.Add(group.number, group.importSettings);

                // Check if group matches up with the file.
                // FIXME Next version could include option to match groups up manually.
                if (group.number >= lxfml.groups.Length || lxfml.groups[group.number].name != group.groupName)
                {
                    EditorUtility.DisplayDialog("Reimport failed", $"Model group {group.number} {group.groupName} does not match up with group in file", "Ok");
                    return;
                }

                model = null;
                ImportModel.group = group;

                GetWindow<ImportModel>(true, "LEGO Model Importer");
            }
            else
            {
                ShowReadError();
            }
        }

        private static void ShowReadError()
        {
            EditorUtility.DisplayDialog("Failed to read model from file", "If you're reading an IO file, please export it as LDR in Studio.\n\nIf you're reading an LXFML or LXF file, make sure that they are using version 5.6 or newer", "Ok");

        }

        private void OnEnable()
        {
            logoTexture = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unity.lego.modelimporter/Textures/LEGO logo.png");
        }

        private void OnGUI()
        {
            // Find max label width.
            var maxLabelWidth = 110.0f;
            for (int i = 0; i < lxfml.groups.Length; i++)
            {
                var size = EditorStyles.boldLabel.CalcSize(new GUIContent(lxfml.groups[i].name));
                maxLabelWidth = Mathf.Max(maxLabelWidth, size.x);
            }

            minSize = new Vector2(leftMargin + maxLabelWidth + 360, 226);
            maxSize = new Vector2(leftMargin + maxLabelWidth + 360, 2000);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            GUI.Box(new Rect(20, 20, 100, 100), logoTexture);

            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 20.0f, 100.0f), "Colliders", "Add colliders to bricks.");
            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 50.0f, 100.0f), "Connectivity", "Add connectivity to bricks. Connectivity requires colliders.");

            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 100.0f, 100.0f), "Static", "Make bricks static.");
            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 130.0f, 100.0f), "Lightmapped", "Add lightmap UVs to bricks. Bricks must be static to be lightmapped.");

            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 180.0f, 100.0f), "Randomize Rotation", "Slightly rotate bricks to improve realism.");
            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 230.0f, 100.0f), "Prefer Legacy", "Prefer legacy geometry over new geometry.");
            CreateHeaderUI(new Vector2(leftMargin + maxLabelWidth + 280.0f, 100.0f), "LOD", "LOD 0 includes chamfered edges.\nLOD 1 does not.\nLOD 2 simplifies knobs.");

            // Reserve the space for the GUILayout scroll view.
            GUILayout.Space(135.0f);
            var nextY = 135.0f;

            var showAllBoolsUI = model == null && group == null && lxfml.groups.Length > 1; // When importing a new model, just check if there is more than one lxfml group.
            showAllBoolsUI |= model != null && importSettings.Count > 1; // When reimporting an entire model, check if the existing model has more than one group.
            if (showAllBoolsUI)
            {
                CreateAllBoolsUI(new Vector2(leftMargin + maxLabelWidth + 15.0f, nextY), importSettings, "colliders", "connectivity", null);
                CreateAllBoolsUI(new Vector2(leftMargin + maxLabelWidth + 45.0f, nextY), importSettings, "connectivity", null, "colliders");

                CreateAllBoolsUI(new Vector2(leftMargin + maxLabelWidth + 95.0f, nextY), importSettings, "isStatic", "lightmapped", null);
                CreateAllBoolsUI(new Vector2(leftMargin + maxLabelWidth + 125.0f, nextY), importSettings, "lightmapped", null, "isStatic");

                CreateAllBoolsUI(new Vector2(leftMargin + maxLabelWidth + 175.0f, nextY), importSettings, "randomizeRotation");
                CreateAllBoolsUI(new Vector2(leftMargin + maxLabelWidth + 225.0f, nextY), importSettings, "preferLegacy");
                CreateAllLODsUI(new Vector2(leftMargin + maxLabelWidth + 275.0f, nextY), importSettings);

                // Reserve the space for the GUILayout scroll view.
                GUILayout.Space(25.0f);
                nextY += 25.0f;
            }

            var collidersOrConnectivityWhilePreferringLegacy = false;
            for (int i = 0; i < lxfml.groups.Length; i++)
            {
                var showGroup = group == null && model == null; // When importing a new model, show all groups.
                showGroup |= model != null && importSettings.ContainsKey(i); // When reimporting an entire model, only show groups already in the existing model.
                showGroup |= group != null && i == group.number; // When reimporting a model group, only show that group.
                if (showGroup)
                {
                    GUI.Label(new Rect(leftMargin, nextY, maxLabelWidth, 20.0f), lxfml.groups[i].name);

                    CreateBoolUI(new Vector2(leftMargin + maxLabelWidth + 15.0f, nextY), importSettings, "colliders", i, "connectivity", null);
                    CreateBoolUI(new Vector2(leftMargin + maxLabelWidth + 45.0f, nextY), importSettings, "connectivity", i, null, "colliders");

                    CreateBoolUI(new Vector2(leftMargin + maxLabelWidth + 95.0f, nextY), importSettings, "isStatic", i, "lightmapped", null);
                    CreateBoolUI(new Vector2(leftMargin + maxLabelWidth + 125.0f, nextY), importSettings, "lightmapped", i, null, "isStatic");

                    CreateBoolUI(new Vector2(leftMargin + maxLabelWidth + 175.0f, nextY), importSettings, "randomizeRotation", i);
                    CreateBoolUI(new Vector2(leftMargin + maxLabelWidth + 225.0f, nextY), importSettings, "preferLegacy", i);
                    CreateLODUI(new Vector2(leftMargin + maxLabelWidth + 275.0f, nextY), importSettings, i);

                    if ((importSettings[i].colliders || importSettings[i].connectivity) && importSettings[i].preferLegacy)
                    {
                        collidersOrConnectivityWhilePreferringLegacy = true;
                    }

                    // Reserve the space for the GUILayout scroll view.
                    GUILayout.Space(20.0f);
                    nextY += 20.0f;
                }
            }

            if (collidersOrConnectivityWhilePreferringLegacy)
            {
                EditorGUI.HelpBox(new Rect(leftMargin, nextY, position.width - leftMargin - 20.0f, 38.0f), "Legacy parts might not contain colliders or connectivity information.", MessageType.Warning);
                // Reserve the space for the GUILayout scroll view.
                GUILayout.Space(42.0f);
                nextY += 42.0f;
            }

            // Reserve the space for the GUILayout scroll view.
            GUILayout.Space(5.0f);
            nextY += 5.0f;

            // Only show pivot option when not reimporting group.
            if (group == null)
            {
                GUI.Label(new Rect(leftMargin, nextY, maxLabelWidth, 20.0f), "Pivot");
                pivot = (Model.Pivot)EditorGUI.EnumPopup(new Rect(leftMargin + maxLabelWidth + 15.0f, nextY, 126.0f, 16.0f), pivot);

                // Reserve the space for the GUILayout scroll view.
                GUILayout.Space(25.0f);
                nextY += 25.0f;
            }

            // Create the right import/reimport button and handle the import/reimport based on the three cases:
            // - Reimport model
            // - Reimport model group
            // - Import model
            bool importPressed;
            if (model)
            {
                // ----------------------
                // Reimport entire model.
                // ----------------------
                importPressed = GUI.Button(new Rect(leftMargin, nextY, maxLabelWidth + 15.0f + 126.0f, 32.0f), "Reimport Model");

                if (importPressed)
                {
                    // Register undo.
                    Undo.RegisterFullObjectHierarchyUndo(model.gameObject, "Reimport");
                    var oldPivot = model.pivot;
                    model.pivot = pivot;

                    // Update the path if it is new.
                    if (filePath != null)
                    {
                        model.absoluteFilePath = filePath;
                        model.relativeFilePath = PathUtils.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
                    }

                    ModelImporter.ReimportModel(lxfml, model, oldPivot, importSettings);

                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                    {
                        EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                    }
                    SceneBrickBuilder.MarkSceneDirty();
                }
            }
            else if (group)
            {
                // ---------------------
                // Reimport model group.
                // ---------------------
                importPressed = GUI.Button(new Rect(leftMargin, nextY, maxLabelWidth + 15.0f + 126.0f, 32.0f), "Reimport Model Group");

                if (importPressed)
                {
                    // Register undo.
                    Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Reimport");

                    ModelImporter.ReimportModelGroup(lxfml, group, importSettings[group.number], true);

                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                    {
                        EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                    }
                    SceneBrickBuilder.MarkSceneDirty();
                }
            }
            else
            {
                // ----------------------
                // Import model.
                // ----------------------
                importPressed = GUI.Button(new Rect(leftMargin, nextY, maxLabelWidth + 15.0f + 126.0f, 32.0f), "Import Model");

                if (importPressed)
                {
                    // Check for connectivity updates.
                    ConnectivityVersionChecker.CheckForUpdates();

                    model = ModelImporter.InstantiateModel(lxfml, filePath, pivot, importSettings).GetComponent<Model>();
                    var camera = SceneView.lastActiveSceneView.camera;
                    if(camera)
                    {
                        var cameraRay = new Ray(camera.transform.position, camera.transform.forward);

                        var bricksInModel = model.GetComponentsInChildren<Brick>();
                        var bricks = new HashSet<Brick>(bricksInModel);
                        var sourceBrick = bricks.First();

                        BrickBuildingUtility.AlignBricks(sourceBrick, bricks, BrickBuildingUtility.ComputeBounds(bricks), sourceBrick.transform.position, Vector3.zero, cameraRay, 
                        new Plane(Vector3.up, Vector3.zero), 100.0f, out Vector3 offset, out Vector3 alignedOffset, out _, out _);

                        var offsetPosition = model.transform.position + alignedOffset;

                        model.transform.position = offsetPosition;

                        Selection.activeGameObject = model.gameObject;
                        Physics.SyncTransforms();
                    }
                    SceneBrickBuilder.SyncAndUpdateBrickCollision(true);
                }                
            }

            // Reserve the space for the GUILayout scroll view.
            GUILayout.Space(36.0f);
            nextY += 36.0f;

            // List tracked errors.
            foreach (var trackedError in trackedErrors)
            {
                EditorGUI.HelpBox(new Rect(leftMargin, nextY, maxLabelWidth + 340.0f, 38.0f), trackedError.Key, MessageType.Warning);
                // Reserve the space for the GUILayout scroll view.
                GUILayout.Space(42.0f);
                nextY += 42.0f;

                foreach (var id in trackedError.Value)
                {
                    GUI.Label(new Rect(leftMargin, nextY, maxLabelWidth + 340.0f, 16.0f), id);
                    // Reserve the space for the GUILayout scroll view.
                    GUILayout.Space(20.0f);
                    nextY += 20.0f;
                }
            }

            GUILayout.EndScrollView();

            if (importPressed)
            {
                this.Close();
            }
        }

        private static void CreateHeaderUI(Vector2 position, string header, string tooltip)
        {
            GUIUtility.RotateAroundPivot(-45.0f, position + new Vector2(5.0f, 15.0f));
            GUI.Label(new Rect(position, new Vector2(150.0f, 20.0f)), new GUIContent(header, tooltip), EditorStyles.boldLabel);
            GUIUtility.RotateAroundPivot(45.0f, position + new Vector2(5.0f, 15.0f));
        }

        private static void CreateAllBoolsUI(Vector2 position, DictionaryIntToModelGroupImportSettings importSettings, string valueName, string onFalseName = null, string onTrueName = null)
        {
            var type = typeof(ModelGroupImportSettings);
            var valueField = type.GetField(valueName);
            var onFalseField = onFalseName != null ? type.GetField(onFalseName) : null;
            var onTrueField = onTrueName != null ? type.GetField(onTrueName) : null;

            EditorGUI.showMixedValue = importSettings.Values.Any(setting => (bool)valueField.GetValue(setting)) && importSettings.Values.Any(setting => !(bool)valueField.GetValue(setting));
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.Toggle(new Rect(position, new Vector2(16, 16)), (bool)valueField.GetValue(importSettings[0]));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var key in new List<int>(importSettings.Keys))
                {
                    valueField.SetValue(importSettings[key], newValue);
                }
                if (onFalseField != null && !newValue)
                {
                    foreach (var key in new List<int>(importSettings.Keys))
                    {
                        onFalseField.SetValue(importSettings[key], false);
                    }
                }
                if (onTrueField != null && newValue)
                {
                    foreach (var key in new List<int>(importSettings.Keys))
                    {
                        onTrueField.SetValue(importSettings[key], true);
                    }
                }
            }

            EditorGUI.showMixedValue = false;
        }

        private static void CreateBoolUI(Vector2 position, DictionaryIntToModelGroupImportSettings importSettings, string valueName, int index, string onFalseName = null, string onTrueName = null)
        {
            var type = typeof(ModelGroupImportSettings);
            var valueField = type.GetField(valueName);
            var onFalseField = onFalseName != null ? type.GetField(onFalseName) : null;
            var onTrueField = onTrueName != null ? type.GetField(onTrueName) : null;

            var newValue = EditorGUI.Toggle(new Rect(position, new Vector2(16, 16)), (bool)valueField.GetValue(importSettings[index]));
            valueField.SetValue(importSettings[index], newValue);

            if (onFalseField != null && !newValue)
            {
                onFalseField.SetValue(importSettings[index], false);
            }
            if (onTrueField != null && newValue)
            {
                onTrueField.SetValue(importSettings[index], true);
            }
        }

        private static void CreateAllLODsUI(Vector2 position, DictionaryIntToModelGroupImportSettings importSettings)
        {
            var lodSet = new HashSet<int>();
            foreach (var key in new List<int>(importSettings.Keys))
            {
                 lodSet.Add(importSettings[key].lod);
            }

            EditorGUI.showMixedValue = lodSet.Count > 1;
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.Popup(new Rect(position, new Vector2(64, 16)), importSettings[0].lod, lodOptions);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var key in new List<int>(importSettings.Keys))
                {
                    importSettings[key].lod = newValue;
                }
            }

            EditorGUI.showMixedValue = false;
        }

        private static void CreateLODUI(Vector2 position, DictionaryIntToModelGroupImportSettings importSettings, int index)
        {
            importSettings[index].lod = EditorGUI.Popup(new Rect(position, new Vector2(64, 16)), importSettings[index].lod, lodOptions);
        }

        private static LXFMLDoc ReadFileLogic(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();

            // Reset tracked errors.
            trackedErrors = new Dictionary<string, List<string>>();

            XmlDocument doc = null;

            switch (extension)
            {
                case ".lxfml":
                    {
                        doc = new XmlDocument();
                        doc.LoadXml(File.ReadAllText(path));
                        break;
                    }
                case ".lxf":
                    {
                        // Open LXF file.
                        using (var lxfArchive = ZipFile.OpenRead(path))
                        {
                            var entry = lxfArchive.GetEntry("IMAGE100.LXFML");
                            if (entry != null)
                            {
                                doc = new XmlDocument();
                                var lxfmlStream = entry.Open();
                                doc.Load(lxfmlStream);
                                lxfmlStream.Dispose();
                            }
                        }
                        break;
                    }
                case ".ldr":
                    {
                        var ldrStream = new FileStream(path, FileMode.Open);
                        doc = LDrawConverter.ConvertLDrawToLXFML(ldrStream, path);
                        ldrStream.Dispose();
                        trackedErrors = LDrawConverter.GetErrors();
                        break;
                    }
                case ".io":
                    {
                        // Cannot open IO file.

                        break;
                    }
            }

            if (doc != null)
            {
                var lxfml = new LXFMLDoc();

                if (LXFMLReader.ReadLxfml(doc, ref lxfml))
                {
                    if (lxfml.groups == null)
                    {
                        Debug.Log("No groups in " + path + " Creating default group.");
                        CreateDefaultGroup(lxfml);
                    }
                    else if (FixLooseBricks(lxfml))
                    {
                        Debug.Log("Found bricks with no group. Adding them to main model.");
                    }

                    return lxfml;
                }
            }

            return null;
        }

        private static void CreateDefaultGroup(LXFMLDoc lxfml)
        {
            lxfml.groups = new LXFMLDoc.BrickGroup[] { new LXFMLDoc.BrickGroup() };
            var group = lxfml.groups[0];
            group.name = "Default";
            group.number = 0;

            group.brickRefs = new int[lxfml.bricks.Count];
            for (var i = 0; i < lxfml.bricks.Count; ++i)
            {
                group.brickRefs[i] = lxfml.bricks[i].refId;
                group.bricks.Add(lxfml.bricks[i]);
            }
        }

        private static bool FixLooseBricks(LXFMLDoc lxfml)
        {
            var looseBricks = new List<LXFMLDoc.Brick>();

            foreach(var brick in lxfml.bricks)
            {
                if (!FindBrickInGroups(brick, lxfml.groups))
                {
                    looseBricks.Add(brick);
                }
            }

            if (looseBricks.Count > 0)
            {
                var newGroups = new List<LXFMLDoc.BrickGroup>(lxfml.groups);
                newGroups.Insert(0, new LXFMLDoc.BrickGroup());
                lxfml.groups = newGroups.ToArray();

                var group = newGroups[0];
                group.name = "Main model";
                group.number = 0;

                for (var i = 1; i < newGroups.Count; ++i)
                {
                    newGroups[i].number = i;
                }

                group.brickRefs = new int[looseBricks.Count];
                for (var i = 0; i < looseBricks.Count; ++i)
                {
                    group.brickRefs[i] = looseBricks[i].refId;
                    group.bricks.Add(looseBricks[i]);
                }

                return true;
            }

            return false;
        }

        private static bool FindBrickInGroups(LXFMLDoc.Brick brick, LXFMLDoc.BrickGroup[] groups)
        {
            foreach(var group in groups)
            {
                if (Array.IndexOf(group.brickRefs, brick.refId) >= 0)
                {
                    return true;
                }

                if (group.children != null && FindBrickInGroups(brick, group.children))
                {
                    return true;
                }
            }

            return false;
        }
    }

}