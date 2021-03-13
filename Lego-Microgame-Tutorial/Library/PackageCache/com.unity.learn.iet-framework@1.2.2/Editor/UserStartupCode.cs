using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Runs IET project initialization logic.
    /// </summary>
    [InitializeOnLoad]
    public static class UserStartupCode
    {
        internal static void RunStartupCode()
        {
            var projectSettings = TutorialProjectSettings.instance;
            if (projectSettings.initialScene != null)
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(projectSettings.initialScene));

            TutorialManager.WriteAssetsToTutorialDefaultsFolder();

            // Ensure Editor is in predictable state
            EditorPrefs.SetString("ComponentSearchString", string.Empty);
            Tools.current = Tool.Move;

            var readme = TutorialWindow.FindReadme();
            if (readme != null)
            {
                ShowTutorialWindow();
            }

            // NOTE camera settings can be applied successfully only after potential layout changes
            if (projectSettings.InitialCameraSettings != null && projectSettings.InitialCameraSettings.enabled)
                projectSettings.InitialCameraSettings.Apply();

            if (projectSettings.WelcomePage)
                TutorialModalWindow.TryToShow(projectSettings.WelcomePage, () => {});
        }

        /// <summary>
        /// Shows Tutorials window using the currently specified behaviour:
        /// - if TutorialContainer exists and TutorialContainer.ProjectLayout is specified,
        ///   the window is loaded and shown using the specified window layout (old behaviour), or
        /// - else the window is shown by anchoring and docking next to the Inspector (new behaviour).
        /// </summary>
        public static void ShowTutorialWindow()
        {
            var readme = TutorialWindow.FindReadme();
            if (readme == null || readme.ProjectLayout == null)
                TutorialWindow.CreateNextToInspector();
            else if (readme.ProjectLayout != null)
                TutorialWindow.CreateWindowAndLoadLayout();
        }

        internal static readonly string initFileMarkerPath = "InitCodeMarker";
        // Folder so that user can easily create this from the Editor's Project view.
        internal static readonly string dontRunInitCodeMarker = "Assets/DontRunInitCodeMarker";

        static UserStartupCode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || TutorialManager.IsLoadingLayout)
                return;

            // Language change triggers an assembly reload.
            if (LoadPreviousEditorLanguage() != LocalizationDatabaseProxy.currentEditorLanguage)
            {
                SaveCurrentEditorLanguage();
                // There are several smaller and bigger localization issues with if we don't restart
                // the Editor so let's query the user to do so.
                var title = Localization.Tr("Editor Language Change Detected");
                var msg = Localization.Tr("It's recommended to restart the Editor for the language change to be applied fully.");
                var ok = Localization.Tr("Restart");
                var cancel = Localization.Tr("Continue without restarting");
                if (EditorUtility.DisplayDialog(title, msg, ok, cancel))
                    RestartEditor();
            }

            EditorApplication.update += InitRunStartupCode;
        }

        static void InitRunStartupCode()
        {
            if (IsDontRunInitCodeMarkerSet())
                return;

            if (LocalizationDatabaseProxy.enableEditorLocalization && !IsLanguageInitialized())
            {
                // Need to Request a script reload in order overcome Editor Localization issues
                // with static initialization when opening the project for the first time.
                SetLanguageInitialized();
                EditorUtility.RequestScriptReload();
                return;
            }

            // Prepare the layout always. For example, the user might have moved the project around,
            // so we need to ensure the file paths in the layouts are correct.
            PrepareWindowLayouts();

            EditorApplication.update -= InitRunStartupCode;

            if (IsInitialized())
                return;

            SetInitialized();
            RunStartupCode();
        }

        /// <summary>
        /// Has the IET project initialization been performed?
        /// TODO 2.0 make private
        /// </summary>
        /// <returns></returns>
        public static bool IsInitialized() => File.Exists(initFileMarkerPath);

        static bool IsDontRunInitCodeMarkerSet() => Directory.Exists(dontRunInitCodeMarker);

        /// <summary>
        /// Marks the IET project initialization to be done.
        /// TODO 2.0 Make internal/private
        /// </summary>
        public static void SetInitialized() => File.CreateText(initFileMarkerPath).Close();

        static bool IsLanguageInitialized() => SessionState.GetBool("EditorLanguageInitialized", false);

        static void SetLanguageInitialized() => SessionState.SetBool("EditorLanguageInitialized", true);

        // Replaces LastProjectPaths in window layouts used in tutorials so that e.g.
        // pre-saved Project window states work correctly.
        static void PrepareWindowLayouts()
        {
            AssetDatabase.FindAssets($"t:{typeof(TutorialContainer).FullName}")
                .Select(guid =>
                    AssetDatabase.LoadAssetAtPath<TutorialContainer>(AssetDatabase.GUIDToAssetPath(guid)).ProjectLayoutPath
                )
                .Concat(
                    AssetDatabase.FindAssets($"t:{typeof(Tutorial).FullName}")
                        .Select(guid =>
                            AssetDatabase.LoadAssetAtPath<Tutorial>(AssetDatabase.GUIDToAssetPath(guid)).windowLayoutPath
                        )
                )
                .Where(StringExt.IsNotNullOrEmpty)
                .Distinct()
                .ToList()
                .ForEach(layoutPath => TutorialManager.PrepareWindowLayout(layoutPath));
        }

        static SystemLanguage LoadPreviousEditorLanguage() =>
            (SystemLanguage)EditorPrefs.GetInt("EditorLanguage", (int)SystemLanguage.English);

        static void SaveCurrentEditorLanguage() =>
            EditorPrefs.SetInt("EditorLanguage", (int)LocalizationDatabaseProxy.currentEditorLanguage);

        /// <summary>
        /// Restart the Editor.
        /// </summary>
        // TODO Make internal?
        public static void RestartEditor()
        {
            // In older versions, calling EditorApplication.OpenProject() while having unsaved modifications
            // can cause us to get stuck in a dialog loop. This seems to be fixed in 2020.1 (and newer?).
            // As a workaround, ask for saving before starting to restart the Editor for real. However,
            // we get the dialog twice and it can cause issues if user chooses first "Don't save" and then tries
            // to "Cancel" in the second dialog.
#if !UNITY_2020_1_OR_NEWER
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
#endif
            {
                EditorApplication.OpenProject(".");
            }
        }
    }
}
