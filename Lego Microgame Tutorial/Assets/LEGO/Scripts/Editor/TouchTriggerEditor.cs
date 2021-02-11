using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(TouchTrigger), true)]
    public class TouchTriggerEditor : SensoryTriggerEditor
    {
        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_ScopeProp);
            CreateTargetGUI();
            EditorGUILayout.PropertyField(m_SenseProp);
            if ((SensoryTrigger.Sense)m_SenseProp.enumValueIndex == SensoryTrigger.Sense.Tag)
            {
                m_SenseTagProp.stringValue = EditorGUILayout.TagField(new GUIContent("Tag", "The tag to sense."), m_SenseTagProp.stringValue);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_RepeatProp);

            CreateConditionsGUI();
        }
    }
}
