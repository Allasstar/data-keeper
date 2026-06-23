using System;
using UnityEngine;

namespace DataKeeper.Attributes
{
    /// <summary>
    /// Constrains a serialized UnityEngine.Object field so the inspector only
    /// accepts objects implementing the given interface type.
    /// The field must be typed as UnityEngine.Object (interfaces aren't serializable).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RequireInterfaceAttribute : PropertyAttribute
    {
        public Type InterfaceType { get; }

        public RequireInterfaceAttribute(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"{nameof(RequireInterfaceAttribute)}: {interfaceType.Name} is not an interface.");

            InterfaceType = interfaceType;
        }
    }
}
