using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class Vector3Extension
    {
        // --- TRANSFORM POSITION SETTERS (Optimized: cache position once) ---

        public static void SetPosX(this Transform tr, float x)
        {
            var pos = tr.position;
            pos.x = x;
            tr.position = pos;
        }

        public static void SetPosY(this Transform tr, float y)
        {
            var pos = tr.position;
            pos.y = y;
            tr.position = pos;
        }

        public static void SetPosZ(this Transform tr, float z)
        {
            var pos = tr.position;
            pos.z = z;
            tr.position = pos;
        }

        public static void SetLocalPosX(this Transform tr, float x)
        {
            var pos = tr.localPosition;
            pos.x = x;
            tr.localPosition = pos;
        }

        public static void SetLocalPosY(this Transform tr, float y)
        {
            var pos = tr.localPosition;
            pos.y = y;
            tr.localPosition = pos;
        }

        public static void SetLocalPosZ(this Transform tr, float z)
        {
            var pos = tr.localPosition;
            pos.z = z;
            tr.localPosition = pos;
        }

        // --- VECTOR MODIFY HELPERS ---

        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        // --- AREA CHECKS ---

        public static bool IsInsideCube(this Vector3 point, Vector3 cubePos, Vector3 cubeSize)
        {
            // multiply is faster than 3 individual divisions
            var half = cubeSize * 0.5f;

            var min = cubePos - half;
            var max = cubePos + half;

            return point.x >= min.x && point.x <= max.x
                && point.y >= min.y && point.y <= max.y
                && point.z >= min.z && point.z <= max.z;
        }

        public static bool IsInsideCube(this Transform tr, Vector3 cubePos, Vector3 cubeSize)
            => tr.position.IsInsideCube(cubePos, cubeSize);


        /// <summary>
        /// Cheapest sphere check: pass an already-squared radius so no multiply/sqrt happens here.
        /// Only the radius is squared — the position is a normal world position.
        /// </summary>
        public static bool IsInsideSphereSqrRadius(this Vector3 point, Vector3 spherePos, float sqrSphereRadius)
            => (point - spherePos).sqrMagnitude <= sqrSphereRadius;

        public static bool IsInsideSphere(this Vector3 point, Vector3 spherePos, float sphereRadius)
        {
            float sqr = sphereRadius * sphereRadius;
            return (point - spherePos).sqrMagnitude <= sqr;
        }

        public static bool IsInsideSphere(this Transform tr, Vector3 spherePos, float sphereRadius)
            => tr.position.IsInsideSphere(spherePos, sphereRadius);


        // --- MATH HELPERS ---

        public static Vector3 Abs(this Vector3 v)
            => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        public static Vector3 Clamp(this Vector3 v, Vector3 min, Vector3 max)
            => new Vector3(
                Mathf.Clamp(v.x, min.x, max.x),
                Mathf.Clamp(v.y, min.y, max.y),
                Mathf.Clamp(v.z, min.z, max.z)
            );

        public static float MaxComponent(this Vector3 v)
            => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

        public static float MinComponent(this Vector3 v)
            => Mathf.Min(v.x, Mathf.Min(v.y, v.z));

        public static Vector3 Multiply(this Vector3 a, Vector3 b)
            => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static Vector3 Divide(this Vector3 a, Vector3 b)
            => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);


        // --- TRANSFORMATIONS ---

        public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Vector3 eulerAngles)
        {
            return Quaternion.Euler(eulerAngles) * (point - pivot) + pivot;
        }

        /// <summary>Returns <paramref name="v"/> rescaled to exactly <paramref name="magnitude"/> (zero vector stays zero).</summary>
        public static Vector3 SetMagnitude(this Vector3 v, float magnitude)
        {
            float sqrMag = v.sqrMagnitude;

            if (sqrMag < 1e-12f)  // handle zero vector safely
                return Vector3.zero;

            return v * (magnitude / Mathf.Sqrt(sqrMag));
        }

        /// <summary>Returns <paramref name="v"/> capped to <paramref name="maxMagnitude"/>; shorter vectors pass through unchanged.</summary>
        public static Vector3 ClampMagnitude(this Vector3 v, float maxMagnitude)
        {
            float sqrMag = v.sqrMagnitude;

            if (sqrMag <= maxMagnitude * maxMagnitude)
                return v;

            return v * (maxMagnitude / Mathf.Sqrt(sqrMag));
        }

        /// <summary>Random point inside a sphere centred on <paramref name="center"/> with the given <paramref name="radius"/>.</summary>
        public static Vector3 RandomPointInSphereAround(this Vector3 center, float radius)
            => center + Random.insideUnitSphere * radius;

        public static Vector3 LerpUnclamped(this Vector3 a, Vector3 b, float t)
            => new Vector3(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t
            );

        public static bool ApproximatelyEqual(this Vector3 a, Vector3 b, float tolerance = 0.01f)
        {
            float sqrTol = tolerance * tolerance;
            return (a - b).sqrMagnitude <= sqrTol;
        }

        public static bool ApproximatelyEqual(this Transform tr, Vector3 b, float tolerance = 0.01f)
            => tr.position.ApproximatelyEqual(b, tolerance);


        // --- VECTOR2 CONVERSIONS (drop an axis to flatten 3D -> 2D) ---

        /// <summary>Drops the Y axis: (x, y, z) -> (x, z). The common ground-plane flatten.</summary>
        public static Vector2 ToVector2XZ(this Vector3 v) => new Vector2(v.x, v.z);

        /// <summary>Drops the Z axis: (x, y, z) -> (x, y).</summary>
        public static Vector2 ToVector2XY(this Vector3 v) => new Vector2(v.x, v.y);

        /// <summary>Drops the X axis: (x, y, z) -> (y, z).</summary>
        public static Vector2 ToVector2YZ(this Vector3 v) => new Vector2(v.y, v.z);


        // --- LOCAL / WORLD SPACE CONVERSIONS ---

        // Position (applies translation, rotation and scale)

        /// <summary>Converts a point from <paramref name="relativeTo"/>'s local space to world space (translation, rotation and scale).</summary>
        public static Vector3 ToWorldPosition(this Vector3 localPoint, Transform relativeTo)
            => relativeTo.TransformPoint(localPoint);

        /// <summary>Converts a point from world space to <paramref name="relativeTo"/>'s local space (translation, rotation and scale).</summary>
        public static Vector3 ToLocalPosition(this Vector3 worldPoint, Transform relativeTo)
            => relativeTo.InverseTransformPoint(worldPoint);

        // Direction (applies rotation only, ignores translation and scale)

        /// <summary>Converts a direction from <paramref name="relativeTo"/>'s local space to world space (rotation only; length preserved).</summary>
        public static Vector3 ToWorldDirection(this Vector3 localDir, Transform relativeTo)
            => relativeTo.TransformDirection(localDir);

        /// <summary>Converts a direction from world space to <paramref name="relativeTo"/>'s local space (rotation only; length preserved).</summary>
        public static Vector3 ToLocalDirection(this Vector3 worldDir, Transform relativeTo)
            => relativeTo.InverseTransformDirection(worldDir);

        // Rotation (composes euler angles with the transform's rotation)

        /// <summary>Converts local euler angles to world euler angles by composing them with <paramref name="relativeTo"/>'s rotation.</summary>
        public static Vector3 ToWorldRotation(this Vector3 localEuler, Transform relativeTo)
            => (relativeTo.rotation * Quaternion.Euler(localEuler)).eulerAngles;

        /// <summary>Converts world euler angles to euler angles local to <paramref name="relativeTo"/>'s rotation.</summary>
        public static Vector3 ToLocalRotation(this Vector3 worldEuler, Transform relativeTo)
            => (Quaternion.Inverse(relativeTo.rotation) * Quaternion.Euler(worldEuler)).eulerAngles;

        // Scale (applies lossy scale, ignores translation and rotation)

        /// <summary>Converts a local scale to world scale by multiplying with <paramref name="relativeTo"/>'s lossy scale.</summary>
        public static Vector3 ToWorldScale(this Vector3 localScale, Transform relativeTo)
            => localScale.Multiply(relativeTo.lossyScale);

        /// <summary>Converts a world scale to scale local to <paramref name="relativeTo"/> by dividing by its lossy scale.</summary>
        public static Vector3 ToLocalScale(this Vector3 worldScale, Transform relativeTo)
            => worldScale.Divide(relativeTo.lossyScale);
    }
}
