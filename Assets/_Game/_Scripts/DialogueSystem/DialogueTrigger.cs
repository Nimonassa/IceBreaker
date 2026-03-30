using UnityEngine;
using UnityEngine.InputSystem;
using XNode;

public enum TriggerType
{
    Manual,
    KeyPress,
    InputAction,
    OnTriggerEnter,
    OnTriggerExit
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Setup")]
    public DialogueManager dialogueManager;
    public DialogueSceneGraph dialogueSceneGraph;

    [HideInInspector]
    public BaseNode startingNode;

    [Header("Trigger Settings")]
    public TriggerType triggerType = TriggerType.Manual;

    [Tooltip("Used if TriggerType is KeyPress")]
    public Key startKey = Key.E;

    [Tooltip("Used if TriggerType is InputAction")]
    public InputActionReference startAction;

    [Tooltip("Which layers can trigger this dialogue? (Select 'Nothing' to ignore layer filtering)")]
    public LayerMask triggerLayerMask;

    [Tooltip("Which tag can trigger this dialogue? (Select 'Nothing' to ignore tag filtering)")]
    public string triggerTag = "Nothing";
    public bool triggerOnce = false; 


    [HideInInspector]
    public bool displayOnlyRootNodes = true;

    [HideInInspector]
    public bool autoAddedCollider = false;

    private bool isDialogueActive = false;

    private void Reset()
    {
        gameObject.name = "Dialogue Trigger";

        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        if (dialogueSceneGraph == null)
        {
            dialogueSceneGraph = FindFirstObjectByType<DialogueSceneGraph>();
        }
    }

    private void OnEnable()
    {
        if (startAction != null)
        {
            startAction.action.performed += OnActionPerformed;
        }

        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueStarted += HandleDialogueStarted;
            dialogueManager.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void OnDisable()
    {
        if (startAction != null)
        {
            startAction.action.performed -= OnActionPerformed;
        }

        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueStarted -= HandleDialogueStarted;
            dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        }
    }

    private void HandleDialogueStarted()
    {
        isDialogueActive = true;
    }

    private void HandleDialogueEnded()
    {
        isDialogueActive = false;
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        if (isDialogueActive) return;
        if (triggerType == TriggerType.InputAction) TriggerDialogue();
    }

    private void Update()
    {
        if (Keyboard.current == null || isDialogueActive) return;

        if (triggerType == TriggerType.KeyPress)
        {
            if (Keyboard.current[startKey].wasPressedThisFrame)
            {
                TriggerDialogue();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDialogueActive) return;
        if (triggerType == TriggerType.OnTriggerEnter) EvaluateTrigger(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDialogueActive) return;
        if (triggerType == TriggerType.OnTriggerExit) EvaluateTrigger(other.gameObject);
    }

    private void EvaluateTrigger(GameObject obj)
    {
        bool hasLayerFilter = triggerLayerMask.value != 0;
        bool hasTagFilter = !string.IsNullOrEmpty(triggerTag) && triggerTag != "Nothing";

        bool layerPass = !hasLayerFilter || ((triggerLayerMask.value & (1 << obj.layer)) > 0);
        bool tagPass = !hasTagFilter || obj.CompareTag(triggerTag);

        if (layerPass && tagPass) TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        if (isDialogueActive) return;

        if (dialogueManager != null && startingNode != null)
        {
            dialogueManager.StartDialogue(startingNode);
            if (triggerOnce) this.enabled = false;
        }
        else
        {
            Debug.LogWarning("DialogueTrigger: Missing references! Assign the DialogueManager, SceneGraph, and pick a Starting Node.");
        }
    }
}
