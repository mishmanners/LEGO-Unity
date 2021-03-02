using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(NearbyTrigger), true)]
    public class NearbyTriggerEditor : SensoryTriggerEditor
    {
        SerializedProperty m_DistanceProp;

        static readonly Color s_BacksideColour = new Color(0.1f, 1.0f, 0.0f, 0.1f);

        protected override void OnEnable()
        {
            base.OnEnable();

            m_DistanceProp = serializedObject.FindProperty("m_Distance");
        }

        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_ScopeProp);
            CreateTargetGUI();
            EditorGUILayout.PropertyField(m_SenseProp);
            if ((SensoryTrigger.Sense)m_SenseProp.enumValueIndex == SensoryTrigger.Sense.Tag)
            {
                m_SenseTagProp.stringValue = EditorGUILayout.TagField(new GUIContent("Tag", "The tag to sense."), m_SenseTagProp.stringValue);
            }

            EditorGUILayout.PropertyField(m_DistanceProp);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_RepeatProp);

            CreateConditionsGUI();
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_Trigger && m_Trigger.IsPlacedOnBrick())
                {
                    var scopedBricks = m_Trigger.GetScopedBricks();
                    var scopedBounds = m_Trigger.GetScopedBounds(scopedBricks, out _, out _);

                    var center = scopedBounds.center;
                    var radius = m_DistanceProp.intValue * LEGOBehaviour.LEGOHorizontalModule;

                    // Plane-plane intersections.
                    var cameraPlaneNormal = Camera.current.transform.forward;
                    var xyPlaneNormal = Vector3.forward;
                    var xyDirection = Vector3.Cross(xyPlaneNormal, cameraPlaneNormal).normalized * radius;
                    var xzPlaneNormal = Vector3.up;
                    var xzDirection = Vector3.Cross(xzPlaneNormal, cameraPlaneNormal).normalized * radius;
                    var yzPlaneNormal = Vector3.right;
                    var yzDirection = Vector3.Cross(yzPlaneNormal, cameraPlaneNormal).normalized * radius;

                    // Draw outline.
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(center, Camera.current.transform.forward, radius);

                    // Draw frontside.
                    Handles.DrawWireArc(center, Vector3.forward, xyDirection, 180.0f, radius);
                    Handles.DrawWireArc(center, Vector3.up, xzDirection, 180.0f, radius);
                    Handles.DrawWireArc(center, Vector3.right, yzDirection, 180.0f, radius);

                    // Draw backside.
                    Handles.color = s_BacksideColour;
                    Handles.DrawWireArc(center, Vector3.forward, -xyDirection, 180.0f, radius);
                    Handles.DrawWireArc(center, Vector3.up, -xzDirection, 180.0f, radius);
                    Handles.DrawWireArc(center, Vector3.right, -yzDirection, 180.0f, radius);
                }
            }
        }
    }
}
