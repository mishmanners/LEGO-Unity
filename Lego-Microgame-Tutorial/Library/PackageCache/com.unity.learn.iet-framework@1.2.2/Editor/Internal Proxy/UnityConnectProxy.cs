using UnityEditor.Connect;

namespace Unity.InteractiveTutorials
{
    public static class UnityConnectProxy
    {
        public static string GetUserId() => UnityConnect.instance.GetUserId();

        public static bool loggedIn => UnityConnect.instance.loggedIn;

        public static string GetAccessToken() => UnityConnect.instance.GetAccessToken();

        // NOTE no-op if user is not logged in
        public static void OpenAuthorizedURLInWebBrowser(string url) =>
            UnityConnect.instance.OpenAuthorizedURLInWebBrowser(url);
    }
}
