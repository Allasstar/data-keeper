using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.Editor.Drawer;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        private bool _isEnabled = true;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var showIfAttribute = (ShowIfAttribute)attribute;

            if (showIfAttribute.FieldToCheck == null) return;

            if (TryGetBoolValue(property, showIfAttribute.FieldToCheck, out var value))
            {
                _isEnabled = showIfAttribute.Inverse ? !value : value;

                if (_isEnabled)
                {
                    PropertyGUI.DrawGUI(position, property, label);
                }
            }
            else
            {
                _isEnabled = true;
                Debug.LogError("Field or property not found (or not bool): " + showIfAttribute.FieldToCheck);
            }
        }

        private static bool TryGetBoolValue(SerializedProperty property, string memberName, out bool value)
        {
            value = false;

            var targetObject = property.serializedObject.targetObject;
            var type = targetObject.GetType();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo propertyInfo = type.GetProperty(memberName, flags);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
            {
                value = (bool)propertyInfo.GetValue(targetObject);
                return true;
            }

            FieldInfo fieldInfo = type.GetField(memberName, flags);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(bool))
            {
                value = (bool)fieldInfo.GetValue(targetObject);
                return true;
            }

            return false;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (_isEnabled)
            {
                return PropertyGUI.GetPropertyHeight(property);
            }

            return 0f;
        }
    }
}