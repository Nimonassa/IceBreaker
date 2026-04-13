using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(CharacterController))]
public class PlayerSplineMovement : MonoBehaviour
{
    [Header("Rig References")]
    public CharacterController characterController;

    [Header("Speed & Timing")]
    public float velocity = 0.75f;
    [Tooltip("Use an S-Curve (Ease In/Out) for natural starts and stops.")]
    public AnimationCurve movementPacing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("VR Smoothing & Gaze")]
    public float lookAheadTime = 1.5f;
    public float minLookAheadDistance = 1.0f;
    public float rotationSmoothTime = 0.2f;
    public float maxRotationSpeed = 120f;
    
    [Header("Human Biomechanics")]
    public float strideLength = 1.5f;
    [Range(0, 0.05f)] public float verticalBobAmplitude = 0.0125f; 
    [Range(0, 0.05f)] public float lateralSwayAmplitude = 0.01f;
    
    [Header("Gait Realism")]
    public float gaitSurgeIntensity = 0.12f; 
    [Tooltip("The baseline peak of the stride when a footstep triggers.")]
    public float baseStepThreshold = 0.75f;
    [Tooltip("How much randomness to apply to the step timing so it feels less robotic.")]
    public float stepThresholdVariation = 0.05f;
    
    [Header("Centripetal Lean")]
    public float leanIntensity = 0.12f;
    public float leanSmoothTime = 0.3f;

    [Header("Elevation & Pitch")]
    public float pitchSmoothTime = 0.75f;

    [Header("Events")]
    public UnityEvent OnMovementComplete;

    // Active State
    private SplineContainer currentSpline;
    public bool isPlaying { get; private set; }

    // Internal Math State
    private float currentTime = 0f;
    private float splineLength;
    private float currentYawVelocity;
    private float currentPitchVelocity;
    private float currentRollVelocity;
    
    private bool _isStepTriggered = false;
    private float _currentStepThreshold; // Replaced the const with a dynamic variable

    void Start()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Call this from your cutscene manager to begin movement along a specific spline.
    /// </summary>
    public void StartMovement(SplineContainer targetSpline)
    {
        if (targetSpline == null)
        {
            Debug.LogWarning("SplineMovement: Cannot start, target spline is null.");
            return;
        }

        currentSpline = targetSpline;
        splineLength = currentSpline.CalculateLength();

        // Reset all internal state so we can reuse this component multiple times
        currentTime = 0f;
        currentYawVelocity = 0f;
        currentPitchVelocity = 0f;
        currentRollVelocity = 0f;
        
        _isStepTriggered = false;
        _currentStepThreshold = baseStepThreshold; // Initialize the first threshold

        isPlaying = true;
    }

    /// <summary>
    /// Instantly halts the movement.
    /// </summary>
    public void StopMovement()
    {
        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying || currentSpline == null) return;

        // 1. Calculate Progress
        float totalDuration = splineLength / velocity;
        currentTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(currentTime / totalDuration);
        
        // Use the pacing curve to find our current "Target T" on the spline
        float evaluativeT = movementPacing.Evaluate(normalizedTime);
        
        EvaluateMovement(evaluativeT);

        // Stop if we reached the end
        if (normalizedTime >= 1.0f) 
        {
            isPlaying = false;
            OnMovementComplete?.Invoke();
        }
    }

    private void EvaluateMovement(float t)
    {
        float distanceTraveled = t * splineLength;
        

        // 2. Gait Surge Math 
        float stridePhase = (distanceTraveled / strideLength) * (Mathf.PI * 2f);
        float surge = 1.0f + (Mathf.Cos(stridePhase * 2f) * gaitSurgeIntensity);
        
        // 3. Evaluate Spline Position
        currentSpline.Evaluate(t, out float3 pos, out float3 tan, out float3 up);
        Vector3 pathRight = math.normalize(math.cross(up, tan));
        Vector3 pathUp = up;

        // 4. Step Detection & Audio Triggers
        float stepDetectionValue = Mathf.Abs(Mathf.Cos(stridePhase));
        
        // Trigger the step using our dynamically changing threshold
        if (stepDetectionValue >= _currentStepThreshold && !_isStepTriggered)
        {
            PlayerEvents.OnStepTaken.Invoke(); 
            _isStepTriggered = true;
        }
        // Reset the step and generate a new random threshold for the next footfall
        else if (stepDetectionValue < _currentStepThreshold - 0.1f)
        {
            if (_isStepTriggered)
            {
                // Calculate a new threshold slightly higher or lower than the base
                _currentStepThreshold = baseStepThreshold + UnityEngine.Random.Range(-stepThresholdVariation, stepThresholdVariation);
            }
            _isStepTriggered = false;
        }

        // 5. Head Bob & Lateral Sway
        float verticalBob = -stepDetectionValue * verticalBobAmplitude;
        float lateralSway = Mathf.Sin(stridePhase) * lateralSwayAmplitude;

        Vector3 finalPos = (Vector3)pos + (pathUp * verticalBob) + (pathRight * lateralSway);

        if (characterController != null)
        {
            float distanceToFeet = (characterController.height / 2f) - characterController.center.y;
            finalPos.y += distanceToFeet;
        }

        transform.position = finalPos;

        // 6. Apply Rotation 
        ApplyVRRotation(pos, t);
    }

    private void ApplyVRRotation(float3 pos, float currentT)
    {
        float lookAheadOffset = Mathf.Max(minLookAheadDistance, velocity * lookAheadTime) / splineLength;
        float futureT = Mathf.Clamp01(currentT + lookAheadOffset);
        currentSpline.Evaluate(futureT, out float3 futurePos, out float3 _, out float3 _);

        Vector3 direction = (Vector3)futurePos - (Vector3)pos;
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z);

        if (flatDirection.sqrMagnitude > 0.001f)
        {
            float targetYaw = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
            float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref currentYawVelocity, rotationSmoothTime, maxRotationSpeed);

            float targetPitch = -Mathf.Atan2(direction.y, flatDirection.magnitude) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, -30f, 30f);
            float smoothedPitch = Mathf.SmoothDampAngle(transform.eulerAngles.x, targetPitch, ref currentPitchVelocity, pitchSmoothTime);

            float yawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, targetYaw);
            float targetRoll = -yawDelta * leanIntensity; 
            float smoothedRoll = Mathf.SmoothDampAngle(transform.eulerAngles.z, targetRoll, ref currentRollVelocity, leanSmoothTime);

            transform.rotation = Quaternion.Euler(smoothedPitch, smoothedYaw, smoothedRoll);
        }
    }
}