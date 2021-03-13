// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.IO;
using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{

    public class CheckConnectivityFeatureLayer
    {
        static readonly string connectivityFeatureReceptorLayerAttempPrefsKey = "com.unity.lego.modelimporter.attemptCreatingMissingConnectivityFeatureReceptorLayer";
        static readonly string connectivityFeatureConnectorLayerAttempPrefsKey = "com.unity.lego.modelimporter.attemptCreatingMissingConnectivityFeatureConnectorLayer";

        [InitializeOnLoadMethod]
        static void DoCheckConnectivityFeatureLayer()
        {
            // Do not perform the check when playing.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var existingReceptorLayer = LayerMask.NameToLayer(Connection.connectivityReceptorLayerName);
            var existingConnectorLayer = LayerMask.NameToLayer(Connection.connectivityConnectorLayerName);
            var attempt = EditorPrefs.GetBool(connectivityFeatureReceptorLayerAttempPrefsKey, true);

            if(existingReceptorLayer == -1 && attempt)
            {
                EditorApplication.delayCall += CreateReceptorLayer;
            }

            attempt =  EditorPrefs.GetBool(connectivityFeatureConnectorLayerAttempPrefsKey, true);
            if(existingConnectorLayer == -1 && attempt)
            {
                EditorApplication.delayCall += CreateConnectorLayer;
            }
        }

        private static void ReportError(string layer, string prefsKey)
        {
            EditorUtility.DisplayDialog("Connectivity feature layer required by LEGO packages", "Could not set up layer used for connectivity features automatically. Please add a layer called '" + layer + "'", "Ok");
            EditorPrefs.SetBool(prefsKey, false);
        }

        private static void AddLayer(string layer, string prefsKey)
        {
            var tagManagerAsset = AssetDatabase.LoadAssetAtPath<Object>(Path.Combine("ProjectSettings", "TagManager.asset"));
            if (tagManagerAsset == null)
            {
                ReportError(layer, prefsKey);
                return;
            }

            SerializedObject tagManagerObject = new SerializedObject(tagManagerAsset);
            if (tagManagerObject == null)
            {
                ReportError(layer, prefsKey);
                return;
            }

            SerializedProperty layersProp = tagManagerObject.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray)
            {
                ReportError(layer, prefsKey);                                
                return;
            }            

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (layerProp.stringValue == layer)
                {
                    return;
                }
            }

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (layerProp.stringValue == "")
                {
                    layerProp.stringValue = layer;
                    EditorUtility.DisplayDialog("Connectivity feature layer required by LEGO packages", "Set up layer used for connectivity features called '" + layer + "' at index " + i, "Ok");
                    break;
                }
            }

            EditorPrefs.SetBool(prefsKey, true);

            tagManagerObject.ApplyModifiedProperties();
        }

        public static void CreateReceptorLayer()
        {
            AddLayer(Connection.connectivityReceptorLayerName, connectivityFeatureReceptorLayerAttempPrefsKey);
        }

        public static void CreateConnectorLayer()
        {
            AddLayer(Connection.connectivityConnectorLayerName, connectivityFeatureConnectorLayerAttempPrefsKey);
        }
    }
}