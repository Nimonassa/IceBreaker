using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    [Header("Systems")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerGrabbing grabbing;




    [Header("Hands")]
    [SerializeField] private PlayerHand leftHand;
    [SerializeField] private PlayerHand rightHand;
    public PlayerGrabbing Grabbing => grabbing;
    public PlayerMovement Movement => movement;
    public PlayerHand LeftHand => leftHand;
    public PlayerHand RightHand => rightHand;


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
        if (movement == null) movement = GetComponentInChildren<PlayerMovement>(true);

        if (leftHand == null || rightHand == null)
        {
            PlayerHand[] hands = GetComponentsInChildren<PlayerHand>(true);
            foreach (var hand in hands)
            {
                if (hand.side == HandSide.Left) leftHand = hand;
                if (hand.side == HandSide.Right) rightHand = hand;
            }
        }
    }

}