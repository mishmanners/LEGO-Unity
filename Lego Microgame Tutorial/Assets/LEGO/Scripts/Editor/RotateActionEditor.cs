using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(RotateAction), true)]
    public class RotateActionEditor : MovementActionEditor
    {
        RotateAction m_RotateAction;

        SerializedProperty m_AngleProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_RotateAction = (RotateAction)m_Action;

            m_AngleProp = serializedObject.FindProperty("m_Angle");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);
            EditorGUILayout.PropertyField(m_AngleProp);
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
                if (m_RotateAction && m_RotateAction.IsPlacedOnBrick())
                {
                    var rotationAxis = m_RotateAction.GetBrickRotation() * Vector3.up;
                    var forward = m_RotateAction.GetBrickRotation() * Vector3.forward;
                    var center = m_RotateAction.GetBrickCenter();
                    var angle = m_RotateAction.GetRemainingAngle();
                    var end = m_RotateAction.GetBrickRotation() * Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.forward * 3.2f + center;
                    Handles.color = Color.green;
                    Handles.DrawWireArc(center, rotationAxis, forward, Mathf.Clamp(angle, -360.0f, 360.0f), 3.2f);
                    Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
                    var fullRounds = Mathf.FloorToInt(Mathf.Abs(angle) / 360);
                    if (fullRounds > 0)
                    {
                        for (var i = 0; i < fullRounds; ++i)
                        {
                            Handles.DrawWireDisc(end, Camera.current.transform.forward, 0.16f + (i + 1) * 0.08f);
                        }
                    }
                }
            }
        }
    }
}
