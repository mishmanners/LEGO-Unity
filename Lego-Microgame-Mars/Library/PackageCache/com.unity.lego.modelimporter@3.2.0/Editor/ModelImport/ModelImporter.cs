// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;
using System.Text.RegularExpressions;
using LEGOMaterials;
using UnityEditor.SceneManagement;

namespace LEGOModelImporter
{

    public class ModelImporter
    {
        //private static readonly string decorationMaterialsPath = "Assets/LEGOIntegrationHub/Internal/LXFML/Resources/Materials";
        //public static Material decoCutoutMaterial = Resources.Load<Material>("LXFMLMaterials/transpcutoutMinifigure");

        /// <summary>
        /// Translate the bricks in a given LXFML-group to interactable objects
        /// </summary>
        /// <param name="group">The LXFML-group</param>
        /// <param name="index">Index of the group</param>
        /// <param name="parent">The imported asset</param>
        /// <param name="absoluteFilePath">Path of the imported asset</param>
        /// <param name="relativeFilePath">Path of the imported asset</param>
        /// <param name="resultBricks">Dictionary containing the simple bricks</param>
        /// <param name="isSubGroup">Whether it is a subgroup or not</param>
        /// <param name="lightmapped">Whether it is lightmapped or not</param>
        /// <param name="missingGroups">List of groups containing missing elements</param>
        public static GameObject InstantiateModelGroup(LXFMLDoc.BrickGroup group, int index, GameObject parent, string absoluteFilePath, string relativeFilePath, ref Dictionary<int, Brick> resultBricks, bool isSubGroup, ModelGroupImportSettings importSettings)
        {
            ModelGroup groupComp;
            GameObject groupParent;

            if (isSubGroup)
            {
                groupParent = new GameObject("SubGroup " + index + " - " + group.name);
            }
            else
            {
                groupParent = new GameObject(group.name);
            }

            // FIXME Handle subgroups properly.
            //Recursively check subgroups
            if (group.children != null)
            {
                foreach (var subGroup in group.children)
                {
                    foreach (var part in group.brickRefs)
                    {
                        //Apparently supergroups contain elements from subgroups. Duplicates are removed from supergroups.
                        if (subGroup.brickRefs.Contains(part))
                        {
                            group.brickRefs[Array.IndexOf(group.brickRefs, part)] = -1;
                        }
                    }
                    InstantiateModelGroup(subGroup, Array.IndexOf(group.children, subGroup), groupParent, absoluteFilePath, relativeFilePath, ref resultBricks, true, importSettings);
                }
            }

            importSettings.lightmapped &= importSettings.isStatic;

            SetStaticAndGIParams(groupParent, importSettings.isStatic, importSettings.lightmapped);

            groupParent.transform.parent = parent.transform;
            groupParent.transform.SetSiblingIndex(index);
            if (!isSubGroup)
            {
                groupComp = groupParent.AddComponent<ModelGroup>();
                groupComp.absoluteFilePath = absoluteFilePath;
                groupComp.relativeFilePath = relativeFilePath;

                groupComp.importSettings = importSettings;

                groupComp.groupName = group.name;
                groupComp.number = index;
                groupComp.parentName = parent.name;

                groupComp.optimizations = ModelGroup.Optimizations.Everything;

                // Add LEGOModelGroupAsset component.
                groupParent.AddComponent<LEGOModelGroupAsset>();
            }

            var groupBricks = new HashSet<Brick>();
            foreach (int id in group.brickRefs)
            {
                if (id == -1)
                {
                    continue;
                }
                if (resultBricks.ContainsKey(id))
                {
                    groupBricks.Add(resultBricks[id]);
                    resultBricks[id].transform.SetParent(groupParent.transform);
                }
            }

            return groupParent;
        }

