using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private AudioHighPassFilter highPass;
    private AudioDistortionFilter distortion;
    private AudioEchoFilter echo;
    
    private AudioPreset currentPreset;
    private int currentIndex = 0;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        lowPass = GetOrAddFilter<AudioLowPassFilter>();
        highPass = GetOrAddFilter<AudioHighPassFilter>();
        distortion = GetOrAddFilter<AudioDistortionFilter>();
        echo = GetOrAddFilter<AudioEchoFilter>();
        
        echo.decayRatio = 0f; 
    }

    private T GetOrAddFilter<T>() where T : Component
    {
        T filter = GetComponent<T>();
        if (filter == null) filter = gameObject.AddComponent<T>();
        return filter;
    }

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

        PlaySingleStrike(clipToPlay, preset);
    }


    private void PlaySingleStrike(AudioClip clip, AudioPreset preset)
    {
        // Base modifications
        source.pitch = preset.pitch + Random.Range(-preset.pitchRandomness, preset.pitchRandomness);
        
        float finalVolume = preset.volume + Random.Range(-preset.volumeRandomness, preset.volumeRandomness);
        finalVolume = Mathf.Clamp01(finalVolume);

        // 1. Panning 
        if (preset.advanced.enablePanRandomness)
        {
            source.panStereo = Random.Range(-preset.advanced.panRandomness, preset.advanced.panRandomness);
        }
        else 
        {
            source.panStereo = 0f; // Reset to center if disabled
        }

        // 2. Low Pass Filter
        if (preset.advanced.enableLowPass)
        {
            lowPass.enabled = true;
            lowPass.cutoffFrequency = Mathf.Clamp(preset.advanced.lowPassCutoff + Random.Range(-preset.advanced.lowPassRandomness, preset.advanced.lowPassRandomness), 10f, 22000f);
        }
        else lowPass.enabled = false;

        // 3. High Pass Filter
        if (preset.advanced.enableHighPass)
        {
            highPass.enabled = true;
            highPass.cutoffFrequency = Mathf.Clamp(preset.advanced.highPassCutoff + Random.Range(-preset.advanced.highPassRandomness, preset.advanced.highPassRandomness), 10f, 22000f);
        }
        else highPass.enabled = false;

        // 4. Distortion Filter
        if (preset.advanced.enableDistortion)
        {
            distortion.enabled = true;
            distortion.distortionLevel = Mathf.Clamp01(preset.advanced.distortionLevel + Random.Range(-preset.advanced.distortionRandomness, preset.advanced.distortionRandomness));
        }
        else distortion.enabled = false;

        // 5. Echo Filter
        if (preset.advanced.enableEcho)
        {
            echo.enabled = true;
            echo.delay = Mathf.Clamp(preset.advanced.microEchoDelay + Random.Range(-preset.advanced.microEchoRandomness, preset.advanced.microEchoRandomness), 1f, 300f);
            echo.wetMix = preset.advanced.microEchoMix;
        }
        else echo.enabled = false;


        source.clip = clip; 
        source.PlayOneShot(clip, finalVolume);
    }
}