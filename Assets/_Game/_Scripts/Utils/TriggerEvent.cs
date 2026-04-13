using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TriggerEvent : MonoBehaviour
{
    public enum TriggerMode { Enter, Exit, Manual }

    [Header("Trigger Settings")]
    public TriggerMode mode = TriggerMode.Enter;
    public bool triggerOnce = false;

    [Header("Filters")]
    public bool useLayer = false;
    public LayerMask targetLayer;
    public bool useTag = false;
    public string targetTag = "Untagged";

    [Header("Events")]
    public UnityEvent onTriggerEvent;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (mode == TriggerMode.Enter)
        {
            ProcessTrigger(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (mode == TriggerMode.Exit)
        {
            ProcessTrigger(other.gameObject);
        }
    }

    public void ManualTrigger()
    {
        if (mode == TriggerMode.Manual && !hasTriggered)
        {
            FireEvent();
        }
    }

    private void ProcessTrigger(GameObject obj)
    {
        if (hasTriggered)
        {
            return;
        }

        if (useLayer && ((1 << obj.layer) & targetLayer) == 0)
        {
            return;
        }

        if (useTag && !obj.CompareTag(targetTag))
        {
            return;
        }

        FireEvent();
    }

    private void FireEvent()
    {
        onTriggerEvent?.Invoke();

        if (triggerOnce)
        {
            hasTriggered = true;
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
