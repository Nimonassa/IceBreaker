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

}