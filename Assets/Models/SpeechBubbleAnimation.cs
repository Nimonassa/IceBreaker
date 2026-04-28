using UnityEngine;

public class SpeechBubbleAnimation : MonoBehaviour
{
    public enum ScaleAnchor { Left, Center, Right }

    [Header("Line Transforms")]
    public Transform line1;
    public Transform line2;
    public Transform line3;

    [Header("Glow Transform")]
    [Tooltip("The background glow image to pulse.")]
    public Transform glowBackground;

    [Header("Line Animation Settings")]
    [Tooltip("Where the scaling should anchor from horizontally.")]
    public ScaleAnchor horizontalAnchor = ScaleAnchor.Center;
    
    [Tooltip("Minimum position offset (the line's resting state).")]
    public Vector3 minPositionOffset = Vector3.zero;
    [Tooltip("Maximum position offset (the peak of the animation).")]
    public Vector3 maxPositionOffset = new Vector3(0, 0, 0.5f);
    
    [Tooltip("Minimum scale offset (the line's resting state).")]
    public Vector3 minScaleOffset = Vector3.zero;
    [Tooltip("Maximum scale offset (the peak of the animation).")]
    public Vector3 maxScaleOffset = Vector3.zero;
    
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
    
    private Vector3 line1StartScale;
    private Vector3 line2StartScale;
    private Vector3 line3StartScale;
    
    private Vector3 glowStartScale;

    void Start()
    {
        if (line1 != null) { line1StartPos = line1.localPosition; line1StartScale = line1.localScale; }
        if (line2 != null) { line2StartPos = line2.localPosition; line2StartScale = line2.localScale; }
        if (line3 != null) { line3StartPos = line3.localPosition; line3StartScale = line3.localScale; }
        
        if (glowBackground != null) glowStartScale = glowBackground.localScale;
    }

    void Update()
    {
        AnimateLine(line1, line1StartPos, line1StartScale, 0f);
        AnimateLine(line2, line2StartPos, line2StartScale, delayBetweenLines);
        AnimateLine(line3, line3StartPos, line3StartScale, delayBetweenLines * 2f);

        AnimateGlow();
    }

    private void AnimateLine(Transform line, Vector3 startPos, Vector3 startScale, float offset)
    {
        if (line == null) return;

        float timeValue = (Time.time * speed) - offset;
        float wave = (Mathf.Sin(timeValue) + 1f) / 2f; 

        // Smoothly blend between Min and Max scale offsets
        Vector3 currentScaleOffset = Vector3.Lerp(minScaleOffset, maxScaleOffset, wave);
        line.localScale = startScale + currentScaleOffset;

        // Smoothly blend between Min and Max position offsets
        Vector3 currentPosOffset = Vector3.Lerp(minPositionOffset, maxPositionOffset, wave);
        Vector3 currentPos = startPos + currentPosOffset;

        // Adjust the X position based on the chosen anchor
        if (horizontalAnchor == ScaleAnchor.Left)
        {
            currentPos.x += currentScaleOffset.x / 2f;
        }
        else if (horizontalAnchor == ScaleAnchor.Right)
        {
            currentPos.x -= currentScaleOffset.x / 2f;
        }

        line.localPosition = currentPos;
    }

    private void AnimateGlow()
    {
        if (glowBackground == null) return;

        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, wave);
        glowBackground.localScale = glowStartScale * scaleMultiplier;
    }
}