using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class StaticClassInspectorWindow : EditorWindow
    {
        private Type selectedType;
        private Vector2 scrollPosition;
        private AdvancedDropdown dropdown;
        private Dictionary<string, List<Type>> categoryToTypes = new Dictionary<string, List<Type>>();

        [MenuItem("Tools/Windows/Static Class Inspector", priority = 2)]
        public static void ShowWindow()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_UnityEditor.DebugInspectorWindow");

            var window = GetWindow<StaticClassInspectorWindow>();
            window.titleContent = new GUIContent("Static Class Inspector", icon);
        }

        private void OnEnable()
        {
            // Find all static classes with StaticClassInspectorAttribute
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && // Static class check
                            t.GetCustomAttribute<StaticClassInspectorAttribute>() != null)
                .ToList();

            // Group types by category
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

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.toolbar);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(selectedType != null ? selectedType.Name : "Select a class", 
                    EditorStyles.toolbarPopup, GUILayout.Width(200)))
            {
                new StaticClassDropdown(new AdvancedDropdownState(), categoryToTypes, OnStaticClassSelected)
                    .Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (selectedType == null)
            {
                EditorGUILayout.HelpBox("Select a static class to inspect its members.", MessageType.Info);
                return;
            }

            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
            // Get all fields and properties of the selected type
            var fields = selectedType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var properties = selectedType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0); // Only properties that we can read and aren't indexers

            // Display fields
            if (fields.Length > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Fields", EditorStyles.boldLabel);
            
                foreach (var field in fields)
                {
                    if (field.IsLiteral) continue; // Skip constants
                
                    EditorGUI.BeginChangeCheck();
                
                    // Draw field editor based on type
                    object newValue = DrawPropertyEditor(field.Name, field.FieldType, field.GetValue(null));
                
                    // Set the value if changed
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Static Field");
                        field.SetValue(null, newValue);
                        EditorUtility.SetDirty(this);
                    }
                }
            }

            // Display properties
            if (properties.Count() > 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            
                foreach (var property in properties)
                {
                    bool canWrite = property.CanWrite && property.GetSetMethod(false) != null;
                
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.BeginDisabledGroup(!canWrite);

                    // Draw property editor based on type
                    object value = property.GetValue(null);
                    object newValue = DrawPropertyEditor(property.Name, property.PropertyType, value);
                
                    // Set the value if changed and property is writable
                    if (canWrite && EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Static Property");
                        try
                        {
                            property.SetValue(null, newValue);
                            EditorUtility.SetDirty(this);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error setting property {property.Name}: {e.Message}");
                        }
                    }
                
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private object DrawPropertyEditor(string name, Type type, object value)
        {
            // Handle different property types
            if (type == typeof(int))
            {
                return EditorGUILayout.IntField(name, (int)value);
            }
            else if (type == typeof(float))
            {
                return EditorGUILayout.FloatField(name, (float)value);
            }
            else if (type == typeof(bool))
            {
                return EditorGUILayout.Toggle(name, (bool)value);
            }
            else if (type == typeof(string))
            {
                return EditorGUILayout.TextField(name, (string)value);
            }
            else if (type == typeof(Vector2))
            {
                return EditorGUILayout.Vector2Field(name, (Vector2)value);
            }
            else if (type == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field(name, (Vector3)value);
            }
            else if (type == typeof(Vector4))
            {
                return EditorGUILayout.Vector4Field(name, (Vector4)value);
            }
            else if (type == typeof(Color))
            {
                return EditorGUILayout.ColorField(name, (Color)value);
            }
            else if (type == typeof(Rect))
            {
                return EditorGUILayout.RectField(name, (Rect)value);
            }
            else if (type == typeof(Bounds))
            {
                return EditorGUILayout.BoundsField(name, (Bounds)value);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Reactive<>))
            {
                // TODO: Handle Reactive<T> properties
                Type innerType = type.GetGenericArguments()[0];
                object innerValue = value != null ? type.GetProperty("Value").GetValue(value) : null;
        
                EditorGUI.BeginChangeCheck();
                object newInnerValue = DrawPropertyEditor($"{name}.Value", innerType, innerValue);
        
                if (EditorGUI.EndChangeCheck() && value != null)
                {
                    // Set the inner value using the Value property
                    type.GetProperty("Value").SetValue(value, newInnerValue);
                }
        
                return value; // Return the original reactive object
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReactivePref<>))
            {
                // TODO: Handle ReactivePref<T> properties
                Type innerType = type.GetGenericArguments()[0];
                object innerValue = value != null ? type.GetProperty("Value").GetValue(value) : null;
        
                EditorGUI.BeginChangeCheck();
                object newInnerValue = DrawPropertyEditor($"{name}.Value", innerType, innerValue);
        
                if (EditorGUI.EndChangeCheck() && value != null)
                {
                    // Set the inner value using the Value property
                    type.GetProperty("Value").SetValue(value, newInnerValue);
                }
        
                return value; // Return the original reactive object
            }
            else if (type.IsEnum)
            {
                return EditorGUILayout.EnumPopup(name, (Enum)value);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return EditorGUILayout.ObjectField(name, (UnityEngine.Object)value, type, false);
            }
            else
            {
                // For types we can't edit, at least display the value as string
                EditorGUILayout.LabelField(name, value != null ? value.ToString() : "null");
                return value;
            }
        }

        private void OnStaticClassSelected(Type selectedType)
        {
            this.selectedType = selectedType;
            Repaint();
        }

        // Custom AdvancedDropdown for selecting static classes
        private class StaticClassDropdown : AdvancedDropdown
        {
            private Dictionary<string, List<Type>> categoryToTypes;
            private System.Action<Type> onSelectionCallback;

            public StaticClassDropdown(AdvancedDropdownState state, Dictionary<string, List<Type>> categoryToTypes, System.Action<Type> callback) 
                : base(state)
            {
                this.categoryToTypes = categoryToTypes;
                this.onSelectionCallback = callback;
                minimumSize = new Vector2(250, 300);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Static Classes");

                foreach (var category in categoryToTypes.Keys)
                {
                    var categoryItem = new AdvancedDropdownItem(category);
                    root.AddChild(categoryItem);

                    foreach (var type in categoryToTypes[category])
                    {
                        var typeItem = new StaticClassDropdownItem(type.Name, type);
                        categoryItem.AddChild(typeItem);
                    }
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is StaticClassDropdownItem typeItem)
                {
                    onSelectionCallback?.Invoke(typeItem.Type);
                }
            }

            private class StaticClassDropdownItem : AdvancedDropdownItem
            {
                public Type Type { get; private set; }

                public StaticClassDropdownItem(string name, Type type) : base(name)
                {
                    Type = type;
                }
            }
        }
    }
}
