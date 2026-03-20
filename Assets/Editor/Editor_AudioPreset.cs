using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioPreset))]
public class AudioPresetEditor : Editor
{
    private GameObject previewObject;
    private AudioSource previewSource;

    private bool isLooping = false;
    private double nextPlayTime = 0;
    private float stepInterval = 0.5f; 
    

    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
    
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        
        CleanUpOrphans();
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        StopAndCleanUp();
    }
    
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            isLooping = false;
            StopAndCleanUp();
        }
    }

    private void CleanUpOrphans()
    {
        GameObject[] ghosts = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in ghosts)
        {
            if (go.name == "AudioPreview_Temp")
            {
                DestroyImmediate(go);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(15);
        
        AudioPreset preset = (AudioPreset)target;

        GUI.enabled = preset.clips != null && preset.clips.Length > 0;

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("▶ Play", GUILayout.Height(30)))
        {
            if (!isLooping) StopAndCleanUp(); 
            PlaySinglePreview(preset);
        }
        
        if (GUILayout.Button("■ Stop", GUILayout.Width(60), GUILayout.Height(30)))
        {
            isLooping = false;
            StopAndCleanUp();
        }

        isLooping = GUILayout.Toggle(isLooping, "Loop", "Button", GUILayout.Width(60), GUILayout.Height(30));
        
        EditorGUILayout.EndHorizontal();

        if (isLooping)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Trigger Every (sec):", GUILayout.Width(115));
            
            float maxSliderValue = 1.5f; 
            if (preset.clips != null)
            {
                foreach (AudioClip clip in preset.clips)
                {
                    if (clip != null && clip.length > maxSliderValue)
                    {
                        maxSliderValue = clip.length; 
                    }
                }
            }
            
            maxSliderValue = Mathf.Max(maxSliderValue, stepInterval);
            stepInterval = EditorGUILayout.Slider(stepInterval, 0.05f, maxSliderValue);
            
            if (GUILayout.Button("Fit to Clip", GUILayout.Width(75)))
            {
                float totalLength = 0f;
                int validClips = 0;
                foreach (AudioClip clip in preset.clips)
                {
                    if (clip != null)
                    {
                        totalLength += clip.length;
                        validClips++;
                    }
                }
                
                if (validClips > 0)
                {
                    stepInterval = totalLength / validClips;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Set the exact time between footsteps. If the time is shorter than the clip, it will aggressively cut off the sound.", MessageType.Info);
        }

        GUI.enabled = true; 
    }

    private void PlaySinglePreview(AudioPreset preset)
    {
        if (preset.clips == null || preset.clips.Length == 0) return;

        if (previewObject == null)
        {
            previewObject = EditorUtility.CreateGameObjectWithHideFlags("AudioPreview_Temp", HideFlags.HideAndDontSave);
            previewSource = previewObject.AddComponent<AudioSource>();
            previewObject.AddComponent<AudioLowPassFilter>();
            previewObject.AddComponent<AudioHighPassFilter>();
            previewObject.AddComponent<AudioDistortionFilter>();
            previewObject.AddComponent<AudioEchoFilter>();
        }

        AudioLowPassFilter lowPass = previewObject.GetComponent<AudioLowPassFilter>();
        AudioHighPassFilter highPass = previewObject.GetComponent<AudioHighPassFilter>();
        AudioDistortionFilter distortion = previewObject.GetComponent<AudioDistortionFilter>();
        AudioEchoFilter echo = previewObject.GetComponent<AudioEchoFilter>();

        AudioClip clipToPlay = preset.clips[Random.Range(0, preset.clips.Length)];
        previewSource.clip = clipToPlay;

        previewSource.pitch = preset.pitch + Random.Range(-preset.pitchRandomness, preset.pitchRandomness);
        previewSource.volume = Mathf.Clamp01(preset.volume + Random.Range(-preset.volumeRandomness, preset.volumeRandomness));
        
        if (preset.advanced.enablePanRandomness)
            previewSource.panStereo = Random.Range(-preset.advanced.panRandomness, preset.advanced.panRandomness);

        lowPass.enabled = preset.advanced.enableLowPass;
        if (lowPass.enabled) lowPass.cutoffFrequency = Mathf.Clamp(preset.advanced.lowPassCutoff + Random.Range(-preset.advanced.lowPassRandomness, preset.advanced.lowPassRandomness), 10f, 22000f);

        highPass.enabled = preset.advanced.enableHighPass;
        if (highPass.enabled) highPass.cutoffFrequency = Mathf.Clamp(preset.advanced.highPassCutoff + Random.Range(-preset.advanced.highPassRandomness, preset.advanced.highPassRandomness), 10f, 22000f);

        distortion.enabled = preset.advanced.enableDistortion;
        if (distortion.enabled) distortion.distortionLevel = Mathf.Clamp01(preset.advanced.distortionLevel + Random.Range(-preset.advanced.distortionRandomness, preset.advanced.distortionRandomness));

        echo.enabled = preset.advanced.enableEcho;
        if (echo.enabled)
        {
            echo.delay = Mathf.Clamp(preset.advanced.microEchoDelay + Random.Range(-preset.advanced.microEchoRandomness, preset.advanced.microEchoRandomness), 1f, 300f);
            echo.wetMix = preset.advanced.microEchoMix;
            echo.decayRatio = 0f; 
        }

        previewSource.Play();

        nextPlayTime = EditorApplication.timeSinceStartup + stepInterval;
    }

    private void UpdatePreview()
    {
        if (isLooping)
        {
            if (EditorApplication.timeSinceStartup >= nextPlayTime)
            {
                PlaySinglePreview((AudioPreset)target);
            }
        }
        else
        {
            if (previewObject != null && previewSource != null && !previewSource.isPlaying)
            {
                StopAndCleanUp();
            }
        }
    }

    private void StopAndCleanUp()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
            previewSource = null;
        }
    }
}