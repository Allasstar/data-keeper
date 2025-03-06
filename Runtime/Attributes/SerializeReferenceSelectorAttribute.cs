using System;
using UnityEngine;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeReferenceSelectorAttribute : PropertyAttribute
    {
        public Type BaseType { get; private set; }
    
        public SerializeReferenceSelectorAttribute(Type baseType = null)
        {
            BaseType = baseType;
        }
    }
}
