using System.Collections;
using System.Reflection;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    /// <summary>
    /// Evaluates <see cref="ShowIfAttribute"/> for a property.
    ///
    /// ShowIf is intentionally NOT a PropertyDrawer: Unity runs only one PropertyDrawer per
    /// field, so a ShowIf drawer would fight the field's real drawer (e.g.
    /// <see cref="SerializeReferenceSelectorDrawer"/>) for that single slot — only one could win,
    /// breaking either the hiding or the custom UI. Instead the inspector loop
    /// (ComponentDrawer / SODrawer) asks <see cref="IsVisible"/> whether to draw each property,
    /// then draws it with PropertyField, which still invokes its own drawer. This composes with
    /// any other drawer.
    /// </summary>
    public static class ShowIfUtility
    {
        private const BindingFlags MemberFlags =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        /// True when <paramref name="property"/> has no ShowIf, or its condition is satisfied.
        /// </summary>
        public static bool IsVisible(SerializedProperty property)
        {
            var showIf = GetShowIfAttribute(property);
            if (showIf == null || string.IsNullOrEmpty(showIf.FieldToCheck)) return true;

            if (TryGetBoolValue(property, showIf.FieldToCheck, out var value))
            {
                return showIf.Inverse ? !value : value;
            }

            Debug.LogError($"[ShowIf] Bool field or property not found: '{showIf.FieldToCheck}' " +
                           $"(relative to '{property.propertyPath}')");
            return true;
        }

        private static ShowIfAttribute GetShowIfAttribute(SerializedProperty property)
        {
            var field = GetFieldInfo(property);
            return field?.GetCustomAttribute<ShowIfAttribute>(true);
        }

        private static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var container = GetContainingObject(property);
            if (container == null) return null;

            var name = property.name;
            for (var type = container.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, MemberFlags);
                if (field != null) return field;

                // [field: ShowIf] auto-properties serialize as "<Name>k__BackingField".
                var backing = type.GetField($"<{name}>k__BackingField", MemberFlags);
                if (backing != null) return backing;
            }

            return null;
        }

        private static bool TryGetBoolValue(SerializedProperty property, string memberName, out bool value)
        {
            value = false;

            // 1) Resolve as a sibling serialized property. This is path-relative, so it works at
            //    any nesting depth: nested structs/classes, array elements and [SerializeReference]
            //    managed objects, where reflection on the root targetObject would fail.
            var sibling = FindSiblingProperty(property, memberName);
            if (sibling != null && sibling.propertyType == SerializedPropertyType.Boolean)
            {
                value = sibling.boolValue;
                return true;
            }

            // 2) Fall back to reflection on the object that directly contains this property
            //    (covers C# bool properties and non-serialized bool fields).
            var container = GetContainingObject(property);
            if (container != null && TryGetBoolViaReflection(container, memberName, out value))
            {
                return true;
            }

            return false;
        }

        private static SerializedProperty FindSiblingProperty(SerializedProperty property, string memberName)
        {
            var path = property.propertyPath;
            int lastDot = path.LastIndexOf('.');

            if (lastDot < 0)
            {
                return property.serializedObject.FindProperty(memberName);
            }

            var parentPath = path.Substring(0, lastDot);
            var parent = property.serializedObject.FindProperty(parentPath);
            return parent?.FindPropertyRelative(memberName);
        }

        private static bool TryGetBoolViaReflection(object container, string memberName, out bool value)
        {
            value = false;

            for (var type = container.GetType(); type != null; type = type.BaseType)
            {
                var prop = type.GetProperty(memberName, MemberFlags);
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    value = (bool)prop.GetValue(container);
                    return true;
                }

                var field = type.GetField(memberName, MemberFlags);
                if (field != null && field.FieldType == typeof(bool))
                {
                    value = (bool)field.GetValue(container);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the object instance that directly contains <paramref name="property"/>
        /// (the parent in the serialization path), resolving through nesting and arrays.
        /// </summary>
        private static object GetContainingObject(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            // Walk every element except the last (the property itself) to reach its container.
            for (int i = 0; i < elements.Length - 1; i++)
            {
                obj = GetValue(obj, elements[i]);
                if (obj == null) return null;
            }

            return obj;
        }

        private static object GetValue(object source, string element)
        {
            if (source == null) return null;

            int bracket = element.IndexOf('[');
            if (bracket < 0)
            {
                return GetMemberValue(source, element);
            }

            // Array / list element: "name[index]"
            var name = element.Substring(0, bracket);
            var index = int.Parse(element.Substring(bracket).Trim('[', ']'));

            if (GetMemberValue(source, name) is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                for (int i = 0; i <= index; i++)
                {
                    if (!enumerator.MoveNext()) return null;
                }
                return enumerator.Current;
            }

            return null;
        }

        private static object GetMemberValue(object source, string name)
        {
            if (source == null) return null;

            for (var type = source.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, MemberFlags);
                if (field != null) return field.GetValue(source);

                var prop = type.GetProperty(name, MemberFlags);
                if (prop != null) return prop.GetValue(source);
            }

            return null;
        }
    }
}
