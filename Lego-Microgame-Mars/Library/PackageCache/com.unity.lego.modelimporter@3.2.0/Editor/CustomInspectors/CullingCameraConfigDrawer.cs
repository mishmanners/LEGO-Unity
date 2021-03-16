// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{

    [CustomPropertyDrawer(typeof(CullingCameraConfig))]
    public class CullingCameraConfigDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0.0f;

            // Foldout item.
            height += EditorGUIUtility.singleLineHeight;

            if (property.FindPropertyRelative("foldout").boolValue)
            {
                height += MeasurePropertyHeight(property.FindPropertyRelative("name"));
                height += MeasurePropertyHeight(property.FindPropertyRelative("position"));
                height += MeasurePropertyHeight(property.FindPropertyRelative("rotation"));
                var perspectiveProp = property.FindPropertyRelative("perspective");
                height += MeasurePropertyHeight(perspectiveProp);
                if (perspectiveProp.boolValue)
                {
                    height += MeasurePropertyHeight(property.FindPropertyRelative("fov"));
                }
                else
                {
                    height += MeasurePropertyHeight(property.FindPropertyRelative("size"));
                }
                height += MeasurePropertyHeight(property.FindPropertyRelative("minRange"));
                height += MeasurePropertyHeight(property.FindPropertyRelative("maxRange"));
                height += MeasurePropertyHeight(property.FindPropertyRelative("aspect"));
            }

            // Spacing.
            height += EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position.height = EditorGUIUtility.singleLineHeight;
            var foldoutProp = property.FindPropertyRelative("foldout");
            foldoutProp.boolValue = EditorGUI.Foldout(position, foldoutProp.boolValue, property.FindPropertyRelative("name").stringValue, true);
            position.y += position.height;

            if (foldoutProp.boolValue)
            {
                position.y += ShowProperty(position, property.FindPropertyRelative("name"));
                position.y += ShowProperty(position, property.FindPropertyRelative("position"));
                // FIXME There is currently a bug in Unity 2019.3.4f1 causing the incorrect height to be computed for quaternions..
                position.y += ShowProperty(position, property.FindPropertyRelative("rotation"));

                var perspectiveProp = property.FindPropertyRelative("perspective");
                position.y += ShowProperty(position, perspectiveProp);

                if (perspectiveProp.boolValue)
                {
                    position.y += ShowProperty(position, property.FindPropertyRelative("fov"));
                }
                else
                {
                    position.y += ShowProperty(position, property.FindPropertyRelative("size"));
                }

                position.y += ShowProperty(position, property.FindPropertyRelative("minRange"));
                position.y += ShowProperty(position, property.FindPropertyRelative("maxRange"));
                position.y += ShowProperty(position, property.FindPropertyRelative("aspect"));
            }

            EditorGUI.EndProperty();
        }
        private float MeasurePropertyHeight(SerializedProperty property)
        {
            var height = EditorGUI.GetPropertyHeight(property);
            return height + EditorGUIUtility.standardVerticalSpacing;
        }


        private float ShowProperty(Rect position, SerializedProperty property)
        {
            position.height = EditorGUI.GetPropertyHeight(property);
            EditorGUI.PropertyField(position, property);
            return position.height + EditorGUIUtility.standardVerticalSpacing;
        }
    }

}

