using UnityEngine;
using UnityEngine.Events;
using XNode;

public abstract class BaseNode : Node
{
    [Input(ShowBackingValue.Never, ConnectionType.Multiple)]
    public int enter;

    // This is visible in the Unity Inspector, but hidden in our xNode custom editors
    public GameLanguage editingLanguage;

    [Header("Game Logic")]
    [xNodeUnityEvent] public UnityEvent onEnter;
    [xNodeUnityEvent] public UnityEvent onExit;

    public override object GetValue(NodePort port)
    {
        return null;
    }
}