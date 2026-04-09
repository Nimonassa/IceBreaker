using UnityEngine;

public enum ControllerSide { Left, Right }

public enum XRButtonType 
{ 
    None, Trigger, Grip, Primary, Secondary, Thumbstick 
}

public class XRControllerButtonMap : MonoBehaviour
{
    [Tooltip("Is this the Left or Right controller?")]
    public ControllerSide side = ControllerSide.Right;

    [System.Serializable]
    public struct ButtonReference
    {
        public XRButtonType buttonType;
        public Transform buttonTransform;
    }

    [SerializeField] private ButtonReference[] buttons;

    private void Start()
    {
        // Tell the manager to save a permanent reference to this script
        if (XRTooltipManager.Instance != null)
        {
            XRTooltipManager.Instance.RegisterControllerMap(this, side);
        }
    }

    public Transform GetButtonTransform(XRButtonType type)
    {
        if (type == XRButtonType.None) return transform;

        foreach (var btn in buttons)
        {
            if (btn.buttonType == type) return btn.buttonTransform;
        }
        
        return transform; 
    }
}