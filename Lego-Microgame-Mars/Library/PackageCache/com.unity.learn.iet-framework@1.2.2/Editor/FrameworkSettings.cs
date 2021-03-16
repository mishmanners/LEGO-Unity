using System.Linq;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    using static Localization;

    static class FrameworkSettings
    {
        const string k_PackageName = "com.unity.learn.iet-framework";
        static Settings s_Instance;

        static readonly float k_OriginalLabelWidth = EditorGUIUtility.labelWidth;

        internal static Settings Instance =>
            s_Instance = s_Instance ?? new Settings(k_PackageName);

        static readonly string k_Category = Tr("In-Editor Tutorials");

        [SettingsProviderGroup]
        static SettingsProvider[] CreateSettingsProviders()
        {
            // We need to add the name of the each setting on our own as keywords as we don't use the default
            // UserSettingsProvider. Add also "iet" shortcut, allowing "iet some setting" searches.
            var keywords = new[]
            {
                "iet",
                MaskingManager.MaskingEnabled.Name,
            };
            var userSettings = new SettingsProvider("Preferences/" + k_Category, SettingsScope.User, keywords)
            {
                guiHandler = DrawUserSettings,
            };

            // Uncomment this and implement DrawProjectSettings when we add Project Settings.
            // Remember to populate keywords.
            //var projectSettings = new SettingsProvider("Project/" + k_Category, SettingsScope.Project, keywords) { guiHandler = DrawProjectSettings }
            return new[] { userSettings, /*projectSettings*/ };
        }

        static void SetLabelWidth(float w) { EditorGUIUtility.labelWidth = w; }
        static void RestoreOriginalLabelWidth() { EditorGUIUtility.labelWidth = k_OriginalLabelWidth; }

        static bool DrawToggle(BaseSetting<bool> value, string searchContext)
        {
            return SettingsGUILayout.SettingsToggle(value.GetGuiContent(), value, searchContext);
        }

        static void DrawUserSettings(string searchContext)
        {
            SetLabelWidth(300);

            // Space and indentation to mimic the default settings GUI layout as closely as possible.
            EditorGUILayout.Space();

            using (new SettingsGUILayout.IndentedGroup())
            {
                MaskingManager.MaskingEnabled.value = DrawToggle(MaskingManager.MaskingEnabled, searchContext);
            }

            RestoreOriginalLabelWidth();
        }

        static void DrawProjectSettings(string searchContext)
        {
            SetLabelWidth(300);

            EditorGUILayout.Space();

            using (new SettingsGUILayout.IndentedGroup())
            {
                // Add Project Settings here
            }

            RestoreOriginalLabelWidth();
        }
    }
}
