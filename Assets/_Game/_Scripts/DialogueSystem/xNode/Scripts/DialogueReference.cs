using System;

[Serializable]
public class DialogueReference
{
    public DialogueSceneGraph sceneGraph;
    public BaseNode node;

    public bool IsValid => sceneGraph != null && sceneGraph.graph != null && node != null;

    // Helper to play this specific reference with a callback
    public void Play(System.Action onComplete = null)
    {
        if (IsValid)
        {
            // Assuming DialogueManager is a Singleton or accessible
            DialogueManager.Instance?.StartDialogue(node, onComplete);
        }
    }
}