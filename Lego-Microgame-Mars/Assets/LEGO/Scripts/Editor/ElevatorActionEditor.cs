using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(ElevatorAction), true)]
    public class ElevatorActionEditor : MovementActionEditor
    {
        ElevatorAction m_ElevatorAction;

        SerializedProperty m_DistanceProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ElevatorAction = (ElevatorAction)m_Action;

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
                if (m_ElevatorAction && m_ElevatorAction.IsPlacedOnBrick())
                {
                    var start = m_ElevatorAction.GetBrickCenter() - m_ElevatorAction.GetOffset();
                    var end = start + Vector3.up * m_DistanceProp.intValue * LEGOBehaviour.LEGOVerticalModule;
                    Handles.color = Color.green;
                    Handles.DrawLine(start, end);
                    Handles.DrawSolidDisc(start, Camera.current.transform.forward, 0.16f);
                    Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
                }
            }
        }
    }
}
