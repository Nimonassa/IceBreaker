using UnityEngine;
using XNode;

// This adds a button to Unity's right-click menu so writers can create new conversations
[CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue System/Dialogue Graph")]
public class DialogueGraph : NodeGraph 
{
    // A graph doesn't need logic! It's just a container that holds our nodes.
}