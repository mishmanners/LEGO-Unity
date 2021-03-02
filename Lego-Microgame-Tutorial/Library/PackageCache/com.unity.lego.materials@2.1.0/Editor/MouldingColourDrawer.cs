// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;

namespace LEGOMaterials
{

    [CustomPropertyDrawer(typeof(MouldingColourAttribute))]
    public class MouldingColourDrawer : PropertyDrawer
    {
        static readonly float alphaBarHeight = 3.0f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var mouldingColourAttribute = (MouldingColourAttribute)attribute;

            var colour = property.colorValue;
            var mouldingColourId = MouldingColour.GetId(colour);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Create box and tooltip.
            GUI.Box(position, new GUIContent("", ObjectNames.NicifyVariableName((int)mouldingColourId + " - " + mouldingColourId.ToString())));

            // Draw rects with colour.
            var colorRect = new Rect(position.x + 1.0f, position.y + 1.0f, position.width - 2.0f, position.height - 2.0f - alphaBarHeight);
            var alphaRect = new Rect(position.x + 1.0f, position.y + 1.0f + colorRect.height, Mathf.Round((position.width - 2.0f) * colour.a), alphaBarHeight);
            var blackRect = new Rect(position.x + 1.0f + alphaRect.width, position.y + 1.0f + colorRect.height, position.width - 2.0f - alphaRect.width, alphaBarHeight);
            EditorGUI.DrawRect(colorRect, new Color(colour.r, colour.g, colour.b));
            EditorGUI.DrawRect(alphaRect, Color.white);
            EditorGUI.DrawRect(blackRect, Color.black);

            // Detect click.
            if (Event.current.type == EventType.MouseDown)
            {
                if (position.Contains(Event.current.mousePosition))
                {
                    MouldingColourPicker.Show((c) =>
                    {
                        property.colorValue = c;
                        property.serializedObject.ApplyModifiedProperties();
                    },
                    mouldingColourAttribute.excludeTransparent,
                    mouldingColourAttribute.excludeBrightYellow,
                    mouldingColourAttribute.excludeLegacy
                    );
                }
            }
        }
    }

}
