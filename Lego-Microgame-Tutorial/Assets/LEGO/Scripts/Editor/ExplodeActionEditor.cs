using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(ExplodeAction), true)]
    public class ExplodeActionEditor : ActionEditor
    {
        SerializedProperty m_PowerProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PowerProp = serializedObject.FindProperty("m_Power");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);
            EditorGUILayout.PropertyField(m_PowerProp);
        }
    }
}
