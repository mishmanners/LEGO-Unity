using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(AliveAction), true)]
    public class AliveActionEditor : ActionEditor
    {
        SerializedProperty m_TypeProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_TypeProp = serializedObject.FindProperty("m_Type");
        }

        protected override void CreateGUI()
        {
            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_TypeProp);

            EditorGUI.EndDisabledGroup();
        }
    }
}
