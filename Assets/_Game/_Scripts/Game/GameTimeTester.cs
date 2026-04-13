using UnityEngine;
using UnityEngine.InputSystem;

public class GameTimeTester : MonoBehaviour
{
    [Header("Controls")]
    public Key pauseKey = Key.P;
    public Key slowMoKey = Key.T;
    [Header("Settings")]
    public float slowMoTimeScale = 0.2f;
    public float pauseTransitionSeconds = 1f;
    public float resumeTransitionSeconds = 1f;
    private bool isPaused = false;
    private bool isSlowMode = false;

    void Update()
    {
        if (Keyboard.current == null) 
            return;

        if (Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                GameTime.Instance.SetTimeScale(0f, pauseTransitionSeconds);
            }
            else
            {
                GameTime.Instance.SetTimeScale(1f, resumeTransitionSeconds);
            }
        }

        if (Keyboard.current[slowMoKey].wasPressedThisFrame)
        {
            isSlowMode = !isSlowMode;

            if (isSlowMode)
            {
                GameTime.Instance.SetTimeScale(slowMoTimeScale);
            }
            else
            {
                GameTime.Instance.SetTimeScale(1f);
            }
            
        }
    }
}
