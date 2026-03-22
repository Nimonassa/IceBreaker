using UnityEngine;

public static class AudioManager
{
    public static bool IsGloballyPaused => AudioListener.pause;
    public static void Pause()
    {
        AudioListener.pause = true;
    }
    public static void Unpause()
    {
        AudioListener.pause = false;
    }
}