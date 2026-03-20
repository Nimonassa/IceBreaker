using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioPreset))]
public class AudioPresetEditor : Editor
{
    private GameObject previewObject;
    private AudioSource cachedSource;
    private AudioLowPassFilter cachedLP;
    private AudioHighPassFilter cachedHP;
    private AudioDistortionFilter cachedDist;
    private AudioEchoFilter cachedEcho;

    // The "Source of Truth" trackers
    private bool lastLP, lastHP, lastDist, lastEcho;

    private bool isLooping = false;
    private double nextPlayTime = 0;
    private float stepInterval = 0.5f;

    // Randomness storage so the "character" of the sound stays while sliding values
    private float curPitch, curVol, curPan, curLPRand, curHPRand, curDistRand, curEchoRand;

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
            if (go != null && go.name == "AudioPreview_Temp") DestroyImmediate(go);
        }
    }

    public override void OnInspectorGUI()
    {
        AudioPreset preset = (AudioPreset)target;

        // 1. Draw the UI
        DrawDefaultInspector();

        // 2. IMMEDIATE TOGGLE DETECTION
        // If the checkbox in the UI doesn't match our 'last' stored state, they clicked it.
        bool toggleDetected = (preset.advanced.enableLowPass != lastLP) ||
                              (preset.advanced.enableHighPass != lastHP) ||
                              (preset.advanced.enableDistortion != lastDist) ||
                              (preset.advanced.enableEcho != lastEcho);

        if (toggleDetected)
        {
            // Update our trackers immediately
            lastLP = preset.advanced.enableLowPass;
            lastHP = preset.advanced.enableHighPass;
            lastDist = preset.advanced.enableDistortion;
            lastEcho = preset.advanced.enableEcho;

            // If we are currently playing/looping, RESTART EVERYTHING
            if (previewObject != null || isLooping)
            {
                PlaySinglePreview(preset);
            }
        }
        else
        {
            // If no toggle, just update the slider values live (this works fine mid-play)
            if (previewObject != null && cachedSource != null && cachedSource.isPlaying)
            {
                ApplyValues(preset);
            }
        }

        EditorGUILayout.Space(15);
        GUI.enabled = preset.clips != null && preset.clips.Length > 0;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ Play", GUILayout.Height(30)))
        {
            isLooping = false; 
            PlaySinglePreview(preset);
        }
        if (GUILayout.Button("■ Stop", GUILayout.Width(60), GUILayout.Height(30)))
        {
            isLooping = false;
            StopAndCleanUp();
        }
        isLooping = GUILayout.Toggle(isLooping, "Loop", "Button", GUILayout.Width(60), GUILayout.Height(30));
        EditorGUILayout.EndHorizontal();

        if (isLooping) DrawLoopUI(preset);
        GUI.enabled = true;
    }

    private void DrawLoopUI(AudioPreset preset)
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Trigger Every (sec):", GUILayout.Width(115));
        float maxVal = 1.5f;
        if (preset.clips != null)
        {
            foreach (var c in preset.clips) if (c != null && c.length > maxVal) maxVal = c.length;
        }
        stepInterval = EditorGUILayout.Slider(stepInterval, 0.05f, Mathf.Max(maxVal, stepInterval));
        if (GUILayout.Button("Fit", GUILayout.Width(40)))
        {
            float total = 0; int count = 0;
            foreach (var c in preset.clips) if (c != null) { total += c.length; count++; }
            if (count > 0) stepInterval = total / count;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void PlaySinglePreview(AudioPreset preset)
    {
        // Force a total rebuild of the object to avoid MissingComponentExceptions
        StopAndCleanUp();
        EnsureRig();

        if (preset.clips == null || preset.clips.Length == 0) return;

        cachedSource.clip = preset.clips[Random.Range(0, preset.clips.Length)];

        // Roll new randomness
        curPitch = Random.Range(-preset.pitchRandomness, preset.pitchRandomness);
        curVol = Random.Range(-preset.volumeRandomness, preset.volumeRandomness);
        curPan = Random.Range(-preset.advanced.panRandomness, preset.advanced.panRandomness);
        curLPRand = Random.Range(-preset.advanced.lowPassRandomness, preset.advanced.lowPassRandomness);
        curHPRand = Random.Range(-preset.advanced.highPassRandomness, preset.advanced.highPassRandomness);
        curDistRand = Random.Range(-preset.advanced.distortionRandomness, preset.advanced.distortionRandomness);
        curEchoRand = Random.Range(-preset.advanced.microEchoRandomness, preset.advanced.microEchoRandomness);

        ApplyValues(preset);
        cachedSource.Play();
        nextPlayTime = EditorApplication.timeSinceStartup + stepInterval;
    }

    private void ApplyValues(AudioPreset preset)
    {
        // 1. Set Enabled States (Match AudioPlayer.cs exactly)
        cachedLP.enabled = preset.advanced.enableLowPass;
        cachedHP.enabled = preset.advanced.enableHighPass;
        cachedDist.enabled = preset.advanced.enableDistortion;
        cachedEcho.enabled = preset.advanced.enableEcho;

        // 2. Set Volume/Pitch/Pan
        cachedSource.volume = Mathf.Clamp01(preset.volume + curVol);
        cachedSource.pitch = preset.pitch + curPitch;
        cachedSource.panStereo = preset.advanced.enablePanRandomness ? curPan : 0f;

        // 3. Set Filter Math
        if (cachedLP.enabled) cachedLP.cutoffFrequency = Mathf.Clamp(preset.advanced.lowPassCutoff + curLPRand, 10f, 22000f);
        if (cachedHP.enabled) cachedHP.cutoffFrequency = Mathf.Clamp(preset.advanced.highPassCutoff + curHPRand, 10f, 22000f);
        if (cachedDist.enabled) cachedDist.distortionLevel = Mathf.Clamp01(preset.advanced.distortionLevel + curDistRand);
        if (cachedEcho.enabled)
        {
            cachedEcho.delay = Mathf.Clamp(preset.advanced.microEchoDelay + curEchoRand, 1f, 300f);
            cachedEcho.wetMix = preset.advanced.microEchoMix;
        }
    }

    private void UpdatePreview()
    {
        if (isLooping)
        {
            if (EditorApplication.timeSinceStartup >= nextPlayTime) PlaySinglePreview((AudioPreset)target);
        }
        else if (previewObject != null && cachedSource != null && !cachedSource.isPlaying)
        {
            StopAndCleanUp();
        }
    }

    private void EnsureRig()
    {
        // NUCLEAR OPTION: If anything is null, wipe and start over
        if (previewObject == null || cachedSource == null || cachedLP == null || cachedHP == null || cachedDist == null || cachedEcho == null)
        {
            StopAndCleanUp();
            previewObject = EditorUtility.CreateGameObjectWithHideFlags("AudioPreview_Temp", HideFlags.HideAndDontSave);
            cachedSource = previewObject.AddComponent<AudioSource>();
            cachedLP = previewObject.AddComponent<AudioLowPassFilter>();
            cachedHP = previewObject.AddComponent<AudioHighPassFilter>();
            cachedDist = previewObject.AddComponent<AudioDistortionFilter>();
            cachedEcho = previewObject.AddComponent<AudioEchoFilter>();
            cachedEcho.decayRatio = 0f; 
        }
    }

    private void StopAndCleanUp()
    {
        if (previewObject != null) DestroyImmediate(previewObject);
        previewObject = null; cachedSource = null; cachedLP = null; cachedHP = null; cachedDist = null; cachedEcho = null;
    }
}