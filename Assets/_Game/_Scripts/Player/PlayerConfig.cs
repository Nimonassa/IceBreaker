using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;

// Ensure your enums are accessible (either in this file or a shared namespace)
// public enum TurnType { Continuous, Snap, Shift, None }
// public enum MoveType { Continuous, Teleport, Shift, None }
// public enum LocomotionHand { Left, Right, Both }
// public enum InteractionHand { Left, Right, Both } 

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement Settings")]
    public MoveType moveMode = MoveType.Continuous;
    public LocomotionHand moveHand = LocomotionHand.Left;
    public float moveSpeed = 3.0f;
    public float teleportDistance = 1.5f;
    public float shiftDistance = 4.0f;
    public float shiftSpeed = 25.0f;

    [Header("Turning Settings")]
    public TurnType turnMode = TurnType.Snap;
    public LocomotionHand turnHand = LocomotionHand.Right;
    public float continuousTurnSpeed = 60.0f;
    public float snapTurnAmount = 45.0f;
    public float shiftTurnAmount = 45.0f;
    public float shiftTurnSpeed = 180.0f;

    [Header("Grabbing Settings")]
    public InteractionHand grabHand = InteractionHand.Both;
    public InteractorFarAttachMode attachMode = InteractorFarAttachMode.Far;
    public float grabRayDistance = 5.0f;
}
