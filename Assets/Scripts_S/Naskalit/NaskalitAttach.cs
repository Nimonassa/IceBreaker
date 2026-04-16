using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NaskalitAttach : MonoBehaviour
{
    [Header("References")]
    public Transform neckSocket;

    [Header("Settings")]
    public float attachDistance = 0.2f;

    private Rigidbody rb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private bool isAttached = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void Update()
    {
        if (isAttached)
        {
            transform.position = neckSocket.position;
            transform.rotation = neckSocket.rotation;
            return;
        }

        if (IsBeingHeld())
            return;

        // check distance to neck
        float distance = Vector3.Distance(transform.position, neckSocket.position);

        if (distance < attachDistance)
        {
            AttachToNeck();
        }
    }

    void AttachToNeck()
    {
        isAttached = true;

        transform.SetParent(neckSocket);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
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

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = true;
    }

    public bool IsAttached()
    {
        return isAttached;
    } 

    bool IsBeingHeld()
    {
        if (grab != null)
            return grab.isSelected;

        return false;
    }

    void OnEnable()
    {
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrab);
        }
    }

    void OnDisable()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (isAttached)
        {
            Detach();
        }
    }
}