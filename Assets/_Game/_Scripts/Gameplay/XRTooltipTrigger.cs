using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRTooltipTrigger : MonoBehaviour
{
    [Header("Tooltip Settings")]
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private XRButtonType targetButton = XRButtonType.Primary;

    public void ShowTooltip(HoverEnterEventArgs args)
    {
        XRTooltipManager.Instance.Show(prompt, args.interactorObject, targetButton, transform);
    }

    public void HideTooltip(HoverExitEventArgs args)
    {
        XRTooltipManager.Instance.Hide();
    }

    public void HideTooltip()
    {
        XRTooltipManager.Instance.Hide();
    }
}