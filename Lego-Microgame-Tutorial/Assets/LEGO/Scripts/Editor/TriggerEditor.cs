using System.Collections.Generic;
using System.IO;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;
using Unity.LEGO.Game;
using UnityEditor;
using UnityEngine;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(Trigger), true)]
    public abstract class TriggerEditor : LEGOBehaviourEditor
    {
        static readonly string editorPrefsKey = "com.unity.template.lego.showExtraConditions";

        static bool s_ShowExtraConditions;

        protected Trigger m_Trigger;

        protected SerializedProperty m_RepeatProp;

        SerializedProperty m_TargetProp;
        SerializedProperty m_SpecificTargetActionsProp;
        SerializedProperty m_ConditionsProp;

        Action m_FocusedAction = null;

        List<Editor> m_VariableEditors = new List<Editor>();

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Trigger = (Trigger)target;

            m_RepeatProp = serializedObject.FindProperty("m_Repeat");
            m_TargetProp = serializedObject.FindProperty("m_Target");
            m_ConditionsProp = serializedObject.FindProperty("m_Conditions");
            m_SpecificTargetActionsProp = serializedObject.FindProperty("m_SpecificTargetActions");

            s_ShowExtraConditions = EditorPrefs.GetBool(editorPrefsKey, false);
        }

        protected void OnDisable()
        {
            foreach (var editor in m_VariableEditors)
            {
                DestroyImmediate(editor);
            }

            m_VariableEditors.Clear();
        }

        public override void OnSceneGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_Trigger)
                {
                    DrawConnections(m_Trigger, m_Trigger.GetTargetedActions(), true, Color.cyan, m_FocusedAction);
                }
            }
        }

        protected void CreateTargetGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            EditorGUILayout.PropertyField(m_TargetProp);
            if ((Trigger.Target)m_TargetProp.enumValueIndex == Trigger.Target.SpecificActions)
            {
                if (EditorGUILayout.PropertyField(m_SpecificTargetActionsProp, new GUIContent("Specific Actions"), false))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_SpecificTargetActionsProp.FindPropertyRelative("Array.size"));
                    for (var i = 0; i < m_SpecificTargetActionsProp.arraySize; ++i)
                    {
                        GUI.SetNextControlName("Action " + i);
                        EditorGUILayout.PropertyField(m_SpecificTargetActionsProp.GetArrayElementAtIndex(i));
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndDisabledGroup();

            var previousFocusedAction = m_FocusedAction;

            // Find the currently focused Action.
            var focusedControlName = GUI.GetNameOfFocusedControl();
            var lastSpace = focusedControlName.LastIndexOf(' ');
            if (focusedControlName.StartsWith("Action") && lastSpace >= 0)
            {
                var index = int.Parse(focusedControlName.Substring(lastSpace + 1));
                if (index < m_SpecificTargetActionsProp.arraySize)
                {
                    m_FocusedAction = (Action)m_SpecificTargetActionsProp.GetArrayElementAtIndex(index).objectReferenceValue;
                }
                else
                {
                    m_FocusedAction = null;
                }
            }
            else
            {
                m_FocusedAction = null;
            }

            if (m_FocusedAction != previousFocusedAction)
            {
                SceneView.RepaintAll();
            }
        }

        protected void CreateConditionsGUI(bool useFoldout = true)
        {
            if (useFoldout)
            {
                EditorGUI.BeginChangeCheck();
                s_ShowExtraConditions = EditorGUILayout.Foldout(s_ShowExtraConditions, "Extra Conditions");
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(editorPrefsKey, s_ShowExtraConditions);
                }
            }
            if (!useFoldout || s_ShowExtraConditions)
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

                // Refresh variable list.
                var variables = GetAvailableVariables();
                variables.Item2.Add("[Add New Variable]");

                // Show conditions.
                var anyAvailableVariablesInConditions = false;
                for (var i = 0; i < m_ConditionsProp.arraySize; ++i)
                {
                    var conditionProp = m_ConditionsProp.GetArrayElementAtIndex(i);
                    var variableProp = conditionProp.FindPropertyRelative("Variable");
                    var index = variables.Item1.FindIndex(item => item == (Variable)variableProp.objectReferenceValue);

                    index = EditorGUILayout.Popup(new GUIContent("Variable", "The variable to check."), index, variables.Item2.ToArray());

                    if (index > -1)
                    {
                        EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("Type"));
                        EditorGUILayout.PropertyField(conditionProp.FindPropertyRelative("Value"));

                        if (index == variables.Item2.Count - 1)
                        {
                            var newVariable = CreateInstance<Variable>();
                            newVariable.Name = "Variable";
                            var newVariableAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(VariableManager.k_VariablePath, "Variable.asset"));
                            AssetDatabase.CreateAsset(newVariable, newVariableAssetPath);
                            variableProp.objectReferenceValue = newVariable;
                        }
                        else
                        {
                            variableProp.objectReferenceValue = variables.Item1[index];
                        }

                        anyAvailableVariablesInConditions = true;
                    }

                    GUILayout.Space(16);
                }

                if (GUILayout.Button("Add Condition"))
                {
                    m_ConditionsProp.arraySize++;
                }

                if (m_ConditionsProp.arraySize > 0)
                {
                    if (GUILayout.Button("Remove Condition"))
                    {
                        m_ConditionsProp.arraySize--;
                    }
                }

                if (anyAvailableVariablesInConditions)
                {
                    DrawSeparator();
                    EditorGUILayout.LabelField("Variable Settings", EditorStyles.boldLabel);
                }

                var shownVariables = new HashSet<Variable>();

                // Create and show editors for used variables.
                for (var i = 0; i < m_ConditionsProp.arraySize; ++i)
                {
                    var conditionProp = m_ConditionsProp.GetArrayElementAtIndex(i);
                    var variableProp = conditionProp.FindPropertyRelative("Variable");
                    var variable = (Variable)variableProp.objectReferenceValue;
                    if (variable)
                    {
                        var index = variables.Item1.FindIndex(item => item == variable);

                        if (index > -1 && !shownVariables.Contains(variable))
                        {
                            var variableEditor = m_VariableEditors.Find(item => item.target == variable);
                            if (!variableEditor)
                            {
                                variableEditor = CreateEditor(variable);
                                m_VariableEditors.Add(variableEditor);
                            }

                            if (shownVariables.Count > 0)
                            {
                                GUILayout.Space(16);
                            }

                            variableEditor.OnInspectorGUI();
                            shownVariables.Add(variable);

                            if (GUILayout.Button("Delete Variable"))
                            {
                                AssetDatabase.DeleteAsset(variables.Item3[index]);
                            }
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();

                // Remove editors that represent deleted or unused variables.
                for (var i = m_VariableEditors.Count - 1; i >= 0; i--)
                {
                    var variableEditor = m_VariableEditors[i];
                    if (!variableEditor.target || !shownVariables.Contains((Variable)variableEditor.target))
                    {
                        DestroyImmediate(variableEditor);
                        m_VariableEditors.RemoveAt(i);
                    }
                }
            }
        }
    }
}
