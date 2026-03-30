using UnityEngine;

public class GrannyBreather : MonoBehaviour
{
    [HideInInspector] public Vector3 momentumOffset;
    [HideInInspector] public Vector3 lookAtOffset; // NEW: Receives tracking rotation from GrannyLookAt

    [Header("Rhythm")]
    public float breathsPerMinute = 12f;
    [Range(0.1f, 3f)] public float breathSmoothness = 1.5f;

    [Header("Bone Target")] public Transform spine;

    [Header("Local Directional Movement (Inhale)")]
    public Vector3 rotationOffset = new Vector3(0f, 0f, -3f);
    public Vector3 positionOffset = new Vector3(0.1f, 0.02f, 0.00f);
    public Vector3 scaleOffset = Vector3.zero;

    private Vector3 initialPos, initialScale;
    private Quaternion initialRot;

    void Start()
    {
        if (!spine) return;
        initialPos = spine.localPosition;
        initialRot = spine.localRotation;
        initialScale = spine.localScale;
    }

    void LateUpdate()
    {
        if (!spine) return;

        float breathT = Mathf.Pow((Mathf.Sin(Time.time * (breathsPerMinute / 60f) * Mathf.PI * 2f) + 1f) / 2f, breathSmoothness);
        Quaternion breathRot = Quaternion.Euler(rotationOffset * breathT);

        // Layers Initial Pose -> Breath -> Momentum -> LookAt Target
        spine.localRotation = initialRot * breathRot * Quaternion.Euler(momentumOffset) * Quaternion.Euler(lookAtOffset);
        spine.localPosition = initialPos + (rotationOffset.sqrMagnitude > 0 ? (breathRot * (positionOffset * breathT)) : (positionOffset * breathT));
        spine.localScale = initialScale + (scaleOffset * breathT);
    }
}