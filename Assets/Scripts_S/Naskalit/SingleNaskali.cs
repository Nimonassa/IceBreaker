using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SingleNaskali : MonoBehaviour
{
    private Rigidbody rb;
    private XRGrabInteractable grab;
    private bool isAttached = false;

    public Transform neckSocket;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
    }

    public void AttachToNeck()
    {
        isAttached = true;

        transform.SetParent(neckSocket);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void Detach()
    {
        isAttached = false;

        transform.SetParent(null);

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    void OnEnable()
    {
        if (grab != null)
            grab.selectEntered.AddListener(OnGrab);
    }

    void OnDisable()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (isAttached)
        {
            Detach();
        }
    }

    public bool IsAttached()
    {
        return isAttached;
    }
}