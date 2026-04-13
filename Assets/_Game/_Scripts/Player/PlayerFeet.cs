using UnityEngine;

// Detects physical movement and triggers footstep events based on distance traveled
public class PlayerFeet : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement movement;

    [Header("Step Settings")]
    [SerializeField] private float stepDistance = 0.75f;
    [SerializeField, Range(0f, 1f)] private float stepDistanceVariance = 0.05f;

    private Vector3 lastPosition;
    private float distanceAccumulator;
    private float currentTargetDistance;

    private void Reset()
    {
        movement = GetComponentInParent<PlayerMovement>(); // Auto-assign dependency in editor
    }

    private void Start()
    {
        if (movement != null)
        {
            lastPosition = movement.transform.position; // Track starting position
        }
        
        CalculateNextStepDistance();
    }

    private void Update()
    {
        HandleContinuousFootsteps();
    }

    private void HandleContinuousFootsteps()
    {
        // Only process footsteps if the player is in continuous movement mode
        if (movement == null || movement.CurrentLocomotion != MoveType.Continuous)
            return;

        // Reset tracking if the player is airborne
        if (!movement.IsGrounded())
        {
            lastPosition = movement.transform.position;
            return;
        }

        // Calculate horizontal distance moved (ignoring height changes)
        Vector3 currentPos = movement.transform.position;
        Vector3 flatCurrent = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatLast = new Vector3(lastPosition.x, 0, lastPosition.z);

        float distanceMoved = Vector3.Distance(flatCurrent, flatLast);

        lastPosition = currentPos;
        distanceAccumulator += distanceMoved;

        if (distanceAccumulator >= currentTargetDistance)
        {
            distanceAccumulator = 0f;
            CalculateNextStepDistance();
        
            PlayerEvents.OnStepTaken.Invoke();
        }
    }

    private void CalculateNextStepDistance()
    {
        currentTargetDistance = stepDistance + Random.Range(-stepDistanceVariance, stepDistanceVariance);
        currentTargetDistance = Mathf.Max(0.1f, currentTargetDistance);
    }
}