using UnityEngine;
using UnityEngine.Events;

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

    private Vector3 _lastPosition;
    private float _distanceAccumulator;
    private float _currentTargetDistance;

    private void Reset()
    {
        movement = GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        if (movement != null)
        {
            _lastPosition = movement.transform.position;
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
            _lastPosition = movement.transform.position;
            return;
        }

        Vector3 currentPos = movement.transform.position;
        Vector3 flatCurrent = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatLast = new Vector3(_lastPosition.x, 0, _lastPosition.z);

        float distanceMoved = Vector3.Distance(flatCurrent, flatLast);

        _lastPosition = currentPos;
        _distanceAccumulator += distanceMoved;

        if (_distanceAccumulator >= _currentTargetDistance)
        {
            _distanceAccumulator = 0f;
            CalculateNextStepTarget();
            events.onStepTaken?.Invoke();
        }
    }

    private void CalculateNextStepTarget()
    {
        _currentTargetDistance = stepDistance + Random.Range(-stepDistanceVariance, stepDistanceVariance);
        _currentTargetDistance = Mathf.Max(0.1f, _currentTargetDistance);
    }
}