using System;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Attributes
{
    /// <summary>
    /// Draws a UnityEngine.Object field that only accepts objects implementing
    /// the interface declared by <see cref="RequireInterfaceAttribute"/>.
    /// Dropping a GameObject picks the first component implementing the interface.
    /// </summary>
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "[RequireInterface] only works on Object fields.");
                return;
            }

            Type interfaceType = ((RequireInterfaceAttribute)attribute).InterfaceType;

            EditorGUI.BeginProperty(position, label, property);

            Object assigned = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(Object), true);

            property.objectReferenceValue = Resolve(assigned, interfaceType);

            EditorGUI.EndProperty();
        }

        private static Object Resolve(Object candidate, Type interfaceType)
        {
            if (candidate == null)
                return null;

            if (interfaceType.IsInstanceOfType(candidate))
                return candidate;

            // Dropped a GameObject -> grab the first matching component.
            if (candidate is GameObject go && go.TryGetComponent(interfaceType, out Component component))
                return component;

            return null;
        }
    }
}
