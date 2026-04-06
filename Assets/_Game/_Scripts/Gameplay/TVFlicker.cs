using UnityEngine;

[RequireComponent(typeof(Light))]
public class TVFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 2.0f;

    [Space]
    public float minFlickerSpeed = 0.05f;
    public float maxFlickerSpeed = 0.2f;

    private Light tvLight;
    private float timer;

    void Start()
    {
        // Grab the Light component attached to this GameObject
        tvLight = GetComponent<Light>();
    }

    void Update()
    {
        // Count down the timer
        timer -= Time.deltaTime;

        // When the timer hits zero, change the light and reset the timer
        if (timer <= 0f)
        {
            tvLight.intensity = Random.Range(minIntensity, maxIntensity);
            timer = Random.Range(minFlickerSpeed, maxFlickerSpeed);
        }
    }
}
