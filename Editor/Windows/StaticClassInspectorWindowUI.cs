using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows
{
    public class StaticClassInspectorWindowUI : EditorWindow
    {
        private Dictionary<string, List<Type>> categoryToTypes = new();
        private ScrollView scrollView;
        private Toggle showPrivateToggle;
        private DropdownField classDropdown;
    
        // [MenuItem("Tools/Windows/Static Class Inspector UI (Beta)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StaticClassInspectorWindowUI>();
            window.minSize = new Vector2(400, 300);
            window.titleContent = new GUIContent("Static Class");
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            GetTypes();
            CreateUI();
        }

        private void GetTypes()
        {
            var types = TypeCache.GetTypesWithAttribute(typeof(StaticClassInspectorAttribute));
            categoryToTypes.Clear();
            
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<StaticClassInspectorAttribute>();
                if (!categoryToTypes.ContainsKey(attr.Category))
                {
                    categoryToTypes[attr.Category] = new List<Type>();
                }
                categoryToTypes[attr.Category].Add(type);
            }
        }

        private void CreateUI()
        {
            var toolbar = new Toolbar();
            classDropdown = new DropdownField();
            showPrivateToggle = new Toggle("Show Private");
            showPrivateToggle.AddToClassList("unity-toolbar-toggle");

            toolbar.Add(classDropdown);
            toolbar.Add(showPrivateToggle);
            rootVisualElement.Add(toolbar);

            scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

            PopulateDropdown();
        }

        private void PopulateDropdown()
        {
            List<string> options = categoryToTypes.Values.SelectMany(t => t.Select(type => type.Name)).ToList();
            classDropdown.choices = options;
            classDropdown.RegisterValueChangedCallback(evt => OnClassSelected(evt.newValue));
        }

        private void OnClassSelected(string className)
        {
            Type selectedType = categoryToTypes.Values.SelectMany(t => t).FirstOrDefault(t => t.Name == className);
            if (selectedType != null)
            {
                DrawClassMembers(selectedType);
            }
        }

        private void DrawClassMembers(Type selectedType)
        {
            scrollView.Clear();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            if (showPrivateToggle.value)
                flags |= BindingFlags.NonPublic;
        
            var fields = selectedType.GetFields(flags);
            var properties = selectedType.GetProperties(flags).Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
        
            foreach (var field in fields)
            {
                if (!field.IsLiteral)
                    scrollView.Add(CreateFieldUI(field));
            }
        
            foreach (var property in properties)
            {
                scrollView.Add(CreatePropertyUI(property));
            }
        }

        private VisualElement CreateFieldUI(FieldInfo field)
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var label = new Label(field.Name) { style = { width = 150 } };
            container.Add(label);
        
            var valueField = CreateValueField(field.FieldType, 
                field.GetValue(null), 
                obj => field.SetValue(null, obj));
            
            container.Add(valueField);
        
            return container;
        }

        private VisualElement CreatePropertyUI(PropertyInfo property)
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var label = new Label(property.Name) { style = { width = 150 } };
            container.Add(label);
        
            var valueField = CreateValueField(property.PropertyType, 
                property.GetValue(null), 
                obj =>
                {
                    if (property.CanWrite)
                    {
                        property.SetValue(null, obj);
                    }
                });
            
            if (!property.CanWrite)
            {
                valueField.SetEnabled(false);
            }
            
            container.Add(valueField);
        
            return container;
        }

        private VisualElement CreateValueField(Type type, object value, Action<object> onValueChanged)
        {
            if (type == typeof(int))
            {
                var field = new IntegerField { value = (int)(value ?? 0) };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                return field;
            }

            if (type == typeof(float))
            {
                var field = new FloatField { value = (float)(value ?? 0f) };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                return field;
            }

            if (type == typeof(bool))
            {
                var field = new Toggle { value = (bool)(value ?? false) };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                return field;
            }

            if (type == typeof(string))
            {
                var field = new TextField { value = (string)(value ?? "") };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                return field;
            }

            if (type == typeof(Vector3))
            {
                var field = new Vector3Field { value = (Vector3)(value ?? Vector3.zero) };
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                return field;
            }

            if (type.IsEnum)
            {
                var field = new EnumField((Enum)(value ?? Enum.GetValues(type).GetValue(0)));
                field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                return field;
            }

            return new Label(value?.ToString() ?? "N/A");
        }

    }
}
