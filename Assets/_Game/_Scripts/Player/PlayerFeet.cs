using UnityEngine;
using UnityEngine.Events;

// probably useful abstraction for the future
public class PlayerFeet : MonoBehaviour
{
    [System.Serializable]
    public class FeetEvents
    {
        public UnityEvent onStepTaken = new();
    }

    [Header("Events")]
    public FeetEvents events = new FeetEvents();

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
        movement = GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        if (movement != null)
        {
            lastPosition = movement.transform.position;
        }
        CalculateNextStepTarget();
    }

    private void Update()
    {
        HandleContinuousFootsteps();
    }

    private void HandleContinuousFootsteps()
    {
        if (movement == null || movement.CurrentLocomotion != MoveType.Continuous)
            return;

        if (!movement.IsGrounded())
        {
            lastPosition = movement.transform.position;
            return;
        }

        Vector3 currentPos = movement.transform.position;
        Vector3 flatCurrent = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatLast = new Vector3(lastPosition.x, 0, lastPosition.z);

        float distanceMoved = Vector3.Distance(flatCurrent, flatLast);

        lastPosition = currentPos;
        distanceAccumulator += distanceMoved;

        if (distanceAccumulator >= currentTargetDistance)
        {
            distanceAccumulator = 0f;
            CalculateNextStepTarget();
            events.onStepTaken?.Invoke();
        }
    }

    private void CalculateNextStepTarget()
    {
        currentTargetDistance = stepDistance + Random.Range(-stepDistanceVariance, stepDistanceVariance);
        currentTargetDistance = Mathf.Max(0.1f, currentTargetDistance);
    }
}