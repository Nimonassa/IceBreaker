using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BreakableIceZone))]
public class BreakableIceWaterRecovery : MonoBehaviour, IBreakableIcePostBreakHandler
{
    private static readonly WaitForEndOfFrame EndOfFrame = new();

    private sealed class HandPullState
    {
        public Transform handTransform;
        public bool isAnchored;
        public Vector3 anchorWorldPosition;
        public GripFrame anchorGripFrame;
        public Vector3 plantedStrokeDirection;
        public Vector3 previousHandWorldPosition;
        public Vector3 smoothedPullMotion;

        public HandPullState(Transform transform)
        {
            handTransform = transform;
        }
    }

    private struct GripFrame
    {
        public Vector3 anchorWorldPosition;
        public Vector3 outward;
        public Vector3 sideways;
    }

    [Header("Recovery Exit")]
    [Tooltip("Optional custom point used as the final assisted pull-out destination. Leave empty to derive it automatically from the broken edge.")]
    [SerializeField] private Transform recoveryExitPoint;
    [Tooltip("Extra distance beyond the broken edge used for the final assisted pull onto the ice and to extend the outer recovery grab area.")]
    [SerializeField, Min(0.05f)] private float recoveryPullOutDistance = 0.36f;
    [Tooltip("How long the assisted pull from the water onto the ice takes once the player has climbed high enough.")]
    [SerializeField, Min(0.05f)] private float recoveryPullDuration = 0.4f;
    [Tooltip("Extra distance added around the ice volume where the player must keep crawling before normal control returns.")]
    [SerializeField, Min(0f)] private float recoveryCrawlAreaExtension = 0.6f;
    [Tooltip("Approximate head height above the ice while the player is still in the forced crawl phase after getting out of the water.")]
    [SerializeField, Min(0f)] private float recoveryCrawlHeadHeightAboveSurface = 0.14f;
    [Tooltip("How long it takes to raise the player back to the normal standing rig height after they clear the crawl area.")]
    [SerializeField, Min(0.01f)] private float recoveryStandUpDuration = 0.22f;

    [Header("Grip Detection")]
    [Tooltip("How far inward from the broken rim the hand grip band sits.")]
    [SerializeField, Min(0f)] private float recoveryHandleInset = 0.06f;
    [Tooltip("How much horizontal tolerance around the broken rim counts as a valid grip zone for starting a stroke.")]
    [SerializeField, Min(0.05f)] private float recoveryGripBandWidth = 0.28f;
    [Tooltip("How far below the ice surface the hands can still catch the grip band.")]
    [SerializeField, Min(0.05f)] private float recoveryGripHeightBelowSurface = 0.42f;
    [Tooltip("How far above the ice surface the grip band still counts as a valid pull zone.")]
    [SerializeField, Min(0f)] private float recoveryGripHeightAboveSurface = 0.12f;
    [Tooltip("Extra sideways drift allowed after a hand has armed a stroke so the pull is not cancelled too easily.")]
    [SerializeField, Min(0f)] private float recoveryArmedGripDriftAllowance = 0.18f;
    [Tooltip("Extra vertical slack allowed after a hand has armed a stroke before the grip is released.")]
    [SerializeField, Min(0f)] private float recoveryArmedGripHeightSlack = 0.12f;
    [Tooltip("How close to the ice surface the hand must be to arm a new recovery stroke.")]
    [SerializeField, Min(0.01f)] private float recoveryStrokeArmHeightBelowSurface = 0.14f;
    [Tooltip("How far the hand must pull down from its armed position to trigger one recovery stroke.")]
    [SerializeField, Min(0.02f)] private float recoveryStrokePullDownDistance = 0.14f;
    [Tooltip("How far the hand must lift back toward the surface before the next stroke can arm again.")]
    [SerializeField, Min(0.01f)] private float recoveryStrokeRearmRiseDistance = 0.07f;
    [Tooltip("How much outward body movement one valid recovery stroke applies.")]
    [SerializeField, Min(0.01f)] private float recoveryStrokeForward = 0.1f;
    [Tooltip("Minimum total recovery motion that must be accumulated before the player is allowed to finish climbing out.")]
    [SerializeField, Min(0f)] private float recoveryMinProgressToExit = 0.16f;
    [Tooltip("Minimum number of valid pull strokes required before the player can finish climbing out.")]
    [SerializeField, Min(1)] private int recoveryRequiredPulls = 2;

    [Header("Pull Feel")]
    [Tooltip("How quickly the planted-pick pull catches up to the controller motion. Higher values feel snappier, lower values feel smoother.")]
    [SerializeField, Min(1f)] private float recoveryPullSmoothing = 14f;
    [Tooltip("How much of the height difference to the planted hand is mixed into the pull direction.")]
    [SerializeField, Range(0f, 1f)] private float recoveryPullUpBias = 0.35f;
    [Tooltip("Extra radial slack around the broken edge that still lets a hand catch and snap an ice-pick anchor onto the rim.")]
    [SerializeField, Min(0f)] private float recoveryAnchorCatchSlack = 0.1f;
    [Tooltip("Small pull amount ignored to reduce controller jitter when the hand is planted.")]
    [SerializeField, Min(0f)] private float recoveryPullDeadzone = 0.012f;
    [Tooltip("How much player body motion is applied from one meter of valid hand pull away from the planted pick.")]
    [SerializeField, Min(0.1f)] private float recoveryHandMotionToBodyScale = 2.25f;

