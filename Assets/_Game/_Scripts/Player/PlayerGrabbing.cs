using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public enum InteractionHand { Left, Right, Both, None }

public class PlayerGrabbing : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private InteractionHand grabHand = InteractionHand.Both;
    [SerializeField] private InteractorFarAttachMode attachMode = InteractorFarAttachMode.Far;
    [SerializeField] private float grabRayDistance = 5f;

    private void Start()
    {
        UpdateSettings(); // Initializing hand settings on start
    }

    private void OnEnable()
    {
        // Subscribe to XR interactor events for all children
        foreach (var interactor in GetComponentsInChildren<XRBaseInteractor>(true))
        {
            interactor.selectEntered.AddListener(HandleGrab);
            interactor.selectExited.AddListener(HandleRelease);
            interactor.hoverEntered.AddListener(HandleHover);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        foreach (var interactor in GetComponentsInChildren<XRBaseInteractor>(true))
        {
            interactor.selectEntered.RemoveListener(HandleGrab);
            interactor.selectExited.RemoveListener(HandleRelease);
            interactor.hoverEntered.RemoveListener(HandleHover);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateSettings();
    }
#endif

    private void HandleGrab(SelectEnterEventArgs args)
    {
        // Invoke the global static event with the grabbed GameObject
        PlayerEvents.OnObjectGrabbed.Invoke(args.interactableObject.transform.gameObject);
    }

    private void HandleRelease(SelectExitEventArgs args)
    {
        PlayerEvents.OnObjectReleased.Invoke(args.interactableObject.transform.gameObject);
    }

    private void HandleHover(HoverEnterEventArgs args)
    {
        PlayerEvents.OnHoverEnter.Invoke();
    }

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
            player.LeftHand.SetGrabRayDistance(distance); // Update left hand distance
            player.RightHand.SetGrabRayDistance(distance); // Update right hand distance
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

            player.LeftHand.SetGrabRayActive(enableLeft); // Toggle left hand ray
            player.RightHand.SetGrabRayActive(enableRight); // Toggle right hand ray
        }
    }

    public void SetGrabAttachMode(InteractorFarAttachMode mode)
    {
        attachMode = mode;

        var player = PlayerManager.Instance;
        if (player != null)
        {
            player.LeftHand.SetGrabAttachMode(mode); // Set left hand attach mode
            player.RightHand.SetGrabAttachMode(mode); // Set right hand attach mode
        }
    }
}
