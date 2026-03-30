using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;


public enum InteractionHand { Left, Right, Both, None }

public class PlayerGrabbing : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private InteractionHand grabHand = InteractionHand.Both;
    [SerializeField] private InteractorFarAttachMode attachMode = InteractorFarAttachMode.Far;
    [SerializeField] private float grabRayDistance = 5f;

    private void Start()
    {
        UpdateSettings();
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateSettings();
    }
#endif

    public void UpdateSettings()
    {
        SetGrabDistance(grabRayDistance);
        SetGrabHand(grabHand);
        SetGrabAttachMode(attachMode);
    }

    public void SetGrabDistance(float distance)
    {
        grabRayDistance = distance;

        var player = PlayerManager.Instance;
        if (player != null)
        {
            player.LeftHand.SetGrabRayDistance(distance);
            player.RightHand.SetGrabRayDistance(distance);
        }
    }

    public void SetGrabHand(InteractionHand hand)
    {
        grabHand = hand;

        var player = PlayerManager.Instance;
        if (player != null)
        {
            bool enableLeft = (hand == InteractionHand.Left || hand == InteractionHand.Both);
            bool enableRight = (hand == InteractionHand.Right || hand == InteractionHand.Both);

            player.LeftHand.SetGrabRayActive(enableLeft);
            player.RightHand.SetGrabRayActive(enableRight);
        }
    }
    public void SetGrabAttachMode(InteractorFarAttachMode mode)
    {
        attachMode = mode;

        var player = PlayerManager.Instance;
        if (player != null)
        {
            player.LeftHand.SetGrabAttachMode(mode);
            player.RightHand.SetGrabAttachMode(mode);
        }
    }
}
