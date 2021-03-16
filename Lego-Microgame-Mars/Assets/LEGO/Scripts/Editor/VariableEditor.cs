using System.IO;
using Unity.LEGO.Game;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Variable))]
public class VariableEditor : Editor
{
    SerializedProperty m_NameProp;
    SerializedProperty m_InitialValueProp;
    SerializedProperty m_UseUIProp;
    SerializedProperty m_UIPrefabProp;

    void OnEnable()
    {
        m_NameProp = serializedObject.FindProperty("Name");
        m_InitialValueProp = serializedObject.FindProperty("InitialValue");
        m_UseUIProp = serializedObject.FindProperty("UseUI");
        m_UIPrefabProp = serializedObject.FindProperty("UIPrefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_NameProp);
        if (EditorGUI.EndChangeCheck())
        {
            // A variable cannot have an empty name.
            if (m_NameProp.stringValue == "")
            {
                m_NameProp.stringValue = "Variable";
            }

            // Update variable asset filename.
            var assetPath = AssetDatabase.GetAssetPath(target);

            // Clear out directory separators.
            var sanitizedVariableName = m_NameProp.stringValue.Replace('/', '_');
            sanitizedVariableName = sanitizedVariableName.Replace('\\', '_');

            var newAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(VariableManager.k_VariablePath, sanitizedVariableName + ".asset"));
            AssetDatabase.RenameAsset(assetPath, Path.GetFileName(newAssetPath));
        }
        EditorGUILayout.PropertyField(m_InitialValueProp);
        EditorGUILayout.PropertyField(m_UseUIProp);

        if (m_UseUIProp.boolValue)
        {
            EditorGUILayout.PropertyField(m_UIPrefabProp);
        }

        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
