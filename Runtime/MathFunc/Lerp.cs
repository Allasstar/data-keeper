using UnityEngine;

namespace DataKeeper.MathFunc
{
    /// <summary>
    /// Utility helpers for Lerp, Remap, and Map operations.
    /// Includes normal and extension variants, with clamped versions.
    /// </summary>
    public static class Lerp
    {
        // -------- FLOAT --------
        public static float Float(float from, float to, float t)
            => Mathf.Lerp(from, to, t);

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));

        public static float RemapClamped(float value, float fromMin, float fromMax, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value)));

        public static float Map(float t, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, t);

        public static float MapClamped(float t, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(t));


        // -------- INT --------
        public static int Int(int from, int to, float t)
            => Mathf.RoundToInt(Mathf.Lerp(from, to, t));

        public static int Remap(int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));
        }

        public static int RemapClamped(int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value));
            return Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));
        }

        public static int Map(int t, int toMin, int toMax)
            => Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));

        public static int MapClamped(int t, int toMin, int toMax)
            => Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, Mathf.Clamp01(t)));
    }
    
    /// <summary>
    /// Extensions helpers for Lerp, Remap, and Map operations.
    /// Includes normal and extension variants, with clamped versions.
    /// </summary>
    public static class LerpExtensions
    {
        // -------- FLOAT EXTENSIONS --------
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));

        public static float RemapClamped(this float value, float fromMin, float fromMax, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value)));

        public static float Map(this float t, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, t);

        public static float MapClamped(this float t, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(t));


        // -------- INT EXTENSIONS --------
        public static int Remap(this int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));
        }

        public static int RemapClamped(this int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value));
            return Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));
        }

        public static int Map(this int t, int toMin, int toMax)
            => Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));

        public static int MapClamped(this int t, int toMin, int toMax)
            => Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, Mathf.Clamp01(t)));
    }
}
