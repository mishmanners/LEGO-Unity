using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(MovementAction), true)]
    public abstract class MovementActionEditor :  RepeatableActionEditor
    {
        protected SerializedProperty m_TimeProp;
        protected SerializedProperty m_CollideProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TimeProp = serializedObject.FindProperty("m_Time");
            m_CollideProp = serializedObject.FindProperty("m_Collide");
        }
    }
}
