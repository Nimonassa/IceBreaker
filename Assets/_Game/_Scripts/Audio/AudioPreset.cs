using UnityEngine;

public enum PlayOrder { Random, Sequential }

[CreateAssetMenu(fileName = "NewAudioPreset", menuName = "Audio/Audio Preset")]
public class AudioPreset : ScriptableObject
{
    public AudioClip[] clips;
    public PlayOrder playOrder = PlayOrder.Random;

    [Header("Base Settings")]
    public bool isPausable = true;
    public bool isLooping = false;
    [Tooltip("If true, this sound will finish playing even if the object that started it is destroyed.")]
    public bool playToCompletion = false;
    [Space(10)]

    [Range(0f, 1f)] public float volume = 1.0f;
    [Range(0f, 0.5f)] public float volumeRandomness = 0.05f;

    [Range(0.1f, 3.0f)] public float pitch = 1.0f;
    [Range(0f, 0.5f)] public float pitchRandomness = 0.05f;


    [Header("Advanced Settings")]
    [ContextMenuItem("Reset Advanced Settings to Defaults", "ResetAdvancedSettings")]
    public AudioFilter advanced = new();

    private void ResetAdvancedSettings()
    {
        advanced = new AudioFilter();
    }

    public void LoadFromTemplate(AudioFilter templateSettings)
    {
        advanced.CopyFrom(templateSettings);
    }
}
