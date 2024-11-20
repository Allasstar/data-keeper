using UnityEngine;

namespace DataKeeper.ActCore
{
    public static class ActEase
    {
        private const float PI = Mathf.PI;
        private const float HALF_PI = Mathf.PI / 2;

        public static FloatEase Linear(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t);

        public static FloatEase InSine(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - Mathf.Cos(t * HALF_PI));
        public static FloatEase OutSine(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, Mathf.Sin(t * HALF_PI));
        public static FloatEase InOutSine(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, -(Mathf.Cos(PI * t) - 1) / 2);

        public static FloatEase InQuad(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t * t);
        public static FloatEase OutQuad(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - (1 - t) * (1 - t));
        public static FloatEase InOutQuad(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2);

        public static FloatEase InCubic(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t * t * t);
        public static FloatEase OutCubic(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - Mathf.Pow(1 - t, 3));
        public static FloatEase InOutCubic(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2);

        public static FloatEase InQuart(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t * t * t * t);
        public static FloatEase OutQuart(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - Mathf.Pow(1 - t, 4));
        public static FloatEase InOutQuart(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t < 0.5f ? 8 * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 4) / 2);

        public static FloatEase InQuint(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t * t * t * t * t);
        public static FloatEase OutQuint(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - Mathf.Pow(1 - t, 5));
        public static FloatEase InOutQuint(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t < 0.5f ? 16 * t * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 5) / 2);

        public static FloatEase InExpo(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t == 0f ? 0f : Mathf.Pow(2, 10 * t - 10));
        public static FloatEase OutExpo(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t == 1f ? 1f : 1 - Mathf.Pow(2, -10 * t));
        public static FloatEase InOutExpo(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t == 0f ? 0f : t == 1f ? 1f : t < 0.5f ? Mathf.Pow(2, 20 * t - 10) / 2 : (2 - Mathf.Pow(2, -20 * t + 10)) / 2);

        public static FloatEase InCirc(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - Mathf.Sqrt(1 - Mathf.Pow(t, 2)));
        public static FloatEase OutCirc(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, Mathf.Sqrt(1 - Mathf.Pow(t - 1, 2)));
        public static FloatEase InOutCirc(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t < 0.5f ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * t, 2))) / 2 : (Mathf.Sqrt(1 - Mathf.Pow(-2 * t + 2, 2)) + 1) / 2);

        public static FloatEase InBack(float t, float from = 0f, float to = 1f)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return Mathf.Lerp(from, to, c3 * t * t * t - c1 * t * t);
        }

        public static FloatEase OutBack(float t, float from = 0f, float to = 1f)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return Mathf.Lerp(from, to, 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2));
        }

        public static FloatEase InOutBack(float t, float from = 0f, float to = 1f)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return Mathf.Lerp(from, to, t < 0.5f
                ? (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
                : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2);
        }

        public static FloatEase InElastic(float t, float from = 0f, float to = 1f)
        {
            const float c4 = (2 * PI) / 3;
            return Mathf.Lerp(from, to, t == 0f ? 0f : t == 1f ? 1f : -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * c4));
        }

        public static FloatEase OutElastic(float t, float from = 0f, float to = 1f)
        {
            const float c4 = (2 * PI) / 3;
            return Mathf.Lerp(from, to, t == 0f ? 0f : t == 1f ? 1f : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1);
        }

        public static FloatEase InOutElastic(float t, float from = 0f, float to = 1f)
        {
            const float c5 = (2 * PI) / 4.5f;
            return Mathf.Lerp(from, to, t == 0f ? 0f : t == 1f ? 1f : t < 0.5f
                ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2
                : (Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2 + 1);
        }

        public static FloatEase InBounce(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, 1 - OutBounce(1 - t));

        public static FloatEase OutBounce(float t, float from = 0f, float to = 1f)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            float value;

            if (t < 1 / d1)
            {
                value = n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                value = n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                value = n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                value = n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }

            return Mathf.Lerp(from, to, value);
        }

        public static FloatEase InOutBounce(float t, float from = 0f, float to = 1f) => Mathf.Lerp(from, to, t < 0.5f
            ? (1 - OutBounce(1 - 2 * t)) / 2
            : (1 + OutBounce(2 * t - 1)) / 2);
    }
}