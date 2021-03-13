#if UNITY_2020_1_OR_NEWER
[assembly: UnityEditor.Localization]
#elif UNITY_2019_3_OR_NEWER
[assembly: UnityEditor.Localization.Editor.Localization]
#endif

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// A helper class for Localization.
    /// </summary>
    // TODO 2.0 make private
    public static class Localization
    {
        /// <summary>
        /// Routes the call to the correct, or none, Tr() implementation, depending on the used Unity version.
        /// See https://docs.unity3d.com/ScriptReference/Localization.Editor.Localization.Tr.html.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Tr(string str)
#if UNITY_2020_1_OR_NEWER
            => UnityEditor.L10n.Tr(str);
#elif UNITY_2019_3_OR_NEWER
            => UnityEditor.Localization.Editor.Localization.Tr(str);
#else
            => str;
#endif
    }
}
