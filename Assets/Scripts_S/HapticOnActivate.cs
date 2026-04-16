using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HapticOnActivate : MonoBehaviour
{
    public float intensity = 0.4f;
    public float duration = 0.08f;

    private XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        interactable.activated.AddListener(OnActivated);
    }

    void OnDisable()
    {
        interactable.activated.RemoveListener(OnActivated);
    }

    void OnActivated(ActivateEventArgs args)
    {
        var controller = args.interactorObject.transform.GetComponent<XRBaseController>();

        if (controller != null)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }
}