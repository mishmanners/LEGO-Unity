using System.IO;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Game;
using UnityEditor;
using UnityEngine;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(CounterAction), true)]
    public class CounterActionEditor : RepeatableActionEditor
    {
        SerializedProperty m_OperatorProp;
        SerializedProperty m_ValueProp;
        SerializedProperty m_VariableProp;

        Editor m_VariableEditor;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_OperatorProp = serializedObject.FindProperty("m_Operator");
            m_ValueProp = serializedObject.FindProperty("m_Value");
            m_VariableProp = serializedObject.FindProperty("m_Variable");
        }

        protected void OnDisable()
        {
            DestroyImmediate(m_VariableEditor);
        }

        protected override void CreateGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            // Refresh variable list.
            var variables = GetAvailableVariables();
            variables.Item2.Add("[Add New Variable]");

            // Update variable index.
            var index = variables.Item1.FindIndex(item => item == (Variable)m_VariableProp.objectReferenceValue);

            index = EditorGUILayout.Popup(new GUIContent("Variable", "The variable to modify."), index, variables.Item2.ToArray());

            if (index > -1)
            {
                EditorGUILayout.PropertyField(m_OperatorProp);
                EditorGUILayout.PropertyField(m_ValueProp);

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(m_PauseProp);
                EditorGUILayout.PropertyField(m_RepeatProp);

                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

                DrawSeparator();
                EditorGUILayout.LabelField("Variable Settings", EditorStyles.boldLabel);

                if (index == variables.Item2.Count - 1)
                {
                    var newVariable = CreateInstance<Variable>();
                    newVariable.Name = "Variable";
                    var newVariableAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(VariableManager.k_VariablePath, "Variable.asset"));
                    AssetDatabase.CreateAsset(newVariable, newVariableAssetPath);
                    m_VariableProp.objectReferenceValue = newVariable;
                } 
                else
                {
                    m_VariableProp.objectReferenceValue = variables.Item1[index];

                    // Only recreate editor if necessary.
                    if (!m_VariableEditor || m_VariableEditor.target != m_VariableProp.objectReferenceValue)
                    {
                        DestroyImmediate(m_VariableEditor);
                        m_VariableEditor = CreateEditor(m_VariableProp.objectReferenceValue);
                    }

                    m_VariableEditor.OnInspectorGUI();

                    if (GUILayout.Button("Delete Variable"))
                    {
                        AssetDatabase.DeleteAsset(variables.Item3[index]);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
