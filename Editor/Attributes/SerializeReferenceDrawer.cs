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

        // Cache for script icons: Type -> Texture2D (null means script asset not found)
        private static Dictionary<Type, Texture2D> s_IconCache = new Dictionary<Type, Texture2D>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawSerializeReferenceField(position, property, label);
            }
            else if (property.propertyType == SerializedPropertyType.Generic &&
                     property.isArray &&
                     property.arrayElementType.StartsWith(MANAGED_REFERENCE))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
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

            // Determine current type and its icon
            string currentTypeName = NULL_TYPE_NAME;
            Texture2D currentIcon = null;

            if (property.managedReferenceValue != null)
            {
                Type currentType = property.managedReferenceValue.GetType();
                currentTypeName = ObjectNames.NicifyVariableName(currentType.Name);
                currentIcon = GetScriptIcon(currentType);
            }

            Rect popupRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            popupRect.height = EditorGUIUtility.singleLineHeight;

            // Build button label with icon if one exists
            GUIContent buttonContent;
            if (currentIcon != null)
            {
                buttonContent = new GUIContent($" {currentTypeName}", currentIcon);
            }
            else
            {
                buttonContent = new GUIContent($"{currentTypeName}");
            }

            if (GUI.Button(popupRect, buttonContent, EditorStyles.popup))
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

        /// <summary>
        /// Returns the icon for the given type's MonoScript.
        /// Uses the custom icon if one is assigned, otherwise falls back to
        /// the default Unity C# script icon so something is always shown.
        /// </summary>
        private static Texture2D GetScriptIcon(Type type)
        {
            if (type == null) return null;

            if (s_IconCache.TryGetValue(type, out Texture2D cachedIcon))
                return cachedIcon;

            Texture2D icon = null;

            MonoScript script = FindMonoScript(type);
            if (script != null)
            {
                // Returns a custom icon if one was assigned in the importer, null otherwise
                icon = EditorGUIUtility.GetIconForObject(script) as Texture2D;
            }

            // Fall back to the default C# script icon so it's never blank
            if (icon == null)
                icon = EditorGUIUtility.FindTexture("cs Script Icon")
                    ?? EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            s_IconCache[type] = icon;
            return icon;
        }

        /// <summary>
        /// Finds the MonoScript asset for the given C# type.
        /// </summary>
        private static MonoScript FindMonoScript(Type type)
        {
            // Fast path: use the type name to locate the script asset
            string typeName = type.Name;
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeName}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return script;
            }

            return null;
        }

        private Type[] GetValidTypes(Type baseType)
        {
            if (s_TypeCache.TryGetValue(baseType, out Type[] types))
                return types;

            List<Type> derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch (Exception) { return Type.EmptyTypes; }
                })
                .Where(type => baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.Name)
                .ToList();

            s_TypeCache[baseType] = derivedTypes.ToArray();
            return s_TypeCache[baseType];
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        // ─── Dropdown ────────────────────────────────────────────────────────────

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

                root.AddChild(new TypeDropdownItem(NULL_TYPE_NAME, null));

                var typesWithNamespace = _validTypes
                    .Where(t => !string.IsNullOrEmpty(t.Namespace))
                    .GroupBy(t => t.Namespace)
                    .OrderBy(g => g.Key);

                foreach (var type in _validTypes.Where(t => string.IsNullOrEmpty(t.Namespace)).OrderBy(t => t.Name))
                    root.AddChild(BuildTypeItem(type));

                foreach (var namespaceGroup in typesWithNamespace)
                {
                    var namespaceItem = new AdvancedDropdownItem(namespaceGroup.Key);
                    root.AddChild(namespaceItem);

                    foreach (var type in namespaceGroup.OrderBy(t => t.Name))
                        namespaceItem.AddChild(BuildTypeItem(type));
                }

                return root;
            }

            private static TypeDropdownItem BuildTypeItem(Type type)
            {
                string niceName = ObjectNames.NicifyVariableName(type.Name);
                var item = new TypeDropdownItem(niceName, type);

                // Assign icon to the dropdown item if the script has a custom one
                // Texture2D icon = GetScriptIcon(type);
                // if (icon != null)
                //     item.icon = icon;

                return item;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is TypeDropdownItem typeItem)
                    _onSelected?.Invoke(typeItem.Type);
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