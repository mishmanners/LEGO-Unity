using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(HoverAction), true)]
    public class HoverActionEditor : MovementActionEditor
    {
        HoverAction m_HoverAction;

        SerializedProperty m_AmplitudeProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_HoverAction = (HoverAction)m_Action;

            m_AmplitudeProp = serializedObject.FindProperty("m_Amplitude");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AmplitudeProp);
            EditorGUILayout.PropertyField(m_TimeProp);
            EditorGUILayout.PropertyField(m_CollideProp);
        }
    
        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_HoverAction && m_HoverAction.IsPlacedOnBrick())
                {
                    var start = m_HoverAction.GetBrickCenter() - m_HoverAction.GetOffset() - Vector3.up * m_AmplitudeProp.intValue * LEGOBehaviour.LEGOVerticalModule;
                    var end = start + Vector3.up * 2.0f * m_AmplitudeProp.intValue * LEGOBehaviour.LEGOVerticalModule;
                    Handles.color = Color.green;
                    Handles.DrawLine(start, end);
                    Handles.DrawSolidDisc(start, Camera.current.transform.forward, 0.16f);
                    Handles.DrawSolidDisc(end, Camera.current.transform.forward, 0.16f);
                }
            }
        }
    }
}
