
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField, HideInInspector] private AudioRolloffMode lastRolloffMode = AudioRolloffMode.Logarithmic;
    private AudioPreset currentPreset;
    private int currentIndex = 0;

    [Header("Settings")]
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

        AudioPool.Get().Play(clipToPlay, this, preset);
    }


    private void OnValidate()
    {
        if (rolloffMode != lastRolloffMode)
        {
            switch (rolloffMode)
            {
                case AudioRolloffMode.Custom: GenerateCustomCurve(); break;
                case AudioRolloffMode.Logarithmic: GenerateLogarithmicCurve(); break;
                case AudioRolloffMode.Linear: GenerateLinearCurve(); break;
            }
            lastRolloffMode = rolloffMode;
        }

        if (rolloffMode == AudioRolloffMode.Custom) minDistance = 0f;
        else if (minDistance < 0.001f) minDistance = 0.001f;

        if (maxDistance <= minDistance) maxDistance = minDistance + 0.1f;
        
        if (rolloffMode != AudioRolloffMode.Custom)
        {
            if (rolloffMode == AudioRolloffMode.Logarithmic) GenerateLogarithmicCurve();
            else GenerateLinearCurve();
        }
    }

    private void GenerateCustomCurve()
    {
        rolloffCurve.keys = new Keyframe[0];
        rolloffCurve.AddKey(new Keyframe(0f, 1f));
        rolloffCurve.AddKey(new Keyframe(1f, 0f));
    }

    private void GenerateLinearCurve()
    {
        rolloffCurve.keys = new Keyframe[0];
        float normMin = minDistance / maxDistance;
        float slope = -1f / (1f - normMin); // Normalized slope
        rolloffCurve.AddKey(new Keyframe(normMin, 1f, 0f, slope));
        rolloffCurve.AddKey(new Keyframe(1f, 0f, slope, 0f));
    }

    private void GenerateLogarithmicCurve()
    {
        rolloffCurve.keys = new Keyframe[0];
        float safeMin = Mathf.Max(minDistance, 0.001f);
        float ratio = maxDistance / safeMin;
        int steps = Mathf.CeilToInt(Mathf.Log(ratio, 2f));
        float multiplier = Mathf.Pow(ratio, 1f / steps);

        for (int i = 0; i <= steps; i++)
        {
            float dist = safeMin * Mathf.Pow(multiplier, i);
            if (i == steps) dist = maxDistance;

            float vol = safeMin / dist;
            float t = dist / maxDistance; // Normalized time

            // Calculate normalized tangent: f'(t) = -min / (t^2 * max)
            float slope = -safeMin / (t * t * maxDistance);
            
            rolloffCurve.AddKey(new Keyframe(t, vol, slope, slope));
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
#endif
}