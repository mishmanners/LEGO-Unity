using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(MoveAction), true)]
    public class MoveActionEditor : MovementActionEditor
    {
        MoveAction m_MoveAction;

        SerializedProperty m_DistanceProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_MoveAction = (MoveAction)m_Action;

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
                if (m_MoveAction && m_MoveAction.IsPlacedOnBrick())
                {
                    var start = m_MoveAction.GetBrickCenter();
                    var end = start + m_Action.transform.forward * m_MoveAction.GetRemainingDistance() * LEGOBehaviour.LEGOHorizontalModule;
                    Handles.color = Color.green;
                    Handles.DrawLine(start, end);
                    Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
                }
            }
        }
    }
}
