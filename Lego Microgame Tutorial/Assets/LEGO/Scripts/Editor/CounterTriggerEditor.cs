using UnityEditor;
using Unity.LEGO.Behaviours.Triggers;
using UnityEngine;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(CounterTrigger), true)]
    public class CounterTriggerEditor : TriggerEditor
    {
        protected override void CreateGUI()
        {
            CreateTargetGUI();

            EditorGUILayout.PropertyField(m_RepeatProp);

            GUILayout.Label("Conditions", EditorStyles.boldLabel);

            CreateConditionsGUI(false);
        }
    }
}
