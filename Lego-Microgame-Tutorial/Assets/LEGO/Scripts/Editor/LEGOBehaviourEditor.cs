using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Game;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(LEGOBehaviour), true)]
    public abstract class LEGOBehaviourEditor : Editor
    {
        SerializedProperty m_BrickProp;
        protected SerializedProperty m_ScopeProp;

        protected virtual void OnEnable()
        {
            m_BrickProp = serializedObject.FindProperty("m_Brick");
            m_ScopeProp = serializedObject.FindProperty("m_Scope");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_BrickProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("LEGO Behaviours only work when put on LEGO bricks.", MessageType.Warning);
            }

            CreateGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected abstract void CreateGUI();

        public abstract void OnSceneGUI();

        protected void DrawConnections(LEGOBehaviour source, IEnumerable<LEGOBehaviour> destinations, bool fromSourceToDestination, Color color, LEGOBehaviour focusedDestination = null)
        {
            if (!source)
            {
                return;
            }

            Bounds sourceBounds = source.GetBrickBounds();

            foreach (var destination in destinations)
            {
                if (!destination)
                {
                    continue;
                }

                Handles.color = (destination == focusedDestination ? Color.yellow : color);

                Bounds destinationBounds = destination.GetBrickBounds();

                var sourceToDestinationRay = new Ray(sourceBounds.center, destinationBounds.center - sourceBounds.center);
                var destinationToSourceRay = new Ray(destinationBounds.center, sourceBounds.center - destinationBounds.center);

                float destinationToSourceDistance;
                float sourceToDestinationToDistance;

                sourceBounds.IntersectRay(destinationToSourceRay, out destinationToSourceDistance);
                destinationBounds.IntersectRay(sourceToDestinationRay, out sourceToDestinationToDistance);

                var sourceEnd = destinationToSourceRay.origin + destinationToSourceRay.direction * destinationToSourceDistance;
                var destinationEnd = sourceToDestinationRay.origin + sourceToDestinationRay.direction * sourceToDestinationToDistance;

                var diff = destinationEnd - sourceEnd;

                if (diff.sqrMagnitude > 0.8f * 0.8f)
                {
                    var sourceTangent = sourceEnd + diff * 0.25f + Vector3.up * diff.magnitude * 0.2f;
                    var destinationTangent = destinationEnd - diff * 0.25f + Vector3.up * diff.magnitude * 0.2f;

                    var bezierPoints = Handles.MakeBezierPoints(sourceEnd, destinationEnd, sourceTangent, destinationTangent, 8);
                    Handles.DrawPolyLine(bezierPoints);

                    if (fromSourceToDestination)
                    {
                        var arrowDirection = (bezierPoints[7] - bezierPoints[6]).normalized;
                        Handles.ConeHandleCap(0, destinationEnd - arrowDirection * 0.16f, Quaternion.LookRotation(arrowDirection), 0.32f, EventType.Repaint);
                    }
                    else
                    {
                        var arrowDirection = (bezierPoints[0] - bezierPoints[1]).normalized;
                        Handles.ConeHandleCap(0, sourceEnd - arrowDirection * 0.16f, Quaternion.LookRotation(arrowDirection), 0.32f, EventType.Repaint);
                    }
                }
            }
        }

        protected void DrawSeparator()
        {
            GUILayout.Space(8);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(r, Color.grey);
            GUILayout.Space(8);
        }

        /// <summary>
        /// Get all available variables.
        /// Result is a tuple with the list of variables, the list of display names to use, and an array of paths to the variable assets.
        /// </summary>
        protected (List<Variable>, List<string>, string[]) GetAvailableVariables()
        {
            var variables = new List<Variable>();
            var variableDisplayNames = new List<string>();
            var variableNameCount = new Dictionary<string, int>();

            var variableAssetPaths = Directory.GetFiles(VariableManager.k_VariablePath, "*.asset");
            foreach (var variableAssetPath in variableAssetPaths)
            {
                var variable = AssetDatabase.LoadAssetAtPath<Variable>(variableAssetPath);
                variables.Add(variable);
                // Popup does not allow duplicate labels. This is a bug: https://forum.unity.com/threads/popupview-doesnt-show-duplicate-names.933483/
                if (!variableNameCount.ContainsKey(variable.Name))
                {
                    variableNameCount.Add(variable.Name, 0);
                }
                variableNameCount[variable.Name]++;
                variableDisplayNames.Add(variable.Name + (variableNameCount[variable.Name] > 1 ? " [Duplicate " + (variableNameCount[variable.Name] - 1) + "]" : ""));
            }

            return (variables, variableDisplayNames, variableAssetPaths);
        }
    }
}
