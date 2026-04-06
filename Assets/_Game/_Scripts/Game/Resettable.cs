using UnityEngine;

public abstract class Resettable : MonoBehaviour
{
    // Using ResetState instead of Reset to avoid conflicts with Unity's internal Reset method
    public abstract void ResetState();
}