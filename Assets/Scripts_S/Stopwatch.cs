using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TextMeshProUGUI))]
public class Stopwatch : MonoBehaviour
{
    public enum Condition { Awake, Manual }

    [System.Serializable]
    public class WatchEvents { public UnityEvent onTick, onStart, onStop;  }

    [Header("References")]
    public TextMeshProUGUI uiText;
    [Header("Settings")]
    public float stopTime = 60f;
    public Condition trigger = Condition.Awake;
    public WatchEvents events = new WatchEvents();  


   
        
    
    private float time;
    private int lastTick;
    private bool isRunning = true;

    void Start()
    {
        if(uiText == null)
            uiText = GetComponent<TextMeshProUGUI>();

        UpdateUI();

        if (trigger == Condition.Awake)
        {
            StartTimer();
        }
            
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
            events.onTick?.Invoke();
        }

        // Stop limit
        if (stopTime > 0 && time >= stopTime)
        {
            time = stopTime;
            isRunning = false;
            events.onStop?.Invoke();
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
        events.onStart?.Invoke();
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

