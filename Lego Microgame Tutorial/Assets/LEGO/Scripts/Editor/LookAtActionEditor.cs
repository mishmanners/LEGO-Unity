using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(LookAtAction), true)]
    public class LookAtActionEditor : MovementActionEditor
    {
        LookAtAction m_LookAtAction;

        SerializedProperty m_LookAtProp;
        SerializedProperty m_TransformModeTransformProp;
        SerializedProperty m_SpeedProp;
        SerializedProperty m_RotateProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_LookAtAction = (LookAtAction)m_Action;

            m_LookAtProp = serializedObject.FindProperty("m_LookAt");
            m_TransformModeTransformProp = serializedObject.FindProperty("m_TransformModeTransform");
            m_SpeedProp = serializedObject.FindProperty("m_Speed");
            m_RotateProp = serializedObject.FindProperty("m_Rotate");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);
            EditorGUILayout.PropertyField(m_LookAtProp);

            if ((LookAtAction.LookAt)m_LookAtProp.enumValueIndex == LookAtAction.LookAt.Transform)
            {
                if (m_TransformModeTransformProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("You must set a specific transform to look at.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(m_TransformModeTransformProp, new GUIContent("Specific Transform"));
            }

            EditorGUILayout.PropertyField(m_SpeedProp);
            EditorGUILayout.PropertyField(m_RotateProp);
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
                if (m_LookAtAction && m_LookAtAction.IsPlacedOnBrick())
                {
                    var start = m_LookAtAction.GetBrickCenter();
                    var direction = m_LookAtAction.GetBrickRotation() * Vector3.forward;
                    var end = start + direction * 3.2f;
                    Handles.color = Color.green;
                    Handles.DrawDottedLine(start, end, 5.0f);

                    var angleRange = m_SpeedProp.intValue * m_TimeProp.floatValue;
                    var rotate = (LookAtAction.Rotate)m_RotateProp.enumValueIndex;
                    switch (rotate)
                    {
                        case LookAtAction.Rotate.Horizontally:
                            {
                                DrawRange(start, direction, Vector3.up, angleRange, m_LookAtAction.GetHorizontalRotatedAngle());
                                break;
                            }
                        case LookAtAction.Rotate.Vertically:
                            {
                                var rotationAxis = Vector3.Cross(direction, Vector3.up);
                                DrawRange(start, direction, rotationAxis, angleRange, m_LookAtAction.GetVerticalRotatedAngle());
                                break;
                            }
                        case LookAtAction.Rotate.Freely:
                            {
                                DrawRange(start, direction, Vector3.up, angleRange, m_LookAtAction.GetHorizontalRotatedAngle());
                                var rotationAxis = Vector3.Cross(direction, Vector3.up);
                                DrawRange(start, direction, rotationAxis, angleRange, m_LookAtAction.GetVerticalRotatedAngle());
                                break;
                            }
                    }
                }
            }
        }

        void DrawRange(Vector3 start, Vector3 direction, Vector3 rotationAxis, float angleRange, float rotatedAngle)
        {
            var positiveAngle = angleRange - rotatedAngle;
            var negativeAngle = -angleRange - rotatedAngle;
            var totalAngle = positiveAngle - negativeAngle;
            if (totalAngle > 360.0f)
            {
                Handles.DrawWireDisc(start, rotationAxis, 3.2f);
            }
            else
            {
                Handles.DrawWireArc(start, rotationAxis, direction, positiveAngle, 3.2f);
                Handles.DrawWireArc(start, rotationAxis, direction, negativeAngle, 3.2f);
                var end = Quaternion.AngleAxis(positiveAngle, rotationAxis) * direction * 3.2f + start;
                Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
                end = Quaternion.AngleAxis(negativeAngle, rotationAxis) * direction * 3.2f + start;
                Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
            }
        }
    }
}
