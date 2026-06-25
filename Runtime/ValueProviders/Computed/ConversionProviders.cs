using System;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.ValueProviders
{
    // Conversion / composition providers: derive a value from another provider's output.
    // Dependency-free; they compose other providers through the SerializeReference selector.

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
