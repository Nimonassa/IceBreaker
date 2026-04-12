using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    private float elapsedTime = 0f;
    private bool isRunning = true;

    private TextMeshProUGUI timerText;

    void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        isRunning = true;
    }

    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Stop the timer
    public void StopTimer()
    {
        isRunning = false;
    }

    // Start / resume the timer
    public void StartTimer()
    {
        isRunning = true;
    }

    // Reset timer
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerUI(); // immediately refresh UI
    }
}