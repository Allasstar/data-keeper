using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(SerializeReferenceSelectorAttribute))]
    public class SerializeReferenceDrawer : PropertyDrawer
    {
        private const string NULL_TYPE_NAME = "- null -";
        private const string MANAGED_REFERENCE = "managedReference";

        private static Dictionary<Type, Type[]> s_TypeCache = new Dictionary<Type, Type[]>();
        private static AdvancedDropdownState s_DropdownState = new AdvancedDropdownState();

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
            SerializeReferenceSelectorAttribute attribute = (SerializeReferenceSelectorAttribute)this.attribute;

            Type baseType = attribute.BaseType ?? fieldInfo.FieldType;

            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
            {
                baseType = baseType.GetGenericArguments()[0];
            }

            Type[] validTypes = GetValidTypes(baseType);

            string currentTypeName = NULL_TYPE_NAME;
            if (property.managedReferenceValue != null)
            {
                Type currentType = property.managedReferenceValue.GetType();
                currentTypeName = currentType.Name;
            }

            Rect popupRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            popupRect.height = EditorGUIUtility.singleLineHeight;
            
            if (GUI.Button(popupRect, $"<{currentTypeName}>", EditorStyles.popup))
            {
                var dropdown = new TypeDropdown(s_DropdownState, validTypes, baseType, selectedType =>
                {
                    foreach (var target in property.serializedObject.targetObjects)
                    {
                        SerializedObject serializedObject = new SerializedObject(target);
                        SerializedProperty targetProperty = serializedObject.FindProperty(property.propertyPath);

                        if (selectedType == null)
                        {
                            targetProperty.managedReferenceValue = null;
                        }
                        else
                        {
                            targetProperty.managedReferenceValue = Activator.CreateInstance(selectedType);
                        }

                        serializedObject.ApplyModifiedProperties();
                    }
                });

                dropdown.Show(popupRect);
            }

            if (property.managedReferenceValue != null)
            {
                Rect contentRect = position;
                contentRect.height = position.height;
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
                .SelectMany(assembly => 
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (Exception)
                    {
                        // Handle reflection exceptions gracefully
                        return Type.EmptyTypes;
                    }
                })
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

        private class TypeDropdown : AdvancedDropdown
        {
            private readonly Type[] _validTypes;
            private readonly Type _baseType;
            private readonly Action<Type> _onSelected;

            public TypeDropdown(AdvancedDropdownState state, Type[] validTypes, Type baseType, Action<Type> onSelected) 
                : base(state)
            {
                _validTypes = validTypes;
                _baseType = baseType;
                _onSelected = onSelected;
                minimumSize = new Vector2(200, 200);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Types");

                // Add null option
                var nullItem = new TypeDropdownItem(NULL_TYPE_NAME, null);
                root.AddChild(nullItem);

                // Group types by namespace
                var typesWithNamespace = _validTypes.Where(t => !string.IsNullOrEmpty(t.Namespace))
                    .GroupBy(t => t.Namespace)
                    .OrderBy(g => g.Key);
                
                // Add types without namespace directly to root
                foreach (var type in _validTypes.Where(t => string.IsNullOrEmpty(t.Namespace)).OrderBy(t => t.Name))
                {
                    root.AddChild(new TypeDropdownItem(type.Name, type));
                }

                // Add namespaced types to their respective groups
                foreach (var namespaceGroup in typesWithNamespace)
                {
                    var namespaceItem = new AdvancedDropdownItem(namespaceGroup.Key);
                    root.AddChild(namespaceItem);
                    
                    foreach (var type in namespaceGroup.OrderBy(t => t.Name))
                    {
                        namespaceItem.AddChild(new TypeDropdownItem(type.Name, type));
                    }
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is TypeDropdownItem typeItem)
                {
                    _onSelected?.Invoke(typeItem.Type);
                }
            }
        }

        private class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }

            public TypeDropdownItem(string displayName, Type type) : base(displayName)
            {
                Type = type;
            }
        }
    }
}