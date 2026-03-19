using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public enum HandSide { Left, Right }

public class PlayerHand : MonoBehaviour
{
    public HandSide side;

    [Header("Interactors")]
    [SerializeField] private NearFarInteractor nearFar;
    [SerializeField] private XRRayInteractor teleportRay;
    [SerializeField] private CurveInteractionCaster farGrabCaster;

    private void OnValidate()
    {
        if (nearFar == null)
            nearFar = GetComponent<NearFarInteractor>();

        if (farGrabCaster == null)
            farGrabCaster = GetComponentInChildren<CurveInteractionCaster>(true);

        if (teleportRay == null)
        {
            var teleportMarker = GetComponentInChildren<TeleportRayTag>(true);
            if (teleportMarker != null)
            {
                teleportRay = teleportMarker.GetComponent<XRRayInteractor>();
            }
        }
    }
    

    private void Awake()
    {
        SetTeleportActive(false);
    }

    public void SetTeleportActive(bool isActive)
    {
        if (teleportRay != null)
            teleportRay.gameObject.SetActive(isActive);
    }

    public void SetTeleportDistance(float distance)
    {
        if (teleportRay == null) return;

        if (teleportRay.lineType == XRRayInteractor.LineType.StraightLine)
        {
            teleportRay.maxRaycastDistance = distance;
        }
        else if (teleportRay.lineType == XRRayInteractor.LineType.ProjectileCurve)
        {
            teleportRay.velocity = Mathf.Sqrt(distance * teleportRay.acceleration); // We use physics math (v = sqrt(d * g)) to limit the arc to your desired distance.
        }
    }


    public void SetInteractionActive(bool isActive)
    {
        if (nearFar != null)
        {
            nearFar.enableNearCasting = isActive;
            nearFar.enableFarCasting = isActive;
        }
    }

    public void SetGrabRayActive(bool isActive)
    {
        if (nearFar != null)
        {
            nearFar.enableFarCasting = isActive;
        }
    }

    public void SetGrabRayDistance(float distance)
    {
        if (farGrabCaster != null)
        {
            farGrabCaster.castDistance = distance;
        }
    }

    public void SetGrabAttachMode(InteractorFarAttachMode mode)
    {
        if (nearFar != null)
        {
            nearFar.farAttachMode = mode;
        }
    }
}