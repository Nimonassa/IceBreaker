using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class BreakableIceZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Mesh object for the intact ice surface shown before the break.")]
    [SerializeField] private GameObject intactRoot;
    [Tooltip("Collider used as the physical surface while the ice is still intact.")]
    [SerializeField] private Collider intactCollider;
    [Tooltip("Teleportation area on the intact ice that gets disabled once the ice breaks.")]
    [SerializeField] private Behaviour intactTeleportArea;
    [Tooltip("Root object shown after the intact ice is hidden.")]
    [SerializeField] private GameObject brokenRoot;
    [Tooltip("Parent transform that holds the generated Voronoi shard objects.")]
    [SerializeField] private Transform generatedShardsRoot;
    [Tooltip("Optional legacy broken-fill root. Leave empty unless you intentionally use extra fill geometry.")]
    [SerializeField] private Transform brokenFillRoot;
    [Tooltip("Water surface visual shown below the broken ice.")]
    [SerializeField] private Transform waterSurface;
    [Tooltip("Marker used to determine how far the player sinks before respawning.")]
    [SerializeField] private Transform waterSurfaceMarker;
    [Tooltip("World position and rotation where the player is restored after falling through the ice.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Break Sequence")]
    [Tooltip("How long the player takes to sink into the water once the ice breaks.")]
    [SerializeField] private float sinkDuration = 0.6f;
    [Tooltip("How high the player's head should stay above the water marker during the sink.")]
    [SerializeField] private float headClearanceAboveWater = 0.12f;
    [Tooltip("Pause after the crack appears before the shards start moving and the player begins sinking.")]
    [SerializeField] private float breakEffectDelay = 0f;
    [Tooltip("How long to wait after the fall before teleporting the player back to the respawn point.")]
    [SerializeField] private float respawnDelay = 2f;

    [Header("Shard Animation")]
    [Tooltip("How long the initial crack-and-jiggle phase lasts before the shards fully sink.")]
    [SerializeField] private float shardScatterDuration = 0.3f;
    [Tooltip("How far shards can drift sideways from the impact point while breaking.")]
    [SerializeField] private float shardScatterDistance = 0.09f;
    [Tooltip("How much small up-and-down bobbing the shards get during the wobble phase.")]
    [SerializeField] private float shardScatterLift = 0.03f;
    [Tooltip("How deep the shards sink into the water before they disappear.")]
    [SerializeField] private float shardScatterDrop = 0.55f;
    [Tooltip("Maximum tilt range used by the shard wobble animation.")]
    [SerializeField] private float shardSpinDegrees = 18f;
    [Tooltip("Overall multiplier for the visible wobble. Lower this if the shards shake too much.")]
    [SerializeField, Range(0f, 1.5f)] private float shardWobbleAmount = 0.55f;
    [Tooltip("How long shards remain visible before they are hidden at the end of the animation.")]
    [SerializeField] private float hideReleasedShardsAfter = 0.95f;

    [Header("Broken Surface")]
    [Tooltip("How wide the edge band of nearby shards is around the main impact hole.")]
    [SerializeField, Min(0.08f)] private float edgeShardBand = 0.24f;
    [Tooltip("Extra overlap used by optional broken-fill geometry near the edge of the broken area.")]
    [SerializeField, Min(0f)] private float brokenFillOverlap = 0.04f;

    [Header("Break Shape")]
    [Tooltip("Base radius of the hole that opens around the player's impact point.")]
    [SerializeField, Min(0.2f)] private float breakRadius = 0.55f;
    [Tooltip("Amount of irregular edge variation applied to the hole shape.")]
    [SerializeField, Range(0f, 0.4f)] private float breakEdgeNoise = 0.24f;
    [Tooltip("How pointy and sharp the broken edge shape can become.")]
    [SerializeField, Range(0f, 0.35f)] private float breakEdgeSpikiness = 0.18f;

    [Header("Voronoi Generation")]
    [Tooltip("Minimum number of Voronoi shards to generate for this ice volume.")]
    [SerializeField, Min(4)] private int minVoronoiShardCount = 12;
    [Tooltip("Maximum number of Voronoi shards to generate for this ice volume.")]
    [SerializeField, Min(4)] private int maxVoronoiShardCount = 18;
    [Tooltip("How much each generated shard is inset from the outer ice bounds.")]
    [SerializeField, Min(0f)] private float shardInset = 0.02f;
    [Tooltip("Gap size between neighboring generated shards.")]
    [SerializeField, Min(0f)] private float shardGap = 0.03f;
    [Tooltip("How uneven the Voronoi shard layout is. Higher values create less uniform shard shapes.")]
    [SerializeField, Range(0f, 0.45f)] private float voronoiJitter = 0.34f;
    [Tooltip("Seed for the generated shard layout. Set to 0 to derive it automatically from the object and collider shape.")]
    [SerializeField] private int shardSeed;

    [Header("Optional Effects")]
    [Tooltip("Extra objects to activate when the ice breaks, such as particles, audio, or other scene effects.")]
    [SerializeField] private GameObject[] activateOnBreak;

    private const float IntactThickness = 0.06f;
    private const float WaterPadding = 0.8f;
    private const float IntactPadding = 0.2f;
    private const float SurfaceOffsetFromTriggerBottom = 0.12f;
    private const float WaterOffsetBelowSurface = 0.595f;
    private const float WaterMarkerOffsetBelowSurface = 0.47f;
    private const float GeneratedShardThickness = 0.04f;
    private const float MinimumShardFootprint = 0.1f;
    private const float MinimumShardArea = 0.0125f;
    private const float VoronoiOuterPadding = 0.02f;
    private const string BrokenFillRootName = "BrokenFill";
    private const string BrokenFillNorthName = "Fill_North";
    private const string BrokenFillSouthName = "Fill_South";
    private const string BrokenFillWestName = "Fill_West";
    private const string BrokenFillEastName = "Fill_East";

    private static readonly Vector3 DefaultTriggerCenter = new(0f, 0.4f, 0f);
    private static readonly Vector3 DefaultTriggerSize = new(1.4f, 1.1f, 1.4f);

    private BoxCollider triggerCollider;
    private BreakableIceShardPiece[] generatedShards = Array.Empty<BreakableIceShardPiece>();
    private BreakableIceShardPiece[] edgeShards = Array.Empty<BreakableIceShardPiece>();
    private BreakableIceShardPiece[] releasedShards = Array.Empty<BreakableIceShardPiece>();
    private PlayerManager cachedPlayer;
    private Camera headCamera;
    private Coroutine breakRoutine;
    private bool isBroken;
    private bool pendingTeleportBreak;
    private bool teleportSubscribed;
    private PlayerManager pendingBreakPlayer;
    private MoveType cachedMoveMode = MoveType.Continuous;
    private TurnType cachedTurnMode = TurnType.Snap;
    private Transform brokenFillNorth;
    private Transform brokenFillSouth;
    private Transform brokenFillWest;
    private Transform brokenFillEast;
