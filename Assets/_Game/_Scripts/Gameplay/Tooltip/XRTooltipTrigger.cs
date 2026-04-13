using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRTooltipTrigger : MonoBehaviour, IHandHover, IPointerEnterHandler, IPointerExitHandler
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData is TrackedDeviceEventData trackedEvent && trackedEvent.interactor != null)
        {
            IXRInteractor xriInteractor = trackedEvent.interactor as IXRInteractor;
            Component interactorComponent = trackedEvent.interactor as Component;

            if (xriInteractor != null && interactorComponent != null)
            {
                ControllerSide side = interactorComponent.name.ToLower().Contains("left") 
                    ? ControllerSide.Left 
                    : ControllerSide.Right;

                XRTooltipManager.Instance.SetActiveInteractor(side, xriInteractor);
                XRTooltipManager.Instance.Show(prompt, side, targetButton, transform);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        XRTooltipManager.Instance.Hide();
    }
}