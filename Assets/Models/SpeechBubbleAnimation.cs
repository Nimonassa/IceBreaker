using UnityEngine;

public class SpeechBubbleAnimation : MonoBehaviour
{
    [Header("Line Transforms")]
    public Transform line1;
    public Transform line2;
    public Transform line3;

    [Header("Animation Settings")]
    [Tooltip("How far forward the lines move.")]
    public float moveDistance = 0.5f; 
    [Tooltip("How fast the looping animation plays.")]
    public float speed = 3f;          
    [Tooltip("The time delay before the next line starts moving.")]
    public float delayBetweenLines = 0.5f; 

    // To store the original positions so they always return to their starting point
    private Vector3 line1StartPos;
    private Vector3 line2StartPos;
    private Vector3 line3StartPos;

    void Start()
    {
        if (line1 != null) line1StartPos = line1.localPosition;
        if (line2 != null) line2StartPos = line2.localPosition;
        if (line3 != null) line3StartPos = line3.localPosition;
    }

    void Update()
    {
        AnimateLine(line1, line1StartPos, 0f);
        AnimateLine(line2, line2StartPos, delayBetweenLines);
        AnimateLine(line3, line3StartPos, delayBetweenLines * 2f);
    }

    private void AnimateLine(Transform line, Vector3 startPos, float offset)
    {
        if (line == null) return;

        float timeValue = (Time.time * speed) - offset;
        float wave = (Mathf.Sin(timeValue) + 1f) / 2f;
        line.localPosition = startPos + (Vector3.forward * wave * moveDistance);
    }
}