using System.Collections.Generic;
using UnityEditor;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(WinAction), true)]
    public class WinActionEditor : ObjectiveActionEditor
    {
        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);

            // Collect all Triggers that target this Win Action.
            List<Trigger> targetingTriggers = m_ObjectiveAction.GetTargetingTriggers();

            // Show a message if no Triggers are targeting this Win Action.
            if (targetingTriggers.Count == 0)
            {
                EditorGUILayout.HelpBox("You should connect a Trigger Brick to this Win Action -  otherwise it is won automatically. That would be too easy!", MessageType.Warning);
            }

            base.CreateGUI();
        }
    }
}