        private static void DetectConnectivity(HashSet<Brick> onlyConnectTo, HashSet<Brick> ignoreForCollision = null)
        {
            Physics.SyncTransforms();

            var ignored = onlyConnectTo;

            if(ignoreForCollision != null)
            {
                ignored = new HashSet<Brick>(ignored.Union(ignoreForCollision));
            }

            foreach (var brick in onlyConnectTo)
            {
                var modelGroup = brick.GetComponentInParent<ModelGroup>();
                var fields = brick.GetComponentsInChildren<ConnectionField>();

                foreach(var field in fields)
                {
                    var query = field.QueryConnections(out _, onlyConnectTo);
                    foreach(var (connection, otherConnection) in query)
                    {
                        var position = connection.field.GetPosition(connection);
                        if(ConnectionField.IsConnectionValid(connection, otherConnection, position, ignored))
                        {
                            var connections = ConnectionField.Connect(connection, otherConnection, position, onlyConnectTo, ignored);                            
                            foreach(var c in connections)
                            {
                                var model = c.connectivity.part.brick.GetComponentInParent<Model>();
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static LXFMLDoc.BrickGroup FindGroup(LXFMLDoc lxfml, LXFMLDoc.Brick brick)
        {
            foreach (var group in lxfml.groups)
            {
                if (group.brickRefs.Contains(brick.refId))
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Instantiate game objects for each brick in an LXFML-file
        /// </summary>
        /// <param name="lxfml">The LXFML-file</param>
        /// <param name="colliders">Add colliders to part</param>
        /// <param name="connectivity">Add connectivity to part</param>
        /// <param name="isStatic">Make the part static</param>
        /// <param name="lightmapped">Instantiate meshes with or without lightmap UVs</param>
        /// <param name="randomizeRotation">Slightly rotate rotation of part</param>
        /// <param name="preferLegacy">Choose legacy meshes if available</param>
        /// <param name="lod">Instantiate meshes of a certain LOD</param>
        /// <param name="resultBricks">Dictionary that contains brick component, using refID as key</param>
        /// <param name="groupNumber">If non-negative, only instantiate bricks from the specified group number</param>
        public static void InstantiateModelBricks(LXFMLDoc lxfml, DictionaryIntToModelGroupImportSettings importSettings, ref Dictionary<int, Brick> resultBricks, int groupNumber = -1)
        {
            for (var i = 0; i < lxfml.bricks.Count; ++i)
            {
                if (i % 200 == 0)
                {
                    EditorUtility.DisplayProgressBar("Importing", "Creating bricks.", ((float)i / lxfml.bricks.Count) * 0.7f);
                }

                var brick = lxfml.bricks[i];

                var group = FindGroup(lxfml, brick);

                // Discard bricks from other groups if group number is specified.
                if (groupNumber >= 0 && group != null && group.number != groupNumber)
                {
                    continue;
                }

                // Determine whether or not to be static and to generate light map UVs.
                var brickStatic = (group != null ? importSettings[group.number].isStatic : false);
                var brickLightmapped = brickStatic && (group != null ? importSettings[group.number].lightmapped : false);
                var brickLod =(group != null ? importSettings[group.number].lod : 0);

                var brickGO = new GameObject(brick.designId, typeof(Brick));
                var brickComp = brickGO.GetComponent<Brick>();
                Undo.RegisterCreatedObjectUndo(brickGO, "Brick");

                foreach (var part in brick.parts)
                {
                    GameObject partToInstantiate = null;

                    var partExistenceResult = PartUtility.UnpackPart(part.partDesignId, brickLightmapped, group != null ? importSettings[group.number].preferLegacy : false, brickLod);

                    if (partExistenceResult.existence != PartUtility.PartExistence.None)
                    {
                        // FIXME Make a note of changed design ids.
                        partToInstantiate = PartUtility.LoadPart(partExistenceResult.designID, brickLightmapped, partExistenceResult.existence == PartUtility.PartExistence.Legacy, brickLod);
                    }

                    if (partToInstantiate == null)
                    {
                        Debug.LogError("Missing part FBX -> " + partExistenceResult.designID);
                        continue;
                    }
                    var partGO = Object.Instantiate(partToInstantiate);
                    partGO.name = partToInstantiate.name;

                    // Assign legacy, material IDs and set up references.
                    var partComp = partGO.AddComponent<Part>();
                    partComp.designID = Convert.ToInt32(part.partDesignId);
                    partComp.legacy = partExistenceResult.existence == PartUtility.PartExistence.Legacy;
                    foreach(var material in part.materials)
                    {
                        partComp.materialIDs.Add(material.colorId);
                    }
                    partComp.brick = brickComp;
                    brickComp.parts.Add(partComp);


                    if (partExistenceResult.existence == PartUtility.PartExistence.New)
                    {
                        // FIXME Handle normal mapped model.
                        InstantiateKnobsAndTubes(partComp, brickLightmapped, brickLod);
                    }

                    // Create collider and connectivity information.
                    var brickColliders = (group != null ? importSettings[group.number].colliders : false);
                    var brickConnectivity = brickColliders && (group != null ? importSettings[group.number].connectivity : false);

                    if (brickColliders)
                    {
                        GameObject collidersToInstantiate = null;

                        var collidersAvailable = PartUtility.UnpackCollidersForPart(partExistenceResult.designID);
                        if (collidersAvailable)
                        {
                            collidersToInstantiate = PartUtility.LoadCollidersPrefab(partExistenceResult.designID);
                        }

                        if (collidersToInstantiate == null && partExistenceResult.existence != PartUtility.PartExistence.Legacy)
                        {
                            Debug.LogError("Missing part collider information -> " + partExistenceResult.designID);
                        }

                        if (collidersToInstantiate)
                        {
                            var collidersGO = Object.Instantiate(collidersToInstantiate);
                            collidersGO.name = "Colliders";
                            collidersGO.transform.SetParent(partGO.transform, false);
                            var colliderComps = collidersGO.GetComponentsInChildren<Collider>();
                            partComp.colliders.AddRange(colliderComps);
                        }
                    }

                    if (brickConnectivity)
                    {
                        GameObject connectivityToInstantiate = null;

                        var connectivityAvailable = PartUtility.UnpackConnectivityForPart(partExistenceResult.designID);
                        if (connectivityAvailable)
                        {
                            connectivityToInstantiate = PartUtility.LoadConnectivityPrefab(partExistenceResult.designID);
                        }

                        if (connectivityToInstantiate == null && partExistenceResult.existence != PartUtility.PartExistence.Legacy)
                        {
                            Debug.LogError("Missing part connectivity information -> " + partExistenceResult.designID);
                        }

                        if (connectivityToInstantiate)
                        {
                            var connectivityGO = Object.Instantiate(connectivityToInstantiate);
                            connectivityGO.name = "Connectivity";
                            connectivityGO.transform.SetParent(partGO.transform, false);
                            var connectivityComp = connectivityGO.GetComponent<Connectivity>();
                            partComp.connectivity = connectivityComp;
                            brickComp.totalBounds.Encapsulate(connectivityComp.extents);
                            connectivityComp.part = partComp;

                            foreach (var field in connectivityComp.connectionFields)
                            {
                                foreach (var connection in field.connections)
                                {
                                    MatchConnectionWithKnob(connection, partComp.knobs);
                                    MatchConnectionWithTubes(connection, partComp.tubes);
                                }
                            }
                        }              
                    }

                    SetMaterials(partComp, part.materials, partExistenceResult.existence == PartUtility.PartExistence.Legacy);
                    SetDecorations(partComp, part.decorations, partExistenceResult.existence == PartUtility.PartExistence.Legacy);

                    SetStaticAndGIParams(partGO, brickStatic, brickLightmapped, true);

                    // Set Position & Rotation
                    SetPositionRotation(partGO, part);

                    if (group != null ? importSettings[group.number].randomizeRotation : false)
                    {
                        RandomizeRotation(partComp, brickConnectivity);
                    }

                    // If first part, place brick at same position.
                    if (brickGO.transform.childCount == 0)
                    {
                        brickGO.transform.position = partGO.transform.position;
                        brickGO.transform.rotation = partGO.transform.rotation;
                        brickGO.transform.localScale = Vector3.one;

                    }
                    partGO.transform.SetParent(brickGO.transform, true);

                    if(!brickConnectivity)
                    {
                        var worldBounds = ComputeBounds(partGO.transform);
                        worldBounds.SetMinMax(brickComp.transform.InverseTransformPoint(worldBounds.min), brickComp.transform.InverseTransformPoint(worldBounds.max));
                        brickComp.totalBounds.Encapsulate(worldBounds);
                    }
                }

                // If all parts were missing, discard brick.
                if (brickGO.transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(brickGO);
                    continue;
                }

                SetStaticAndGIParams(brickGO, brickStatic, brickLightmapped);

                // Assign uuid
                brickComp.designID = Convert.ToInt32(brick.designId);
                brickComp.uuid = brick.uuid;

                // Add LEGOBrickAsset component.
                brickGO.AddComponent<LEGOBrickAsset>();

                resultBricks[brick.refId] = brickComp;
            }
        }

        private static void SetPositionRotation(GameObject partGO, LXFMLDoc.Brick.Part part)
        {
            foreach (var bone in part.bones)
            {

                partGO.transform.localPosition = bone.position;
                partGO.transform.localRotation = bone.rotation;
                break; // no support for flex
            }
        }

        private static void RandomizeRotation(Part part, bool moveCollidersAndConnectivity)
        {
            var partRenderers = part.GetComponentsInChildren<MeshRenderer>(true);
            if (partRenderers.Length == 0)
            {
                return;
            }

            // Get the part bounds.
            var partBounds = partRenderers[0].bounds;
            foreach (var partRenderer in partRenderers)
            {
                partBounds.Encapsulate(partRenderer.bounds);
            }

            // Randomly rotate part. Scale the rotation down by the square of the part's size.
            Vector3 size = partBounds.size;
            Vector3 noise = (UnityEngine.Random.insideUnitSphere * 1.5f) / Mathf.Max(1.0f, size.sqrMagnitude);
            var partRotation = Quaternion.Euler(noise);

            part.transform.localRotation *= partRotation;

            if (!moveCollidersAndConnectivity)
            {
                // Rotate colliders and connectivity by inverse rotation to make them stay in place.
                var colliders = part.transform.Find("Colliders");
                if (colliders)
                {
                    colliders.localRotation *= Quaternion.Inverse(partRotation);
                }
                var connectivity = part.transform.Find("Connectivity");
                if (connectivity)
                {
                    connectivity.localRotation *= Quaternion.Inverse(partRotation);
                }
            }
        }

        public static void MatchConnectionWithKnob(Connection connection, List<Knob> knobs)
        {
            var POS_EPSILON = 0.01f;
            var ROT_EPSILON = 0.01f;
            var position = connection.field.GetPosition(connection);
            foreach (var knob in knobs)
            {
                if (Vector3.Distance(position, knob.transform.position) < POS_EPSILON && 1.0f - Vector3.Dot(connection.field.transform.up, knob.transform.up) < ROT_EPSILON)
                {
                    connection.knob = knob;
                    knob.connectionIndex = connection.index;
                    knob.field = connection.field;
                    return;
                }
            }
        }

        public static void MatchConnectionWithTubes(Connection connection, List<Tube> tubes)
        {
            // FIXME Temporary fix to tube removal while we work on connections that are related/non-rejecting but not connected.
            if (connection.IsRelevantForTube())
            {
                var position = connection.field.GetPosition(connection);
                var DIST_EPSILON = 0.01f * 0.01f;
                var ROT_EPSILON = 0.01f;
                foreach (var tube in tubes)
                {
                    var meshFilter = tube.GetComponent<MeshFilter>();
                    if(!meshFilter || !meshFilter.sharedMesh)
                    {
                        continue;
                    }

                    var bounds = meshFilter.sharedMesh.bounds;
                    var extents = bounds.extents;
                    extents.x += 0.4f;
                    extents.z += 0.4f;
                    bounds.extents = extents;
                    var localConnectionPosition = tube.transform.InverseTransformPoint(position);

                    if (bounds.SqrDistance(localConnectionPosition) < DIST_EPSILON && 1.0f - Vector3.Dot(connection.field.transform.up, tube.transform.up) < ROT_EPSILON)
                    {
                        connection.tubes.Add(tube);
                        tube.connections.Add(connection.index);
                        tube.field = connection.field;
                    }

                    if (connection.tubes.Count == 4)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Instantiate all bricks and groups in an LXFML document
        /// </summary>
        /// <param name="lxfml">The LXFML document</param>
        /// <param name="nameOfObject">Path of the LXFML document</param>
        public static GameObject InstantiateModel(LXFMLDoc lxfml, string filePath, Model.Pivot pivot, DictionaryIntToModelGroupImportSettings importSettings)
        {
            //Create "root" LXFML gameobject
            GameObject parent = new GameObject(Path.GetFileNameWithoutExtension(filePath));
            Undo.RegisterCreatedObjectUndo(parent, "Model");
            parent.transform.position = Vector3.zero;

            var model = parent.AddComponent<Model>();
            model.absoluteFilePath = filePath;
            model.relativeFilePath = PathUtils.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            model.pivot = pivot;
            model.importSettings = new DictionaryIntToModelGroupImportSettings(importSettings);

            EditorUtility.DisplayProgressBar("Importing", "Creating bricks.", 0.0f);

            var resultBricks = new Dictionary<int, Brick>(lxfml.bricks.Count);
            InstantiateModelBricks(lxfml, importSettings, ref resultBricks);

            EditorUtility.DisplayProgressBar("Importing", "Creating groups.", 0.8f);

            if (resultBricks.Count > 0)
            {
                var bricksWithConnectivity = new HashSet<Brick>();
                var groups = lxfml.groups;

                for (int i = 0; i < groups.Length; i++)
                {
                    var number = groups[i].number;
                    GameObject groupParent = InstantiateModelGroup(groups[i], i, parent, filePath, model.relativeFilePath, ref resultBricks, false, importSettings[number]);
                    if(importSettings[number].connectivity)
                    {
                        bricksWithConnectivity.UnionWith(groupParent.GetComponentsInChildren<Brick>());
                    }
                }

                if (bricksWithConnectivity.Count > 0)
                {
                    var sceneBricks = new HashSet<Brick>(StageUtility.GetCurrentStageHandle().FindComponentsOfType<Brick>());
                    DetectConnectivity(bricksWithConnectivity, sceneBricks);
                }


                if(SceneBrickBuilder.GetAutoUpdateHierarchy())
                {
                    var bricks = new HashSet<Brick>();
                    foreach(var pair in resultBricks)
                    {
                        foreach(var part in pair.Value.parts)
                        {
                            if(part.connectivity)
                            {
                                bricks.Add(pair.Value);
                                break;
                            }
                        }
                    }

                    // On import, the model will be positioned weirdly compared to the rest of the scene, so we just ignore all other bricks
                    ModelGroupUtility.RecomputeHierarchy(bricks, false, ModelGroupUtility.UndoBehavior.withoutUndo);
                }
            }

            // Change the pivot.
            if (pivot != Model.Pivot.Original)
            {
                EditorUtility.DisplayProgressBar("Importing", "Computing bounds.", 0.9f);

                var bounds = ComputeBounds(parent.transform);
                var newPivot = bounds.center;
                switch (pivot)
                {
                    case Model.Pivot.BottomCenter:
                        {
                            newPivot += -parent.transform.up * bounds.extents.y;
                            break;
                        }
                }

                var difference = parent.transform.position - newPivot;

                foreach (Transform child in parent.transform)
                {
                    child.position -= difference;
                }
                parent.transform.position = newPivot;
            }

            // Add LEGOModelAsset component.
            parent.AddComponent<LEGOModelAsset>();

            EditorUtility.ClearProgressBar();

            return parent;
        }

        public static void ReimportModel(LXFMLDoc lxfml, Model model, Model.Pivot previousPivot, DictionaryIntToModelGroupImportSettings importSettings)
        {
            var brickBuilding = SceneBrickBuilder.GetToggleBrickBuildingStatus();        
            if(brickBuilding)
            {
                SceneBrickBuilder.ToggleBrickBuilding();
            }

            // FIXME Next version could include option to match groups up manually.

            var oldPosition = model.transform.position;
            var oldRotation = model.transform.rotation;
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.identity;

            var groups = model.GetComponentsInChildren<ModelGroup>();            

            for (var i = groups.Length - 1; i >= 0; i--)
            {
                var group = groups[i];
                if(group.autoGenerated)
                {
                    Undo.DestroyObjectImmediate(group.gameObject);
                }
                else if (group.number >= lxfml.groups.Length)
                {
                    Debug.LogWarning("Group " + group.number + " " + group.groupName + " does not match up with files. Wiping.");
                    Undo.DestroyObjectImmediate(group.gameObject);
                }
            }

            groups = model.GetComponentsInChildren<ModelGroup>();

            var removedGroups = new List<LXFMLDoc.BrickGroup>();

            for(var i = 0; i < lxfml.groups.Length; i++)
            {
                LXFMLDoc.BrickGroup group = lxfml.groups[i];
                bool exists = false;
                for(var j = 0; j < groups.Length; j++)
                {
                    if(groups[j].number == group.number)
                    {
                        exists = true;
                        break;
                    }
                }

                if(!exists)
                {
                    removedGroups.Add(group);
                }
            }

            // Assign the new model import settings to the model.
            model.importSettings = new DictionaryIntToModelGroupImportSettings(importSettings);
            var bricksWithConnectivity = new HashSet<Brick>();

            if(removedGroups.Count > 0)
            {
                var resultBricks = new Dictionary<int, Brick>(lxfml.bricks.Count);
                foreach(var group in removedGroups)
                {
                    var number = group.number;
                    InstantiateModelBricks(lxfml, model.importSettings, ref resultBricks, number);
                    var groupGO = InstantiateModelGroup(group, number, model.gameObject, model.absoluteFilePath, model.relativeFilePath, ref resultBricks, false, importSettings[number]);
                    groupGO.transform.position = model.transform.position;
                    Undo.RegisterCreatedObjectUndo(groupGO, "Re-creating model group");
                    if(importSettings[number].connectivity)
                    {
                        bricksWithConnectivity.UnionWith(groupGO.GetComponentsInChildren<Brick>());
                    }
                }
            }

            foreach (var group in groups)
            {                
                group.absoluteFilePath = model.absoluteFilePath;
                group.relativeFilePath = model.relativeFilePath;
                ReimportModelGroup(lxfml, group, importSettings[group.number]);
                if(group.importSettings.connectivity)
                {
                    bricksWithConnectivity.UnionWith(group.GetComponentsInChildren<Brick>());
                }
            }

            if(bricksWithConnectivity.Count > 0)
            {
                var sceneBricks = new HashSet<Brick>(StageUtility.GetCurrentStageHandle().FindComponentsOfType<Brick>());
                DetectConnectivity(bricksWithConnectivity, sceneBricks);
            }                        

            if(SceneBrickBuilder.GetAutoUpdateHierarchy())
            {
                groups = model.GetComponentsInChildren<ModelGroup>();
                var bricks = new HashSet<Brick>();
                foreach(var group in groups)
                {
                    if(group.importSettings.connectivity)
                    {
                        var groupBricks = group.GetComponentsInChildren<Brick>();
                        foreach(var brick in groupBricks)
                        {
                            bricks.Add(brick);
                        }
                    }
                }
                ModelGroupUtility.RecomputeHierarchy(bricks, false, ModelGroupUtility.UndoBehavior.withUndo);

                var oldToNew = model.transform.position;
                model.transform.rotation = oldRotation;
                model.transform.position = oldPosition;
                oldToNew = model.transform.TransformVector(oldToNew);
                
                if((previousPivot == Model.Pivot.Center || previousPivot == Model.Pivot.BottomCenter) && model.pivot == Model.Pivot.Original)
                {
                    ModelGroupUtility.RecomputePivot(model.transform, previousPivot, true, ModelGroupUtility.UndoBehavior.withoutUndo);                
                    var oldPivotPos = model.transform.position;
                    model.transform.position = oldPosition;
                    var diff = model.transform.position - oldPivotPos;
                    model.transform.position += diff;
                    foreach(var brick in model.GetComponentsInChildren<Brick>())
                    {
                        brick.transform.position -= diff;
                    }
                    model.transform.rotation = oldRotation;
                }

                if(model.pivot != Model.Pivot.Original && previousPivot == Model.Pivot.Original)
                {
                    model.transform.position += oldToNew;
                    model.transform.rotation = oldRotation;
                }

                if(model.pivot != Model.Pivot.Original && previousPivot != Model.Pivot.Original)
                {
                    if(model.pivot != previousPivot)
                    {
                        ModelGroupUtility.RecomputePivot(model.transform, previousPivot, true, ModelGroupUtility.UndoBehavior.withoutUndo);
                        model.transform.position = oldPosition;
                        ModelGroupUtility.RecomputePivot(model, false, ModelGroupUtility.UndoBehavior.withoutUndo);
                        model.transform.rotation = oldRotation;
                    }
                }            
            }            

            if(brickBuilding)
            {
                SceneBrickBuilder.ToggleBrickBuilding();
            }            

            EditorUtility.ClearProgressBar();
        }

        public static void ReimportModelGroup(LXFMLDoc lxfml, ModelGroup group, ModelGroupImportSettings importSettings, bool detectConnectivity = false)
        {
            // Assign the new group import settings to the group.
            group.importSettings = importSettings;

            // We assume that the group can be found, so reimport it.
            if (group.processed)
            {
                // Remove all processed meshes.
                var renderers = group.GetComponentsInChildren<MeshRenderer>();
                foreach(var renderer in renderers)
                {
                    // FIXME Destroy the mesh? Prevents undo..
                    var filter = renderer.GetComponent<MeshFilter>();
                    //Undo.DestroyObjectImmediate(filter.sharedMesh);

                    if (renderer.GetComponent<ModelGroup>() == null)
                    {
                        // Destroy submesh game objects entirely.
                        Undo.DestroyObjectImmediate(renderer.gameObject);
                    } else
                    {
                        // Destroy mesh related components on group game object.
                        Object.DestroyImmediate(filter);
                        Object.DestroyImmediate(renderer);
                    }
                }
            }

            // FIXME Check if bricks are referenced.
            // FIXME Check if bricks have custom components attached.

            // Remove group bricks.            
            var existingBricks = group.GetComponentsInChildren<Brick>();
            foreach (var brick in existingBricks)
            {                
                Undo.DestroyObjectImmediate(brick.gameObject);
            }

            var groupLightMapped = group.importSettings.isStatic && group.importSettings.lightmapped;

            SetStaticAndGIParams(group.gameObject, group.importSettings.isStatic, groupLightMapped);

            // Move group to origo to ensure that bricks are instantiated in the correct positions.
            var originalGroupParent = group.transform.parent;
            var originalGroupSiblingIndex = group.transform.GetSiblingIndex();
            group.transform.parent = null;
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            // Create dictionary with just this group.
            var modelGroupImportSettingsDictionary = new DictionaryIntToModelGroupImportSettings();
            modelGroupImportSettingsDictionary.Add(group.number, group.importSettings);

            // Instantiate group bricks.
            var resultBricks = new Dictionary<int, Brick>(lxfml.bricks.Count);
            InstantiateModelBricks(lxfml, modelGroupImportSettingsDictionary, ref resultBricks, group.number);
            
            // Assign bricks to group.
            foreach (var brick in resultBricks.Values)
            {
                brick.transform.SetParent(group.transform);
            }

            // Set parent of group back to original.
            group.transform.parent = originalGroupParent;
            group.transform.SetSiblingIndex(originalGroupSiblingIndex);

            if(detectConnectivity && group.importSettings.connectivity)
            {
                var sceneBricks = new HashSet<Brick>(StageUtility.GetCurrentStageHandle().FindComponentsOfType<Brick>());
                DetectConnectivity(new HashSet<Brick>(resultBricks.Values), sceneBricks);
            }

            EditorUtility.ClearProgressBar();
        }

        private static Bounds ComputeBounds(Transform root)
        {
            var meshRenderers = root.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length > 0)
            {
                var bounds = meshRenderers[0].bounds;
                foreach (var renderer in meshRenderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }                
                return bounds;
            }
            return new Bounds(root.position, Vector3.zero);
        }
        
        /// <summary>
        /// Applying materials to imported objects.
        /// Ignores shader id of material.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="materials"></param>
        /// <param name="isLegacy"></param>
        public static void SetMaterials(Part part, LXFMLDoc.Brick.Part.Material[] materials, bool isLegacy)
        {
            if (materials.Length > 0)
            {
                if (isLegacy)
                {
                    var mr = part.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = GetMaterial(materials[0].colorId);
                }
                else
                {
                    if (part.transform.childCount > 0)
                    {
                        var colourChangeSurfaces = part.transform.Find("ColourChangeSurfaces");

                        // Assign materials to shell, knobs, tubes and colour change surfaces
                        for (var i = 0; i < materials.Length; ++i)
                        {
                            if (i == 0)
                            {
                                // Shell.
                                var shell = part.transform.Find("Shell");
                                if (shell)
                                {
                                    var mr = shell.GetComponent<MeshRenderer>();
                                    mr.sharedMaterial = GetMaterial(materials[i].colorId);
                                }
                                else
                                {
                                    Debug.LogError("Missing shell submesh on item " + part.name);
                                }

                                // Knobs.
                                foreach (var knob in part.knobs)
                                {
                                    var mr = knob.GetComponent<MeshRenderer>();
                                    mr.sharedMaterial = GetMaterial(materials[i].colorId);
                                }

                                // Tubes.
                                foreach (var tube in part.tubes)
                                {
                                    var mr = tube.GetComponent<MeshRenderer>();
                                    mr.sharedMaterial = GetMaterial(materials[i].colorId);
                                }
                            }
                            else
                            {
                                // Colour change surfaces.
                                if (colourChangeSurfaces)
                                {
                                    var surface = colourChangeSurfaces.GetChild(i - 1);
                                    if (surface)
                                    {
                                        var mr = surface.GetComponent<MeshRenderer>();
                                        mr.sharedMaterial = GetMaterial(materials[i].colorId);
                                    }
                                    else
                                    {
                                        Debug.LogError("Missing colour change surface " + (i - 1) + " on item " + part.name);
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Missing colour change surface group on multi material item " + part.name);
                                }
                            }
                        }

                        // Check if all colour change surfaces have been assigned a material.
                        if (colourChangeSurfaces)
                        {
                            if (materials.Length - 1 < colourChangeSurfaces.childCount)
                            {
                                Debug.LogError("Missing material for colour change surface(s) on item " + part.name);

                                for (var i = materials.Length - 1; i < colourChangeSurfaces.childCount; ++i)
                                {
                                    var surface = colourChangeSurfaces.GetChild(i);
                                    if (surface)
                                    {
                                        var mr = surface.GetComponent<MeshRenderer>();
                                        mr.sharedMaterial = GetMaterial(materials[materials.Length - 1].colorId);
                                    }
                                    else
                                    {
                                        Debug.LogError("Missing colour change surface " + i + " on item " + part.name);
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }

        private static Material GetMaterial(int colourId)
        {
            var materialExistence = MaterialUtility.CheckIfMaterialExists(colourId);

            if (materialExistence == MaterialUtility.MaterialExistence.Legacy)
            {
                Debug.LogWarning("Legacy material " + colourId);
            } else if(materialExistence == MaterialUtility.MaterialExistence.None)
            {
                Debug.LogError("Missing material " + colourId);
            }

            if (materialExistence != MaterialUtility.MaterialExistence.None)
            {
                return MaterialUtility.LoadMaterial(colourId, materialExistence == MaterialUtility.MaterialExistence.Legacy);
            }

            return null;
        }

        private static void SetStaticAndGIParams(GameObject go, bool isStatic, bool lightmapped, bool recursive = false)
        {
            if (isStatic)
            {
                go.isStatic = true;

                var mr = go.GetComponent<MeshRenderer>();
                if (mr)
                {
                    if (lightmapped)
                    {
                        mr.receiveGI = ReceiveGI.Lightmaps;
                    }
                    else
                    {
                        mr.receiveGI = ReceiveGI.LightProbes;
                    }
                }

                if (recursive)
                {
                    foreach (Transform child in go.transform)
                    {
                        SetStaticAndGIParams(child.gameObject, isStatic, lightmapped, recursive);
                    }
                }
            }
        }

        private static void InstantiateKnobsAndTubes(Part part, bool lightmapped, int lod)
        {
            var knobs = part.transform.Find("Knobs_loc");
            if (knobs)
            {
                InstantiateCommonParts<Knob>(part, part.knobs, knobs, lightmapped, lod);
                knobs.name = "Knobs";
            }

            var tubes = part.transform.Find("Tubes_loc");
            if (tubes)
            {
                InstantiateCommonParts<Tube>(part, part.tubes, tubes, lightmapped, lod);
                tubes.name = "Tubes";
            }
        }

        private static void InstantiateCommonParts<T>(Part part, List<T> partsList, Transform parent, bool lightmapped, int lod) where T : CommonPart
        {
            int count = parent.childCount;
            // Instantiate common parts using locators.
            for (int i = 0; i < count; i++)
            {
                var commonPartLocation = parent.GetChild(i);
                var name = Regex.Split(commonPartLocation.name, "(_[0-9]+ 1)");

                GameObject commonPartToInstantiate = null;

                var commonPartAvailable = PartUtility.UnpackCommonPart(name[0], lightmapped, lod);
                if (commonPartAvailable)
                {
                    commonPartToInstantiate = PartUtility.LoadCommonPart(name[0], lightmapped, lod);
                }

                if (commonPartToInstantiate == null)
                {
                    Debug.LogError("Missing Common Part -> " + name[0]);
                    continue;
                }

                var commonPartGO = Object.Instantiate(commonPartToInstantiate);
                commonPartGO.name = commonPartToInstantiate.name;

                var commonPartComponent = commonPartGO.AddComponent<T>();
                commonPartComponent.part = part;

                // Set position and rotation.
                commonPartGO.transform.position = commonPartLocation.position;
                commonPartGO.transform.rotation = commonPartLocation.rotation;
                
                commonPartGO.transform.SetParent(parent, true);

                partsList.Add(commonPartComponent);
            }
            // Remove locators.
            for (int i = 0; i < count; i++)
            {
                Object.DestroyImmediate(parent.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// For setting decorations on imported objects. Not modified.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="decorations"></param>
        /// <param name="isLegacy"></param>
        public static void SetDecorations(Part part, LXFMLDoc.Brick.Part.Decoration[] decorations, bool isLegacy)
        {
            if (isLegacy)
            {
            }
            else
            {
                // Disable decoration surfaces.
                var decorationSurfaces = part.transform.Find("DecorationSurfaces");
                if (decorationSurfaces)
                {
                    decorationSurfaces.gameObject.SetActive(false);
                }
            }
            /*
            for (var i = 0; i < obj.transform.childCount; ++i)
            {
                var t = obj.transform.GetChild(i);

                if (t.gameObject.name.StartsWith("Decoration_"))
                {
                    if (decorations != null && i < decorations.Length && decorations[i] != 0)
                    {
                        if (!mats.ContainsKey(decorations[i]))
                        {
                            var t2d = Util.LoadObjectFromResources<Texture2D>("Decorations/" + decorations[i]);
                            if (t2d != null)
                            {
                                // Generate new material for our prefabs
                                t2d.wrapMode = TextureWrapMode.Clamp;
                                t2d.anisoLevel = 4;
                                var newDecoMat = new Material(decoCutoutMaterial);
                                newDecoMat.SetTexture("_MainTex", t2d);
                                AssetDatabase.CreateAsset(newDecoMat,
                                    decorationMaterialsPath + "/" + decorations[i] + ".mat");
                                mats.Add(decorations[i], newDecoMat);
                                t.gameObject.GetComponent<Renderer>().sharedMaterial = mats[decorations[i]];
                            }
                            else
                            {
                                Debug.Log("Missing decoration -> " + decorations[i]);
                            }
                        }
                        else
                        {
                            t.gameObject.GetComponent<Renderer>().sharedMaterial = mats[decorations[i]];
                        }
                    }
                    else
                    {
                        Object.DestroyImmediate(t.gameObject);
                    }
                }
            }
            */
        }
    }
}