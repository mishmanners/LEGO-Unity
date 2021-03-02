using UnityEditor;
using Unity.LEGO.Behaviours.Actions;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(AudioAction), true)]
    public class AudioActionEditor : ActionEditor
    {
        SerializedProperty m_SpatialProp;
        SerializedProperty m_LoopProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_SpatialProp = serializedObject.FindProperty("m_Spatial");
            m_LoopProp = serializedObject.FindProperty("m_Loop");
        }

        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && m_LoopProp.boolValue);

            EditorGUILayout.PropertyField(m_AudioProp);
            EditorGUILayout.PropertyField(m_AudioVolumeProp);
            EditorGUILayout.PropertyField(m_SpatialProp);
            EditorGUILayout.PropertyField(m_LoopProp);

            EditorGUI.EndDisabledGroup();
        }
    }
}
