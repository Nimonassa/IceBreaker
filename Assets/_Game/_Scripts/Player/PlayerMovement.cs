using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;


public enum TurnType { Continuous, Snap, Shift, None }
public enum MoveType { Continuous, Teleport, Shift, None }
public enum LocomotionHand { Left, Right, Both }

public class PlayerMovement : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ContinuousMoveProvider continuousMove;
    [SerializeField] private PlayerTeleportProvider teleportProvider;
    [SerializeField] private ContinuousTurnProvider continuousTurn;
    [SerializeField] private PlayerTurnProvider turnProvider;
    [SerializeField] private CharacterController characterController;

    [Header("Movement Settings")]
    [SerializeField] private MoveType moveMode = MoveType.Continuous;
    [SerializeField] private LocomotionHand moveHand = LocomotionHand.Left;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float teleportDistance = 1.5f;
    [SerializeField] private float shiftDistance = 4.0f;
    [SerializeField] private float shiftSpeed = 25.0f;

    [Header("Turning Settings")]
    [SerializeField] private TurnType turnMode = TurnType.Snap;
    [SerializeField] private LocomotionHand turnHand = LocomotionHand.Right;
    [SerializeField] private float continuousTurnSpeed = 60f;
    [SerializeField] private float snapTurnAmount = 45f;
    [SerializeField] private float shiftTurnAmount = 45f;
    [SerializeField] private float shiftTurnSpeed = 180f;

    public float CurrentMoveSpeed => moveSpeed;
    public MoveType CurrentLocomotion => moveMode;

    private void Awake() => InitComponents();
    private void Start() => UpdateSettings();

    private void OnEnable()
    {
        if (teleportProvider != null)
        {
            teleportProvider.onBlinkStarted.AddListener(OnBlinkStarted);
            teleportProvider.locomotionEnded += OnBlinkEnded;
            teleportProvider.onShiftStarted.AddListener(OnShiftStarted);
            teleportProvider.onShiftEnded.AddListener(OnShiftEnded);
        }

        if (turnProvider != null)
        {
            turnProvider.onSnapTurn.AddListener(OnSnapTurn);
            turnProvider.onShiftStarted.AddListener(OnShiftTurnStarted);
            turnProvider.onShiftEnded.AddListener(OnShiftTurnEnded);
        }
    }

    private void OnDisable()
    {
        if (teleportProvider != null)
        {
            teleportProvider.onBlinkStarted.RemoveListener(OnBlinkStarted);
            teleportProvider.locomotionEnded -= OnBlinkEnded;
            teleportProvider.onShiftStarted.RemoveListener(OnShiftStarted);
            teleportProvider.onShiftEnded.RemoveListener(OnShiftEnded);
        }

        if (turnProvider != null)
        {
            turnProvider.onSnapTurn.RemoveListener(OnSnapTurn);
            turnProvider.onShiftStarted.RemoveListener(OnShiftTurnStarted);
            turnProvider.onShiftEnded.RemoveListener(OnShiftTurnEnded);
        }
    }


    private void OnBlinkStarted() => PlayerEvents.OnTeleportStarted.Invoke();
    private void OnBlinkEnded(LocomotionProvider _) => PlayerEvents.OnTeleportEnded.Invoke();
    private void OnShiftStarted() => PlayerEvents.OnShiftStarted.Invoke();
    private void OnShiftEnded() => PlayerEvents.OnShiftEnded.Invoke();

    private void OnSnapTurn() => PlayerEvents.OnSnapTurn.Invoke();
    private void OnShiftTurnStarted() => PlayerEvents.OnShiftTurnStarted.Invoke();
    private void OnShiftTurnEnded() => PlayerEvents.OnShiftTurnEnded.Invoke();

    #region Configuration Logic
    // ... (Keep InitComponents, UpdateSettings, SetMoveMode, etc., from your original file)
    #endregion

    public void UpdateSettings()
    {
        SetMoveMode(moveMode);
        SetMoveSpeed(moveSpeed);
        SetTeleportDistance(teleportDistance);
        SetShiftDistance(shiftDistance);
        SetShiftSpeed(shiftSpeed);

        SetTurnMode(turnMode);
        SetContinuousTurnSpeed(continuousTurnSpeed);
        SetSnapTurnAmount(snapTurnAmount);
        SetShiftTurnAmount(shiftTurnAmount);
        SetShiftTurnSpeed(shiftTurnSpeed);

        SetMoveHand(moveHand);
        SetTurnHand(turnHand);
    }

    public void SetMoveMode(MoveType type)
    {
        moveMode = type;
        if (continuousMove != null) continuousMove.enabled = (moveMode == MoveType.Continuous);
        if (teleportProvider != null)
        {
            teleportProvider.enabled = (moveMode == MoveType.Teleport || moveMode == MoveType.Shift);
            teleportProvider.SetMode(moveMode == MoveType.Shift ? PlayerTeleportProvider.TeleportMode.Shift : PlayerTeleportProvider.TeleportMode.Blink);
        }
        UpdateHandRayDistance();
        EnableTeleportationRay(moveHand);
    }

    // [Rest of your original logic for SetMoveSpeed, SetTurnMode, etc., goes here]
    private void InitComponents()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (continuousMove == null) continuousMove = GetComponentInChildren<ContinuousMoveProvider>(true);
        if (teleportProvider == null) teleportProvider = GetComponentInChildren<PlayerTeleportProvider>(true);
        if (continuousTurn == null) continuousTurn = GetComponentInChildren<ContinuousTurnProvider>(true);
        if (turnProvider == null) turnProvider = GetComponentInChildren<PlayerTurnProvider>(true);
    }

    private void UpdateHandRayDistance()
    {
        float targetDist = (moveMode == MoveType.Shift) ? shiftDistance : teleportDistance;
        var player = PlayerManager.Instance;
        if (player != null)
        {
            player.LeftHand.SetTeleportDistance(targetDist);
            player.RightHand.SetTeleportDistance(targetDist);
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        if (continuousMove != null) continuousMove.moveSpeed = moveSpeed;
    }

    public void SetTeleportDistance(float distance)
    {
        teleportDistance = distance;
        if (moveMode == MoveType.Teleport) UpdateHandRayDistance();
    }

    public void SetShiftDistance(float distance)
    {
        shiftDistance = distance;
        if (moveMode == MoveType.Shift) UpdateHandRayDistance();
    }

    public void SetShiftSpeed(float speed)
    {
        shiftSpeed = speed;
        if (teleportProvider != null) teleportProvider.SetShiftSpeed(shiftSpeed);
    }

    public void SetTurnMode(TurnType type)
    {
        turnMode = type;
        if (continuousTurn != null) continuousTurn.enabled = (turnMode == TurnType.Continuous);
        if (turnProvider != null)
        {
            turnProvider.enabled = (turnMode == TurnType.Snap || turnMode == TurnType.Shift);
            turnProvider.SetMode(turnMode == TurnType.Shift ? PlayerTurnProvider.TurnMode.Shift : PlayerTurnProvider.TurnMode.Snap);
        }
        UpdateTurnAmount();
    }

    private void UpdateTurnAmount()
    {
        if (turnProvider != null)
            turnProvider.turnAmount = (turnMode == TurnType.Shift) ? shiftTurnAmount : snapTurnAmount;
    }

    public void SetContinuousTurnSpeed(float speed)
    {
        continuousTurnSpeed = speed;
        if (continuousTurn != null) continuousTurn.turnSpeed = continuousTurnSpeed;
    }

    public void SetSnapTurnAmount(float amount)
    {
        snapTurnAmount = amount;
        if (turnMode == TurnType.Snap) UpdateTurnAmount();
    }

    public void SetShiftTurnAmount(float amount)
    {
        shiftTurnAmount = amount;
        if (turnMode == TurnType.Shift) UpdateTurnAmount();
    }

    public void SetShiftTurnSpeed(float speed)
    {
        shiftTurnSpeed = speed;
        if (turnProvider != null) turnProvider.SetShiftSpeed(shiftTurnSpeed);
    }

    public void SetMoveHand(LocomotionHand hand)
    {
        moveHand = hand;
        bool enableLeft = (moveHand == LocomotionHand.Left || moveHand == LocomotionHand.Both);
        bool enableRight = (moveHand == LocomotionHand.Right || moveHand == LocomotionHand.Both);
        SetReaderState(continuousMove?.leftHandMoveInput, enableLeft);
        SetReaderState(continuousMove?.rightHandMoveInput, enableRight);
        EnableTeleportationRay(moveHand);
    }

    public void SetTeleportDelay(float delay)
    {
        if (teleportProvider != null) teleportProvider.delayTime = delay;
    }

    public void SetTurnHand(LocomotionHand hand)
    {
        turnHand = hand;
        bool enableLeft = (turnHand == LocomotionHand.Left || turnHand == LocomotionHand.Both);
        bool enableRight = (turnHand == LocomotionHand.Right || turnHand == LocomotionHand.Both);
        SetReaderState(continuousTurn?.leftHandTurnInput, enableLeft);
        SetReaderState(continuousTurn?.rightHandTurnInput, enableRight);
        SetReaderState(turnProvider?.leftHandTurnInput, enableLeft);
        SetReaderState(turnProvider?.rightHandTurnInput, enableRight);
    }

    private void SetReaderState(XRInputValueReader reader, bool isActive)
    {
        if (reader?.inputActionReference?.action == null) return;
        if (isActive) reader.inputActionReference.action.Enable();
        else reader.inputActionReference.action.Disable();
    }

    private void EnableTeleportationRay(LocomotionHand hand)
    {
        PlayerManager player = PlayerManager.Instance;
        if (player == null) return;
        bool isTeleporting = (moveMode == MoveType.Teleport || moveMode == MoveType.Shift);
        player.LeftHand.SetTeleportActive(isTeleporting && (hand == LocomotionHand.Left || hand == LocomotionHand.Both));
        player.RightHand.SetTeleportActive(isTeleporting && (hand == LocomotionHand.Right || hand == LocomotionHand.Both));
    }

    public bool IsGrounded()
    {
        if (characterController == null) return false;
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        Vector3 bottom = transform.TransformPoint(characterController.center) + Vector3.down * (characterController.height * 0.5f);
        return Physics.CheckSphere(bottom + Vector3.down * 0.05f, characterController.radius * 0.8f, layerMask, QueryTriggerInteraction.Ignore);
    }



    #region UnityEvent Enum Wrappers

    // --- MoveType Wrappers ---
    public void SetMoveModeContinuous() => SetMoveMode(MoveType.Continuous);
    public void SetMoveModeTeleport() => SetMoveMode(MoveType.Teleport);
    public void SetMoveModeShift() => SetMoveMode(MoveType.Shift);
    public void SetMoveModeNone() => SetMoveMode(MoveType.None);

    // --- TurnType Wrappers ---
    public void SetTurnModeContinuous() => SetTurnMode(TurnType.Continuous);
    public void SetTurnModeSnap() => SetTurnMode(TurnType.Snap);
    public void SetTurnModeShift() => SetTurnMode(TurnType.Shift);
    public void SetTurnModeNone() => SetTurnMode(TurnType.None);

    // --- Move Hand Wrappers ---
    public void SetMoveHandLeft() => SetMoveHand(LocomotionHand.Left);
    public void SetMoveHandRight() => SetMoveHand(LocomotionHand.Right);
    public void SetMoveHandBoth() => SetMoveHand(LocomotionHand.Both);

    // --- Turn Hand Wrappers ---
    public void SetTurnHandLeft() => SetTurnHand(LocomotionHand.Left);
    public void SetTurnHandRight() => SetTurnHand(LocomotionHand.Right);
    public void SetTurnHandBoth() => SetTurnHand(LocomotionHand.Both);

    #endregion
    
}