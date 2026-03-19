using UnityEngine;

[CreateAssetMenu(fileName = "NewAudioPreset", menuName = "Audio/Audio Preset")]
public class AudioPreset : ScriptableObject
{
    public AudioClip clip;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 1.0f;

    [Tooltip("Base pitch of the sound. 1.0 is normal speed.")]
    [Range(0.1f, 3.0f)] public float pitch = 1.0f;

    [Tooltip("How much the pitch can vary randomly (-value to +value). Set to 0 for no variation.")]
    [Range(0f, 0.5f)] public float pitchRandomness = 0.05f;

    public void Play(AudioSource source)
    {
        if (source == null || clip == null) return;
        float finalPitch = pitch + Random.Range(-pitchRandomness, pitchRandomness);

        source.pitch = finalPitch;
        source.PlayOneShot(clip, volume);
    }
}