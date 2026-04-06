using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "IceSafety/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement Settings")]
    public MoveType moveMode = MoveType.Continuous;
    public LocomotionHand moveHand = LocomotionHand.Left;
    public float moveSpeed = 3.0f;
    public float teleportDistance = 1.5f;

    [Header("Turning Settings")]
    public TurnType turnMode = TurnType.Snap;
    public LocomotionHand turnHand = LocomotionHand.Right;
    public float continuousTurnSpeed = 60.0f;
    public float snapTurnAmount = 45.0f;

    [Header("Grabbing Settings")]
    public InteractionHand grabHand = InteractionHand.Both;
    public InteractorFarAttachMode attachMode = InteractorFarAttachMode.Far;
    public float grabRayDistance = 5.0f;
}