    [Header("Debug")]
    [Tooltip("Draws the grip area gizmo in the Scene view when this object is selected.")]
    [SerializeField] private bool showDebugGripZone = true;
    [Tooltip("Draws the grip area gizmo even when the object is not selected, which is useful while tuning the recovery setup.")]
    [SerializeField] private bool showDebugGripZoneAlways = true;
    [Tooltip("How smooth the debug grip-area gizmo appears in the Scene view.")]
    [SerializeField, Range(12, 128)] private int debugGripSegments = 48;
    [Tooltip("Extra upward offset applied to the debug grip gizmo so it is easier to see above the ice surface.")]
    [SerializeField, Min(0f)] private float debugGripVerticalOffset = 0.03f;
    [Tooltip("Size of the debug marker spheres used to highlight the grip band in the Scene view.")]
    [SerializeField, Min(0.001f)] private float debugGripMarkerRadius = 0.02f;
    [Tooltip("Also draws the crawl-area boundary in the Scene view for debugging.")]
    [SerializeField] private bool showDebugCrawlArea = true;
    [Tooltip("Shows text labels for the debug reference markers in the Scene view.")]
    [SerializeField] private bool showDebugReferenceLabels = true;

    private BreakableIceZone iceZone;
    private Camera headCamera;
    private float accumulatedRecoveryProgress;
    private int validPullCount;
    private Vector3 lastRecoveryStrokeDirection;

    private void Awake()
    {
        AutoAssignReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences();
    }
#endif

    public bool CanHandlePostBreak(BreakableIceZone zone, PlayerManager player)
    {
        return zone != null && player != null && (iceZone == null || zone == iceZone);
    }

    public IEnumerator HandlePostBreak(BreakableIceZone zone, PlayerManager player)
    {
        if (zone == null || player == null)
            yield break;

        CacheHeadCamera(player);

        CharacterController controller = player.GetComponent<CharacterController>();
        bool restoreControllerCollisions = false;
        if (controller != null)
        {
            restoreControllerCollisions = controller.detectCollisions;
            controller.detectCollisions = false;
        }

        ContinuousMoveProvider continuousMove = player.GetComponentInChildren<ContinuousMoveProvider>(true);
        bool gravityLocked = continuousMove != null && continuousMove.TryLockGravity(GravityOverride.ForcedOff);

        if (player.Movement != null)
        {
            player.Movement.SetMoveInputEnabled(false);
            player.Movement.SetTurnInputEnabled(true);
        }

        HandPullState leftHand = new(player.LeftHand != null ? player.LeftHand.GetRecoveryPullPoint() : null);
        HandPullState rightHand = new(player.RightHand != null ? player.RightHand.GetRecoveryPullPoint() : null);
        accumulatedRecoveryProgress = 0f;
        validPullCount = 0;
        lastRecoveryStrokeDirection = Vector3.zero;
        bool isCrawlingOnIce = false;
        float recoveryWaterRootY = player.transform.position.y;
        float recoveryMinHeadWorldY = headCamera != null ? headCamera.transform.position.y : float.NaN;

        while (player != null)
        {
            CacheHeadCamera(player);

            Vector3 motion = Vector3.zero;
            UpdateHandPull(zone, player, leftHand, isCrawlingOnIce, ref motion);
            UpdateHandPull(zone, player, rightHand, isCrawlingOnIce, ref motion);

            if (motion.sqrMagnitude > 0f)
                player.transform.position += motion;

            if (!isCrawlingOnIce)
                PreventAdditionalWaterSink(player, recoveryWaterRootY, recoveryMinHeadWorldY);

            if (!isCrawlingOnIce)
            {
                bool canExitWater = accumulatedRecoveryProgress >= recoveryMinProgressToExit;
                if (canExitWater && TryGetRecoveryExitPose(zone, player, out Vector3 targetPosition))
                {
                    yield return PullPlayerOntoIce(player, targetPosition);
                    isCrawlingOnIce = true;
                    RestoreOnIceLocomotion(player);
                    ResetHandState(leftHand);
                    ResetHandState(rightHand);
                }
            }
            else if (HasClearedCrawlArea(zone, player))
            {
                yield return StandPlayerUp(player, zone);
                break;
            }

            yield return EndOfFrame;
        }

        if (gravityLocked && continuousMove != null)
            continuousMove.RemoveGravityLock();

        if (controller != null)
            controller.detectCollisions = restoreControllerCollisions;
    }

    private void AutoAssignReferences()
    {
        if (iceZone == null)
            iceZone = GetComponent<BreakableIceZone>();

        if (recoveryExitPoint == null)
        {
            Transform found = transform.Find("RecoveryExitPoint");
            if (found != null)
                recoveryExitPoint = found;
        }
    }

