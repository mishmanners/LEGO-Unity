using System.Collections;
using UnityEditor;

namespace Unity.LEGO.Dev
{
    /// <summary>
    /// Enables all scene effects (skybox, flares, etc..) at startup
    /// </summary>
    [InitializeOnLoad]
    public class SceneFxEnabler
    {
        const string k_SettingsKey = "AlreadyAutoEnabledEditorSceneFx";
        static SceneFxEnabler()
        {
            //if (EditorPrefs.GetBool(k_SettingsKey, false)) { return; }
            EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(EnableSceneFx());
        }

        static IEnumerator EnableSceneFx()
        {
            while (SceneView.sceneViews.Count < 1)
            {
                yield return null;
            }
            (SceneView.sceneViews[0] as SceneView).sceneViewState.SetAllEnabled(true);
            EditorPrefs.SetBool(k_SettingsKey, true); 
        }
    }
}
