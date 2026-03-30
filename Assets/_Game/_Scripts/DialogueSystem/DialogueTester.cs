using UnityEngine;
using UnityEngine.InputSystem; // <-- Added this to use the New Input System
using XNode;

public class DialogueTester : MonoBehaviour
{
    [Header("Setup")]
    public DialogueManager dialogueManager;
    public DialogueSceneGraph dialogueSceneGraph; 

    // Hidden so our Custom Editor can draw the dropdown menu instead!
    [HideInInspector] 
    public BaseNode startingNode;

    [Header("Controls")]
    // Swapped KeyCode out for the new system's Key enum
    public Key startKey = Key.E;
    public Key advanceKey = Key.Space;

    void Update()
    {
        // Safety check to ensure a keyboard is connected
        if (Keyboard.current == null) return;

        // Press 'E' to kick off the conversation
        if (Keyboard.current[startKey].wasPressedThisFrame)
        {
            if (dialogueManager != null && startingNode != null)
            {
                Debug.Log($"Starting Dialogue at node: {startingNode.name}");
                dialogueManager.StartDialogue(startingNode);
            }
            else
            {
                Debug.LogWarning("Missing references! Assign the DialogueManager, SceneGraph, and pick a Starting Node.");
            }
        }

        // Press 'Space' to advance text
        if (Keyboard.current[advanceKey].wasPressedThisFrame)
        {
            if (dialogueManager != null)
            {
                dialogueManager.AdvanceFromChat(); 
            }
        }
    }
}