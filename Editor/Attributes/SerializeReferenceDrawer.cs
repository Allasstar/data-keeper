using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(SerializeReferenceSelectorAttribute))]
    public class SerializeReferenceDrawer : PropertyDrawer
    {
        private const string NULL_TYPE_NAME = "- null -";
        private const string MANAGED_REFERENCE = "managedReference";

        private static Dictionary<Type, Type[]> s_TypeCache = new Dictionary<Type, Type[]>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // Handle single SerializeReference field
                DrawSerializeReferenceField(position, property, label);
            }
            else if (property.propertyType == SerializedPropertyType.Generic &&
                     property.isArray &&
                     property.arrayElementType.StartsWith(MANAGED_REFERENCE))
            {
                // Draw the default property for arrays
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                // For any other property type, just draw the default property field
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private void DrawSerializeReferenceField(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializeReferenceSelectorAttribute attribute = (SerializeReferenceSelectorAttribute) this.attribute;

            Type baseType = attribute.BaseType ?? fieldInfo.FieldType;

            // If it's a generic type (like List<T>), get the element type
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
            {
                baseType = baseType.GetGenericArguments()[0];
            }

            string currentTypeName = NULL_TYPE_NAME;
            int currentTypeIndex = 0;

            // Get all valid types that can be assigned
            Type[] validTypes = GetValidTypes(baseType);

            // Make the list include "null" as the first option
            string[] options = new string[validTypes.Length + 1];
            options[0] = NULL_TYPE_NAME;

            for (int i = 0; i < validTypes.Length; i++)
            {
                options[i + 1] = validTypes[i].Name;

                // Check if this is the current type
                if (property.managedReferenceValue != null && property.managedReferenceValue.GetType() == validTypes[i])
                {
                    currentTypeIndex = i + 1;
                }
            }

            // Determine the main fieldRect and the dropdown rect
            Rect fieldRect = position;
            fieldRect.height = EditorGUIUtility.singleLineHeight;

            Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Draw the dropdown field using EditorGUI.Popup
            EditorGUI.BeginChangeCheck();
            int selectedIndex = EditorGUI.Popup(dropdownRect, label.text, currentTypeIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex == 0)
                {
                    // Selected "null"
                    property.managedReferenceValue = null;
                }
                else
                {
                    // Create instance of selected type
                    Type selectedType = validTypes[selectedIndex - 1];
                    property.managedReferenceValue = Activator.CreateInstance(selectedType);
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            // Draw the property fields if not null
            if (property.managedReferenceValue != null)
            {
                Rect contentRect = position;
                contentRect.height = position.height;

                // Use a sub-property drawer without the label
                EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);
            }
        }

        private Type[] GetValidTypes(Type baseType)
        {
            // Check the cache first
            if (s_TypeCache.TryGetValue(baseType, out Type[] types))
            {
                return types;
            }

            // Get all types that derive from the base type
            List<Type> derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.Name) // Sort alphabetically
                .ToList();

            // Cache the result
            s_TypeCache[baseType] = derivedTypes.ToArray();

            return s_TypeCache[baseType];
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}