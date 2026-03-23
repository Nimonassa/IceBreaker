using UnityEngine;

public class AudioInstance : MonoBehaviour
{
    public int PlaybackID { get; private set; }
    private static int globalIDCounter = 1;
    private int currentClipIndex = 0;

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private AudioHighPassFilter highPass;
    private AudioDistortionFilter distortion;
    private AudioEchoFilter echo;

    private float baseVolume;
    private float basePitch;
    private bool isPlaying;
    private bool isPaused;
    public bool IsPausable => CurrentPreset != null && CurrentPreset.isPausable;
    private Transform followTarget;
    public AudioPlayer CurrentPlayer;
    public AudioPreset CurrentPreset;

    private Coroutine fadeRoutine;
    private float currentFadeMultiplier = 1f;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.dopplerLevel = 0f;

        lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        highPass = gameObject.AddComponent<AudioHighPassFilter>();
        distortion = gameObject.AddComponent<AudioDistortionFilter>();
        echo = gameObject.AddComponent<AudioEchoFilter>();
    }

    private void Update()
    {
        if (!isPlaying || isPaused) return;

        if (AudioListener.pause && IsPausable)
            return;

        if (followTarget != null)
            transform.position = followTarget.position;

        if (!source.isPlaying)
        {
            if (CurrentPreset != null && CurrentPreset.isLooping)
            {
                if (CurrentPreset.playOrder == PlayOrder.Random)
                {
                    source.clip = CurrentPreset.clips[Random.Range(0, CurrentPreset.clips.Length)];
                }
                else
                {
                    currentClipIndex = (currentClipIndex + 1) % CurrentPreset.clips.Length;
                    source.clip = CurrentPreset.clips[currentClipIndex];
                }

                PlayCurrentClip();
            }
            else
            {
                Stop();
            }
        }
    }

    public void Play(AudioClip clip, AudioPlayer player, AudioPreset preset, bool follow = true)
    {
        PlaybackID = globalIDCounter++;
        if (globalIDCounter == int.MaxValue) globalIDCounter = 1;

        CurrentPlayer = player;
        CurrentPreset = preset;

        currentClipIndex = System.Array.IndexOf(preset.clips, clip);
        if (currentClipIndex == -1) currentClipIndex = 0;

        source.clip = clip;
        transform.position = CurrentPlayer.transform.position;
        followTarget = follow ? CurrentPlayer.transform : null;

        source.ignoreListenerPause = !preset.isPausable;
        source.outputAudioMixerGroup = player.mixerGroup;
        source.mute = CurrentPlayer.mute;

        source.spatialBlend = player.spatialBlend;
        source.rolloffMode = player.rolloffMode;
        source.minDistance = player.minDistance;
        source.maxDistance = player.maxDistance;

        if (player.rolloffMode == AudioRolloffMode.Custom)
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, player.rolloffCurve);

        if (CurrentPlayer != null)
            CurrentPlayer.RegisterInstance(this);

        isPlaying = true;
        isPaused = false;

        if (CurrentPlayer != null && CurrentPlayer.IsPaused)
            Pause();

        PlayCurrentClip();
    }

    private void PlayCurrentClip()
    {
        basePitch = CurrentPreset.pitch + Random.Range(-CurrentPreset.pitchRandomness, CurrentPreset.pitchRandomness);
        baseVolume = CurrentPreset.volume + Random.Range(-CurrentPreset.volumeRandomness, CurrentPreset.volumeRandomness);

        float playerVolume = CurrentPlayer != null ? CurrentPlayer.volume : 1f;
        float playerPitch = CurrentPlayer != null ? CurrentPlayer.pitch : 1f;

        source.pitch = basePitch * playerPitch;
        source.volume = Mathf.Clamp01(baseVolume * playerVolume);

        if (CurrentPreset.advanced.enablePanRandomness)
            source.panStereo = Random.Range(-CurrentPreset.advanced.panRandomness, CurrentPreset.advanced.panRandomness);
        else
            source.panStereo = 0f;

        lowPass.enabled = CurrentPreset.advanced.enableLowPass;
        if (lowPass.enabled) lowPass.cutoffFrequency = Mathf.Clamp(CurrentPreset.advanced.lowPassCutoff + Random.Range(-CurrentPreset.advanced.lowPassRandomness, CurrentPreset.advanced.lowPassRandomness), 10f, 22000f);

        highPass.enabled = CurrentPreset.advanced.enableHighPass;
        if (highPass.enabled) highPass.cutoffFrequency = Mathf.Clamp(CurrentPreset.advanced.highPassCutoff + Random.Range(-CurrentPreset.advanced.highPassRandomness, CurrentPreset.advanced.highPassRandomness), 10f, 22000f);

        distortion.enabled = CurrentPreset.advanced.enableDistortion;
        if (distortion.enabled) distortion.distortionLevel = Mathf.Clamp01(CurrentPreset.advanced.distortionLevel + Random.Range(-CurrentPreset.advanced.distortionRandomness, CurrentPreset.advanced.distortionRandomness));

        echo.enabled = CurrentPreset.advanced.enableEcho;
        if (echo.enabled)
        {
            echo.delay = Mathf.Clamp(CurrentPreset.advanced.microEchoDelay + Random.Range(-CurrentPreset.advanced.microEchoRandomness, CurrentPreset.advanced.microEchoRandomness), 1f, 300f);
            echo.wetMix = CurrentPreset.advanced.microEchoMix;
        }

        source.Play();
    }

    public void Stop()
    {
        if (!isPlaying) 
            return;

        if (source == null)
            return;

        if (fadeRoutine != null) 
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (CurrentPlayer != null)
        {
            CurrentPlayer.UnregisterInstance(this);
        }
        currentFadeMultiplier = 1f;
        source.Stop();
        isPlaying = false;
        isPaused = false;

        CurrentPlayer = null;
        followTarget = null;
        source.clip = null;

        source.rolloffMode = AudioRolloffMode.Logarithmic;

        if (this != null)
        {
            AudioPool.Return(this);
        }
    }

    public void Pause()
    {
        if (!isPlaying || isPaused) return;
        source.Pause();
        isPaused = true;
    }

    public void Unpause()
    {
        if (!isPlaying || !isPaused) return;
        source.UnPause();
        isPaused = false;
    }

    public void SetVolume(float playerVolume)
    {
        if (source != null)
            source.volume = Mathf.Clamp01(baseVolume * playerVolume * currentFadeMultiplier);
    }

    public void SetPitch(float playerPitch)
    {
        if (source != null)
            source.pitch = basePitch * playerPitch;
    }

    public void SetSpatialBlend(float blend)
    {
        if (source != null)
            source.spatialBlend = blend;
    }

    public void SetMute(bool isMuted)
    {
        if (source != null)
            source.mute = isMuted;
    }

    public void SetRolloffMode(AudioRolloffMode mode)
    {
        if (source != null)
            source.rolloffMode = mode;
    }

    public void SetMinDistance(float min)
    {
        if (source != null)
            source.minDistance = min;
    }

    public void SetMaxDistance(float max)
    {
        if (source != null)
            source.maxDistance = max;
    }

    public void SetRolloffCurve(AnimationCurve curve)
    {
        if (source != null)
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
    }

    public void FadeTo(float targetMultiplier, float duration, System.Action onComplete = null)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (gameObject.activeInHierarchy)
            fadeRoutine = StartCoroutine(FadeRoutine(targetMultiplier, duration, onComplete));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetMult, float duration, System.Action onComplete)
    {
        float startMult = currentFadeMultiplier;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            currentFadeMultiplier = Mathf.Lerp(startMult, targetMult, time / duration);
            SetVolume(CurrentPlayer != null ? CurrentPlayer.volume : 1f); // Update volume dynamically
            yield return null;
        }

        currentFadeMultiplier = targetMult;
        SetVolume(CurrentPlayer != null ? CurrentPlayer.volume : 1f);
        onComplete?.Invoke();
    }
}