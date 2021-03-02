using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(RandomTrigger), true)]
    public class RandomTriggerEditor : TriggerEditor
    {
        RandomTrigger m_RandomTrigger;

        SerializedProperty m_MinTimeProp;
        SerializedProperty m_MaxTimeProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_RandomTrigger = (RandomTrigger)m_Trigger;

            m_MinTimeProp = serializedObject.FindProperty("m_MinTime");
            m_MaxTimeProp = serializedObject.FindProperty("m_MaxTime");
        }

        protected override void CreateGUI()
        {
            CreateTargetGUI();
            EditorGUILayout.PropertyField(m_MinTimeProp);
            EditorGUILayout.PropertyField(m_MaxTimeProp);
            CreateConditionsGUI();
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (EditorApplication.isPlaying)
                {
                    if (m_RandomTrigger && m_RandomTrigger.IsPlacedOnBrick())
                    {
                        var center = m_RandomTrigger.GetBrickCenter() + Vector3.up * 5.0f;
                        var ratio = m_RandomTrigger.GetElapsedRatio();
                        Handles.color = Color.green;
                        Handles.DrawWireDisc(center, Camera.current.transform.forward, 0.5f);
                        Handles.DrawSolidArc(center, -Camera.current.transform.forward, Camera.current.transform.up, (1.0f - ratio) * 360.0f, 0.5f);
                    }
                }
            }
        }
    }
}
