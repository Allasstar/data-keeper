using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class Vector3Extension
    {
        public static void SetPosX(this Transform tr, float x) => tr.position = tr.position.WithX(x);
        public static void SetPosY(this Transform tr, float y) => tr.position = tr.position.WithY(y);
        public static void SetPosZ(this Transform tr, float z) => tr.position = tr.position.WithZ(z);
        
        public static void SetLocalPosX(this Transform tr, float x) => tr.localPosition = tr.localPosition.WithX(x);
        public static void SetLocalPosY(this Transform tr, float y) => tr.localPosition = tr.localPosition.WithY(y);
        public static void SetLocalPosZ(this Transform tr, float z) => tr.localPosition = tr.localPosition.WithZ(z);

        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        public static bool IsInsideCube(this Vector3 point, Vector3 cubePos, Vector3 cubeSize)
        {
            var min = cubePos - cubeSize / 2f;
            var max = cubePos + cubeSize / 2f;
            return point.x >= min.x && point.x <= max.x
                && point.y >= min.y && point.y <= max.y
                && point.z >= min.z && point.z <= max.z;
        }

        public static bool IsInsideCube(this Transform tr, Vector3 cubePos, Vector3 cubeSize)
            => tr.position.IsInsideCube(cubePos, cubeSize);

        public static bool IsInsideSphere(this Vector3 point, Vector3 spherePos, float sphereRadius)
            => (point - spherePos).sqrMagnitude <= sphereRadius * sphereRadius;

        public static bool IsInsideSphere(this Transform tr, Vector3 spherePos, float sphereRadius)
            => tr.position.IsInsideSphere(spherePos, sphereRadius);

        public static Vector3 Abs(this Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        public static Vector3 Clamp(this Vector3 v, Vector3 min, Vector3 max)
            => new Vector3(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y), Mathf.Clamp(v.z, min.z, max.z));

        public static float MaxComponent(this Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

        public static float MinComponent(this Vector3 v) => Mathf.Min(v.x, Mathf.Min(v.y, v.z));

        public static Vector3 Multiply(this Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static Vector3 Divide(this Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);

        public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        public static Vector3 SetMagnitude(this Vector3 v, float magnitude)
            => v.normalized * magnitude;

        public static Vector2 ToVector2XZ(this Vector3 v) => new Vector2(v.x, v.z);

        public static Vector3 RandomInSphere(this Vector3 center, float radius)
            => center + Random.insideUnitSphere * radius;

        public static Vector3 LerpUnclamped(this Vector3 a, Vector3 b, float t)
            => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

        public static bool ApproximatelyEqual(this Vector3 a, Vector3 b, float tolerance = 0.01f)
            => (a - b).sqrMagnitude <= tolerance * tolerance;
    }
}