using UnityEngine;

public class XRTooltipTrigger : MonoBehaviour, IHandHover
{
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private XRButtonType targetButton = XRButtonType.Primary;

    public void OnHoverEnter(ControllerSide side)
    {
        XRTooltipManager.Instance.Show(prompt, side, targetButton, transform);
    }

    public void OnHoverExit(ControllerSide side)
    {
        XRTooltipManager.Instance.Hide();
    }
}