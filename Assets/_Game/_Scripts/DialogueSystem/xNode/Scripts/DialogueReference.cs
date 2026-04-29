using System;

[Serializable]
public class DialogueReference
{
    public DialogueSceneGraph sceneGraph;
    public BaseNode node;

    public bool IsValid => sceneGraph != null && sceneGraph.graph != null && node != null;
}