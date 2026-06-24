using System;
using UnityEngine;

namespace DataKeeper.Attributes
{
    /// <summary>
    /// Show/Hide field in inspector. Input any boolean field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string FieldToCheck { get; private set; }
        public bool Inverse { get; private set; }

        public ShowIfAttribute(string fieldToCheck, bool inverse = false)
        {
            FieldToCheck = fieldToCheck;
            Inverse = inverse;
        }
    }
}
