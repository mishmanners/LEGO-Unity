using LEGOModelImporter;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.LEGO.EditorExt
{
    public class ActionPicker : EditorWindow
    {
        static readonly string editorPrefsKey = "com.unity.template.lego.frameActions";

        static Dictionary<string, Texture> s_ActionTextures;

        static GUIStyle s_ButtonStyle;
        static Vector2 s_ScrollPosition;

        static bool s_FrameActions;

        static Behaviours.Actions.Action s_OriginalAction;
        static Behaviours.Actions.Action s_SelectedAction;
        static Behaviours.Actions.Action[] s_Actions;

        static Action<Behaviours.Actions.Action> s_ActionPicked;

        void OnEnable()
        {
            if (s_ActionTextures == null)
            {
                s_ActionTextures = new Dictionary<string, Texture>();

                var iconFiles = Directory.GetFiles("Assets/LEGO/Gizmos/LEGO Behaviour Icons");

                foreach(var file in iconFiles)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture>(file);
                    if (texture)
                    {
                        if (texture.name.EndsWith("Action"))
                        {
                            var actionName = texture.name.Replace(" ", "");
                            s_ActionTextures.Add(actionName, texture);
                        }
                    }
                }
            }

            s_FrameActions = EditorPrefs.GetBool(editorPrefsKey, true);
        }

        public static ActionPicker Show(Behaviours.Actions.Action originalAction, Type actionType, Action<Behaviours.Actions.Action> actionPicked)
        {
            s_ActionPicked = actionPicked;
            s_OriginalAction = originalAction;
            s_SelectedAction = null;

            // Find all Actions, filter them to only include subtypes the specified type, and sort them by name.
            s_Actions = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Behaviours.Actions.Action>();
            var filteredActions = new List<Behaviours.Actions.Action>(s_Actions);
            filteredActions = filteredActions.FindAll(a => a.GetType().IsSubclassOf(actionType) || a.GetType() == actionType);
            filteredActions.Sort((a, b) => a.name.CompareTo(b.name));
            s_Actions = filteredActions.ToArray();

            var window = GetWindow<ActionPicker>(true, "Action Picker", true);

            return window;
        }

        void OnGUI()
        {
            if (s_ButtonStyle == null)
            {
                s_ButtonStyle = new GUIStyle(EditorStyles.miniButton);
                s_ButtonStyle.fixedWidth = 96;
                s_ButtonStyle.fixedHeight = 96;
                s_ButtonStyle.imagePosition = ImagePosition.ImageAbove;
                s_ButtonStyle.padding = new RectOffset(0, 0, 0, 10);
            }

            s_ActionPicked(s_OriginalAction);

            s_ScrollPosition = GUILayout.BeginScrollView(s_ScrollPosition, false, false);

            var buttonsPerRow = Mathf.FloorToInt((position.width - 3.0f) / (96.0f + 2.0f));

            var buttonCount = 0;
            foreach (var action in s_Actions)
            {
                if (buttonCount == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                var actionName = action.GetType().Name;
                var nicifiedActionName = ObjectNames.NicifyVariableName(actionName).Split(' ')[0];

                // Find the model name if contained in a model.
                var modelName = "";
                var model = action.GetComponentInParent<Model>();
                if (model)
                {
                    modelName = model.name;
                }

                // Generate button text from action name and model name.
                var buttonText = nicifiedActionName;
                if (modelName != "" && modelName != actionName)
                {
                    buttonText = modelName + "\n" + buttonText;
                }

                Texture actionTexture;
                var buttonPressed = false;

                if (s_ActionTextures.TryGetValue(actionName, out actionTexture))
                {
                    buttonPressed = GUILayout.Button(new GUIContent(buttonText, actionTexture), s_ButtonStyle);
                }
                else
                {
                    buttonPressed = GUILayout.Button(buttonText, s_ButtonStyle);
                }

                if (buttonPressed)
                {
                    s_SelectedAction = action;
                    Close();
                }
                else if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    s_ActionPicked(action);

                    // Frame the action.
                    if (s_FrameActions)
                    {
                        if (SceneView.lastActiveSceneView)
                        {
                            var brickBounds = action.GetBrickBounds();
                            brickBounds.Expand(5.0f);
                            SceneView.lastActiveSceneView.Frame(brickBounds, false);
                        }
                    }
                }

                buttonCount++;
                if (buttonCount == buttonsPerRow)
                {
                    buttonCount = 0;
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (buttonCount > 0)
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.BeginChangeCheck();
            s_FrameActions = EditorGUILayout.Toggle("Frame Actions", s_FrameActions);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(editorPrefsKey, s_FrameActions);
            }

            GUILayout.EndScrollView();
        }

        void OnLostFocus()
        {
            Close();    
        }

        void OnDestroy()
        {
            if (s_SelectedAction)
            {
                s_ActionPicked(s_SelectedAction);
            } else
            {
                s_ActionPicked(s_OriginalAction);
            }
        }
    }
}