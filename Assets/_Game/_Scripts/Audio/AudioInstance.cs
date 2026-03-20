using UnityEngine;

public class AudioInstance : MonoBehaviour
{
    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private AudioHighPassFilter highPass;
    private AudioDistortionFilter distortion;
    private AudioEchoFilter echo;

    // Lightweight timer variables to replace heavy Coroutines
    private float _timer;
    private bool _isPlaying;

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

    public void Play(AudioClip clip, AudioPlayer settings, AudioPreset preset)
    {
        transform.position = settings.transform.position;

        // Native 3D Audio Settings
        source.spatialBlend = settings.spatialBlend;
        source.rolloffMode = settings.rolloffMode;
        source.minDistance = settings.minDistance;
        source.maxDistance = settings.maxDistance;

        if (settings.rolloffMode == AudioRolloffMode.Custom)
        {
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, settings.rolloffCurve);
        }

        // Base Randomization
        source.pitch = preset.pitch + Random.Range(-preset.pitchRandomness, preset.pitchRandomness);
        float baseVol = preset.volume + Random.Range(-preset.volumeRandomness, preset.volumeRandomness);
        source.volume = Mathf.Clamp01(baseVol * settings.volume);

        if (preset.advanced.enablePanRandomness)
            source.panStereo = Random.Range(-preset.advanced.panRandomness, preset.advanced.panRandomness);
        else
            source.panStereo = 0f;

        // --- DIRECT FILTER APPLICATION (Eliminates Type-Casting Overhead) ---

        lowPass.enabled = preset.advanced.enableLowPass;
        if (lowPass.enabled)
            lowPass.cutoffFrequency = Mathf.Clamp(preset.advanced.lowPassCutoff + Random.Range(-preset.advanced.lowPassRandomness, preset.advanced.lowPassRandomness), 10f, 22000f);

        highPass.enabled = preset.advanced.enableHighPass;
        if (highPass.enabled)
            highPass.cutoffFrequency = Mathf.Clamp(preset.advanced.highPassCutoff + Random.Range(-preset.advanced.highPassRandomness, preset.advanced.highPassRandomness), 10f, 22000f);

        distortion.enabled = preset.advanced.enableDistortion;
        if (distortion.enabled)
            distortion.distortionLevel = Mathf.Clamp01(preset.advanced.distortionLevel + Random.Range(-preset.advanced.distortionRandomness, preset.advanced.distortionRandomness));

        echo.enabled = preset.advanced.enableEcho;
        if (echo.enabled)
        {
            echo.delay = Mathf.Clamp(preset.advanced.microEchoDelay + Random.Range(-preset.advanced.microEchoRandomness, preset.advanced.microEchoRandomness), 1f, 300f);
            echo.wetMix = preset.advanced.microEchoMix;
        }

        source.clip = clip;
        source.Play();

        _timer = clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch));
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _isPlaying = false;
            source.clip = null;
            AudioPool.Return(this);
        }
    }
}
