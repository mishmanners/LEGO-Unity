using System.Collections.Generic;
using UnityEditor;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(LoseAction), true)]
    public class LoseActionEditor : ObjectiveActionEditor
    {
        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);

            // Collect all Triggers that target this Lose Action.
            List<Trigger> targetingTriggers = m_ObjectiveAction.GetTargetingTriggers();

            // Show a message if no Triggers are targeting this Lose Action.
            if (targetingTriggers.Count == 0)
            {
                EditorGUILayout.HelpBox("You should connect a Trigger Brick to this Lose Action -  otherwise the game is lost immediately. That would be unfair!", MessageType.Warning);
            }

            base.CreateGUI();
        }
    }
}
