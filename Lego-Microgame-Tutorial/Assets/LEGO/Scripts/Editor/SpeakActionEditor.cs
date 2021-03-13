using System;
using Unity.LEGO.Behaviours;
using Unity.LEGO.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(SpeakAction), true)]
    public class SpeakActionEditor : RepeatableActionEditor
    {
        SerializedProperty m_SpeechBubbleInfosProp;

        ReorderableList m_ListOfSpeech;

        static GUIStyle m_LabelStyle;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SpeechBubbleInfosProp = serializedObject.FindProperty("m_SpeechBubbleInfos");

            m_ListOfSpeech = new ReorderableList(serializedObject, m_SpeechBubbleInfosProp, true, true, true, true)
            {
                drawHeaderCallback = DrawHeaderCallback,
                elementHeightCallback = ElementHeightCallback,
                drawElementCallback = DrawElementCallback
            };
        }

        protected override void CreateGUI() 
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(EditorStyles.label);
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            m_ListOfSpeech.DoLayoutList();

            EditorGUILayout.PropertyField(m_PauseProp);
            EditorGUILayout.PropertyField(m_RepeatProp);

            EditorGUI.EndDisabledGroup();
        }

        void DrawElementCallback(Rect rect, int index, bool active, bool focused)
        {
            var speechBubbleInfoProp = m_SpeechBubbleInfosProp.GetArrayElementAtIndex(index);
            var typeProp = speechBubbleInfoProp.FindPropertyRelative("Type");
            var textProp = speechBubbleInfoProp.FindPropertyRelative("Text");
            var typeName = Enum.GetName(typeof(SpeechBubblePrompt.Type), typeProp.intValue);

            EditorGUI.PropertyField(new Rect(rect.x + 15.0f, rect.y, rect.width - 15.0f, EditorGUIUtility.singleLineHeight), speechBubbleInfoProp, new GUIContent(typeName), true);

            if (speechBubbleInfoProp.isExpanded)
            {
                var height = EditorGUI.GetPropertyHeight(speechBubbleInfoProp, true);

                m_LabelStyle.normal.textColor = textProp.arraySize > SpeakAction.MaxCharactersPerSpeechBubble ? Color.red : Color.gray;

                var label = "(" + textProp.arraySize + " / " + SpeakAction.MaxCharactersPerSpeechBubble + ")";
                if (textProp.arraySize > SpeakAction.MaxCharactersPerSpeechBubble)
                {
                    label += " Over the limit, only the first " + SpeakAction.MaxCharactersPerSpeechBubble + " characters will be shown.";
                }
                EditorGUI.LabelField(new Rect(rect.x + 30.0f, rect.y + height, rect.width - 30.0f, EditorGUIUtility.singleLineHeight), label, m_LabelStyle);
            }
        }

        void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Speech Bubbles");
        }

        float ElementHeightCallback(int index)
        {
            var speechInfoProp = m_SpeechBubbleInfosProp.GetArrayElementAtIndex(index);
            var height = EditorGUI.GetPropertyHeight(speechInfoProp, true);

            return speechInfoProp.isExpanded ? height + EditorGUIUtility.singleLineHeight : EditorGUIUtility.singleLineHeight;
        }
    }
}