#if UNITY_EDITOR
    private bool editorRefreshQueued;
#endif

    private void Reset()
    {
        AutoAssignReferences();
        EnsureTriggerSetup();
        ApplyDefaultTriggerShape();
        EnsureGeneratedShardsRoot();
        EnsureBrokenFillRoot();
        FitVisualsToTrigger();
        RegenerateGeneratedShards();
    }

    private void Awake()
    {
        AutoAssignReferences();
        EnsureTriggerSetup();
        EnsureBrokenFillRoot();
        FitVisualsToTrigger();
        RefreshGeneratedShardReferences();
        PrepareInitialState();
        CachePlayerReferences();
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        EnsureTriggerSetup();
        EnsureBrokenFillRoot();
        RefreshGeneratedShardReferences();
        CachePlayerReferences();
        SubscribeTeleportEvent();
    }

    private void Start()
    {
        CachePlayerReferences();
        SubscribeTeleportEvent();
    }

    private void Update()
    {
        if (!pendingTeleportBreak || isBroken || breakRoutine != null)
            return;

        PlayerManager player = pendingBreakPlayer != null ? pendingBreakPlayer : cachedPlayer;
        if (player == null)
        {
            ClearPendingBreak();
            return;
        }

        if (ShouldWaitForTeleportLocomotion(player))
            return;

        if (!IsPlayerInsideTrigger(player))
        {
            ClearPendingBreak();
            return;
        }

        ClearPendingBreak();
        TriggerBreak(player);
    }

    private void OnDisable()
    {
        ClearPendingBreak();
        UnsubscribeTeleportEvent();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayer(other, out PlayerManager player))
            return;

        TryTriggerBreak(player);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!TryGetPlayer(other, out PlayerManager player))
            return;

        TryTriggerBreak(player);
    }

    private void HandleTeleportEnded()
    {
        if (isBroken)
            return;

        CachePlayerReferences();
        if (cachedPlayer == null || !IsPlayerInsideTrigger(cachedPlayer))
            return;

        QueueBreakAfterTeleport(cachedPlayer);
    }

    private void TryTriggerBreak(PlayerManager player)
    {
        if (player == null || isBroken || breakRoutine != null)
            return;

        if (ShouldWaitForTeleportLocomotion(player))
        {
            QueueBreakAfterTeleport(player);
            return;
        }

        ClearPendingBreak();
        TriggerBreak(player);
    }

    private void QueueBreakAfterTeleport(PlayerManager player)
    {
        pendingTeleportBreak = true;
        pendingBreakPlayer = player;
    }

    private void ClearPendingBreak()
    {
        pendingTeleportBreak = false;
        pendingBreakPlayer = null;
    }

    private bool ShouldWaitForTeleportLocomotion(PlayerManager player)
    {
        return player?.Movement != null && player.Movement.IsTeleportLocomotionBusy;
    }

    private bool IsPlayerInsideTrigger(PlayerManager player)
    {
        if (player == null || triggerCollider == null)
            return false;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null && triggerCollider.bounds.Intersects(controller.bounds))
            return true;

        return triggerCollider.bounds.Contains(player.transform.position);
    }

    private void TriggerBreak(PlayerManager player)
    {
        if (isBroken || breakRoutine != null || player == null)
            return;

        ClearPendingBreak();
        breakRoutine = StartCoroutine(BreakSequence(player));
    }

    private IEnumerator BreakSequence(PlayerManager player)
    {
        isBroken = true;

        CachePlayerReferences();
        CachePlayerModes(player);
        SetPlayerControl(player, false);
        yield return null;

        RefreshGeneratedShardReferences();
        SelectBreakShards(player.transform.position);
        ConfigureBrokenVisualState();

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (brokenRoot != null)
            brokenRoot.SetActive(true);

        ActivateBreakEffects();
        SetIntactVisualsEnabled(false);

        if (breakEffectDelay > 0f)
            yield return new WaitForSeconds(breakEffectDelay);

        if (intactTeleportArea != null)
            intactTeleportArea.enabled = false;

        if (intactCollider != null)
            intactCollider.enabled = false;

        if (intactRoot != null)
            intactRoot.SetActive(false);
        ReleaseSelectedShards(player.transform.position);

        CharacterController controller = player.GetComponent<CharacterController>();
        bool restoreControllerCollisions = false;
        if (controller != null)
        {
            restoreControllerCollisions = controller.detectCollisions;
            controller.detectCollisions = false;
        }

        yield return SinkPlayer(player.transform);

        if (respawnPoint != null)
        {
            yield return new WaitForSeconds(respawnDelay);
            player.transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        }

        yield return null;

        if (controller != null)
            controller.detectCollisions = restoreControllerCollisions;

        SetPlayerControl(player, true);
        breakRoutine = null;
    }

    private IEnumerator SinkPlayer(Transform playerRoot)
    {
        if (playerRoot == null)
            yield break;

        Vector3 startPosition = playerRoot.position;
        Vector3 targetPosition = startPosition;

        if (waterSurfaceMarker != null)
        {
            float headOffset = 1.6f;
            if (headCamera != null)
                headOffset = headCamera.transform.position.y - playerRoot.position.y;

            targetPosition = waterSurfaceMarker.position;
            targetPosition.y = waterSurfaceMarker.position.y + headClearanceAboveWater - headOffset;
        }
        else
        {
            targetPosition.y -= 1.6f;
        }

        float elapsed = 0f;
        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sinkDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            playerRoot.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        playerRoot.position = targetPosition;
    }

    private void ReleaseSelectedShards(Vector3 impactWorldPosition)
    {
        if (generatedShards == null || generatedShards.Length == 0)
            return;

        HashSet<BreakableIceShardPiece> releasedSet = releasedShards != null
            ? new HashSet<BreakableIceShardPiece>(releasedShards)
            : new HashSet<BreakableIceShardPiece>();

        HashSet<BreakableIceShardPiece> edgeSet = edgeShards != null
            ? new HashSet<BreakableIceShardPiece>(edgeShards)
            : new HashSet<BreakableIceShardPiece>();

        foreach (BreakableIceShardPiece piece in generatedShards)
        {
            if (piece == null)
                continue;

            float strength = 0.38f;
            float delay = UnityEngine.Random.Range(0.08f, 0.2f);

            if (releasedSet.Contains(piece))
            {
                strength = 1f;
                delay = UnityEngine.Random.Range(0f, 0.05f);
            }
            else if (edgeSet.Contains(piece))
            {
                strength = 0.72f;
                delay = UnityEngine.Random.Range(0.03f, 0.12f);
            }

            StartCoroutine(AnimateReleasedShard(piece, impactWorldPosition, strength, delay));
        }
    }

    private IEnumerator AnimateReleasedShard(
        BreakableIceShardPiece piece,
        Vector3 impactWorldPosition,
        float strength,
        float startDelay)
    {
        if (piece == null)
            yield break;

        piece.RestoreRestPose();
        piece.gameObject.SetActive(true);

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (piece == null)
            yield break;

        float strengthFactor = Mathf.Clamp(strength, 0.25f, 1f);
        Transform shardTransform = piece.transform;
        GameObject shardObject = shardTransform.gameObject;
        Vector3 startPosition = shardTransform.position;
        Quaternion startRotation = shardTransform.rotation;
        Vector3 startScale = shardTransform.localScale;

        Vector3 outward = startPosition - impactWorldPosition;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.001f)
            outward = UnityEngine.Random.insideUnitSphere;

        outward.y = 0f;
        outward.Normalize();

        Vector3 sideways = Vector3.Cross(Vector3.up, outward);
        if (sideways.sqrMagnitude < 0.001f)
            sideways = Vector3.right;

        sideways.Normalize();
        sideways *= UnityEngine.Random.value < 0.5f ? -1f : 1f;

        Vector3 spinAxis = UnityEngine.Random.onUnitSphere;
        if (spinAxis.sqrMagnitude < 0.001f)
            spinAxis = Vector3.up;

        spinAxis.Normalize();

        float durationScale = Mathf.Lerp(1.05f, 0.82f, strengthFactor);
        float distanceScale = Mathf.Lerp(0.18f, 0.48f, strengthFactor);
        float wobbleScale = Mathf.Lerp(0.32f, 1f, strengthFactor) * Mathf.Max(0f, shardWobbleAmount);
        float sinkScale = Mathf.Lerp(0.42f, 1f, strengthFactor);
        float rotationScale = Mathf.Lerp(0.35f, 1f, strengthFactor) * Mathf.Max(0f, shardWobbleAmount);

        float jiggleDuration = Mathf.Max(0.18f, shardScatterDuration * durationScale * UnityEngine.Random.Range(0.92f, 1.1f));
        float sinkDuration = Mathf.Max(0.4f, hideReleasedShardsAfter * Mathf.Lerp(0.9f, 1.18f, strengthFactor) * UnityEngine.Random.Range(0.92f, 1.08f));
        float travelDistance = Mathf.Max(0.02f, shardScatterDistance * distanceScale * UnityEngine.Random.Range(0.88f, 1.12f));
        float jiggleAmplitude = Mathf.Max(0f, shardScatterLift * wobbleScale * UnityEngine.Random.Range(0.85f, 1.15f));
        float sinkDepth = Mathf.Max(0.12f, shardScatterDrop * sinkScale * UnityEngine.Random.Range(0.92f, 1.08f));
        float wobbleAngle = Mathf.Max(0f, shardSpinDegrees * rotationScale * UnityEngine.Random.Range(0.9f, 1.12f));
        float totalDuration = jiggleDuration + sinkDuration;

        float phaseA = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float phaseB = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float phaseC = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float wobbleFrequencyA = UnityEngine.Random.Range(10f, 14f);
        float wobbleFrequencyB = UnityEngine.Random.Range(8f, 12f);
        float wobbleFrequencyC = UnityEngine.Random.Range(9f, 13f);
        Vector3 driftTarget = outward * travelDistance + sideways * (travelDistance * UnityEngine.Random.Range(-0.35f, 0.35f));

        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            if (piece == null)
                yield break;

            elapsed += Time.deltaTime;
            float jiggleProgress = Mathf.Clamp01(elapsed / jiggleDuration);
            float sinkProgress = elapsed <= jiggleDuration
                ? 0f
                : Mathf.Clamp01((elapsed - jiggleDuration) / sinkDuration);
            float driftProgress = 1f - Mathf.Pow(1f - jiggleProgress, 3f);
            float wobbleFade = 1f - sinkProgress;

            float sway = Mathf.Sin(elapsed * wobbleFrequencyA + phaseA) * jiggleAmplitude * wobbleFade;
            float foreAft = Mathf.Sin(elapsed * wobbleFrequencyB + phaseB) * jiggleAmplitude * 0.45f * wobbleFade;
            float bob = (
                Mathf.Sin(elapsed * wobbleFrequencyC + phaseC) * 0.75f
                + Mathf.Sin(elapsed * (wobbleFrequencyA * 0.58f) + phaseB) * 0.35f)
                * jiggleAmplitude
                * wobbleFade;
            float sink = Mathf.SmoothStep(0f, sinkDepth, sinkProgress);

            Vector3 positionOffset = Vector3.Lerp(Vector3.zero, driftTarget, driftProgress)
                + sideways * sway
                + outward * foreAft
                + Vector3.up * bob
                - Vector3.up * sink;

            float tiltA = Mathf.Sin(elapsed * wobbleFrequencyA + phaseA) * wobbleAngle * wobbleFade;
            float tiltB = Mathf.Cos(elapsed * wobbleFrequencyB + phaseB) * (wobbleAngle * 0.6f) * wobbleFade;

            shardTransform.position = startPosition + positionOffset;
            shardTransform.rotation = startRotation
                * Quaternion.AngleAxis(tiltA, sideways)
                * Quaternion.AngleAxis(tiltB, spinAxis);
            shardTransform.localScale = startScale;
            yield return null;
        }

        if (shardObject != null)
            shardObject.SetActive(false);
    }

    private void SetPlayerControl(PlayerManager player, bool enabled)
    {
        if (player == null)
            return;

        if (!enabled)
        {
            player.Movement?.SetLocomotionInputEnabled(false);
            SetHandControl(player.LeftHand, false);
            SetHandControl(player.RightHand, false);
            return;
        }

        if (player.Movement != null)
        {
            player.Movement.SetMoveMode(cachedMoveMode);
            player.Movement.SetTurnMode(cachedTurnMode);
            player.Movement.SetLocomotionInputEnabled(true);
        }

        SetHandControl(player.LeftHand, true);
        SetHandControl(player.RightHand, true);

        if (player.Grabbing != null)
            player.Grabbing.UpdateSettings();
    }

    private void CachePlayerModes(PlayerManager player)
    {
        if (player == null || player.Movement == null)
            return;

        cachedMoveMode = player.Movement.CurrentLocomotion;
        cachedTurnMode = player.Movement.CurrentTurnMode;
    }

    private static void SetHandControl(PlayerHand hand, bool enabled)
    {
        if (hand == null)
            return;

        hand.SetInteractionActive(enabled);
        hand.SetGrabRayActive(enabled);

        if (!enabled)
            hand.SetTeleportActive(false);
    }

    private void PrepareInitialState()
    {
        edgeShards = Array.Empty<BreakableIceShardPiece>();
        releasedShards = Array.Empty<BreakableIceShardPiece>();

        if (triggerCollider != null)
            triggerCollider.enabled = true;

        if (intactRoot != null)
            intactRoot.SetActive(true);

        if (intactCollider != null)
            intactCollider.enabled = true;

        if (intactTeleportArea != null)
            intactTeleportArea.enabled = true;

        if (brokenRoot != null)
            brokenRoot.SetActive(false);

        SetIntactVisualsEnabled(true);
        ResetBrokenFillState();

        foreach (BreakableIceShardPiece piece in generatedShards)
        {
            if (piece != null)
            {
                piece.RestoreRestPose();
                piece.gameObject.SetActive(true);
            }
        }
    }

    private void ActivateBreakEffects()
    {
        if (activateOnBreak == null)
            return;

        foreach (GameObject target in activateOnBreak)
        {
            if (target != null)
                target.SetActive(true);
        }
    }

    private void SetIntactVisualsEnabled(bool enabled)
    {
        if (intactRoot == null)
            return;

        foreach (Renderer renderer in intactRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }

    private void CachePlayerReferences()
    {
        if (cachedPlayer == null)
            cachedPlayer = PlayerManager.Instance;

        if (cachedPlayer != null && headCamera == null)
            headCamera = cachedPlayer.GetComponentInChildren<Camera>(true);

        if (headCamera == null)
            headCamera = Camera.main;
    }

    private void SubscribeTeleportEvent()
    {
        if (teleportSubscribed)
            return;

        CachePlayerReferences();
        if (cachedPlayer?.Movement == null)
            return;

        cachedPlayer.Movement.events.onTeleport.AddListener(HandleTeleportEnded);
        teleportSubscribed = true;
    }

    private void UnsubscribeTeleportEvent()
    {
        if (!teleportSubscribed || cachedPlayer?.Movement == null)
            return;

        cachedPlayer.Movement.events.onTeleport.RemoveListener(HandleTeleportEnded);
        teleportSubscribed = false;
    }

    private bool TryGetPlayer(Collider other, out PlayerManager player)
    {
        player = other.GetComponentInParent<PlayerManager>();
        if (player == null)
            player = cachedPlayer;

        return player != null;
    }

    private void AutoAssignReferences()
    {
        triggerCollider = GetComponent<BoxCollider>();
        Transform root = transform;

        if (intactRoot == null)
        {
            Transform found = root.Find("IceIntact");
            if (found != null)
                intactRoot = found.gameObject;
        }

        if (brokenRoot == null)
        {
            Transform found = root.Find("IceBroken");
            if (found != null)
                brokenRoot = found.gameObject;
        }

        if (generatedShardsRoot == null && brokenRoot != null)
        {
            Transform found = brokenRoot.transform.Find("GeneratedShards");
            if (found != null)
                generatedShardsRoot = found;
        }

        if (brokenFillRoot == null && brokenRoot != null)
        {
            Transform found = brokenRoot.transform.Find(BrokenFillRootName);
            if (found != null)
                brokenFillRoot = found;
        }

        if (waterSurfaceMarker == null)
        {
            Transform found = root.Find("WaterSurfaceMarker");
            if (found != null)
                waterSurfaceMarker = found;
        }

        if (waterSurface == null)
        {
            Transform found = root.Find("WaterSurface");
            if (found != null)
                waterSurface = found;
        }

        if (respawnPoint == null)
        {
            Transform found = root.Find("RespawnPoint");
            if (found != null)
                respawnPoint = found;
        }

        if (intactCollider == null && intactRoot != null)
            intactCollider = intactRoot.GetComponent<Collider>();

        if (intactTeleportArea == null && intactRoot != null)
            intactTeleportArea = FindBehaviourByTypeName(intactRoot, "TeleportationArea");

        if (brokenFillRoot != null)
        {
            brokenFillNorth = brokenFillRoot.Find(BrokenFillNorthName);
            brokenFillSouth = brokenFillRoot.Find(BrokenFillSouthName);
            brokenFillWest = brokenFillRoot.Find(BrokenFillWestName);
            brokenFillEast = brokenFillRoot.Find(BrokenFillEastName);
        }
    }

    private void EnsureTriggerSetup()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void EnsureGeneratedShardsRoot()
    {
        if (brokenRoot == null || generatedShardsRoot != null)
            return;

        Transform found = brokenRoot.transform.Find("GeneratedShards");
        if (found != null)
        {
            generatedShardsRoot = found;
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject shardRoot = new("GeneratedShards");
            shardRoot.transform.SetParent(brokenRoot.transform, false);
            generatedShardsRoot = shardRoot.transform;
        }
#endif
    }

    private void EnsureBrokenFillRoot()
    {
        if (brokenRoot == null)
            return;

        if (brokenFillRoot == null)
        {
            Transform found = brokenRoot.transform.Find(BrokenFillRootName);
            if (found != null)
                brokenFillRoot = found;
        }

        if (brokenFillRoot == null)
            return;

        brokenFillNorth = brokenFillRoot.Find(BrokenFillNorthName);
        brokenFillSouth = brokenFillRoot.Find(BrokenFillSouthName);
        brokenFillWest = brokenFillRoot.Find(BrokenFillWestName);
        brokenFillEast = brokenFillRoot.Find(BrokenFillEastName);
        ResetBrokenFillState();
    }

    private void ApplyDefaultTriggerShape()
    {
        if (triggerCollider == null)
            return;

        if (triggerCollider.center == Vector3.zero && triggerCollider.size == Vector3.one)
        {
            triggerCollider.center = DefaultTriggerCenter;
            triggerCollider.size = DefaultTriggerSize;
        }
    }

    [ContextMenu("Fit Visuals To Trigger")]
    private void FitVisualsToTrigger()
    {
        if (!TryGetSurfaceLayout(out Vector3 surfaceCenter, out float intactWidth, out float intactDepth))
            return;

        Vector3 triggerSize = triggerCollider.size;
        Vector3 triggerCenter = triggerCollider.center;
        float surfaceY = surfaceCenter.y;

        if (intactRoot != null)
        {
            intactRoot.transform.localPosition = surfaceCenter;
            intactRoot.transform.localScale = new Vector3(intactWidth, IntactThickness, intactDepth);
        }

        if (waterSurface != null)
        {
            waterSurface.localPosition = new Vector3(triggerCenter.x, surfaceY - WaterOffsetBelowSurface, triggerCenter.z);
            waterSurface.localScale = new Vector3(triggerSize.x + WaterPadding, 0.25f, triggerSize.z + WaterPadding);
        }

        if (waterSurfaceMarker != null)
            waterSurfaceMarker.localPosition = new Vector3(triggerCenter.x, surfaceY - WaterMarkerOffsetBelowSurface, triggerCenter.z);

        if (brokenRoot != null)
            brokenRoot.transform.localPosition = Vector3.zero;

        if (generatedShardsRoot != null)
        {
            generatedShardsRoot.localPosition = Vector3.zero;
            generatedShardsRoot.localRotation = Quaternion.identity;
            generatedShardsRoot.localScale = Vector3.one;
        }

        if (brokenFillRoot != null)
        {
            brokenFillRoot.localPosition = Vector3.zero;
            brokenFillRoot.localRotation = Quaternion.identity;
            brokenFillRoot.localScale = Vector3.one;
        }
    }

    [ContextMenu("Regenerate Shards")]
    private void RegenerateGeneratedShards()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || brokenRoot == null || intactRoot == null)
            return;

        EnsureGeneratedShardsRoot();
        EnsureBrokenFillRoot();
        NormalizeGeneratedShardHierarchy();
        if (generatedShardsRoot == null)
            return;

        MeshRenderer intactRenderer = intactRoot.GetComponent<MeshRenderer>();
        if (intactRenderer == null || intactRenderer.sharedMaterials == null || intactRenderer.sharedMaterials.Length == 0)
            return;

        if (!TryGetSurfaceLayout(out Vector3 surfaceCenter, out float intactWidth, out float intactDepth))
            return;

        BreakableIceShardGenerator.GenerationSettings settings = new()
        {
            minShardCount = minVoronoiShardCount,
            maxShardCount = maxVoronoiShardCount,
            outerPadding = VoronoiOuterPadding,
            shardInset = shardInset,
            shardGap = shardGap,
            splitJitter = voronoiJitter,
            shardThickness = GeneratedShardThickness,
            minimumShardArea = MinimumShardArea,
            minimumShardFootprint = MinimumShardFootprint,
        };

        BreakableIceShardGenerator.RegenerateShards(
            generatedShardsRoot.gameObject,
            intactRenderer.sharedMaterials[0],
            surfaceCenter,
            intactWidth,
            intactDepth,
            ResolveShardSeed(),
            settings);

        RefreshGeneratedShardReferences();