    private void CacheHeadCamera(PlayerManager player)
    {
        if (player != null)
            headCamera = player.GetComponentInChildren<Camera>(true);

        if (headCamera == null)
            headCamera = Camera.main;
    }

    private void UpdateHandPull(
        BreakableIceZone zone,
        PlayerManager player,
        HandPullState handState,
        bool isCrawlingOnIce,
        ref Vector3 accumulatedMotion)
    {
        if (zone == null || player == null || handState == null || handState.handTransform == null)
            return;

        Vector3 handWorldPosition = handState.handTransform.position;
        bool isInsideGripBand = TryGetGripFrame(zone, player, handWorldPosition, isCrawlingOnIce, out GripFrame gripFrame);
        if (!isInsideGripBand)
        {
            if (!TryRetainAnchor(zone, handState, handWorldPosition, out gripFrame))
            {
                ResetHandState(handState);
                return;
            }
        }

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
        {
            ResetHandState(handState);
            return;
        }

        float surfaceWorldY = zone.transform.TransformPoint(surfaceCenter).y;
        bool nearSurfaceEnoughToAnchor = handWorldPosition.y >= surfaceWorldY - recoveryStrokeArmHeightBelowSurface;

        if (!handState.isAnchored)
        {
            if (!nearSurfaceEnoughToAnchor)
                return;

            handState.isAnchored = true;
            handState.anchorWorldPosition = gripFrame.anchorWorldPosition;
            handState.anchorGripFrame = gripFrame;
            handState.plantedStrokeDirection = gripFrame.outward.sqrMagnitude >= 0.0001f
                ? gripFrame.outward.normalized
                : Vector3.zero;
            handState.previousHandWorldPosition = handWorldPosition;
            handState.smoothedPullMotion = Vector3.zero;
            return;
        }

        Vector3 handMotion = handState.previousHandWorldPosition - handWorldPosition;
        handState.previousHandWorldPosition = handWorldPosition;
        Vector3 planarHandMotion = Vector3.ProjectOnPlane(handMotion, Vector3.up);
        float effectiveMotionDeadzone = Mathf.Min(recoveryPullDeadzone, 0.004f);
        float planarMotionMagnitude = planarHandMotion.magnitude;
        Vector3 targetPullMotion = Vector3.zero;
        if (planarMotionMagnitude > effectiveMotionDeadzone)
            targetPullMotion = planarHandMotion * ((planarMotionMagnitude - effectiveMotionDeadzone) / planarMotionMagnitude);

        float smoothingT = 1f - Mathf.Exp(-Mathf.Max(1f, recoveryPullSmoothing) * Time.deltaTime);
        handState.smoothedPullMotion = Vector3.Lerp(handState.smoothedPullMotion, targetPullMotion, smoothingT);
        if (handState.smoothedPullMotion.sqrMagnitude <= 0.0000001f)
            return;

        Vector3 recoveryMotion = ComputeRecoveryMotion(zone, player, handState, handState.smoothedPullMotion, handState.anchorGripFrame, out Vector3 strokeDirection);
        if (recoveryMotion.sqrMagnitude <= 0f)
            return;

        accumulatedMotion += recoveryMotion;
        Vector3 outwardDirection = handState.anchorGripFrame.outward.sqrMagnitude >= 0.0001f
            ? handState.anchorGripFrame.outward.normalized
            : strokeDirection;
        if (outwardDirection.sqrMagnitude >= 0.0001f)
            lastRecoveryStrokeDirection = outwardDirection;
        else
            lastRecoveryStrokeDirection = strokeDirection;

        if (!isCrawlingOnIce)
        {
            Vector3 planarRecoveryMotion = Vector3.ProjectOnPlane(recoveryMotion, Vector3.up);
            float outwardProgress = outwardDirection.sqrMagnitude >= 0.0001f
                ? Mathf.Max(0f, Vector3.Dot(planarRecoveryMotion, outwardDirection))
                : planarRecoveryMotion.magnitude;
            accumulatedRecoveryProgress += outwardProgress;
        }
    }

