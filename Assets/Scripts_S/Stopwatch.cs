using UnityEngine;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(TextMeshProUGUI))]
public class Stopwatch : MonoBehaviour
{
    public float stopTime = 60f;
    public UnityEvent onTick, onStop;

    private float time;
    private int lastTick;
    private bool isRunning = true;
    private TextMeshProUGUI uiText;

    void Start()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        UpdateUI();
    }

    void Update()
    {
        if (!isRunning)
        {
            return;
        }

        time += Time.deltaTime;

        // Tick event
        if ((int)time > lastTick)
        {
            lastTick = (int)time;
            onTick?.Invoke();
        }

        // Stop limit
        if (stopTime > 0 && time >= stopTime)
        {
            time = stopTime;
            isRunning = false;
            onStop?.Invoke();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        uiText.text = $"{(int)time / 60:00}:{(int)time % 60:00}";
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void StartTimer()
    {
        isRunning = stopTime <= 0 || time < stopTime;
    }

    public void ResetTimer()
    {
        time = 0;
        lastTick = 0;
        UpdateUI();
    }

    public float GetProgress()
    {
        if (stopTime > 0)
        {
            return Mathf.Clamp01(time / stopTime);
        }
        else
        {
            return 0f;
        }
    }
}
