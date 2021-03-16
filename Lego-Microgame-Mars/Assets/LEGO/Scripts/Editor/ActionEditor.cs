using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(Action), true)]
    public abstract class ActionEditor : LEGOBehaviourEditor
    {
        protected Action m_Action;

        protected SerializedProperty m_AudioProp;
        protected SerializedProperty m_AudioVolumeProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Action = (Action)target;

            m_AudioProp = serializedObject.FindProperty("m_Audio");
            m_AudioVolumeProp = serializedObject.FindProperty("m_AudioVolume");
        }

        public override void OnSceneGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_Action)
                {
                    DrawConnections(m_Action, m_Action.GetTargetingTriggers(), false, Color.cyan);
                }
            }
        }
    }
}
