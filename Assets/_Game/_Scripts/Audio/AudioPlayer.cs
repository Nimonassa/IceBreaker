using UnityEngine;
using UnityEngine.Audio;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField, HideInInspector] public AudioRolloffMode lastRolloffMode = AudioRolloffMode.Logarithmic;
    private AudioPreset currentPreset;
    private int currentIndex = 0;

    [Header("Routing")]
    public AudioMixerGroup mixerGroup;

    [Header("Settings")]
    public bool mute = false;
    public float volume = 1.0f;
    public float pitch = 1.0f;
    public float spatialBlend = 1.0f;

    [Header("3D Sound Settings")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    public float minDistance = 1.0f;
    public float maxDistance = 50.0f;

    [Header("Volume Rolloff Graph")]
    public AnimationCurve rolloffCurve = new AnimationCurve();

    public void Play(AudioPreset preset)
    {
        if (preset == null || preset.clips.Length == 0) return;

        if (preset != currentPreset)
        {
            currentPreset = preset;
            currentIndex = 0;
        }

        AudioClip clipToPlay = (preset.playMode == PlayMode.Random)
            ? preset.clips[Random.Range(0, preset.clips.Length)]
            : preset.clips[currentIndex];

        if (preset.playMode == PlayMode.Sequential)
            currentIndex = (currentIndex + 1) % preset.clips.Length;

        AudioInstance instance = AudioPool.Get();
        instance.Play(clipToPlay, this, preset);
    }
}