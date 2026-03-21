using UnityEngine;

public enum PlayMode { Random, Sequential }

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
    public AudioFilter advanced;

    private void ResetAdvancedSettings()
    {
        advanced = new AudioFilter();
    }

    public void LoadFromTemplate(AudioFilter templateSettings)
    {
        string json = JsonUtility.ToJson(templateSettings);
        advanced = JsonUtility.FromJson<AudioFilter>(json);
    }
}