    private bool TryGetGripFrame(
        BreakableIceZone zone,
        PlayerManager player,
        Vector3 handWorldPosition,
        bool isCrawlingOnIce,
        out GripFrame gripFrame)
    {
        gripFrame = default;

        if (zone == null || player == null || !zone.HasBreakImpactPoint)
            return false;

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out float intactWidth, out float intactDepth))
            return false;

        float surfaceWorldY = zone.transform.TransformPoint(surfaceCenter).y;
        if (handWorldPosition.y < surfaceWorldY - recoveryGripHeightBelowSurface ||
            handWorldPosition.y > surfaceWorldY + recoveryGripHeightAboveSurface)
        {
            return false;
        }

        if (isCrawlingOnIce && HasClearedCrawlArea(zone, player))
            return false;

        Vector2 handLocal2D = WorldToSurfaceLocalPoint(zone.transform, handWorldPosition, surfaceCenter);
        float outerGripExtension = Mathf.Max(0f, recoveryPullOutDistance);
        float boundarySlack = recoveryGripBandWidth * 0.5f + recoveryAnchorCatchSlack + outerGripExtension;
        float halfWidth = intactWidth * 0.5f + boundarySlack;
        float halfDepth = intactDepth * 0.5f + boundarySlack;
        if (Mathf.Abs(handLocal2D.x) > halfWidth || Mathf.Abs(handLocal2D.y) > halfDepth)
            return false;

        Vector2 outward2D = handLocal2D - zone.LastBreakImpactLocalPoint;
        float currentDistance = outward2D.magnitude;
        if (currentDistance < 0.0001f)
            return false;

        Vector2 anchorDirection2D = outward2D / currentDistance;
        float edgeAngle = Mathf.Atan2(outward2D.y, outward2D.x);
        float edgeRadius = zone.EvaluateBreakEdgeRadius(edgeAngle);
        float bandHalfWidth = recoveryGripBandWidth * 0.5f + recoveryAnchorCatchSlack;
        float gripBandCenter = Mathf.Max(0.01f, edgeRadius - recoveryHandleInset);
        float minGripDistance = Mathf.Max(0.01f, gripBandCenter - bandHalfWidth);
        float maxGripDistance = gripBandCenter + bandHalfWidth + outerGripExtension;
        if (currentDistance < minGripDistance || currentDistance > maxGripDistance)
            return false;

        float anchorDistance = Mathf.Clamp(currentDistance, minGripDistance, maxGripDistance);
        Vector2 anchorLocal2D = zone.LastBreakImpactLocalPoint + anchorDirection2D * anchorDistance;
        gripFrame.anchorWorldPosition = zone.transform.TransformPoint(new Vector3(
            surfaceCenter.x + anchorLocal2D.x,
            surfaceCenter.y,
            surfaceCenter.z + anchorLocal2D.y));

        Vector3 outward = zone.transform.TransformVector(new Vector3(anchorDirection2D.x, 0f, anchorDirection2D.y));
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f)
            return false;

        outward.Normalize();
        gripFrame.outward = outward;
        gripFrame.sideways = Vector3.Cross(Vector3.up, outward).normalized;
        return true;
    }

    private Vector3 ComputeRecoveryMotion(
        BreakableIceZone zone,
        PlayerManager player,
        HandPullState handState,
        Vector3 pullMotionWorld,
        GripFrame gripFrame,
        out Vector3 strokeDirection)
    {
        strokeDirection = Vector3.zero;

        Vector3 planarPullMotion = Vector3.ProjectOnPlane(pullMotionWorld, Vector3.up);
        if (planarPullMotion.sqrMagnitude <= 0.0000001f)
            return Vector3.zero;

        float effectivePullScale = Mathf.Max(1.75f, recoveryHandMotionToBodyScale);
        Vector3 recoveryMotion = planarPullMotion * effectivePullScale;
        strokeDirection = recoveryMotion.normalized;
        return recoveryMotion;
    }

    private Vector3 ResolveCurrentStrokeDirection(
        PlayerManager player,
        HandPullState handState,
        GripFrame gripFrame)
    {
        Vector3 fallbackDirection = Vector3.zero;
        if (handState != null && handState.plantedStrokeDirection.sqrMagnitude >= 0.0001f)
            fallbackDirection = handState.plantedStrokeDirection.normalized;
        else if (gripFrame.outward.sqrMagnitude >= 0.0001f)
            fallbackDirection = gripFrame.outward.normalized;

        if (handState?.handTransform == null)
            return fallbackDirection;

        Vector3 referencePosition = GetRecoveryReferenceWorldPosition(player);
        Vector3 towardHand = Vector3.ProjectOnPlane(handState.handTransform.position - referencePosition, Vector3.up);
        Vector3 candidateDirection = towardHand.sqrMagnitude >= 0.0001f
            ? towardHand.normalized
            : fallbackDirection;

        if (gripFrame.outward.sqrMagnitude < 0.0001f)
        {
            if (handState != null)
                handState.plantedStrokeDirection = candidateDirection.sqrMagnitude >= 0.0001f ? candidateDirection.normalized : Vector3.zero;

            return handState != null ? handState.plantedStrokeDirection : Vector3.zero;
        }

        float outwardAmount = Mathf.Max(0f, Vector3.Dot(candidateDirection, gripFrame.outward));
        float sidewaysAmount = Vector3.Dot(candidateDirection, gripFrame.sideways);
        Vector3 correctedDirection = gripFrame.outward * outwardAmount + gripFrame.sideways * sidewaysAmount;
        if (correctedDirection.sqrMagnitude < 0.0001f)
            correctedDirection = fallbackDirection.sqrMagnitude >= 0.0001f ? fallbackDirection : gripFrame.outward;

        correctedDirection.y = 0f;
        correctedDirection = correctedDirection.sqrMagnitude >= 0.0001f
            ? correctedDirection.normalized
            : gripFrame.outward.normalized;

        if (handState != null)
            handState.plantedStrokeDirection = correctedDirection;

        return correctedDirection;
    }

    private Vector3 GetPullEffortDirection(
        PlayerManager player,
        HandPullState handState,
        GripFrame gripFrame,
        Vector3 strokeDirection)
    {
        if (strokeDirection.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        Vector3 effortDirection = strokeDirection;
        if (handState == null || recoveryPullUpBias <= 0f)
            return effortDirection.normalized;

        Vector3 referencePosition = GetRecoveryReferenceWorldPosition(player);
        Vector3 towardAnchor = handState.anchorWorldPosition - referencePosition;
        if (towardAnchor.sqrMagnitude < 0.0001f && handState.handTransform != null)
            towardAnchor = handState.handTransform.position - referencePosition;
        if (towardAnchor.sqrMagnitude < 0.0001f)
            towardAnchor = gripFrame.outward;

        float upwardPullWeight = Mathf.Max(0f, Vector3.Dot(towardAnchor.normalized, Vector3.up)) * recoveryPullUpBias;
        if (upwardPullWeight <= 0f)
            return effortDirection.normalized;

        effortDirection = strokeDirection + Vector3.up * upwardPullWeight;
        return effortDirection.sqrMagnitude >= 0.0001f ? effortDirection.normalized : strokeDirection.normalized;
    }

    private bool TryRetainAnchor(
        BreakableIceZone zone,
        HandPullState handState,
        Vector3 handWorldPosition,
        out GripFrame gripFrame)
    {
        gripFrame = default;

        if (zone == null || handState == null || !handState.isAnchored)
            return false;

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
            return false;

        float surfaceWorldY = zone.transform.TransformPoint(surfaceCenter).y;
        float minY = surfaceWorldY - recoveryGripHeightBelowSurface - recoveryArmedGripHeightSlack - recoveryStrokePullDownDistance;
        float maxY = surfaceWorldY + recoveryGripHeightAboveSurface + recoveryArmedGripHeightSlack;
        if (handWorldPosition.y < minY || handWorldPosition.y > maxY)
            return false;

        Vector3 anchorOffset = handWorldPosition - handState.anchorWorldPosition;
        Vector3 planarAnchorOffset = Vector3.ProjectOnPlane(anchorOffset, Vector3.up);
        float maxPlanarDrift = Mathf.Max(
            recoveryArmedGripDriftAllowance * 2f,
            recoveryGripBandWidth + recoveryArmedGripDriftAllowance + recoveryAnchorCatchSlack);
        if (planarAnchorOffset.sqrMagnitude > maxPlanarDrift * maxPlanarDrift)
            return false;

        gripFrame = handState.anchorGripFrame;
        return gripFrame.outward.sqrMagnitude >= 0.0001f;
    }

    private static void ResetHandState(HandPullState handState)
    {
        if (handState == null)
            return;

        handState.isAnchored = false;
        handState.anchorWorldPosition = Vector3.zero;
        handState.anchorGripFrame = default;
        handState.plantedStrokeDirection = Vector3.zero;
        handState.previousHandWorldPosition = Vector3.zero;
        handState.smoothedPullMotion = Vector3.zero;
    }

    private static void RestoreOnIceLocomotion(PlayerManager player)
    {
        if (player == null)
            return;

        player.LeftHand?.SetInteractionActive(true);
        player.RightHand?.SetInteractionActive(true);
        player.LeftHand?.SetGrabRayActive(true);
        player.RightHand?.SetGrabRayActive(true);

        if (player.Grabbing != null)
            player.Grabbing.UpdateSettings();

        if (player.Movement != null)
        {
            player.Movement.SetLocomotionInputEnabled(true);
            player.Movement.RefreshLocomotionState();
        }
    }

    private void PreventAdditionalWaterSink(PlayerManager player, float waterRootY, float minHeadWorldY)
    {
        if (player == null)
            return;

        if (headCamera != null && !float.IsNaN(minHeadWorldY))
        {
            float headSinkDelta = minHeadWorldY - headCamera.transform.position.y;
            if (headSinkDelta > 0.0001f)
            {
                player.transform.position += Vector3.up * headSinkDelta;
                return;
            }
        }

        Vector3 position = player.transform.position;
        if (position.y >= waterRootY - 0.0001f)
            return;
        position.y = waterRootY;
        player.transform.position = position;
    }

    private bool TryGetRecoveryExitPose(BreakableIceZone zone, PlayerManager player, out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (zone == null || player == null || !zone.HasBreakImpactPoint)
            return false;

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
            return false;

        if (!HasReachedWaterExitThreshold(zone, player))
            return false;

        // Preserve the player's horizontal placement and facing when the assisted crawl state begins.
        targetPosition = player.transform.position;
        targetPosition.y = ComputeCrawlRootY(player, surfaceCenter.y);
        return true;
    }

    private bool HasReachedWaterExitThreshold(BreakableIceZone zone, PlayerManager player)
    {
        if (zone == null || player == null)
            return false;

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
            return false;

        Vector2 playerLocal2D = WorldToSurfaceLocalPoint(zone.transform, GetRecoveryReferenceWorldPosition(player), surfaceCenter);
        Vector2 playerDirection = playerLocal2D - zone.LastBreakImpactLocalPoint;
        if (playerDirection.sqrMagnitude < 0.0001f)
            return false;

        float angle = Mathf.Atan2(playerDirection.y, playerDirection.x);
        float edgeRadius = zone.EvaluateBreakEdgeRadius(angle);
        float requiredDistance = Mathf.Max(0.05f, edgeRadius - recoveryHandleInset - recoveryGripBandWidth * 0.25f);
        return Vector2.Distance(playerLocal2D, zone.LastBreakImpactLocalPoint) >= requiredDistance;
    }

    private bool HasClearedCrawlArea(BreakableIceZone zone, PlayerManager player)
    {
        if (zone == null || player == null)
            return false;

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out float intactWidth, out float intactDepth))
            return false;

        Vector2 playerLocal2D = WorldToSurfaceLocalPoint(zone.transform, GetRecoveryReferenceWorldPosition(player), surfaceCenter);
        float halfWidth = intactWidth * 0.5f + recoveryCrawlAreaExtension;
        float halfDepth = intactDepth * 0.5f + recoveryCrawlAreaExtension;
        return Mathf.Abs(playerLocal2D.x) > halfWidth || Mathf.Abs(playerLocal2D.y) > halfDepth;
    }

    private IEnumerator StandPlayerUp(PlayerManager player, BreakableIceZone zone)
    {
        if (player == null || zone == null)
            yield break;

        if (!zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
            yield break;

        Vector3 startPosition = player.transform.position;
        Vector3 targetPosition = startPosition;
        targetPosition.y = ComputeStandingRootY(player, surfaceCenter.y);

        if (Mathf.Abs(targetPosition.y - startPosition.y) <= 0.001f)
        {
            player.transform.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, recoveryStandUpDuration);
        while (elapsed < duration)
        {
            if (player == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            player.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        player.transform.position = targetPosition;
    }

    private IEnumerator PullPlayerOntoIce(PlayerManager player, Vector3 targetPosition)
    {
        if (player == null)
            yield break;

        CharacterController controller = player.GetComponent<CharacterController>();
        bool restoreControllerCollisions = false;
        if (controller != null)
        {
            restoreControllerCollisions = controller.detectCollisions;
            controller.detectCollisions = false;
        }

        Vector3 startPosition = player.transform.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, recoveryPullDuration);

        while (elapsed < duration)
        {
            if (player == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            player.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        player.transform.position = targetPosition;

        if (controller != null)
            controller.detectCollisions = restoreControllerCollisions;
    }

    private static Vector2 WorldToSurfaceLocalPoint(Transform zoneTransform, Vector3 worldPosition, Vector3 surfaceCenter)
    {
        Vector3 localPoint = zoneTransform.InverseTransformPoint(worldPosition);
        return new Vector2(localPoint.x - surfaceCenter.x, localPoint.z - surfaceCenter.z);
    }

    private Vector3 GetStrokeDirection(
        BreakableIceZone zone,
        PlayerManager player,
        HandPullState handState,
        GripFrame gripFrame)
    {
        if (handState != null)
        {
            Vector3 resolvedDirection = ResolveCurrentStrokeDirection(player, handState, gripFrame);
            if (resolvedDirection.sqrMagnitude >= 0.0001f)
                return resolvedDirection.normalized;
        }

        Vector3 referencePosition = GetRecoveryReferenceWorldPosition(player);

        if (handState != null)
        {
            // Pull the rig toward the planted pick, but stay on the ice plane.
            Vector3 towardAnchor = handState.anchorWorldPosition - referencePosition;
            Vector3 horizontalTowardAnchor = Vector3.ProjectOnPlane(towardAnchor, Vector3.up);
            if (horizontalTowardAnchor.sqrMagnitude >= 0.0001f)
                return horizontalTowardAnchor.normalized;
        }

        if (handState?.handTransform != null)
        {
            Vector3 towardHand = handState.handTransform.position - referencePosition;
            Vector3 horizontalTowardHand = Vector3.ProjectOnPlane(towardHand, Vector3.up);
            if (horizontalTowardHand.sqrMagnitude >= 0.0001f)
                return horizontalTowardHand.normalized;
        }

        if (gripFrame.outward.sqrMagnitude >= 0.0001f)
            return gripFrame.outward.normalized;

        if (zone != null)
        {
            Vector3 fallbackForward = zone.transform.forward;
            fallbackForward = Vector3.ProjectOnPlane(fallbackForward, Vector3.up);
            if (fallbackForward.sqrMagnitude >= 0.0001f)
                return fallbackForward.normalized;
        }

        return Vector3.zero;
    }

    private Vector2 GetRecoveryDirectionLocal(BreakableIceZone zone, PlayerManager player)
    {
        if (zone == null)
            return Vector2.zero;

        if (lastRecoveryStrokeDirection.sqrMagnitude >= 0.0001f)
        {
            Vector3 lastLocalDirection = zone.transform.InverseTransformDirection(lastRecoveryStrokeDirection);
            Vector2 lastLocal2D = new(lastLocalDirection.x, lastLocalDirection.z);
            if (lastLocal2D.sqrMagnitude >= 0.0001f)
                return lastLocal2D.normalized;
        }

        if (player != null && zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
        {
            Vector2 playerLocalDirection = WorldToSurfaceLocalPoint(zone.transform, GetRecoveryReferenceWorldPosition(player), surfaceCenter) - zone.LastBreakImpactLocalPoint;
            if (playerLocalDirection.sqrMagnitude >= 0.0001f)
                return playerLocalDirection.normalized;
        }

        CacheHeadCamera(player);

        Vector3 strokeDirection = headCamera != null ? headCamera.transform.forward : Vector3.zero;
        if (strokeDirection.sqrMagnitude < 0.0001f && player != null)
            strokeDirection = player.transform.forward;
        strokeDirection = Vector3.ProjectOnPlane(strokeDirection, Vector3.up);
        if (strokeDirection.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        Vector3 localDirection = zone.transform.InverseTransformDirection(strokeDirection);
        Vector2 local2D = new(localDirection.x, localDirection.z);
        return local2D.sqrMagnitude >= 0.0001f ? local2D.normalized : Vector2.zero;
    }

    private Vector3 GetRecoveryReferenceWorldPosition(PlayerManager player)
    {
        if (player == null)
            return headCamera != null ? headCamera.transform.position : Vector3.zero;

        return player.transform.position;
    }

    private float ComputeCrawlRootY(PlayerManager player, float surfaceY)
    {
        CacheHeadCamera(player);

        float headOffset = 1.6f;
        if (headCamera != null && player != null)
            headOffset = Mathf.Max(0.1f, headCamera.transform.position.y - player.transform.position.y);

        return surfaceY + recoveryCrawlHeadHeightAboveSurface - headOffset;
    }

    private static float ComputeStandingRootY(PlayerManager player, float surfaceY)
    {
        CharacterController controller = player != null ? player.GetComponent<CharacterController>() : null;
        if (controller == null)
            return surfaceY;

        return surfaceY + controller.height * 0.5f - controller.center.y + 0.03f;
    }

    private void OnDrawGizmos()
    {
        if (showDebugGripZoneAlways)
            DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGripZoneAlways)
            DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        if (!showDebugGripZone)
            return;

        AutoAssignReferences();
        BreakableIceZone zone = iceZone != null ? iceZone : GetComponent<BreakableIceZone>();
        if (zone == null || !zone.TryGetSurfaceLayout(out Vector3 surfaceCenter, out float intactWidth, out float intactDepth))
            return;

        Vector2 impactLocalPoint = GetDebugImpactLocalPoint(zone, surfaceCenter);
        DrawGripBandGizmo(zone, surfaceCenter, intactWidth, intactDepth);
        DrawDebugReferenceMarkers(zone, surfaceCenter, impactLocalPoint);

        if (showDebugCrawlArea)
            DrawCrawlAreaGizmo(zone, surfaceCenter, intactWidth, intactDepth);
    }

    private Vector2 GetDebugImpactLocalPoint(BreakableIceZone zone, Vector3 surfaceCenter)
    {
        if (zone.HasBreakImpactPoint)
            return zone.LastBreakImpactLocalPoint;

        PlayerManager player = PlayerManager.Instance;
        if (player != null)
            return WorldToSurfaceLocalPoint(zone.transform, GetRecoveryReferenceWorldPosition(player), surfaceCenter);

        return Vector2.zero;
    }

    private void DrawGripBandGizmo(BreakableIceZone zone, Vector3 surfaceCenter, float intactWidth, float intactDepth)
    {
        Vector3 surfaceWorldCenter = zone.transform.TransformPoint(surfaceCenter);
        float halfWidth = intactWidth * 0.5f;
        float halfDepth = intactDepth * 0.5f;
        float lowerOffset = -recoveryGripHeightBelowSurface + debugGripVerticalOffset;
        float upperOffset = recoveryGripHeightAboveSurface + debugGripVerticalOffset;

        Gizmos.color = new Color(0f, 0.85f, 1f, 0.9f);
        Vector3 lowerA = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(-halfWidth, 0f, -halfDepth)) + Vector3.up * lowerOffset;
        Vector3 lowerB = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(-halfWidth, 0f, halfDepth)) + Vector3.up * lowerOffset;
        Vector3 lowerC = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(halfWidth, 0f, halfDepth)) + Vector3.up * lowerOffset;
        Vector3 lowerD = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(halfWidth, 0f, -halfDepth)) + Vector3.up * lowerOffset;
        Vector3 upperA = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(-halfWidth, 0f, -halfDepth)) + Vector3.up * upperOffset;
        Vector3 upperB = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(-halfWidth, 0f, halfDepth)) + Vector3.up * upperOffset;
        Vector3 upperC = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(halfWidth, 0f, halfDepth)) + Vector3.up * upperOffset;
        Vector3 upperD = surfaceWorldCenter + zone.transform.TransformVector(new Vector3(halfWidth, 0f, -halfDepth)) + Vector3.up * upperOffset;

        Gizmos.DrawLine(lowerA, lowerB);
        Gizmos.DrawLine(lowerB, lowerC);
        Gizmos.DrawLine(lowerC, lowerD);
        Gizmos.DrawLine(lowerD, lowerA);

        Gizmos.DrawLine(upperA, upperB);
        Gizmos.DrawLine(upperB, upperC);
        Gizmos.DrawLine(upperC, upperD);
        Gizmos.DrawLine(upperD, upperA);

        Gizmos.DrawLine(lowerA, upperA);
        Gizmos.DrawLine(lowerB, upperB);
        Gizmos.DrawLine(lowerC, upperC);
        Gizmos.DrawLine(lowerD, upperD);

        Gizmos.DrawSphere(upperA, debugGripMarkerRadius * 0.8f);
        Gizmos.DrawSphere(upperB, debugGripMarkerRadius * 0.8f);
        Gizmos.DrawSphere(upperC, debugGripMarkerRadius * 0.8f);
        Gizmos.DrawSphere(upperD, debugGripMarkerRadius * 0.8f);
    }

    private void DrawCrawlAreaGizmo(BreakableIceZone zone, Vector3 surfaceCenter, float intactWidth, float intactDepth)
    {
        float halfWidth = intactWidth * 0.5f + recoveryCrawlAreaExtension;
        float halfDepth = intactDepth * 0.5f + recoveryCrawlAreaExtension;
        Vector3 centerWorld = zone.transform.TransformPoint(surfaceCenter) + Vector3.up * 0.03f;

        Vector3 cornerA = centerWorld + zone.transform.TransformVector(new Vector3(-halfWidth, 0f, -halfDepth));
        Vector3 cornerB = centerWorld + zone.transform.TransformVector(new Vector3(-halfWidth, 0f, halfDepth));
        Vector3 cornerC = centerWorld + zone.transform.TransformVector(new Vector3(halfWidth, 0f, halfDepth));
        Vector3 cornerD = centerWorld + zone.transform.TransformVector(new Vector3(halfWidth, 0f, -halfDepth));

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f);
        Gizmos.DrawLine(cornerA, cornerB);
        Gizmos.DrawLine(cornerB, cornerC);
        Gizmos.DrawLine(cornerC, cornerD);
        Gizmos.DrawLine(cornerD, cornerA);
    }

    private void DrawDebugReferenceMarkers(BreakableIceZone zone, Vector3 surfaceCenter, Vector2 impactLocalPoint)
    {
        PlayerManager player = PlayerManager.Instance;
        Vector3 surfaceWorldCenter = zone.transform.TransformPoint(surfaceCenter);
        float markerLift = debugGripVerticalOffset + 0.015f;

        if (player != null)
        {
            CacheHeadCamera(player);

            DrawReferenceMarker(zone, surfaceCenter, player.transform.position, new Color(0.2f, 1f, 0.2f, 0.95f), markerLift, "Rig Root");

            if (headCamera != null)
                DrawReferenceMarker(zone, surfaceCenter, headCamera.transform.position, new Color(1f, 0.2f, 1f, 0.95f), markerLift, "Head");

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                DrawReferenceMarker(zone, surfaceCenter, controller.bounds.center, new Color(1f, 0.9f, 0.15f, 0.95f), markerLift, "Controller Center");
        }

        Vector3 impactCenterWorld = surfaceWorldCenter
            + zone.transform.TransformVector(new Vector3(impactLocalPoint.x, 0f, impactLocalPoint.y))
            + Vector3.up * (debugGripVerticalOffset + 0.05f);
        Gizmos.color = new Color(0f, 1f, 1f, 1f);
        Gizmos.DrawSphere(impactCenterWorld, debugGripMarkerRadius * 1.2f);
        Gizmos.DrawLine(impactCenterWorld, impactCenterWorld + Vector3.up * 0.1f);
        DrawReferenceLabel(impactCenterWorld + Vector3.up * 0.02f, "Break Center", new Color(0f, 1f, 1f, 1f));
    }

    private void DrawReferenceMarker(
        BreakableIceZone zone,
        Vector3 surfaceCenter,
        Vector3 worldPosition,
        Color color,
        float verticalOffset,
        string label)
    {
        Vector2 localPoint = WorldToSurfaceLocalPoint(zone.transform, worldPosition, surfaceCenter);
        Vector3 markerWorldPosition = zone.transform.TransformPoint(new Vector3(
            surfaceCenter.x + localPoint.x,
            surfaceCenter.y,
            surfaceCenter.z + localPoint.y)) + Vector3.up * verticalOffset;

        Gizmos.color = color;
        Gizmos.DrawSphere(markerWorldPosition, debugGripMarkerRadius);
        Gizmos.DrawLine(markerWorldPosition, markerWorldPosition + Vector3.up * 0.07f);
        DrawReferenceLabel(markerWorldPosition + Vector3.up * 0.015f, label, color);
    }

    private void DrawReferenceLabel(Vector3 worldPosition, string label, Color color)
    {
#if UNITY_EDITOR
        if (!showDebugReferenceLabels || string.IsNullOrEmpty(label))
            return;

        GUIStyle style = new(EditorStyles.boldLabel)
        {
            normal = { textColor = color }
        };
        Handles.Label(worldPosition, label, style);
#endif
    }
}
