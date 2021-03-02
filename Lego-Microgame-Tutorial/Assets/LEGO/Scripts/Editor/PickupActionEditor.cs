using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;
using UnityEditor.SceneManagement;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(PickupAction), true)]
    public class PickupActionEditor : ActionEditor
    {
        PickupAction m_PickupAction;

        SerializedProperty m_EffectProp;

        List<Trigger> m_DependentTriggers = new List<Trigger>();

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PickupAction = (PickupAction)m_Action;

            m_EffectProp = serializedObject.FindProperty("m_Effect");

            // Collect Pickup Triggers that depend on this Pickup Action.
            m_DependentTriggers.Clear();

            var pickupTriggers = StageUtility.GetCurrentStageHandle().FindComponentsOfType<PickupTrigger>();

            foreach(var trigger in pickupTriggers)
            {
                if (trigger.GetMode() == PickupTrigger.Mode.SpecificPickups)
                {
                    var specificPickups = trigger.GetSpecificPickupActions();
                    if (specificPickups.Contains(m_PickupAction))
                    {
                        m_DependentTriggers.Add(trigger);
                    }
                }
            }
        }

        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_ScopeProp);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_EffectProp);

            EditorGUI.EndDisabledGroup();
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_PickupAction)
                {
                    DrawConnections(m_PickupAction, m_DependentTriggers, true, Color.green);
                }
            }
        }
    }
}
