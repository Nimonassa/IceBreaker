using UnityEngine;
using System.Collections;

public class DialogueAudio : MonoBehaviour
{
    [Header("Settings")]
    public AudioSource audioSource; 

    [Header("Fade Settings")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.3f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;

    private Coroutine fadeRoutineA;
    private Coroutine fadeRoutineB;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        sourceA = audioSource;
        sourceB = gameObject.AddComponent<AudioSource>();

        SyncSettings(sourceA, sourceB);

        // Prepare initial volumes
        sourceA.volume = 0;
        sourceB.volume = 0;
        activeSource = sourceA;
    }

    private void SyncSettings(AudioSource from, AudioSource to)
    {
        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.spatialBlend = from.spatialBlend;
        to.priority = from.priority;
        to.pitch = from.pitch;
        to.panStereo = from.panStereo;
        to.reverbZoneMix = from.reverbZoneMix;
        to.dopplerLevel = from.dopplerLevel;
        to.spread = from.spread;
        to.rolloffMode = from.rolloffMode;
        to.minDistance = from.minDistance;
        to.maxDistance = from.maxDistance;

        to.playOnAwake = false;
        to.loop = false;
        from.playOnAwake = false;
        from.loop = false;
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            StopVoice();
            return;
        }

        AudioSource oldSource = activeSource;
        StopFade(oldSource);
        StartFade(oldSource, 0f, fadeOutDuration, () => oldSource.Stop());

        activeSource = (activeSource == sourceA) ? sourceB : sourceA;

        StopFade(activeSource);
        activeSource.clip = clip;
        activeSource.Play();
        StartFade(activeSource, 1f, fadeInDuration);
    }

    public void StopVoice()
    {
        if (activeSource == null) return;

        StopFade(activeSource);
        StartFade(activeSource, 0f, fadeOutDuration, () => activeSource.Stop());
    }

    private void StartFade(AudioSource source, float target, float duration, System.Action onComplete = null)
    {
        if (source == sourceA)
            fadeRoutineA = StartCoroutine(FadeRoutine(source, target, duration, onComplete));
        else
            fadeRoutineB = StartCoroutine(FadeRoutine(source, target, duration, onComplete));
    }

    private void StopFade(AudioSource source)
    {
        if (source == sourceA && fadeRoutineA != null)
        {
            StopCoroutine(fadeRoutineA);
            fadeRoutineA = null;
        }
        else if (source == sourceB && fadeRoutineB != null)
        {
            StopCoroutine(fadeRoutineB);
            fadeRoutineB = null;
        }
    }

    private IEnumerator FadeRoutine(AudioSource source, float target, float duration, System.Action onComplete)
    {
        float startVolume = source.volume;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, target, time / duration);
            yield return null;
        }

        source.volume = target;
        onComplete?.Invoke();
    }
}
