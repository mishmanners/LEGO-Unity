using UnityEditor;
using UnityEngine;

public static class HandleUtilityProxy
{
    public static GameObject FindSelectionBase(GameObject gameObject)
    {
#if UNITY_2020_2_OR_NEWER
        return HandleUtility.FindSelectionBaseForPicking(gameObject);
#else
        return HandleUtility.FindSelectionBase(gameObject);
#endif
    }
}
