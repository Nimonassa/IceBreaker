using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionTrigger : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("What happens when this trigger is activated?")]
    public UnityEvent onTriggered;

    [Header("Trigger Settings")]
    public TriggerMode triggerMode = TriggerMode.None;
    public InputType inputType = InputType.None;

    public Key startKey = Key.E;
    public InputActionReference startAction;
    
    public XRButtonTarget xrTarget; 
    public XRHandButton xrButton = XRHandButton.Primary; 

    [Tooltip("Which layers can trigger this? (Select 'Nothing' to ignore layer filtering)")]
    public LayerMask triggerLayerMask;

    [Tooltip("Which tag can trigger this? (Select 'Nothing' to ignore tag filtering)")]
    public string triggerTag = "Nothing";
    public bool triggerOnce = false; 

    [HideInInspector] public bool autoAddedCollider = false;

    private int collidersInRange = 0;
    private bool isPlayerInRange => collidersInRange > 0;

    private void OnEnable()
    {
        if (startAction != null) startAction.action.performed += OnActionPerformed;
        
        if (xrTarget != null)
        {
            xrTarget.OnTriggerPressed += HandleXRTrigger;
            xrTarget.OnGripPressed += HandleXRGrip;
            xrTarget.OnPrimaryPressed += HandleXRPrimary;
            xrTarget.OnSecondaryPressed += HandleXRSecondary;
            xrTarget.OnThumbstickPressed += HandleXRThumbstick;
        }
    }

    private void OnDisable()
    {
        if (startAction != null) startAction.action.performed -= OnActionPerformed;
        
        if (xrTarget != null)
        {
            xrTarget.OnTriggerPressed -= HandleXRTrigger;
            xrTarget.OnGripPressed -= HandleXRGrip;
            xrTarget.OnPrimaryPressed -= HandleXRPrimary;
            xrTarget.OnSecondaryPressed -= HandleXRSecondary;
            xrTarget.OnThumbstickPressed -= HandleXRThumbstick;
        }
    }

    // --- INPUT HANDLING ---

    private void TryTriggerFromInput()
    {
        // If it requires a trigger zone (OnEnter/OnExit), make sure the player is actually in it.
        if (triggerMode != TriggerMode.None && !isPlayerInRange) return;
        ExecuteTrigger();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (inputType == InputType.Keyboard && Keyboard.current[startKey].wasPressedThisFrame)
        {
            TryTriggerFromInput();
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        if (inputType == InputType.InputAction) TryTriggerFromInput();
    }

    // --- XR EVENT RECEIVERS ---
    private void HandleXRTrigger(ControllerSide side) => ProcessXRInput(XRHandButton.Trigger);
    private void HandleXRGrip(ControllerSide side) => ProcessXRInput(XRHandButton.Grip);
    private void HandleXRPrimary(ControllerSide side) => ProcessXRInput(XRHandButton.Primary);
    private void HandleXRSecondary(ControllerSide side) => ProcessXRInput(XRHandButton.Secondary);
    private void HandleXRThumbstick(ControllerSide side) => ProcessXRInput(XRHandButton.Thumbstick);

    private void ProcessXRInput(XRHandButton pressedButton)
    {
        if (inputType == InputType.XRHand && xrButton == pressedButton)
        {
            TryTriggerFromInput();
        }
    }

    // --- PHYSICS / COLLIDER HANDLING ---

    private void OnTriggerEnter(Collider other)
    {
        if (EvaluateFilter(other.gameObject))
        {
            collidersInRange++;
            if (triggerMode == TriggerMode.OnEnter && inputType == InputType.None) 
                ExecuteTrigger();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (EvaluateFilter(other.gameObject))
        {
            collidersInRange--;
            if (collidersInRange < 0) collidersInRange = 0; // Safety catch

            if (triggerMode == TriggerMode.OnExit && inputType == InputType.None)
                ExecuteTrigger();
        }
    }

    private bool EvaluateFilter(GameObject obj)
    {
        bool hasLayerFilter = triggerLayerMask.value != 0;
        bool hasTagFilter = !string.IsNullOrEmpty(triggerTag) && triggerTag != "Nothing";

        bool layerPass = !hasLayerFilter || ((triggerLayerMask.value & (1 << obj.layer)) > 0);
        bool tagPass = !hasTagFilter || obj.CompareTag(triggerTag);

        return layerPass && tagPass;
    }

    // --- EXECUTION ---

    public void ExecuteTrigger()
    {
        onTriggered?.Invoke();
        if (triggerOnce) this.enabled = false;
    }
}