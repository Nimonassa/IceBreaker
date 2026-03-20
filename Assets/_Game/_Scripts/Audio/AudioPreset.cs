using UnityEngine;

public enum PlayMode { Random, Sequential }

[System.Serializable]
public class AdvancedFilterSettings
{
    [Header("Stereo Panning")]
    public bool enablePanRandomness = false;
    [Range(0f, 0.5f)] public float panRandomness = 0.05f; // Slight stereo widening

    [Header("Low Pass Filter (Muffles the sound)")]
    public bool enableLowPass = false;
    [Range(10f, 22000f)] public float lowPassCutoff = 20000f; 
    [Range(0f, 5000f)] public float lowPassRandomness = 1500f;

    [Header("High Pass Filter (Removes bass/thud)")]
    public bool enableHighPass = false;
    [Range(10f, 22000f)] public float highPassCutoff = 50f;
    [Range(0f, 500f)] public float highPassRandomness = 30f; 

    [Header("Distortion Filter (Adds grit/dirt)")]
    public bool enableDistortion = false;
    [Range(0f, 1f)] public float distortionLevel = 0.02f;
    [Range(0f, 0.1f)] public float distortionRandomness = 0.02f; 
    
    [Header("Echo Filter (Comb filtering/Texture)")]
    public bool enableEcho = false;
    [Range(1f, 300f)] public float microEchoDelay = 12f; 
    [Range(0f, 50f)] public float microEchoRandomness = 5f;
    [Range(0f, 1f)] public float microEchoMix = 0.15f; 
}

[CreateAssetMenu(fileName = "NewAudioPreset", menuName = "Audio/Audio Preset")]
public class AudioPreset : ScriptableObject
{
    public AudioClip[] clips;
    public PlayMode playMode = PlayMode.Random;

    [Header("Base Settings")]
    [Range(0f, 1f)] public float volume = 1.0f;
    [Range(0f, 0.5f)] public float volumeRandomness = 0.05f; 
    
    [Range(0.1f, 3.0f)] public float pitch = 1.0f;
    [Range(0f, 0.5f)] public float pitchRandomness = 0.05f; 
    
    
    [Header("Advanced Settings")]
    [ContextMenuItem("Reset Advanced Settings to Defaults", "ResetAdvancedSettings")]
    public AdvancedFilterSettings advanced;

    private void ResetAdvancedSettings()
    {
        advanced = new AdvancedFilterSettings();
    }
}