using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(ShootAction), true)]
    public class ShootActionEditor : RepeatableActionEditor
    {
        SerializedProperty m_ProjectileProp;
        SerializedProperty m_VelocityProp;
        SerializedProperty m_AccuracyProp;
        SerializedProperty m_LifetimeProp;
        SerializedProperty m_UseGravityProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ProjectileProp = serializedObject.FindProperty("m_Projectile");
            m_VelocityProp = serializedObject.FindProperty("m_Velocity");
            m_AccuracyProp = serializedObject.FindProperty("m_Accuracy");
            m_LifetimeProp = serializedObject.FindProperty("m_Lifetime");
            m_UseGravityProp = serializedObject.FindProperty("m_UseGravity");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);

            if (m_ProjectileProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("You must set a projectile.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(m_ProjectileProp);
            EditorGUILayout.PropertyField(m_VelocityProp);
            EditorGUILayout.PropertyField(m_AccuracyProp);
            EditorGUILayout.PropertyField(m_LifetimeProp);
            EditorGUILayout.PropertyField(m_PauseProp);
            EditorGUILayout.PropertyField(m_UseGravityProp);
            EditorGUILayout.PropertyField(m_RepeatProp);
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_Action && m_Action.IsPlacedOnBrick())
                {
                    var scopedBricks = m_Action.GetScopedBricks();
                    var scopedBounds = m_Action.GetScopedBounds(scopedBricks, out _, out _);

                    var start = scopedBounds.center;
                    var direction = m_Action.GetBrickRotation() * Vector3.forward;
                    Handles.color = Color.green;
                    DrawProjectileDirection(start, direction);
                }
            }
        }

        void DrawProjectileDirection(Vector3 start, Vector3 direction)
        {
            if (m_UseGravityProp.boolValue)
            {
                var samples = new Vector3[25];
                var current = start;
                var currentVelocity = direction * m_VelocityProp.floatValue;
                var timestep = m_LifetimeProp.floatValue / 25.0f;
                samples[0] = current;
                for (var i = 1; i < 25; ++i)
                {
                    // Do a simple second order approximation of the trajectory.
                    var nextVelocity = currentVelocity + timestep * Physics.gravity;
                    var next = current + 0.5f * timestep * currentVelocity + 0.5f * timestep * nextVelocity;

                    samples[i] = next;
                    current = next;
                    currentVelocity = nextVelocity;
                }
                Handles.DrawPolyLine(samples);
            }
            else
            {
                var end = start + direction * m_VelocityProp.floatValue * m_LifetimeProp.floatValue;
                Handles.DrawLine(start, end);
            }
        }
    }
}
