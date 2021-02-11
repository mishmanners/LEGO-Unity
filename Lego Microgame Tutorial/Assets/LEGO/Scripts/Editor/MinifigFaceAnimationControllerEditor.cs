using UnityEditor;
using UnityEngine;
using Unity.LEGO.Minifig;

namespace Unity.LEGO.EditorExt
{

    [CustomEditor(typeof(MinifigFaceAnimationController))]
    public class MinifigFaceAnimationControllerEditor : Editor
    {
        MinifigFaceAnimationController controller;
        SerializedProperty animationsProp;

        void OnEnable()
        {
            controller = (MinifigFaceAnimationController)target;
            animationsProp = serializedObject.FindProperty("animations");
        }

        public override void OnInspectorGUI()
        {
            if (animationsProp.arraySize == 0)
            {
                GUILayout.Label("No animations prepared");
            }
            else
            {
                for (var i = 0; i < animationsProp.arraySize; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(ObjectNames.NicifyVariableName(((MinifigFaceAnimationController.FaceAnimation)animationsProp.GetArrayElementAtIndex(i).enumValueIndex).ToString()));
                    EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                    if (GUILayout.Button(new GUIContent("Play", !Application.isPlaying ? "Only works in Play Mode" : "")))
                    {
                        controller.PlayAnimation((MinifigFaceAnimationController.FaceAnimation)(animationsProp.GetArrayElementAtIndex(i).enumValueIndex));
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

}