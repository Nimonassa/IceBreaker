using UnityEngine;

public class GrannyLegSwing : MonoBehaviour
{
    public enum LegSide { Left, Right }
    public LegSide side;

    public RockingChair rockingChair;
    public Transform calf, foot;

    [Header("Swing Timing & Amplitude")]
    public float swingAmplitude = 10f, phaseOffset = 1.5f, phaseVariationAmplitude = 0.4f, phaseVariationSpeed = 0.2f;

    [Header("Pitch (Inward/Outward)")]
    public float pitchInwardAmplitude = 5f, pitchOutwardAmplitude = 2f, pitchVariationSpeed = 0.5f;
    public bool invertPitch;

    [Header("Lift & Drag")]
    public float liftAmplitude = 0.007f, liftVariationSpeed = 0.4f, footDragMultiplier = 0.3f;

    private Vector3 pivotCenter, colCenter;
    private Quaternion initCalfRot, initFootRot;
    private float noiseSeed;

    void Start()
    {
        noiseSeed = Random.value * 100f;
        if (calf)
        {
            initCalfRot = calf.localRotation;
            if (calf.TryGetComponent(out SphereCollider col))
            {
                colCenter = col.center;
                pivotCenter = calf.localPosition + (initCalfRot * colCenter);
            }
        }
        if (foot) initFootRot = foot.localRotation;
    }

    void LateUpdate()
    {
        if (!rockingChair || !calf) return;

        float t = Time.time;
        float currentPhase = phaseOffset + ((Mathf.PerlinNoise(t * phaseVariationSpeed + noiseSeed + 100f, 0f) - 0.5f) * 2f * phaseVariationAmplitude);
        float phase = Mathf.Sin(t * rockingChair.rockSpeed + currentPhase);

        float pitch = phase * (phase > 0 ? pitchInwardAmplitude : pitchOutwardAmplitude) * Mathf.PerlinNoise(t * pitchVariationSpeed + noiseSeed, 0f) * (invertPitch ? -1f : 1f);

        Quaternion newRot = initCalfRot * Quaternion.Euler(pitch, 0f, phase * swingAmplitude);
        calf.localRotation = newRot;

        float lift = Mathf.Max(0f, -phase) * liftAmplitude * Mathf.PerlinNoise(t * liftVariationSpeed + noiseSeed + 50f, 0f);
        calf.localPosition = pivotCenter - (newRot * colCenter) + (newRot * new Vector3(lift, 0f, 0f));

        if (foot)
            foot.localRotation = initFootRot * Quaternion.Euler(0f, 0f, -Mathf.Sin(t * rockingChair.rockSpeed + currentPhase - 0.2f) * swingAmplitude * footDragMultiplier);
    }
}