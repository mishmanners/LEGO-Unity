using UnityEditor;
using UnityEngine;

public static class AudioUtilProxy
{
    public static void PlayClip(AudioClip audioClip)
    {
#if UNITY_2020_2_OR_NEWER
        AudioUtil.PlayPreviewClip(audioClip);
#else
        AudioUtil.PlayClip(audioClip);
#endif
    }
}
