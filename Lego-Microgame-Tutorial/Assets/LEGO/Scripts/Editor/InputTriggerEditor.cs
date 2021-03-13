using Unity.LEGO.Behaviours;
using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(InputTrigger), true)]
    public class InputTriggerEditor : SensoryTriggerEditor
    {
        protected SerializedProperty m_TypeProp;
        protected SerializedProperty m_OtherKeyProp;
        protected SerializedProperty m_EnableProp;
        protected SerializedProperty m_DistanceProp;
        protected SerializedProperty m_ShowPromptProp;

        static readonly Color s_BacksideColour = new Color(0.1f, 1.0f, 0.0f, 0.1f);

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TypeProp = serializedObject.FindProperty("m_Type");
            m_OtherKeyProp = serializedObject.FindProperty("m_OtherKey");
            m_EnableProp = serializedObject.FindProperty("m_Enable");
            m_DistanceProp = serializedObject.FindProperty("m_Distance");
            m_ShowPromptProp = serializedObject.FindProperty("m_ShowPrompt");
        }

        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            CreateTargetGUI();

            EditorGUILayout.PropertyField(m_TypeProp, new GUIContent("Input"));
            if ((InputTrigger.Type)m_TypeProp.enumValueIndex == InputTrigger.Type.OtherKey)
            {
                EditorGUILayout.PropertyField(m_OtherKeyProp);
            }

            EditorGUILayout.PropertyField(m_EnableProp);
            if ((InputTrigger.Enable)m_EnableProp.enumValueIndex == InputTrigger.Enable.WhenTagIsNearby)
            {
                m_SenseTagProp.stringValue = EditorGUILayout.TagField(new GUIContent("Tag", "The tag to look for."), m_SenseTagProp.stringValue);
            }

            if ((InputTrigger.Enable)m_EnableProp.enumValueIndex != InputTrigger.Enable.Always)
            {
                EditorGUILayout.PropertyField(m_DistanceProp);
            }
            EditorGUILayout.PropertyField(m_ShowPromptProp);

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
                    if ((InputTrigger.Enable)m_EnableProp.enumValueIndex != InputTrigger.Enable.Always)
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
}
