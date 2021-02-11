using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(PlatformAction), true)]
    public class PlatformActionEditor : MovementActionEditor
    {
        PlatformAction m_PlatformAction;

        SerializedProperty m_DistanceProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PlatformAction = (PlatformAction)m_Action;

            m_DistanceProp = serializedObject.FindProperty("m_Distance");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);
            EditorGUILayout.PropertyField(m_DistanceProp);
            EditorGUILayout.PropertyField(m_TimeProp);
            EditorGUILayout.PropertyField(m_PauseProp);
            EditorGUILayout.PropertyField(m_CollideProp);
            EditorGUILayout.PropertyField(m_RepeatProp);
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_PlatformAction && m_PlatformAction.IsPlacedOnBrick())
                {
                    var start = m_PlatformAction.GetBrickCenter() - m_PlatformAction.GetOffset();
                    var end = start + m_PlatformAction.transform.forward * m_DistanceProp.intValue * LEGOBehaviour.LEGOHorizontalModule;
                    Handles.color = Color.green;
                    Handles.DrawLine(start, end);
                    Handles.DrawSolidDisc(start, Camera.current.transform.forward, 0.16f);
                    Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
                }
            }
        }
    }
}
