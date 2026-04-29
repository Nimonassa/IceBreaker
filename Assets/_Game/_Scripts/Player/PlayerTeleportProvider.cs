using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class PlayerTeleportProvider : TeleportationProvider
{
    public enum TeleportMode { Blink, Shift }

    [Header("Shift Settings")]
    [SerializeField] private float shiftSpeed = 20f;
    [SerializeField] private CharacterController characterController;

    [Header("Internal Events")]
    public UnityEvent onBlinkStarted = new();
    public UnityEvent onShiftStarted = new();
    public UnityEvent onShiftEnded = new();

    private TeleportMode currentMode = TeleportMode.Blink;
    private Coroutine activeShift;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
        }
    }

    public void SetMode(TeleportMode mode)
    {
        currentMode = mode;
    }

    public void SetShiftSpeed(float speed)
    {
        shiftSpeed = speed;
    }

    public override bool QueueTeleportRequest(TeleportRequest teleportRequest)
    {
        if (currentMode == TeleportMode.Blink)
        {
            onBlinkStarted?.Invoke();
            return base.QueueTeleportRequest(teleportRequest);
        }

        // Handle Shift/Dash
        if (activeShift != null)
        {
            StopCoroutine(activeShift);
        }

        activeShift = StartCoroutine(ShiftRoutine(teleportRequest.destinationPosition));
        return true;
    }

    private IEnumerator ShiftRoutine(Vector3 targetPos)
    {
        if (mediator == null || mediator.xrOrigin == null)
        {
            yield break;
        }

        onShiftStarted?.Invoke();
        
        Transform playerTransform = mediator.xrOrigin.transform;
        Vector3 startPos = playerTransform.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / shiftSpeed;
        float elapsed = 0;

        // Disable physics during the dash to prevent "jittering" or overshooting
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

       playerTransform.position = targetPos;

        if (characterController != null)
        {
            float originalStepOffset = characterController.stepOffset;
            characterController.stepOffset = 0.001f;
            characterController.enabled = true;
            characterController.stepOffset = originalStepOffset;
        }

        activeShift = null;
        onShiftEnded?.Invoke();
    }
}