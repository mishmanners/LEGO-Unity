// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;
using LEGOMaterials;

namespace LEGOModelImporter
{
    [CustomEditor(typeof(Part)), CanEditMultipleObjects]
    public class PartEditor : Editor
    {
        static readonly float alphaBarHeight = 3.0f;

        SerializedProperty designIDProp;
        SerializedProperty legacyProp;
        SerializedProperty materialIDsProp;

        private void OnEnable()
        {
            designIDProp = serializedObject.FindProperty("designID");
            legacyProp = serializedObject.FindProperty("legacy");
            materialIDsProp = serializedObject.FindProperty("materialIDs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.PropertyField(designIDProp);

            EditorGUI.EndDisabledGroup();

            if (legacyProp.boolValue)
            {
                EditorGUILayout.HelpBox("Selection contains legacy parts. They do not contain collision and connectivity information.", MessageType.Warning);
            }

            if (PrefabUtility.IsPartOfAnyPrefab(target))
            {
                EditorGUILayout.HelpBox("Selection contains a prefab instance. You cannot recolour a prefab instance. Please perform recolouring on the prefab itself.", MessageType.Warning);
                return;
            }

            for (var i = 0; i < materialIDsProp.arraySize; ++i)
            {
                var property = materialIDsProp.GetArrayElementAtIndex(i);
                DrawMouldingColour(property, new GUIContent("Moulding Colour " + i));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMouldingColour(SerializedProperty property, GUIContent label)
        {
            var position = EditorGUILayout.GetControlRect();

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (property.hasMultipleDifferentValues)
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
                var mouldingColourId = (MouldingColour.Id)property.intValue;
                var colour = MouldingColour.GetColour(mouldingColourId);

                // Create box and tooltip.
                GUI.Box(position, new GUIContent("", ObjectNames.NicifyVariableName((int)mouldingColourId + " - " + mouldingColourId.ToString())));

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
                        property.intValue = (int)MouldingColour.GetId(c);

                        // Collect all materials.
                        LXFMLDoc.Brick.Part.Material[] materials = new LXFMLDoc.Brick.Part.Material[materialIDsProp.arraySize];
                        for(var i = 0; i < materialIDsProp.arraySize; ++i)
                        {
                            materials[i] = new LXFMLDoc.Brick.Part.Material() { colorId = materialIDsProp.GetArrayElementAtIndex(i).intValue, shaderId = 0 };
                        }

                        // Run through all targeted parts and record them for undo.
                        foreach (var target in targets)
                        {
                            var partTarget = (Part)target;

                            // Register state before updating materials.
                            Undo.RegisterFullObjectHierarchyUndo(partTarget.gameObject, "Changed Material");
                        }

                        // Run through all targeted parts and update them.
                        foreach (var target in targets)
                        {
                            var partTarget = (Part)target;

                            // Update the part materials.
                            ModelImporter.SetMaterials(partTarget, materials, legacyProp.boolValue);

                            // Apply the new colour to the serialized property.
                            property.serializedObject.ApplyModifiedProperties();

                            // Update knobs and tubes of this brick and the bricks it is connected to directly.
                            var connectedBricks = partTarget.brick.GetConnectedBricks(false);
                            connectedBricks.Add(partTarget.brick);
                            foreach (var brick in connectedBricks)
                            {
                                foreach (var part in brick.parts)
                                {
                                    if (!part.legacy)
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