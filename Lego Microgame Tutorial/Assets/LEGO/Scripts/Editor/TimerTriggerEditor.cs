using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(TimerTrigger), true)]
    public class TimerTriggerEditor : TriggerEditor
    {
        TimerTrigger m_TimerTrigger;

        SerializedProperty m_TimeProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TimerTrigger = (TimerTrigger)m_Trigger;

            m_TimeProp = serializedObject.FindProperty("m_Time");
        }

        protected override void CreateGUI()
        {
            CreateTargetGUI();
            EditorGUILayout.PropertyField(m_TimeProp);
            CreateConditionsGUI();
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (EditorApplication.isPlaying)
                {
                    if (m_TimerTrigger && m_TimerTrigger.IsPlacedOnBrick())
                    {
                        var center = m_TimerTrigger.GetBrickCenter() + Vector3.up * 5.0f;
                        var ratio = m_TimerTrigger.GetElapsedRatio();
                        Handles.color = Color.green;
                        Handles.DrawWireDisc(center, Camera.current.transform.forward, 0.5f);
                        Handles.DrawSolidArc(center, -Camera.current.transform.forward, Camera.current.transform.up, (1.0f - ratio) * 360.0f, 0.5f);
                    }
                }
            }
        }
    }
}
