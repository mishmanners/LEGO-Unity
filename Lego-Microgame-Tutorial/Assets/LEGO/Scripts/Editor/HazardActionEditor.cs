using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(HazardAction), true)]
    public class HazardActionEditor : ActionEditor
    {
        SerializedProperty m_EffectProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_EffectProp = serializedObject.FindProperty("m_Effect");
        }

        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);
            EditorGUILayout.PropertyField(m_EffectProp);

            EditorGUI.EndDisabledGroup();
        }
    }
}
