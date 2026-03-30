using UnityEngine;
using UnityEngine.Events;

public class GrannyLookAt : MonoBehaviour
{
    [HideInInspector] public Vector3 momentumOffset;

    [Header("Bones & Target")]
    public Transform spine;
    public Transform neck, head, target;
    public GrannyBreather breather;

    [Header("Speeds")]
    public float lookSpeed = 5f;
    public float spineSpeed = 1.5f;
    public float reactionSpeed = 3f;

    [Header("Reaction & FOV")]
    public float baseReactionDelay = 0.8f;
    public float speedSensitivity = 0.2f;
    [Range(0.01f, 1f)] public float peripheralAwareness = 0.2f;
    public float maxVisionAngle = 90f;

    [Header("Idle Behavior")]
    public Transform idleTarget;
    public float idleTransitionDelay = 2f;
    [Tooltip("How much her head dips/arcs when transitioning to the idle target. 1 = straight line, higher = heavier curved drop.")]
    public float idleCurvature = 1.6f;
    [Tooltip("How fast she moves her head when transitioning to the idle target. Keep this lower than reactionSpeed for a lazier feel.")]
    public float idleTransitionSpeed = 1f;

    [Header("Momentum & Rig")]
    [Range(0f, 1f)] public float momentumDampening = 0.75f;
    public Vector3 localForwardAxis = Vector3.up;

    [Header("Limits")]
    public Vector2 spineXLimits = new Vector2(-15f, 15f), spineZLimits = new Vector2(-10f, 15f);
    public Vector2 neckXLimits = new Vector2(-30f, 45f), headZLimits = new Vector2(-15f, 15f);

    [Header("Resistance"), Range(0f, 1f)]
    public float easeZone = 0.8f, pitchResistance = 0.8f, rollResistance = 0.2f;

    private Quaternion spineRest, neckRest, headRest;
    private Vector3 currentTargetPos, lastTargetPos, currentSpineOffset;
    private float delayTimer, lostTimer;
    private bool isTrackingActive = true;
    [SerializeField] UnityEvent coolevent;
    void Start()
    {
        spineRest = spine ? spine.localRotation : Quaternion.identity;
        neckRest = neck ? neck.localRotation : Quaternion.identity;
        headRest = head ? head.localRotation : Quaternion.identity;
        currentTargetPos = target ? target.position : (idleTarget ? idleTarget.position : transform.position);
        lastTargetPos = currentTargetPos;
    }

    public void TriggerTargetSpotted(Transform newT) { target = newT; isTrackingActive = false; delayTimer = baseReactionDelay; if (target) lastTargetPos = target.position; }
    public void TriggerTargetLost() { target = null; lostTimer = idleTransitionDelay; }

