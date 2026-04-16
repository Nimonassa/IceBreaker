using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HapticOnGrab : MonoBehaviour
{
    public float intensity = 0.5f;
    public float duration = 0.1f;

    private XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        var controller = args.interactorObject.transform.GetComponent<XRBaseController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }
}