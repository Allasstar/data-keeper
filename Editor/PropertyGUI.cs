using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor
{
    public static class PropertyGUI
    {
        public static void DrawButtons(Object target)
        {
            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(ButtonAttribute), true);
                if (attributes.Length > 0)
                {
                    ButtonAttribute buttonAttribute = attributes[0] as ButtonAttribute;
                    string buttonLabel = buttonAttribute.ButtonLabel ?? method.Name;

                    EditorGUILayout.Space(buttonAttribute.Space);
                    if (GUILayout.Button(buttonLabel))
                    {
                        method.Invoke(target, null);
                    }
                }
            }
        }
        
        public static void DrawGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.type == typeof(Optional<>).Name)
            {
                DrawGUIOptional(position, property, label);
                return;
            }
            
            EditorGUI.PropertyField(position, property, label, true);
        }
        
        public static float GetPropertyHeight(SerializedProperty property)
        {
            if (property.type == typeof(Optional<>).Name)
            {
                return PropertyHeightOptional(property);
            }
            
            return EditorGUI.GetPropertyHeight(property);
        }
        
        // --- GUI ---
        
        private static void DrawGUIOptional(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            var enabledProperty = property.FindPropertyRelative("enabled");

            EditorGUI.BeginProperty(position, label, property);

            // Define checkbox size
            float checkboxSize = EditorGUIUtility.singleLineHeight;
            position.x -= checkboxSize - 5;

            Rect checkboxRect = new Rect(position.x, position.y, checkboxSize, checkboxSize);

            // Draw checkbox at the top-left corner
            enabledProperty.boolValue = EditorGUI.Toggle(checkboxRect, enabledProperty.boolValue);

            // Adjust position for the main field
            Rect valueRect = new Rect(position.x + checkboxSize + 15, position.y, position.width - checkboxSize - 3, position.height);

            EditorGUI.BeginDisabledGroup(!enabledProperty.boolValue);
            EditorGUI.PropertyField(valueRect, valueProperty, label, true);
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }

        
        private static void DrawGUIOptionalOld(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            var enabledProperty = property.FindPropertyRelative("enabled");

            EditorGUI.BeginProperty(position, label, property);

            position.x = 50;
            position.width -= 30;
            EditorGUI.BeginDisabledGroup(!enabledProperty.boolValue);
            EditorGUI.PropertyField(position, valueProperty, label, true);
            EditorGUI.EndDisabledGroup();

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            position.x = 20;
            position.width = position.height = EditorGUI.GetPropertyHeight(enabledProperty);
            EditorGUI.PropertyField(position, enabledProperty, GUIContent.none);
            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }

        private static float PropertyHeightOptional(SerializedProperty property)
        {
            var valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }
    }
}