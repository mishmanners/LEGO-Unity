using UnityEngine;
using UnityEditor;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Proxy class for accessing UnityEditor.LocalizationDatabase.
    /// </summary>
    public class LocalizationDatabaseProxy
    {
        /// <summary>
        /// Is Editor Localization enabled.
        /// </summary>
        public static bool enableEditorLocalization
        {
            get => LocalizationDatabase.enableEditorLocalization;
            set => LocalizationDatabase.enableEditorLocalization = value;
        }

        /// <summary>
        /// Returns the current Editor language.
        /// </summary>
        public static SystemLanguage currentEditorLanguage =>
            LocalizationDatabase.currentEditorLanguage;

        /// <summary>
        /// Returns available Editor languages.
        /// </summary>
        public static SystemLanguage[] GetAvailableEditorLanguages() =>
            LocalizationDatabase.GetAvailableEditorLanguages();
    }
}
