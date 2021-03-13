using UnityEngine;
using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{

    [CustomPropertyDrawer(typeof(Action), true)]
    public class ActionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue)
            {
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), new GUIContent(property.objectReferenceValue.name));
                if (GUI.Button(position, new GUIContent("Select")))
                {
                    ShowActionPicker(property);
                }
            }
            else
            {
                if (GUI.Button(position, new GUIContent("Select Action")))
                {
                    ShowActionPicker(property);
                }
            }
        }

        void ShowActionPicker(SerializedProperty property)
        {
            // Deduce the specific type of Action to show in the ActionPicker window.
            System.Type actionType;
            if (fieldInfo.FieldType.IsGenericType)
            {
                actionType = fieldInfo.FieldType.GetGenericArguments()[0];
            } else {
                actionType = fieldInfo.FieldType;
            }

            ActionPicker.Show((Action)property.objectReferenceValue, actionType, (action) =>
            {
                property.objectReferenceValue = action;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
    }
}
