using UnityEngine;

namespace DataKeeper.Ease
{
   public static class Easing
    {
        public static float Apply(float t, EaseType easeType)
        {
            t = Mathf.Clamp01(t);

            switch (easeType)
            {
                case EaseType.Linear:
                    return t;

                // ---------------------- QUAD ----------------------
                case EaseType.EaseInQuad:
                    return t * t;

                case EaseType.EaseOutQuad:
                    return 1f - (1f - t) * (1f - t);

                case EaseType.EaseInOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

                // ---------------------- CUBIC ----------------------
                case EaseType.EaseInCubic:
                    return t * t * t;

                case EaseType.EaseOutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);

                case EaseType.EaseInOutCubic:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

                // ---------------------- QUART ----------------------
                case EaseType.EaseInQuart:
                    return t * t * t * t;

                case EaseType.EaseOutQuart:
                    return 1f - Mathf.Pow(1f - t, 4f);

                case EaseType.EaseInOutQuart:
                    return t < 0.5f
                        ? 8f * t * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;

                // ---------------------- QUINT ----------------------
                case EaseType.EaseInQuint:
                    return t * t * t * t * t;

                case EaseType.EaseOutQuint:
                    return 1f - Mathf.Pow(1f - t, 5f);

                case EaseType.EaseInOutQuint:
                    return t < 0.5f
                        ? 16f * t * t * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f;

                // ---------------------- SINE ----------------------
                case EaseType.EaseInSine:
                    return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

                case EaseType.EaseOutSine:
                    return Mathf.Sin(t * Mathf.PI * 0.5f);

                case EaseType.EaseInOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

                // ---------------------- EXPO ----------------------
                case EaseType.EaseInExpo:
                    return Mathf.Approximately(t, 0f) ? 0f : Mathf.Pow(2f, 10f * (t - 1f));

                case EaseType.EaseOutExpo:
                    return Mathf.Approximately(t, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t);

                case EaseType.EaseInOutExpo:
                    if (Mathf.Approximately(t, 0f)) return 0f;
                    if (Mathf.Approximately(t, 1f)) return 1f;
                    return t < 0.5f
                        ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                        : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;

                // ---------------------- CIRC ----------------------
                case EaseType.EaseInCirc:
                    return 1f - Mathf.Sqrt(1f - t * t);

                case EaseType.EaseOutCirc:
                    return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));

                case EaseType.EaseInOutCirc:
                    return t < 0.5f
                        ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) * 0.5f
                        : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) * 0.5f;

                // ---------------------- BOUNCE (DOTween exact) ----------------------
                case EaseType.EaseInBounce:
                    return 1f - EaseOutBounce(1f - t);

                case EaseType.EaseOutBounce:
                    return EaseOutBounce(t);

                case EaseType.EaseInOutBounce:
                    return t < 0.5f
                        ? (1f - EaseOutBounce(1f - t * 2f)) * 0.5f
                        : (1f + EaseOutBounce(t * 2f - 1f)) * 0.5f;

                // ---------------------- BACK (DOTween exact) ----------------------
                case EaseType.EaseInBack:
                {
                    const float s = 1.70158f;
                    return t * t * ((s + 1f) * t - s);
                }

                case EaseType.EaseOutBack:
                {
                    const float s = 1.70158f;
                    t -= 1f;
                    return t * t * ((s + 1f) * t + s) + 1f;
                }

                case EaseType.EaseInOutBack:
                {
                    const float s = 1.70158f * 1.525f;
                    if (t < 0.5f)
                    {
                        float x = 2f * t;
                        return x * x * ((s + 1f) * x - s) * 0.5f;
                    }
                    else
                    {
                        float x = 2f * t - 2f;
                        return (x * x * ((s + 1f) * x + s) + 2f) * 0.5f;
                    }
                }

                // ---------------------- ELASTIC (DOTween exact) ----------------------
                case EaseType.EaseInElastic:
                    if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f)) return t;
                    return -Mathf.Pow(2f, 10f * (t - 1f)) *
                           Mathf.Sin((t - 1.075f) * (2f * Mathf.PI) / 0.3f);

                case EaseType.EaseOutElastic:
                    if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f)) return t;
                    return Mathf.Pow(2f, -10f * t) *
                           Mathf.Sin((t - 0.075f) * (2f * Mathf.PI) / 0.3f) + 1f;

                case EaseType.EaseInOutElastic:
                    if (Mathf.Approximately(t, 0f)) return 0f;
                    if (Mathf.Approximately(t, 1f)) return 1f;
                    t *= 2f;

                    if (t < 1f)
                        return -0.5f * Mathf.Pow(2f, 10f * (t - 1f)) *
                               Mathf.Sin((t - 1.1125f) * (2f * Mathf.PI) / 0.45f);

                    t -= 1f;
                    return Mathf.Pow(2f, -10f * t) *
                           Mathf.Sin((t - 0.1125f) * (2f * Mathf.PI) / 0.45f) * 0.5f + 1f;

                default:
                    return t;
            }
        }

        // DOTween's exact bounce function
        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }

            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
    
    public enum EaseType
    {
        Linear = 0,
        EaseInQuad = 1,
        EaseOutQuad = 2,
        EaseInOutQuad = 3,
        EaseInCubic = 4,
        EaseOutCubic = 5,
        EaseInOutCubic = 6,
        EaseInQuart = 7,
        EaseOutQuart = 8,
        EaseInOutQuart = 9,
        EaseInQuint = 10,
        EaseOutQuint = 11,
        EaseInOutQuint = 12,
        EaseInSine = 13,
        EaseOutSine = 14,
        EaseInOutSine = 15,
        EaseInExpo = 16,
        EaseOutExpo = 17,
        EaseInOutExpo = 18,
        EaseInCirc = 19,
        EaseOutCirc = 20,
        EaseInOutCirc = 21,
        EaseInBounce = 22,
        EaseOutBounce = 23,
        EaseInOutBounce = 24,
        EaseInBack = 25,
        EaseOutBack = 26,
        EaseInOutBack = 27,
        EaseInElastic = 28,
        EaseOutElastic = 29,
        EaseInOutElastic = 30
    }
}