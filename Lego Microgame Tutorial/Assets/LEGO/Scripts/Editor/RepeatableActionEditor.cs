using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(RepeatableAction), true)]
    public abstract class RepeatableActionEditor : ActionEditor
    {
        protected SerializedProperty m_PauseProp;
        protected SerializedProperty m_RepeatProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PauseProp = serializedObject.FindProperty("m_Pause");
            m_RepeatProp = serializedObject.FindProperty("m_Repeat");
        }
    }
}
