using UnityEngine;
using UnityEngine.Events;

public static class PlayerEvents
{
    public static UnityEvent<float> OnFreezingStart = new();
    public static UnityEvent OnFreezingEnd = new();

    // Locomotion - Teleport
    public static UnityEvent OnTeleportStarted = new();
    public static UnityEvent OnTeleportEnded = new();

    // Locomotion - Shift (Move)
    public static UnityEvent OnShiftStarted = new();
    public static UnityEvent OnShiftEnded = new();

    // Turning
    public static UnityEvent OnSnapTurn = new();
    public static UnityEvent OnShiftTurnStarted = new();
    public static UnityEvent OnShiftTurnEnded = new();

    // Interaction
    public static UnityEvent<GameObject> OnObjectGrabbed = new();
    public static UnityEvent<GameObject> OnObjectReleased = new();
    public static UnityEvent OnHoverEnter = new();

    // Movement Details
    public static UnityEvent OnStepTaken = new();
}
