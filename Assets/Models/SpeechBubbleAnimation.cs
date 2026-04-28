using UnityEngine;

public class SpeechBubbleAnimation : MonoBehaviour
{
    [Header("Line Transforms")]
    public Transform line1;
    public Transform line2;
    public Transform line3;

    [Header("Glow Transform")]
    [Tooltip("The background glow image to pulse.")]
    public Transform glowBackground;

    [Header("Line Animation Settings")]
    [Tooltip("How far forward the lines move.")]
    public float moveDistance = 0.5f; 
    [Tooltip("How fast the looping animation plays.")]
    public float speed = 3f;          
    [Tooltip("The time delay before the next line starts moving.")]
    public float delayBetweenLines = 0.5f; 

    [Header("Glow Pulse Settings")]
    [Tooltip("How fast the background pulses.")]
    public float pulseSpeed = 1.5f;
    [Tooltip("The smallest the glow will shrink to (multiplier of its original size).")]
    public float minScale = 0.9f;
    [Tooltip("The largest the glow will grow to (multiplier of its original size).")]
    public float maxScale = 1.1f;

    // Start positions and scales
    private Vector3 line1StartPos;
    private Vector3 line2StartPos;
    private Vector3 line3StartPos;
    private Vector3 glowStartScale;

    void Start()
    {
        if (line1 != null) line1StartPos = line1.localPosition;
        if (line2 != null) line2StartPos = line2.localPosition;
        if (line3 != null) line3StartPos = line3.localPosition;
        
        // Save the initial scale of the glow so we pulse relative to its original size
        if (glowBackground != null) glowStartScale = glowBackground.localScale;
    }

    void Update()
    {
        // Animate the lines
        AnimateLine(line1, line1StartPos, 0f);
        AnimateLine(line2, line2StartPos, delayBetweenLines);
        AnimateLine(line3, line3StartPos, delayBetweenLines * 2f);

        // Animate the background glow
        AnimateGlow();
    }

    private void AnimateLine(Transform line, Vector3 startPos, float offset)
    {
        if (line == null) return;

        float timeValue = (Time.time * speed) - offset;
        float wave = (Mathf.Sin(timeValue) + 1f) / 2f;
        line.localPosition = startPos + (Vector3.forward * wave * moveDistance);
    }

    private void AnimateGlow()
    {
        if (glowBackground == null) return;

        // Generate a continuous wave between 0 and 1
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        // Mathf.Lerp smoothly blends between minScale and maxScale based on the wave value
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, wave);

        // Apply the scale multiplier to the original starting scale
        glowBackground.localScale = glowStartScale * scaleMultiplier;
    }
}