#endif
    }

    private void RefreshGeneratedShardReferences()
    {
        AutoAssignReferences();
        EnsureGeneratedShardsRoot();
        EnsureBrokenFillRoot();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            NormalizeGeneratedShardHierarchy();
#endif

        generatedShards = generatedShardsRoot != null
            ? BreakableIceShardGenerator.CollectGeneratedShards(generatedShardsRoot.gameObject)
            : Array.Empty<BreakableIceShardPiece>();
    }

    private void SelectBreakShards(Vector3 impactWorldPosition)
    {
        edgeShards = Array.Empty<BreakableIceShardPiece>();
        releasedShards = Array.Empty<BreakableIceShardPiece>();
        if (generatedShards == null || generatedShards.Length == 0)
            return;

        if (!TryGetSurfaceLayout(out Vector3 surfaceCenter, out _, out _))
            return;

        Vector2 impactLocalPoint = new(
            impactWorldPosition.x - surfaceCenter.x,
            impactWorldPosition.z - surfaceCenter.z);

        BreakableIceShardGenerator.SelectionSettings settings = new()
        {
            breakRadius = breakRadius,
            edgeNoise = breakEdgeNoise,
            edgeSpikiness = breakEdgeSpikiness,
            seed = ResolveShardSeed(),
        };

        releasedShards = BreakableIceShardGenerator.SelectShardPiecesForBreak(generatedShards, impactLocalPoint, settings);
        edgeShards = SelectEdgeShards(generatedShards, releasedShards);
    }

    private BreakableIceShardPiece[] SelectEdgeShards(
        BreakableIceShardPiece[] allPieces,
        BreakableIceShardPiece[] releasePieces)
    {
        if (allPieces == null || allPieces.Length == 0 || releasePieces == null || releasePieces.Length == 0)
            return Array.Empty<BreakableIceShardPiece>();

        HashSet<BreakableIceShardPiece> releasedSet = new(releasePieces);
        List<BreakableIceShardPiece> selected = new();
        float bandDistance = Mathf.Max(0.08f, edgeShardBand);

        foreach (BreakableIceShardPiece piece in allPieces)
        {
            if (piece == null || releasedSet.Contains(piece))
                continue;

            float nearestDistance = float.MaxValue;
            foreach (BreakableIceShardPiece releasedPiece in releasePieces)
            {
                if (releasedPiece == null)
                    continue;

                float distance = Vector2.Distance(piece.LocalCenter, releasedPiece.LocalCenter) - releasedPiece.InfluenceRadius;
                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            float threshold = bandDistance + piece.InfluenceRadius * 0.35f;
            if (nearestDistance <= threshold)
                selected.Add(piece);
        }

        return selected.ToArray();
    }

    private void ConfigureBrokenVisualState()
    {
        foreach (BreakableIceShardPiece piece in generatedShards)
        {
            if (piece == null)
                continue;

            piece.RestoreRestPose();
            piece.gameObject.SetActive(true);
        }

        ResetBrokenFillState();
    }

    private void ResetBrokenFillState()
    {
        if (brokenFillRoot == null)
            return;

        brokenFillRoot.gameObject.SetActive(false);
        HideBrokenFillPiece(brokenFillNorth);
        HideBrokenFillPiece(brokenFillSouth);
        HideBrokenFillPiece(brokenFillWest);
        HideBrokenFillPiece(brokenFillEast);
    }

    private static void HideBrokenFillPiece(Transform piece)
    {
        if (piece != null)
            piece.gameObject.SetActive(false);
    }

    private bool TryGetSurfaceLayout(out Vector3 surfaceCenter, out float intactWidth, out float intactDepth)
    {
        surfaceCenter = Vector3.zero;
        intactWidth = 0f;
        intactDepth = 0f;

        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        if (triggerCollider == null)
            return false;

        Vector3 triggerSize = triggerCollider.size;
        Vector3 triggerCenter = triggerCollider.center;
        float surfaceY = triggerCenter.y - triggerSize.y * 0.5f + SurfaceOffsetFromTriggerBottom;

        intactWidth = Mathf.Max(0.1f, triggerSize.x + IntactPadding);
        intactDepth = Mathf.Max(0.1f, triggerSize.z + IntactPadding);
        surfaceCenter = new Vector3(triggerCenter.x, surfaceY, triggerCenter.z);
        return true;
    }

    private int ResolveShardSeed()
    {
        if (shardSeed != 0)
            return shardSeed;

        string autoSeed = $"{GetHierarchyPath()}|{triggerCollider.center}|{triggerCollider.size}";
        return Animator.StringToHash(autoSeed);
    }

    private string GetHierarchyPath()
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    private static Behaviour FindBehaviourByTypeName(GameObject target, string typeName)
    {
        if (target == null)
            return null;

        foreach (Behaviour behaviour in target.GetComponents<Behaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

#if UNITY_EDITOR
    private void QueueEditorRefresh()
    {
        if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || editorRefreshQueued)
            return;

        editorRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += RunDeferredEditorRefresh;
    }

    private void RunDeferredEditorRefresh()
    {
        editorRefreshQueued = false;

        if (this == null || gameObject == null)
            return;

        if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        AutoAssignReferences();
        EnsureTriggerSetup();
        ApplyDefaultTriggerShape();
        EnsureGeneratedShardsRoot();
        EnsureBrokenFillRoot();
        NormalizeGeneratedShardHierarchy();
        FitVisualsToTrigger();
        RegenerateGeneratedShards();
        RefreshGeneratedShardReferences();
    }

    private void NormalizeGeneratedShardHierarchy()
    {
        if (brokenRoot == null)
            return;

        List<Transform> shardRoots = new();
        foreach (Transform child in brokenRoot.transform)
        {
            if (child != null && child.name == "GeneratedShards")
                shardRoots.Add(child);
        }

        if (generatedShardsRoot == null && shardRoots.Count > 0)
            generatedShardsRoot = shardRoots[0];

        List<GameObject> objectsToDelete = new();
        foreach (Transform shardRoot in shardRoots)
        {
            if (shardRoot != null && generatedShardsRoot != null && shardRoot != generatedShardsRoot)
                objectsToDelete.Add(shardRoot.gameObject);
        }

        HashSet<string> keptShardNames = new(StringComparer.Ordinal);
        foreach (Transform child in brokenRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == brokenRoot.transform)
                continue;

            if (!child.name.StartsWith("IceShard_", StringComparison.Ordinal))
                continue;

            bool isPrimaryShard = generatedShardsRoot != null && child.parent == generatedShardsRoot;
            if (!isPrimaryShard || !keptShardNames.Add(child.name))
                objectsToDelete.Add(child.gameObject);
        }

        for (int index = objectsToDelete.Count - 1; index >= 0; index--)
        {
            GameObject target = objectsToDelete[index];
            if (target == null)
                continue;

            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private void OnValidate()
    {
        QueueEditorRefresh();
    }
#endif
}
