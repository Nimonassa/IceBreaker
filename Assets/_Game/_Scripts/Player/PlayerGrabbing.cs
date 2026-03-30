using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public enum InteractionHand { Left, Right, Both, None }

public class PlayerGrabbing : MonoBehaviour
{
    [System.Serializable]
    public class GrabbingEvents
    {
        public UnityEvent<GameObject> onObjectGrabbed = new();
        public UnityEvent<GameObject> onObjectReleased = new();
        public UnityEvent onHoverEnter = new();
    }

    [Header("Grabbing Events")]
    public GrabbingEvents events = new GrabbingEvents();

    [Header("Global Settings")]
    [SerializeField] private InteractionHand grabHand = InteractionHand.Both;
    [SerializeField] private InteractorFarAttachMode attachMode = InteractorFarAttachMode.Far;
    [SerializeField] private float grabRayDistance = 5f;

    private void Start()
    {
        UpdateSettings();
    }
    private void OnEnable()
    {
        foreach (var interactor in GetComponentsInChildren<XRBaseInteractor>(true))
        {
            interactor.selectEntered.AddListener(HandleGrab);
            interactor.selectExited.AddListener(HandleRelease);
            interactor.hoverEntered.AddListener(HandleHover);
        }
    }

    private void OnDisable()
    {
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
        events.onObjectGrabbed?.Invoke(args.interactableObject.transform.gameObject);
    }
    
    private void HandleRelease(SelectExitEventArgs args)
    {
        events.onObjectReleased?.Invoke(args.interactableObject.transform.gameObject);
    }

    private void HandleHover(HoverEnterEventArgs args)
    {
        events.onHoverEnter?.Invoke();
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
