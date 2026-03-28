using UnityEngine;
using UnityEngine.InputSystem;


public class TVRemote : MonoBehaviour
{
    public Canvas tvCanvas;                
    public InputActionReference primaryButton; 

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        primaryButton.action.Enable();
    }

    void OnDisable()
    {
        primaryButton.action.Disable();
    }

    void Update()
    {
        if (grab.isSelected && primaryButton.action.WasPressedThisFrame())
        {
            Debug.Log("Primary button pressed");

            tvCanvas.gameObject.SetActive(!tvCanvas.gameObject.activeSelf);
        }
    }
}