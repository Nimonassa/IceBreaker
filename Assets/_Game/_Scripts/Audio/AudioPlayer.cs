using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField, HideInInspector] public AudioRolloffMode lastRolloffMode = AudioRolloffMode.Logarithmic;
    private AudioPreset currentPreset;
    private int currentIndex = 0;
    public bool IsPaused { get; private set; }

    private HashSet<AudioInstance> activeInstances = new HashSet<AudioInstance>();


    [Header("Settings")]
    public AudioMixerGroup mixerGroup;

    
    [field: SerializeField] public bool mute { get; private set; } = false;
    [field: SerializeField] public float volume { get; private set; }= 1.0f;
    [field: SerializeField] public float pitch { get; private set; }= 1.0f;
    [field: SerializeField] public float spatialBlend { get; private set; } = 1.0f;

    [Header("3D Sound Settings")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    public float minDistance = 1.0f;
    public float maxDistance = 50.0f;

    [Header("Volume Rolloff Graph")]
    public AnimationCurve rolloffCurve = new AnimationCurve();



    public void RegisterInstance(AudioInstance instance)
    {
        activeInstances.Add(instance);
    }
    public void UnregisterInstance(AudioInstance instance)
    {
        activeInstances.Remove(instance);
    }
    public void Awake()
    {
        AudioPool.Warmup();
    }

    public AudioHandle Play(AudioPreset preset)
    {
        if (IsPaused)
            return AudioHandle.Invalid;

        if (preset == null || preset.clips.Length == 0)
            return AudioHandle.Invalid;

        if (preset != currentPreset)
        {
            currentPreset = preset;
            currentIndex = 0;
        }

        AudioClip clipToPlay = (preset.playOrder == PlayOrder.Random)
            ? preset.clips[Random.Range(0, preset.clips.Length)]
            : preset.clips[currentIndex];

        if (preset.playOrder == PlayOrder.Sequential)
            currentIndex = (currentIndex + 1) % preset.clips.Length;

        AudioInstance instance = AudioPool.Get();
        instance.Play(clipToPlay, this, preset);

        return new AudioHandle(instance.PlaybackID);
    }
    public void PlayEvent(AudioPreset preset)
    {
        Play(preset);
    }

    public void Stop()
    {
        IsPaused = false;

        List<AudioInstance> instancesToStop = new List<AudioInstance>(activeInstances);
        foreach (var instance in instancesToStop)
        {
            if (instance != null)
            {
                instance.Stop();
            }
        }
        activeInstances.Clear();
    }

    public void Pause()
    {
        IsPaused = true;
        foreach (var instance in activeInstances)
        {
            instance?.Pause();
        }
    }

    public void Unpause()
    {
        IsPaused = false;
        foreach (var instance in activeInstances)
        {
            instance?.Unpause();
        }
    }

    public void StopSound(AudioHandle handle)
    {
        if (!handle.IsValid)
            return;

        foreach (var instance in activeInstances)
        {
            if (instance != null && instance.PlaybackID == handle.ID)
            {
                instance.Stop();
                break;
            }
        }
    }

    public void PauseSound(AudioHandle handle)
    {
        if (!handle.IsValid)
            return;

        foreach (var instance in activeInstances)
        {
            if (instance != null && instance.PlaybackID == handle.ID
            ){
                instance.Pause();
                break;
            }
        }
    }

    public void UnpauseSound(AudioHandle handle)
    {
        if (!handle.IsValid)
            return;

        foreach (var instance in activeInstances)
        {
            if (instance != null && instance.PlaybackID == handle.ID)
            {
                instance.Unpause();
                break;
            }
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = newVolume;
        foreach (var instance in activeInstances)
        {
            instance?.SetVolume(volume);
        }
    }

    public void SetPitch(float newPitch)
    {
        pitch = newPitch;
        foreach (var instance in activeInstances)
        {
            instance?.SetPitch(pitch);
        }
    }

    public void SetSpatialBlend(float newBlend)
    {
        spatialBlend = newBlend;
        foreach (var instance in activeInstances)
        {
            instance?.SetSpatialBlend(spatialBlend);
        }
    }

    public void SetMute(bool isMuted)
    {
        mute = isMuted;
        foreach (var instance in activeInstances)
        {
            instance?.SetMute(mute);
        }
    }

    public void SetRolloffMode(AudioRolloffMode mode)
    {
        rolloffMode = mode;
        foreach (var instance in activeInstances)
        {
            instance?.SetRolloffMode(rolloffMode);
        }
    }

    public void SetMinDistance(float min)
    {
        minDistance = min;
        foreach (var instance in activeInstances)
        {
            instance?.SetMinDistance(minDistance);
        }
    }

    public void SetMaxDistance(float max)
    {
        maxDistance = max;
        foreach (var instance in activeInstances)
        {
            instance?.SetMaxDistance(maxDistance);
        }
    }

    public void SetRolloffCurve(AnimationCurve curve)
    {
        rolloffCurve = curve;
        foreach (var instance in activeInstances)
        {
            instance?.SetRolloffCurve(rolloffCurve);
        }
    }

    public bool IsSoundActive(AudioHandle handle)
    {
        if (!handle.IsValid) return false;
        foreach (var instance in activeInstances)
            if (instance != null && instance.PlaybackID == handle.ID) return true;
        return false;
    }

    public void FadeTo(AudioHandle handle, float targetMultiplier, float duration, System.Action onComplete = null)
    {
        if (!handle.IsValid) return;
        foreach (var instance in activeInstances)
        {
            if (instance != null && instance.PlaybackID == handle.ID)
            {
                instance.FadeTo(targetMultiplier, duration, onComplete);
                break;
            }
        }
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            foreach (var instance in activeInstances)
            {
                if (instance != null)
                {
                    instance.SetVolume(volume);
                    instance.SetPitch(pitch);
                    instance.SetSpatialBlend(spatialBlend);
                    instance.SetMute(mute);
                    instance.SetRolloffMode(rolloffMode);
                    instance.SetMinDistance(minDistance);
                    instance.SetMaxDistance(maxDistance);

                    if (rolloffMode == AudioRolloffMode.Custom)
                        instance.SetRolloffCurve(rolloffCurve);
                    
                }
            }
        }
    }
#endif


    private void OnDestroy()
    {
        bool isSceneUnloading = !gameObject.scene.isLoaded;

        List<AudioInstance> instancesToProcess = new List<AudioInstance>(activeInstances);
        foreach (var instance in instancesToProcess)
        {
            if (instance == null) continue;

            if (isSceneUnloading)
            {
                instance.Stop();
                continue;
            }

            if (instance.CurrentPlayer != null && instance.CurrentPreset.playToCompletion)
            {
                instance.CurrentPlayer = null;
            }
            else
            {
                instance.Stop();
            }
        }

        activeInstances.Clear();
    }
}