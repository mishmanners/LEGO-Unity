using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(ObjectiveAction), true)]
    public class ObjectiveActionEditor : ActionEditor
    {
        protected ObjectiveAction m_ObjectiveAction;

        Trigger m_FocusedTrigger = null;

        SerializedProperty m_ObjectiveConfigurationsProp;
        SerializedProperty m_TriggersProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ObjectiveAction = (ObjectiveAction)m_Action;

            m_ObjectiveConfigurationsProp = serializedObject.FindProperty("m_ObjectiveConfigurations");
            m_TriggersProp = serializedObject.FindProperty("m_Triggers");
        }

        protected override void CreateGUI()
        {
            // Collect all Triggers that target this Objective Action.
            List<Trigger> targetingTriggers = m_ObjectiveAction.GetTargetingTriggers();

            // Check if trigger is already registered with the Objective Action.
            foreach (var targetingTrigger in targetingTriggers)
            {
                var foundTrigger = false;
                for (var i = 0; i < m_TriggersProp.arraySize; ++i)
                {
                    if (m_TriggersProp.GetArrayElementAtIndex(i).objectReferenceValue == targetingTrigger)
                    {
                        foundTrigger = true;
                        break;
                    }
                }

                // If trigger was new, set up the corresponding objective with reasonable default values.
                if (!foundTrigger)
                {
                    m_TriggersProp.arraySize++;
                    m_ObjectiveConfigurationsProp.arraySize++;
                    m_TriggersProp.GetArrayElementAtIndex(m_TriggersProp.arraySize - 1).objectReferenceValue = targetingTrigger;

                    var objectiveConfigurationProp = m_ObjectiveConfigurationsProp.GetArrayElementAtIndex(m_ObjectiveConfigurationsProp.arraySize - 1);
                    var objectiveConfiguration = m_ObjectiveAction.GetDefaultObjectiveConfiguration(targetingTrigger);
                    objectiveConfigurationProp.FindPropertyRelative("Title").stringValue = objectiveConfiguration.Title;
                    objectiveConfigurationProp.FindPropertyRelative("Description").stringValue = objectiveConfiguration.Description;
                    objectiveConfigurationProp.FindPropertyRelative("ProgressType").enumValueIndex = (int)objectiveConfiguration.ProgressType;
                    objectiveConfigurationProp.FindPropertyRelative("Lose").boolValue = objectiveConfiguration.Lose;
                    objectiveConfigurationProp.FindPropertyRelative("Hidden").boolValue = objectiveConfiguration.Hidden;
                }
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            // Show the objectives of the currently targeting triggers.
            for (var i = 0; i < m_TriggersProp.arraySize; ++i)
            {
                var trigger = (Trigger)m_TriggersProp.GetArrayElementAtIndex(i).objectReferenceValue;

                if (targetingTriggers.Contains(trigger))
                {
                    var label = trigger.GetType().ToString();
                    label = label.Substring(label.LastIndexOf('.') + 1);
                    label = ObjectNames.NicifyVariableName(label);
                    var objectiveConfigurationProp = m_ObjectiveConfigurationsProp.GetArrayElementAtIndex(i);
                    GUI.SetNextControlName("Trigger " + i);
                    if (EditorGUILayout.PropertyField(objectiveConfigurationProp, new GUIContent("Objective for " + label), false))
                    {
                        EditorGUI.indentLevel++;
                        GUI.SetNextControlName("Trigger Title " + i);
                        EditorGUILayout.PropertyField(objectiveConfigurationProp.FindPropertyRelative("Title"), new GUIContent("Title", "The title of the objective."));
                        GUI.SetNextControlName("Trigger Description " + i);
                        EditorGUILayout.PropertyField(objectiveConfigurationProp.FindPropertyRelative("Description"), new GUIContent("Details", "The details of the objective."));
                        GUI.SetNextControlName("Trigger Hidden " + i);
                        EditorGUILayout.PropertyField(objectiveConfigurationProp.FindPropertyRelative("Hidden"), new GUIContent("Hidden", "Hide the objective from the player."));
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUI.EndDisabledGroup();

            var previousFocusedTrigger = m_FocusedTrigger;

            // Find the currently focused Trigger.
            var focusedControlName = GUI.GetNameOfFocusedControl();
            var lastSpace = focusedControlName.LastIndexOf(' ');
            if (focusedControlName.StartsWith("Trigger") && lastSpace >= 0)
            {
                var index = int.Parse(focusedControlName.Substring(lastSpace + 1));
                m_FocusedTrigger = (Trigger)m_TriggersProp.GetArrayElementAtIndex(index).objectReferenceValue;
            }
            else
            {
                m_FocusedTrigger = null;
            }

            if (m_FocusedTrigger != previousFocusedTrigger)
            {
                SceneView.RepaintAll();
            }
        }

        public override void OnSceneGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_ObjectiveAction)
                {
                    DrawConnections(m_ObjectiveAction, m_ObjectiveAction.GetTargetingTriggers(), false, Color.cyan, m_FocusedTrigger);
                }
            }
        }
    }
}
