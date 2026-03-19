using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion; // Added for LocomotionProvider
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;



public enum TurnType { Continuous, Snap, None }
public enum MoveType { Continuous, Teleport, None }
public enum LocomotionHand { Left, Right, Both }

public class PlayerMovement : MonoBehaviour
{
    [System.Serializable]
    public class MovementEvents
    {
        public UnityEvent onTeleport = new();
        public UnityEvent onSnapTurn = new();
    }

    [Header("Movement Events")]
    public MovementEvents events = new MovementEvents();

    [Header("Dependencies")]
    [SerializeField] private ContinuousMoveProvider continuousMove;
    [SerializeField] private TeleportationProvider teleportProvider;
    [SerializeField] private ContinuousTurnProvider continuousTurn;
    [SerializeField] private SnapTurnProvider snapTurn;

    [Header("Movement Settings")]
    [SerializeField] private MoveType moveMode = MoveType.Continuous;
    [SerializeField] private LocomotionHand moveHand = LocomotionHand.Left;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float teleportDistance = 1.5f;

    [Header("Turning Settings")]
    [SerializeField] private TurnType turnMode = TurnType.Snap;
    [SerializeField] private LocomotionHand turnHand = LocomotionHand.Right;
    [SerializeField] private float continuousTurnSpeed = 60f;
    [SerializeField] private float snapTurnAmount = 45f;

    public float CurrentMoveSpeed => moveSpeed;
    public MoveType CurrentLocomotion => moveMode;

    private void Awake()
    {
        InitComponents();
    }

    private void Start()
    {
        UpdateSettings();
    }

    private void OnEnable()
    {
        // Replaced endLocomotion with locomotionEnded
        if (teleportProvider != null) teleportProvider.locomotionEnded += OnTeleportEnd;
        if (snapTurn != null) snapTurn.locomotionEnded += OnSnapTurnEnd;
    }

    private void OnDisable()
    {
        if (teleportProvider != null) teleportProvider.locomotionEnded -= OnTeleportEnd;
        if (snapTurn != null) snapTurn.locomotionEnded -= OnSnapTurnEnd;
    }

    // XRI 3.0+ passes LocomotionProvider instead of LocomotionSystem
    private void OnTeleportEnd(LocomotionProvider _) => events.onTeleport?.Invoke();
    private void OnSnapTurnEnd(LocomotionProvider _) => events.onSnapTurn?.Invoke();

#if UNITY_EDITOR
    private void OnValidate()
    {
        InitComponents();
        UpdateSettings();
    }
#endif

    private void InitComponents()
    {
        if (continuousMove == null)
            continuousMove = GetComponentInChildren<ContinuousMoveProvider>(true);
        if (teleportProvider == null)
            teleportProvider = GetComponentInChildren<TeleportationProvider>(true);
        if (continuousTurn == null)
            continuousTurn = GetComponentInChildren<ContinuousTurnProvider>(true);
        if (snapTurn == null)
            snapTurn = GetComponentInChildren<SnapTurnProvider>(true);
    }

    public void UpdateSettings()
    {
        SetMoveMode(moveMode);
        SetMoveSpeed(moveSpeed);
        SetTeleportDistance(teleportDistance);
        SetTurnMode(turnMode);
        SetContinuousTurnSpeed(continuousTurnSpeed);
        SetSnapTurnAmount(snapTurnAmount);
        SetMoveHand(moveHand);
        SetTurnHand(turnHand);
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        if (continuousMove != null) continuousMove.moveSpeed = moveSpeed;
    }

    public void SetTeleportDistance(float distance)
    {
        teleportDistance = distance;
        var player = PlayerManager.Instance;
        if (player != null)
        {
            player.LeftHand.SetTeleportDistance(distance);
            player.RightHand.SetTeleportDistance(distance);
        }
    }

    public void SetMoveMode(MoveType type)
    {
        moveMode = type;

        if (continuousMove != null)
            continuousMove.enabled = (moveMode == MoveType.Continuous);
        if (teleportProvider != null)
            teleportProvider.enabled = (moveMode == MoveType.Teleport);

        EnableTeleportationRay(moveHand);
    }

    public void SetTurnMode(TurnType type)
    {
        turnMode = type;

        if (continuousTurn != null)
        {
            continuousTurn.enabled = (turnMode == TurnType.Continuous);
        }
        if (snapTurn != null)
        {
            snapTurn.enabled = (turnMode == TurnType.Snap);
        }
    }

    public void SetContinuousTurnSpeed(float speed)
    {
        if (continuousTurn != null)
        {
            continuousTurnSpeed = speed;
            continuousTurn.turnSpeed = continuousTurnSpeed;
        }
    }

    public void SetSnapTurnAmount(float amount)
    {
        if (snapTurn != null)
        {
            snapTurnAmount = amount;
            snapTurn.turnAmount = snapTurnAmount;
        }
    }

    public void SetMoveHand(LocomotionHand hand)
    {
        moveHand = hand;

        bool enableLeftHand = (moveHand == LocomotionHand.Left || moveHand == LocomotionHand.Both);
        bool enableRightHand = (moveHand == LocomotionHand.Right || moveHand == LocomotionHand.Both);

        SetReaderState(continuousMove?.leftHandMoveInput, enableLeftHand);
        SetReaderState(continuousMove?.rightHandMoveInput, enableRightHand);
        EnableTeleportationRay(moveHand);
    }

    public void SetTurnHand(LocomotionHand hand)
    {
        turnHand = hand;

        bool enableLeft = (turnHand == LocomotionHand.Left || turnHand == LocomotionHand.Both);
        bool enableRight = (turnHand == LocomotionHand.Right || turnHand == LocomotionHand.Both);

        SetReaderState(continuousTurn?.leftHandTurnInput, enableLeft);
        SetReaderState(continuousTurn?.rightHandTurnInput, enableRight);
        SetReaderState(snapTurn?.leftHandTurnInput, enableLeft);
        SetReaderState(snapTurn?.rightHandTurnInput, enableRight);
    }

    private void SetReaderState(XRInputValueReader reader, bool isActive)
    {
        if (reader == null) return;

        if (reader.inputSourceMode == XRInputValueReader.InputSourceMode.InputActionReference)
        {
            if (reader.inputActionReference != null && reader.inputActionReference.action != null)
            {
                if (isActive)
                    reader.inputActionReference.action.Enable();
                else
                    reader.inputActionReference.action.Disable();
            }
        }
    }

    private void EnableTeleportationRay(LocomotionHand hand)
    {
        PlayerManager player = PlayerManager.Instance;
        if (player == null)
            return;

        bool isTeleport = (moveMode == MoveType.Teleport);
        bool enableLeftRay = isTeleport && (hand == LocomotionHand.Left || hand == LocomotionHand.Both);
        bool enableRightRay = isTeleport && (hand == LocomotionHand.Right || hand == LocomotionHand.Both);

        player.LeftHand.SetTeleportActive(enableLeftRay);
        player.RightHand.SetTeleportActive(enableRightRay);
    }
}