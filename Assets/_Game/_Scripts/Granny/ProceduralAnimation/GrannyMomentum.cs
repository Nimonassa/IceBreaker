using UnityEngine;

public class GrannyMomentum : MonoBehaviour
{
    [Header("References")]
    public RockingChair chair;
    public GrannyBreather breather;
    public GrannyLookAt lookAt;

    [Header("Momentum Settings (Roll / Local Z)")]
    public float spineAmplitude = 2.5f;
    public float neckAmplitude = -0.5f;
    public float phaseDelay = 1.5f;

    [Header("Variation (Organic Feel)")]
    public float variationSpeed = 0.5f;

    private float noiseSeed;

    void Start() => noiseSeed = Random.Range(0f, 100f);

    void LateUpdate()
    {
        if (!chair) return;

        float momentumPhase = Mathf.Sin((Time.time * chair.rockSpeed) - phaseDelay);
        float randomizer = Mathf.PerlinNoise((Time.time * variationSpeed) + noiseSeed, 0f);

        if (breather) breather.momentumOffset = new Vector3(0f, 0f, momentumPhase * spineAmplitude * randomizer);
        if (lookAt) lookAt.momentumOffset = new Vector3(0f, 0f, momentumPhase * neckAmplitude * randomizer);
    }
}