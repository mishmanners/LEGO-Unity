// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{
    [CustomEditor(typeof(LEGOAsset), true)]
    public class LEGOAssetEditor : Editor
    {
        LEGOAsset asset;

        Texture image;

        GUIStyle boldLabel;
        GUIStyle normalLabel;
        GUIStyle imageStyle;

        void OnEnable()
        {
            asset = (LEGOAsset)target;
            asset.HideInInspector();

            try
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unity.lego.modelimporter/Textures/LEGO Logo.png");
                imageStyle = new GUIStyle() { fixedWidth = 64, fixedHeight = 64, margin = new RectOffset(0, 10, 0, 0) };

                normalLabel = new GUIStyle(EditorStyles.label);
                normalLabel.fontSize = 15;
                normalLabel.wordWrap = true;

                boldLabel = new GUIStyle(normalLabel);
                boldLabel.fontStyle = FontStyle.Bold;
            }
            catch (System.Exception) {}    //on script reload, EditorStyles.label might be null
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = true;
                GUILayout.Box(image, imageStyle);
                GUI.enabled = false;
                EditorGUILayout.BeginVertical();
                try
                {
                    EditorGUILayout.LabelField("LEGO Asset:", boldLabel);
                    EditorGUILayout.LabelField(asset.name, normalLabel);
                }
                catch (System.Exception) {}   //on script reload, EditorStyles.label might be null
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
