using System;
using UnityEngine;

namespace DataKeeper.Attributes
{
    /// <summary>
    /// For UnityEngine.Object fields: after dropping a GameObject (or any of its components),
    /// a small dropdown on the right lets you pick which reference to store — the GameObject
    /// itself or any component on it. Solves Unity always landing the drop as a GameObject.
    /// Pass an optional type to filter the dropdown (e.g. typeof(Graphic) or an interface).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ObjectComponentPickerAttribute : PropertyAttribute
    {
        public Type FilterType { get; }

        public ObjectComponentPickerAttribute(Type filterType = null)
        {
            FilterType = filterType;
        }
    }
}
