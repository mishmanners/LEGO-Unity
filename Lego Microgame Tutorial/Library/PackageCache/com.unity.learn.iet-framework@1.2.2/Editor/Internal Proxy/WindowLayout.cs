using UnityEditor;

namespace Unity.InteractiveTutorials
{
    public static class WindowLayoutProxy
    {
        public static void SaveWindowLayout(string path)
        {
            WindowLayout.SaveWindowLayout(path);
        }
    }
}
