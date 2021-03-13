using UnityEditor;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(SensoryTrigger), true)]
    public abstract class SensoryTriggerEditor : TriggerEditor
    {
        protected SerializedProperty m_SenseProp;
        protected SerializedProperty m_SenseTagProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SenseProp = serializedObject.FindProperty("m_Sense");
            m_SenseTagProp = serializedObject.FindProperty("m_SenseTag");
        }
    }
}
