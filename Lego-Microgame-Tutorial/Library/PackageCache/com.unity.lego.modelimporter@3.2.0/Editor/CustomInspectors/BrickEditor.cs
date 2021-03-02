// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using LEGOMaterials;

namespace LEGOModelImporter
{
    [CustomEditor(typeof(Brick)), CanEditMultipleObjects]
    public class BrickEditor : Editor
    {
        static readonly float alphaBarHeight = 3.0f;

        SerializedProperty designIDProp;

        Dictionary<Brick, List<int>> brickToMaterialIDs = new Dictionary<Brick, List<int>>();

        private void OnEnable()
        {
            designIDProp = serializedObject.FindProperty("designID");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.PropertyField(designIDProp);

            EditorGUI.EndDisabledGroup();

            // Collect material IDs for each selected brick.
            brickToMaterialIDs.Clear();
            var anyLegacy = false;
            var anyPrefab = false;
            foreach(var target in targets)
            {
                var brickTarget = (Brick)target;

                anyLegacy |= brickTarget.IsLegacy();

                brickToMaterialIDs.Add(brickTarget, new List<int>());

                foreach(var part in brickTarget.parts)
                {
                    anyPrefab |= PrefabUtility.IsPartOfAnyPrefab(part);

                    brickToMaterialIDs[brickTarget].AddRange(part.materialIDs);
                }
            }

            if (anyLegacy)
            {
                EditorGUILayout.HelpBox("Selection contains legacy parts. They do not contain collision and connectivity information.", MessageType.Warning);
            }

            if (anyPrefab)
            {
                EditorGUILayout.HelpBox("Selection contains a prefab instance. You cannot recolour a prefab instance. Please perform recolouring on the prefab itself.", MessageType.Warning);
                return;
            }

            var sampleList = brickToMaterialIDs[(Brick)target];
            // Check that all lists have same length.
            var materialIDListsDifferentLength = false;
            foreach (var entry in brickToMaterialIDs)
            {
                if (sampleList.Count != entry.Value.Count)
                {
                    materialIDListsDifferentLength = true;
                    break;
                }
            }

            if (materialIDListsDifferentLength)
            {
                EditorGUILayout.HelpBox("The selected bricks do not have the same amount of materials.", MessageType.Info);
            }
            else
            {
                // Check that all lists are the same.
                var materialIDDifferentContent = new bool[sampleList.Count];
                for(var i = 0; i < sampleList.Count; ++i)
                {
                    foreach (var entry in brickToMaterialIDs)
                    {
                        if (sampleList[i] != entry.Value[i])
                        {
                            materialIDDifferentContent[i] = true;
                            break;
                        }
                        materialIDDifferentContent[i] = false;
                    }
                }

                for (var i = 0; i < sampleList.Count; ++i)
                {
                    DrawMouldingColour(sampleList[i], i, materialIDDifferentContent[i]);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMouldingColour(int colourID, int listIndex, bool multipleValues)
        {
            var position = EditorGUILayout.GetControlRect();

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Moulding Colour " + listIndex));

            if (multipleValues)
            {
                // Create box and tooltip.
                GUI.Box(position, new GUIContent("", "Multiple colours selected"));
                var colorRect = new Rect(position.x + 1.0f, position.y + 1.0f, position.width - 2.0f, position.height - 2.0f);
                var lineRect = new Rect(position.x + 1.0f, position.y + 1.0f + colorRect.height / 2.0f, 10.0f, 1.0f);
                EditorGUI.DrawRect(colorRect, new Color32(209, 209, 209, 255));
                EditorGUI.DrawRect(lineRect, Color.grey);
            }
            else
            {
                var mouldingColourID = (MouldingColour.Id)colourID;
                var colour = MouldingColour.GetColour(mouldingColourID);

                // Create box and tooltip.
                GUI.Box(position, new GUIContent("", ObjectNames.NicifyVariableName((int)mouldingColourID + " - " + mouldingColourID.ToString())));

                // Draw rects with colour.
                var colorRect = new Rect(position.x + 1.0f, position.y + 1.0f, position.width - 2.0f, position.height - 2.0f - alphaBarHeight);
                var alphaRect = new Rect(position.x + 1.0f, position.y + 1.0f + colorRect.height, Mathf.Round((position.width - 2.0f) * colour.a), alphaBarHeight);
                var blackRect = new Rect(position.x + 1.0f + alphaRect.width, position.y + 1.0f + colorRect.height, position.width - 2.0f - alphaRect.width, alphaBarHeight);
                EditorGUI.DrawRect(colorRect, new Color(colour.r, colour.g, colour.b));
                EditorGUI.DrawRect(alphaRect, Color.white);
                EditorGUI.DrawRect(blackRect, Color.black);
            }

            // Detect click.
            if (Event.current.type == EventType.MouseDown)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    MouldingColourPicker.Show((c) =>
                    {
                        // Collect all parts to record for undo.
                        foreach (var target in targets)
                        {
                            var brickTarget = (Brick)target;
                            Undo.RegisterFullObjectHierarchyUndo(brickTarget.gameObject, "Changed Material");

                            int indexOffset = 0;
                            foreach (var partTarget in brickTarget.parts)
                            {
                                if (indexOffset <= listIndex && listIndex < indexOffset + partTarget.materialIDs.Count)
                                {
                                    Undo.RegisterFullObjectHierarchyUndo(partTarget.gameObject, "Changed Material");

                                    break;
                                }
                                indexOffset += partTarget.materialIDs.Count;
                            }
                        }

                        // Collect all parts to change the material.
                        foreach (var target in targets)
                        {
                            var brickTarget = (Brick)target;
                            int indexOffset = 0;
                            foreach (var partTarget in brickTarget.parts)
                            {
                                if (indexOffset <= listIndex && listIndex < indexOffset + partTarget.materialIDs.Count)
                                {
                                    // Update material ID.
                                    partTarget.materialIDs[listIndex - indexOffset] = (int)MouldingColour.GetId(c);

                                    // Collect all materials.
                                    LXFMLDoc.Brick.Part.Material[] materials = new LXFMLDoc.Brick.Part.Material[partTarget.materialIDs.Count];
                                    for (var i = 0; i < partTarget.materialIDs.Count; ++i)
                                    {
                                        materials[i] = new LXFMLDoc.Brick.Part.Material() { colorId = partTarget.materialIDs[i], shaderId = 0 };
                                    }

                                    // Update the part materials.
                                    ModelImporter.SetMaterials(partTarget, materials, partTarget.legacy);

                                    // Update knobs and tubes of this brick and the bricks it is connected to directly.
                                    var connectedBricks = partTarget.brick.GetConnectedBricks(false);
                                    connectedBricks.Add(partTarget.brick);
                                    foreach (var brick in connectedBricks)
                                    {
                                        foreach (var part in brick.parts)
                                        {
                                            if (!part.legacy && part.connectivity)
                                            {
                                                foreach (var connectionField in part.connectivity.connectionFields)
                                                {
                                                    foreach (var connection in connectionField.connections)
                                                    {
                                                        connection.UpdateKnobsAndTubes();
                                                    }
                                                }

                                            }
                                        }
                                    }
                                    break;
                                }

                                indexOffset += partTarget.materialIDs.Count;
                            }
                        }

                        // Collect all parts to record prefab instance modifications.
                        foreach (var target in targets)
                        {
                            var brickTarget = (Brick)target;
                            if (PrefabUtility.IsPartOfAnyPrefab(brickTarget.gameObject))
                            {
                                PrefabUtility.RecordPrefabInstancePropertyModifications(brickTarget.gameObject);
                            }

                            int indexOffset = 0;
                            foreach (var partTarget in brickTarget.parts)
                            {
                                if (indexOffset <= listIndex && listIndex < indexOffset + partTarget.materialIDs.Count)
                                {
                                    if (PrefabUtility.IsPartOfAnyPrefab(partTarget.gameObject))
                                    {
                                        PrefabUtility.RecordPrefabInstancePropertyModifications(partTarget.gameObject);
                                    }
                                    break;
                                }
                                indexOffset += partTarget.materialIDs.Count;
                            }
                        }
                    },
                    false,
                    false,
                    true
                    );
                }
            }
        }
    }

}