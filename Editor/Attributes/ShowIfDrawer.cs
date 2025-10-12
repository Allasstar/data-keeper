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
            var enableIfAttribute = (ShowIfAttribute)attribute;
            
            if (enableIfAttribute.FieldToCheck == null) return;
            
            var targetObject = property.serializedObject.targetObject.GetType();
            PropertyInfo propertyInfo = targetObject.GetProperty(enableIfAttribute.FieldToCheck, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            if (propertyInfo != null)
            {
                _isEnabled = (bool)propertyInfo.GetValue(property.serializedObject.targetObject);
                
                if (_isEnabled)
                {
                    PropertyGUI.DrawGUI(position, property, label);
                }
            }
            else
            {
                Debug.LogError("Property not found: " + enableIfAttribute.FieldToCheck);
            }
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