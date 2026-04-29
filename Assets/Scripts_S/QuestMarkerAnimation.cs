using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class QuestMarker : MonoBehaviour
{
    public static UnityEvent OnShow = new();
    public static UnityEvent OnHide = new();


    [Header("Hover Settings")]
    [Tooltip("How fast the marker constantly bobs up and down.")]
    public float hoverSpeed = 2.0f;
    [Tooltip("How far the marker moves up and down while hovering.")]
    public float hoverAmplitude = 0.15f;

    [Header("Scale Settings")]
    [Tooltip("How long it takes to show or hide the marker (in seconds).")]
    public float scaleDuration = 0.3f;

    [Header("Rotation Settings")]
    [Tooltip("Should the marker rotate to face a target?")]
    public bool lookAtTarget = true;
    [Tooltip("The object to look at. If empty, it defaults to the Main Camera.")]
    public Transform targetTransform;

    public enum RotationAxis { All, Y_Only, X_Only, Z_Only }
    [Tooltip("Y_Only is usually best for UI/Sprites so they don't tilt up/down into the ground.")]
    public RotationAxis allowedAxis = RotationAxis.Y_Only;

    [Tooltip("Check this if the back of your marker is facing the player.")]
    public bool invertForward = false;

    private Vector3 originalScale;
    private Vector3 initialEulerAngles;
    private Coroutine activeScaleCoroutine;
    private float currentHoverOffset = 0f;

    private void Awake()
    {
        // Store the original scale and rotation
        originalScale = transform.localScale;
        initialEulerAngles = transform.rotation.eulerAngles;

        // Snap the scale to 0 instantly so the initial Show() has to animate from 0 up to 1
        transform.localScale = Vector3.zero;

        // QoL: If no target is set, try to find the Main Camera automatically
        if (lookAtTarget && targetTransform == null && Camera.main != null)
        {
            targetTransform = Camera.main.transform;
        }

        Show();
    }

    void Update()
    {
        // --- 1. HOVER LOGIC ---
        float newHoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
        float deltaY = newHoverOffset - currentHoverOffset;
        transform.localPosition += new Vector3(0, deltaY, 0);
        currentHoverOffset = newHoverOffset;


        // --- 2. ROTATION LOGIC ---
        if (lookAtTarget && targetTransform != null)
        {
            // Get the direction from the marker to the target
            Vector3 direction = targetTransform.position - transform.position;

            // Flatten the direction vector based on the allowed axis so the math is clean
            switch (allowedAxis)
            {
                case RotationAxis.Y_Only: direction.y = 0; break;
                case RotationAxis.X_Only: direction.x = 0; break;
                case RotationAxis.Z_Only: direction.z = 0; break;
            }

            // Flip the direction if the model was imported backwards
            if (invertForward)
            {
                direction = -direction;
            }

            // Prevent Unity from throwing a "Look rotation viewing vector is zero" error
            if (direction != Vector3.zero)
            {
                // Calculate the new look rotation
                Quaternion lookRot = Quaternion.LookRotation(direction);
                Vector3 finalEulerAngles = lookRot.eulerAngles;

                // Re-apply the initial rotation to the axes we are NOT allowed to change
                switch (allowedAxis)
                {
                    case RotationAxis.Y_Only:
                        finalEulerAngles.x = initialEulerAngles.x;
                        finalEulerAngles.z = initialEulerAngles.z;
                        break;
                    case RotationAxis.X_Only:
                        finalEulerAngles.y = initialEulerAngles.y;
                        finalEulerAngles.z = initialEulerAngles.z;
                        break;
                    case RotationAxis.Z_Only:
                        finalEulerAngles.x = initialEulerAngles.x;
                        finalEulerAngles.y = initialEulerAngles.y;
                        break;
                    case RotationAxis.All:
                        // If 'All' is selected, we combine the initial rotation as an offset
                        finalEulerAngles += initialEulerAngles;
                        break;
                }

                // Apply the final blended rotation
                transform.rotation = Quaternion.Euler(finalEulerAngles);
            }
        }
    }

    /// <summary>
    /// Animates the marker scaling up to its original size.
    /// <param name="forceRestart">If true, snaps the scale to 0 before animating, forcing a full pop-in.</param>
    /// </summary>
    public void Show(bool forceRestart = false)
    {
        if (activeScaleCoroutine != null)
        {
            StopCoroutine(activeScaleCoroutine);
        }

        if (forceRestart)
        {
            transform.localScale = Vector3.zero;
        }

        activeScaleCoroutine = StartCoroutine(ScaleRoutine(originalScale));
    }

    /// <summary>
    /// Animates the marker scaling down to zero.
    /// </summary>
    public void Hide()
    {
        if (activeScaleCoroutine != null)
        {
            StopCoroutine(activeScaleCoroutine);
        }
        activeScaleCoroutine = StartCoroutine(ScaleRoutine(Vector3.zero));
    }

    // The Coroutine that handles the smooth scaling animation over time
    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Vector3 initialScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / scaleDuration;
            t = t * t * (3f - 2f * t); // SmoothStep

            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);

            yield return null;
        }

        transform.localScale = targetScale;
    }
}