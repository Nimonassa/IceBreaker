using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    [Header("Systems")]
    [field: SerializeField] public PlayerMovement Movement { get; private set; }
    [field: SerializeField] public PlayerGrabbing Grabbing { get; private set; }

    [Header("Hands")]
    [field: SerializeField] public PlayerHand LeftHand { get; private set; }
    [field: SerializeField] public PlayerHand RightHand { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        InitComponents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        InitComponents();
    }
#endif

    private void InitComponents()
    {
        if (Movement == null)
            Movement = GetComponentInChildren<PlayerMovement>(true);
        if (Grabbing == null)
            Grabbing = GetComponentInChildren<PlayerGrabbing>(true);

        if (LeftHand == null || RightHand == null)
        {
            PlayerHand[] hands = GetComponentsInChildren<PlayerHand>(true);
            foreach (var hand in hands)
            {
                if (hand.side == HandSide.Left) LeftHand = hand;
                if (hand.side == HandSide.Right) RightHand = hand;
            }
        }
    }
    public void Teleport(Transform point)
    {
        if (point != null)
        {
            this.transform.position = point.position;
            this.transform.rotation = point.rotation;
        }
    }
    public void TeleportPosition(Transform point)
    {
        if (point != null)
        {
            this.transform.position = point.position;
        }
    }


    public void Load(PlayerConfig config)
    {
        if (config == null)
        {
            return;
        }

        if (Movement != null)
        {
            // Movement Settings
            Movement.SetMoveMode(config.moveMode);
            Movement.SetMoveHand(config.moveHand);
            Movement.SetMoveSpeed(config.moveSpeed);
            Movement.SetTeleportDistance(config.teleportDistance);
            Movement.SetShiftDistance(config.shiftDistance);
            Movement.SetShiftSpeed(config.shiftSpeed);

            // Turning Settings
            Movement.SetTurnMode(config.turnMode);
            Movement.SetTurnHand(config.turnHand);
            Movement.SetContinuousTurnSpeed(config.continuousTurnSpeed);
            Movement.SetSnapTurnAmount(config.snapTurnAmount);
            Movement.SetShiftTurnAmount(config.shiftTurnAmount);
            Movement.SetShiftTurnSpeed(config.shiftTurnSpeed);
        }

        if (Grabbing != null)
        {
            // Grabbing Settings
            Grabbing.SetGrabHand(config.grabHand);
            Grabbing.SetGrabAttachMode(config.attachMode);
            Grabbing.SetGrabDistance(config.grabRayDistance);
        }
    }
    

}