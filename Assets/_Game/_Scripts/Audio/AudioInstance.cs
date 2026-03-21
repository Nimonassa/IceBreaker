using UnityEngine;

public class AudioInstance : MonoBehaviour
{
    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private AudioHighPassFilter highPass;
    private AudioDistortionFilter distortion;
    private AudioEchoFilter echo;


    private float timer;
    private bool isPlaying;
    private AudioPlayer currentEmitter;

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
        currentEmitter = settings;
        transform.position = currentEmitter.transform.position;

        source.outputAudioMixerGroup = settings.mixerGroup;
        source.mute = currentEmitter.mute;
        
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

        timer = clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch));
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying) return;

        if (currentEmitter != null)
        {
            transform.position = currentEmitter.transform.position;
            source.mute = currentEmitter.mute;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            isPlaying = false;
            source.clip = null;
            currentEmitter = null;
            AudioPool.Return(this);
        }
    }
    
}

