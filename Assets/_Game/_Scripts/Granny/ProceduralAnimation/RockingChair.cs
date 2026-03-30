using UnityEngine;

public class RockingChair : MonoBehaviour
{
    public enum Axis { Forward, Right, Up }

    [Header("Transforms")]
    public Transform pivot, chair;

    [Header("Rocking Settings")]
    public Axis rockingAxis = Axis.Forward;
    public float rockSpeed = 2f;

    [Header("Angle Limits")]
    public float minAngle = -15f, maxAngle = 15f;

    [Header("Angle Variation (Organic Feel)")]
    public float minAngleVariation = 3f, maxAngleVariation = 3f, variationSpeed = 0.5f;

    public float CurrentPhase { get; private set; }

    // Changed to store local values relative to the pivot
    private Vector3 localOffset;
    private Quaternion localChairRotation;

    void Start()
    {
        if (!pivot || !chair) { Debug.LogWarning("RockingChair missing references!"); return; }

        // Capture offset and rotation in the pivot's local space so it survives parent rotation
        localOffset = pivot.InverseTransformDirection(chair.position - pivot.position);
        localChairRotation = Quaternion.Inverse(pivot.rotation) * chair.rotation;
    }

    void Update()
    {
        if (!pivot || !chair) return;

        Vector3 axisDir = rockingAxis == Axis.Forward ? pivot.forward : (rockingAxis == Axis.Right ? pivot.right : pivot.up);
        float t = Time.time * variationSpeed;

        float dynamicMin = minAngle + ((Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * minAngleVariation);
        float dynamicMax = maxAngle + ((Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * maxAngleVariation);

        CurrentPhase = Mathf.Sin(Time.time * rockSpeed);
        float currentAngle = Mathf.Lerp(dynamicMin, dynamicMax, (CurrentPhase + 1f) / 2f);

        // Convert the stored local values back into current world space
        Vector3 currentWorldOffset = pivot.TransformDirection(localOffset);
        Quaternion currentInitialChairRotation = pivot.rotation * localChairRotation;

        // Apply rocking rotation
        Quaternion rot = Quaternion.AngleAxis(currentAngle, axisDir);
        chair.SetPositionAndRotation(pivot.position + (rot * currentWorldOffset), rot * currentInitialChairRotation);
    }
}
