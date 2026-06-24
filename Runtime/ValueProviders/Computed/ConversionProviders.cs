using System;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.ValueProviders
{
    // Conversion / composition providers: derive a value from another provider's output.
    // Dependency-free; they compose other providers through the SerializeReference selector.

    /// <summary>
    /// Builds a rotation from Euler angles (degrees) supplied by a Vector3 provider —
    /// the author-friendly way to drive a Quaternion field (e.g. RotateNode).
    /// </summary>
    [Serializable]
    public class EulerToQuaternionProvider : IQuaternionProvider
    {
        [field: SerializeReference, SerializeReferenceSelector] public IVector3Provider Euler { get; set; } = new Vector3ConstantProvider();

        public Quaternion GetValue() => Quaternion.Euler(Euler?.GetValue() ?? Vector3.zero);
    }

    /// <summary>
    /// Builds a rotation that looks along a forward direction, with an optional up vector.
    /// </summary>
    [Serializable]
    public class LookRotationProvider : IQuaternionProvider
    {
        [field: SerializeReference, SerializeReferenceSelector] public IVector3Provider Forward { get; set; } = new Vector3ConstantProvider();
        [field: SerializeReference, SerializeReferenceSelector] public IVector3Provider Up { get; set; }

        public Quaternion GetValue()
        {
            var forward = Forward?.GetValue() ?? Vector3.forward;
            if (forward == Vector3.zero) return Quaternion.identity;

            var up = Up?.GetValue() ?? Vector3.up;
            return Quaternion.LookRotation(forward, up);
        }
    }

    /// <summary>
    /// Composes a Color from an RGB Vector3 provider and a separate alpha provider —
    /// useful when colour and opacity come from different sources.
    /// </summary>
    [Serializable]
    public class RgbaColorProvider : IColorProvider
    {
        [field: SerializeReference, SerializeReferenceSelector] public IVector3Provider Rgb { get; set; } = new Vector3ConstantProvider();
        [field: SerializeReference, SerializeReferenceSelector] public IFloatProvider Alpha { get; set; } = new FloatConstantProvider();

        public Color GetValue()
        {
            var rgb = Rgb?.GetValue() ?? Vector3.zero;
            var a = Alpha?.GetValue() ?? 1f;
            return new Color(rgb.x, rgb.y, rgb.z, a);
        }
    }

    /// <summary>
    /// Promotes a Vector2 provider to a Vector3 (z defaults to 0).
    /// </summary>
    [Serializable]
    public class Vector2ToVector3Provider : IVector3Provider
    {
        [field: SerializeReference, SerializeReferenceSelector] public IVector2Provider Source { get; set; } = new Vector2ConstantProvider();
        [field: SerializeField] public float Z { get; set; }

        public Vector3 GetValue()
        {
            var v = Source?.GetValue() ?? Vector2.zero;
            return new Vector3(v.x, v.y, Z);
        }
    }
}
