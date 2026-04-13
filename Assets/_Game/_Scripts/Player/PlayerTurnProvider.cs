using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class PlayerTurnProvider : SnapTurnProvider
{
    public enum TurnMode { Snap, Shift }

    [Header("Shift Settings")]
    [SerializeField] private float shiftTurnSpeed = 180f; // Degrees per second

    [Header("Turn Events")]
    public UnityEvent onSnapTurn = new();
    public UnityEvent onShiftStarted = new();
    public UnityEvent onShiftEnded = new();

    private TurnMode currentMode = TurnMode.Snap;
    private Coroutine activeTurn;
    private float currentTurnDebounceTime;

    public void SetMode(TurnMode mode) => currentMode = mode;
    
    public void SetShiftSpeed(float speed) => shiftTurnSpeed = speed;

    protected new void Update()
    {
        if (currentTurnDebounceTime > 0f)
        {
            currentTurnDebounceTime -= Time.deltaTime;
            return;
        }

        Vector2 leftInput = leftHandTurnInput != null ? leftHandTurnInput.ReadValue() : Vector2.zero;
        Vector2 rightInput = rightHandTurnInput != null ? rightHandTurnInput.ReadValue() : Vector2.zero;
        Vector2 combinedInput = leftInput + rightInput;

        float inputMagnitude = combinedInput.magnitude;

        if (inputMagnitude > 0.5f)
        {
            float amount = 0f;

            if (enableTurnAround && combinedInput.y < -0.5f && Mathf.Abs(combinedInput.x) < 0.5f)
            {
                amount = 180f;
            }
            else if (enableTurnLeftRight && Mathf.Abs(combinedInput.x) >= 0.5f)
            {
                amount = Mathf.Sign(combinedInput.x) * turnAmount;
            }

            if (amount != 0f)
            {
                ExecuteTurn(amount);
            }
        }
    }

    private void ExecuteTurn(float amount)
    {
        if (activeTurn != null) return;

        currentTurnDebounceTime = debounceTime;

        if (currentMode == TurnMode.Snap)
        {
            PerformSnapTurn(amount);
        }
        else
        {
            activeTurn = StartCoroutine(ShiftTurnRoutine(amount));
        }
    }

    private void PerformSnapTurn(float amount)
    {
        if (mediator == null || mediator.xrOrigin == null) return;

        if (TryPrepareLocomotion())
        {
            onSnapTurn?.Invoke();
            mediator.xrOrigin.RotateAroundCameraUsingOriginUp(amount);
            TryEndLocomotion();
        }
    }

    private IEnumerator ShiftTurnRoutine(float amount)
    {
        if (mediator == null || mediator.xrOrigin == null) yield break;

        if (!TryPrepareLocomotion()) yield break;

        onShiftStarted?.Invoke();

        // Calculate duration based on how far we are turning and the speed setting
        float duration = Mathf.Abs(amount) / shiftTurnSpeed;
        float elapsed = 0;
        float lastRotation = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
            float currentRotation = amount * progress;

            mediator.xrOrigin.RotateAroundCameraUsingOriginUp(currentRotation - lastRotation);
            lastRotation = currentRotation;

            yield return null;
        }

        activeTurn = null;
        onShiftEnded?.Invoke();
        TryEndLocomotion();
    }
}