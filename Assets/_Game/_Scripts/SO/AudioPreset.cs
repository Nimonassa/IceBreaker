using UnityEngine;

public enum ClipMode { Single, Multiple }
public enum PlayMode { Random, Sequential }

[CreateAssetMenu(fileName = "NewAudioPreset", menuName = "Audio/Audio Preset")]
public class AudioPreset : ScriptableObject
{
    [Header("Mode Selection")]
    public ClipMode clipMode = ClipMode.Single;

    // Single Clip Mode
    public AudioClip singleClip;

    // Multiple Clip Mode
    public AudioClip[] multipleClips;
    public PlayMode playMode = PlayMode.Random;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 1.0f;
    [Range(0.1f, 3.0f)] public float pitch = 1.0f;
    [Range(0f, 0.5f)] public float pitchRandomness = 0.05f;

    [System.NonSerialized] private int _currentIndex = 0;

    private void OnValidate()
    {
        // If switching to Multiple: automatically assign the single clip to the array if the array is empty
        if (clipMode == ClipMode.Multiple && singleClip != null)
        {
            if (multipleClips == null || multipleClips.Length == 0)
            {
                multipleClips = new AudioClip[] { singleClip };
            }
        }

        if (clipMode == ClipMode.Single && singleClip == null)
        {
            if (multipleClips != null && multipleClips.Length > 0)
            {
                singleClip = multipleClips[0];
            }
        }
    }

    public void Play(AudioSource source)
    {
        if (source == null) return;

        AudioClip clipToPlay = null;

        if (clipMode == ClipMode.Single)
        {
            clipToPlay = singleClip;
        }
        else if (clipMode == ClipMode.Multiple && multipleClips != null && multipleClips.Length > 0)
        {
            if (playMode == PlayMode.Sequential)
            {
                clipToPlay = multipleClips[_currentIndex];
                _currentIndex = (_currentIndex + 1) % multipleClips.Length;
            }
            else
            {
                clipToPlay = multipleClips[Random.Range(0, multipleClips.Length)];
            }
        }

        if (clipToPlay == null) return;

        float finalPitch = pitch + Random.Range(-pitchRandomness, pitchRandomness);
        source.pitch = finalPitch;
        source.PlayOneShot(clipToPlay, volume);
    }
}