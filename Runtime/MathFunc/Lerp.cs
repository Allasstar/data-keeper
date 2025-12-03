using UnityEngine;

namespace DataKeeper.MathFunc
{
    /// <summary>
    /// Utility helpers for Lerp, Remap, and Map operations.
    /// Supports clamped and unclamped modes.
    /// Safe for easing curves that go below 0 or above 1.
    /// </summary>
    public struct Lerp
    {
        // ======================
        //        FLOAT
        // ======================

        /// <summary>Clamped float lerp.</summary>
        public static float Float(float from, float to, float t)
            => Mathf.Lerp(from, to, t);

        /// <summary>Unclamped float lerp (supports easing overshoot).</summary>
        public static float FloatUnclamped(float from, float to, float t)
            => from + (to - from) * t;


        // ======================
        //        VECTOR2
        // ======================

        public static Vector2 LerpVector2(Vector2 from, Vector2 to, float t)
            => Vector2.Lerp(from, to, t);

        public static Vector2 LerpVector2Unclamped(Vector2 from, Vector2 to, float t)
            => from + (to - from) * t;


        // ======================
        //        VECTOR3
        // ======================

        public static Vector3 LerpVector3(Vector3 from, Vector3 to, float t)
            => Vector3.Lerp(from, to, t);

        public static Vector3 LerpVector3Unclamped(Vector3 from, Vector3 to, float t)
            => from + (to - from) * t;


        // ======================
        //      QUATERNION
        // ======================

        public static Quaternion LerpQuaternion(Quaternion from, Quaternion to, float t)
            => Quaternion.Lerp(from, to, t);

        public static Quaternion LerpQuaternionUnclamped(Quaternion from, Quaternion to, float t)
            => Quaternion.SlerpUnclamped(from, to, t);


        // ======================
        //        REMAP
        // ======================

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return FloatUnclamped(toMin, toMax, t);
        }

        public static float RemapClamped(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value));
            return Mathf.Lerp(toMin, toMax, t);
        }


        // ======================
        //        MAP
        // ======================

        public static float Map(float t, float toMin, float toMax)
            => FloatUnclamped(toMin, toMax, t);

        public static float MapClamped(float t, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(t));


        // ======================
        //         INT
        // ======================

        public static int Int(int from, int to, float t)
            => Mathf.RoundToInt(Mathf.Lerp(from, to, t));

        public static int IntUnclamped(int from, int to, float t)
            => Mathf.RoundToInt(from + (to - from) * t);

        public static int Remap(int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return IntUnclamped(toMin, toMax, t);
        }

        public static int RemapClamped(int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value));
            return Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, t));
        }
    }


    // ======================================================
    //                      EXTENSIONS
    // ======================================================

    public static class LerpExtensions
    {
        // -------- FLOAT --------
        public static float LerpTo(this float from, float to, float t)
            => Mathf.Lerp(from, to, t);

        public static float LerpToUnclamped(this float from, float to, float t)
            => from + (to - from) * t;

        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
            => toMin + (toMax - toMin) * Mathf.InverseLerp(fromMin, fromMax, value);

        public static float RemapClamped(this float value, float fromMin, float fromMax, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value)));

        public static float Map(this float t, float toMin, float toMax)
            => toMin + (toMax - toMin) * t;

        public static float MapClamped(this float t, float toMin, float toMax)
            => Mathf.Lerp(toMin, toMax, Mathf.Clamp01(t));


        // -------- VECTOR2 --------
        public static Vector2 LerpToUnclamped(this Vector2 from, Vector2 to, float t)
            => from + (to - from) * t;


        // -------- VECTOR3 --------
        public static Vector3 LerpToUnclamped(this Vector3 from, Vector3 to, float t)
            => from + (to - from) * t;


        // -------- QUATERNION --------
        public static Quaternion LerpToUnclamped(this Quaternion from, Quaternion to, float t)
            => Quaternion.SlerpUnclamped(from, to, t);


        // -------- INT --------
        public static int LerpTo(this int from, int to, float t)
            => Mathf.RoundToInt(Mathf.Lerp(from, to, t));

        public static int LerpToUnclamped(this int from, int to, float t)
            => Mathf.RoundToInt(from + (to - from) * t);

        public static int Remap(this int value, int fromMin, int fromMax, int toMin, int toMax)
            => Mathf.RoundToInt(toMin + (toMax - toMin) * Mathf.InverseLerp(fromMin, fromMax, value));

        public static int RemapClamped(this int value, int fromMin, int fromMax, int toMin, int toMax)
            => Mathf.RoundToInt(Mathf.Lerp(toMin, toMax, Mathf.Clamp01(Mathf.InverseLerp(fromMin, fromMax, value))));
    }
}
