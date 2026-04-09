using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PlayerHandInput : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ControllerSide side;

    [Header("Input Bindings")]
    [SerializeField] private InputActionReference trigger, grip, primary, secondary, thumbstick;

    private XRBaseInteractor[] interactors;
    
    // The "Memory": Tracks what we found so we don't search again on Exit
    private Dictionary<IXRInteractable, IHandHover> hoverMemory = new Dictionary<IXRInteractable, IHandHover>();
    private Dictionary<IXRInteractable, IHandAction> actionMemory = new Dictionary<IXRInteractable, IHandAction>();
    
    private HashSet<IHandAction> hoveredActions = new HashSet<IHandAction>();

    void Awake()
    {
        interactors = GetComponentsInChildren<XRBaseInteractor>();

        foreach (var interactor in interactors)
        {
            interactor.hoverEntered.AddListener(OnXRIHoverEnter);
            interactor.hoverExited.AddListener(OnXRIHoverExit);
        }

        Bind(trigger, (t, s) => t.OnTrigger(s));
        Bind(grip, (t, s) => t.OnGrip(s));
        Bind(primary, (t, s) => t.OnPrimary(s));
        Bind(secondary, (t, s) => t.OnSecondary(s));
        Bind(thumbstick, (t, s) => t.OnThumbstick(s));
    }

    private void OnXRIHoverEnter(HoverEnterEventArgs args)
    {
        IXRInteractable interactable = args.interactableObject;
        GameObject obj = interactable.transform.gameObject;

        // 1. Tell Tooltip Manager which ray is active
        XRTooltipManager.Instance?.SetActiveInteractor(side, args.interactorObject);

        if (!obj.TryGetComponent(out IHandHover handTarget))
            handTarget = obj.GetComponentInParent<IHandHover>();
        if (!obj.TryGetComponent(out IHandAction actionTarget))
            actionTarget = obj.GetComponentInParent<IHandAction>();

        // 3. Store in Memory
        if (handTarget != null)
        {
            hoverMemory[interactable] = handTarget;
            handTarget.OnHoverEnter(side);
        }

        if (actionTarget != null)
        {
            actionMemory[interactable] = actionTarget;
            hoveredActions.Add(actionTarget);
        }
    }

    private void OnXRIHoverExit(HoverExitEventArgs args)
    {
        IXRInteractable interactable = args.interactableObject;

        // 4. Zero Search Lookup (Fastest possible way)
        if (hoverMemory.TryGetValue(interactable, out IHandHover hTarget))
        {
            hTarget.OnHoverExit(side);
            hoverMemory.Remove(interactable);
        }

        if (actionMemory.TryGetValue(interactable, out IHandAction aTarget))
        {
            hoveredActions.Remove(aTarget);
            actionMemory.Remove(interactable);
        }
    }

    private void Bind(InputActionReference actionRef, System.Action<IHandAction, ControllerSide> method)
    {
        if (actionRef == null) return;
        actionRef.action.performed += _ => 
        {
            foreach (var target in hoveredActions)
                method(target, side);
        };
    }
}