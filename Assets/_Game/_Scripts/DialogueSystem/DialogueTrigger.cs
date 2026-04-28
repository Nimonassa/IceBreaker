using UnityEngine;
using UnityEngine.InputSystem;
using XNode;

public enum TriggerMode { None, OnEnter, OnExit }
public enum InputType { None, Keyboard, InputAction, XRHand }
public enum XRHandButton { Trigger, Grip, Primary, Secondary, Thumbstick }

public class DialogueTrigger : MonoBehaviour
{
    [Header("Setup")]
    public DialogueManager dialogueManager;
    public DialogueSceneGraph dialogueSceneGraph;

    [HideInInspector]
    public BaseNode startingNode;

    [Header("Trigger Settings")]
    public TriggerMode triggerMode = TriggerMode.None;
    public InputType inputType = InputType.None;

    public Key startKey = Key.E;
    public InputActionReference startAction;
    
    // --- THE FIX: Drag your separate XRButtonTarget GameObject here ---
    public XRButtonTarget xrTarget; 
    public XRHandButton xrButton = XRHandButton.Primary; 

    [Tooltip("Which layers can trigger this dialogue? (Select 'Nothing' to ignore layer filtering)")]
    public LayerMask triggerLayerMask;

    [Tooltip("Which tag can trigger this dialogue? (Select 'Nothing' to ignore tag filtering)")]
    public string triggerTag = "Nothing";
    public bool triggerOnce = false; 

    [HideInInspector] public bool displayOnlyRootNodes = true;
    [HideInInspector] public bool autoAddedCollider = false;

    private bool isDialogueActive = false;
    private int collidersInRange = 0;
    private bool isPlayerInRange => collidersInRange > 0;

    private void Reset()
    {
        gameObject.name = "Dialogue Trigger";
        if (dialogueManager == null) dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueSceneGraph == null) dialogueSceneGraph = FindFirstObjectByType<DialogueSceneGraph>();
    }

    private void OnEnable()
    {
        if (startAction != null) startAction.action.performed += OnActionPerformed;
        
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueStarted += HandleDialogueStarted;
            dialogueManager.OnDialogueEnded += HandleDialogueEnded;
        }

        // --- SUBSCRIBE TO XR EVENTS ---
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
        
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueStarted -= HandleDialogueStarted;
            dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        }

        // --- UNSUBSCRIBE FROM XR EVENTS ---
        if (xrTarget != null)
        {
            xrTarget.OnTriggerPressed -= HandleXRTrigger;
            xrTarget.OnGripPressed -= HandleXRGrip;
            xrTarget.OnPrimaryPressed -= HandleXRPrimary;
            xrTarget.OnSecondaryPressed -= HandleXRSecondary;
            xrTarget.OnThumbstickPressed -= HandleXRThumbstick;
        }
    }

    private void HandleDialogueStarted() => isDialogueActive = true;
    private void HandleDialogueEnded() => isDialogueActive = false;

    // --- INPUT HANDLING ---

    private void TryTriggerFromInput()
    {
        if (isDialogueActive) return;
        if (triggerMode != TriggerMode.None && !isPlayerInRange) return;
        TriggerDialogue();
    }

    private void Update()
    {
        if (Keyboard.current == null || isDialogueActive) return;
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
            Debug.Log("Input pressed!");
            TryTriggerFromInput();
        }
    }

    // --- PHYSICS / COLLIDER HANDLING ---

    

    private void OnTriggerEnter(Collider other)
    {
        if (EvaluateFilter(other.gameObject))
        {
            collidersInRange++;
            if (!isDialogueActive && triggerMode == TriggerMode.OnEnter && inputType == InputType.None) 
                TriggerDialogue();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (EvaluateFilter(other.gameObject))
        {
            collidersInRange--;
            if (collidersInRange < 0) collidersInRange = 0; // Safety catch

            if (!isDialogueActive && triggerMode == TriggerMode.OnExit && inputType == InputType.None)
                TriggerDialogue();
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

    public void TriggerDialogue()
    {
        if (isDialogueActive) return;

        if (dialogueManager != null && startingNode != null)
        {
            dialogueManager.StartDialogue(startingNode);
            if (triggerOnce) this.enabled = false;
        }
    }
}