    void LateUpdate()
    {
        if (!neck || !head || (!target && !idleTarget)) return;

        // 1. FOV & Target Detection
        Vector3 bodyFwd = neck.parent ? neck.parent.TransformDirection(localForwardAxis) : transform.forward;
        float angle = target ? Vector3.Angle(bodyFwd, target.position - neck.position) : 180f;
        bool visible = target && angle <= maxVisionAngle;

        Vector3 goal = currentTargetPos;
        float activeVerticalBias = 1f;
        float activeTrackingSpeed = reactionSpeed;

        if (visible)
        {
            float speed = (target.position - lastTargetPos).magnitude / Time.deltaTime;
            lastTargetPos = target.position;
            lostTimer = idleTransitionDelay;

            if (!isTrackingActive)
            {
                delayTimer -= Time.deltaTime * (1f + speed * speedSensitivity) * Mathf.Lerp(1f, peripheralAwareness, angle / maxVisionAngle);
                if (delayTimer <= 0) isTrackingActive = true;
            }
            if (isTrackingActive) goal = target.position;
        }
        else if ((lostTimer -= Time.deltaTime) <= 0 && idleTarget)
        {
            goal = idleTarget.position;
            isTrackingActive = false;
            delayTimer = baseReactionDelay;

            activeVerticalBias = idleCurvature;
            activeTrackingSpeed = idleTransitionSpeed;
        }
        else
        {
            goal = lastTargetPos;
        }

        // 2. Gaze Strain 
        float sX = GetAng(neck, neckRest, neck.parent ? neck.parent.rotation : Quaternion.identity, Vector3.right, currentTargetPos);
        float sZ = GetAng(head, headRest, neck.rotation, Vector3.forward, currentTargetPos);
        float strain = Mathf.Min(GetRes(sX, neckXLimits, pitchResistance), GetRes(sZ, headZLimits, rollResistance));

        // Component-wise Lerp
        float t = Time.deltaTime * (activeTrackingSpeed * strain);
        currentTargetPos.y = Mathf.Lerp(currentTargetPos.y, goal.y, t * activeVerticalBias);
        currentTargetPos.x = Mathf.Lerp(currentTargetPos.x, goal.x, t);
        currentTargetPos.z = Mathf.Lerp(currentTargetPos.z, goal.z, t);

        // 3. Spine Rotation
        if (spine && breather)
        {
            Quaternion pRot = spine.parent ? spine.parent.rotation : Quaternion.identity;
            currentSpineOffset.x = Mathf.Lerp(currentSpineOffset.x, Mathf.Clamp(GetAng(spine, spineRest, pRot, Vector3.right, currentTargetPos), spineXLimits.x, spineXLimits.y), Time.deltaTime * spineSpeed);
            currentSpineOffset.z = Mathf.Lerp(currentSpineOffset.z, Mathf.Clamp(GetAng(spine, spineRest, pRot, Vector3.forward, currentTargetPos), spineZLimits.x, spineZLimits.y), Time.deltaTime * spineSpeed);
            breather.lookAtOffset = currentSpineOffset;
        }

        // 4. Neck & Head Application
        float nX = Mathf.Clamp(GetAng(neck, neckRest, neck.parent ? neck.parent.rotation : Quaternion.identity, Vector3.right, currentTargetPos), neckXLimits.x, neckXLimits.y);

        // --- RESTORED ORIGINAL DAMPENING LOGIC ---
        float maxNeckPitch = Mathf.Max(Mathf.Abs(neckXLimits.x), Mathf.Abs(neckXLimits.y));
        float pitchIntensity = maxNeckPitch > 0f ? Mathf.Abs(nX) / maxNeckPitch : 0f;
        float damp = 1f - (pitchIntensity * momentumDampening);

        neck.localRotation = Quaternion.Slerp(neck.localRotation, neckRest * Quaternion.Euler(nX, 0, 0) * Quaternion.Euler(momentumOffset * damp), Time.deltaTime * lookSpeed * GetRes(nX, neckXLimits, pitchResistance));

        float hZ = Mathf.Clamp(GetAng(head, headRest, neck.rotation, Vector3.forward, currentTargetPos), headZLimits.x, headZLimits.y);
        head.localRotation = Quaternion.Slerp(head.localRotation, headRest * Quaternion.Euler(0, 0, hZ), Time.deltaTime * lookSpeed * GetRes(hZ, headZLimits, rollResistance));
    }

    private float GetAng(Transform b, Quaternion r, Quaternion p, Vector3 a, Vector3 t)
    {
        Vector3 l = Quaternion.Inverse(p * r) * (t - b.position);
        return Vector3.SignedAngle(localForwardAxis, Vector3.ProjectOnPlane(l, a), a);
    }

    private float GetRes(float a, Vector2 l, float r)
    {
        float h = (l.y - l.x) * 0.5f;
        if (h <= 0) return 1f;
        float t = Mathf.Clamp01(Mathf.Abs(a - (l.x + l.y) * 0.5f) / h);
        return t > easeZone ? 1f - (Mathf.Pow((t - easeZone) / (1f - easeZone), 2f) * r) : 1f;
    }
    public void hello(int yo)
    {
        print($"number: {yo}");
    }
    public int xd()
    {
        return 1;
    }
}

