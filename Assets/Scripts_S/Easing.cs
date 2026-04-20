
using UnityEngine;


public enum EasingType
{
    // Basic
    Linear,

    // Quadratic
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,

    // Cubic
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,

    // Sinusoidal
    EaseInSine,
    EaseOutSine,
    EaseInOutSine,

    // Exponential
    EaseInExpo,
    EaseOutExpo,
    EaseInOutExpo,
}


public static class Easing
{
    public static float Evaluate(EasingType type, float t)
    {
        t = Mathf.Clamp01(t);

        switch (type)
        {
            // Basic
            case EasingType.Linear: return t;

            // Quadratic
            case EasingType.EaseInQuad: return t * t;
            case EasingType.EaseOutQuad: return 1 - (1 - t) * (1 - t);
            case EasingType.EaseInOutQuad: return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;

            // Cubic
            case EasingType.EaseInCubic: return t * t * t;
            case EasingType.EaseOutCubic: return 1 - Mathf.Pow(1 - t, 3);
            case EasingType.EaseInOutCubic: return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

            // Sinusoidal
            case EasingType.EaseInSine: return 1 - Mathf.Cos((t * Mathf.PI) / 2);
            case EasingType.EaseOutSine: return Mathf.Sin((t * Mathf.PI) / 2);
            case EasingType.EaseInOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;

            // Exponential
            case EasingType.EaseInExpo: return t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
            case EasingType.EaseOutExpo: return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
            case EasingType.EaseInOutExpo:
                if (t == 0) return 0;
                if (t == 1) return 1;
                return t < 0.5f
                    ? Mathf.Pow(2, 20 * t - 10) / 2
                    : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;

            default:
                return t; // Default to Linear
        }
    }
}