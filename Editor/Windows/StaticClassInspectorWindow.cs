using System;
using System.Collections.Generic;
using System.Reflection;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataKeeper.Editor.Windows
{
    public class StaticClassInspectorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private AdvancedDropdown dropdown;
        private Dictionary<string, List<Type>> categoryToTypes = new Dictionary<string, List<Type>>();

        private Type selectedClassType = null;
        private string jsonContent = "";
        private string originalJsonContent = "";
        private bool hasChanges = false;
        private Vector2 jsonScrollPosition;

        [MenuItem("Tools/Windows/Static Class Inspector (Beta)", priority = 3)]
        public static void ShowWindow()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_UnityEditor.DebugInspectorWindow");

            var window = GetWindow<StaticClassInspectorWindow>();
            window.minSize = new Vector2(400, 300);
            window.titleContent = new GUIContent("Static Class", icon);
        }

        private void OnEnable()
        {
            GetTypes();
        }

        private void GetTypes()
        {
            var types = TypeCache.GetTypesWithAttribute(typeof(StaticClassInspectorAttribute));

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
            if (GUILayout.Button(selectedClassType != null ? selectedClassType.Name : "Select a class",
                    EditorStyles.toolbarPopup, GUILayout.Width(200)))
            {
                new StaticClassDropdown(new AdvancedDropdownState(), categoryToTypes, OnStaticClassSelected)
                    .Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }

            GUILayout.FlexibleSpace();

            // Add refresh button
            if (selectedClassType != null)
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RefreshData();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (selectedClassType == null)
            {
                EditorGUILayout.HelpBox(
                    "How to use:\n• Apply \"StaticClassInspector\" attribute to a static class. \n• Select a static class to inspect its members.",
                    MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Draw Text box with json of selected static class
            GUILayout.Space(5);

            EditorGUILayout.LabelField("JSON Content:", EditorStyles.boldLabel);
            
            // Create a custom GUIStyle for the text area
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            EditorGUI.BeginChangeCheck();
            
            // Use a fixed container for the scroll view to prevent layout issues
            EditorGUILayout.BeginVertical();
            jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition);
            
            // Use height expansion for the text area
            jsonContent = EditorGUILayout.TextArea(jsonContent, textAreaStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(300));
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            if (EditorGUI.EndChangeCheck())
            {
                hasChanges = jsonContent != originalJsonContent;
            }

            GUILayout.Space(10);

            // Draw cancel and save buttons
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = hasChanges;
            if (GUILayout.Button("Revert Changes", GUILayout.Width(120)))
            {
                RevertChanges();
            }
            
            if (GUILayout.Button("Save Changes", GUILayout.Width(120)))
            {
                SaveChangesToStaticClass();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
    
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
        }
        
        private void OnStaticClassSelected(Type selectedType)
        {
            selectedClassType = selectedType;
            LoadStaticClassToJson();
            Repaint();
        }

        private void LoadStaticClassToJson()
        {
            try
            {
                var staticFields = selectedClassType.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => !f.IsInitOnly && !f.IsLiteral)
                    .ToList();
        
                var staticProperties = selectedClassType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToList();

                // Create a dictionary to hold field/property values
                var data = new Dictionary<string, object>();
        
                // Populate fields
                foreach (var field in staticFields)
                {
                    try
                    {
                        data[field.Name] = field.GetValue(null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error reading field {field.Name}: {ex.Message}");
                    }
                }
        
                // Populate properties
                foreach (var property in staticProperties)
                {
                    try
                    {
                        data[property.Name] = property.GetValue(null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error reading property {property.Name}: {ex.Message}");
                    }
                }
        
                // Configure JsonSerializer to ignore reference loops
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented
                };
        
                // Convert to JSON with custom settings
                jsonContent = JsonConvert.SerializeObject(data, settings);
                originalJsonContent = jsonContent;
                hasChanges = false;
            }
            catch (Exception ex)
            {
                jsonContent = $"Error: {ex.Message}";
                originalJsonContent = jsonContent;
                hasChanges = false;
            }
        }
        
        private void RefreshData()
        {
            GUI.FocusControl(null);
            // GUI.UnfocusWindow();
            Type currentType = selectedClassType;
            selectedClassType = null;
            
            // Briefly delay before reselecting to ensure UI updates properly
            EditorApplication.delayCall += () =>
            {
                selectedClassType = currentType;
                LoadStaticClassToJson();
                Repaint();
            };
        }
        
        private void RevertChanges()
        {
            GUI.FocusControl(null);

            jsonContent = originalJsonContent;
            hasChanges = false;
            
            Repaint();
        }
        
        private void SaveChangesToStaticClass()
        {
            try
            {
                // Deserialize JSON string to dictionary
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent, settings);
                
                // Get static fields and properties
                var staticFields = selectedClassType.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => !f.IsInitOnly && !f.IsLiteral)
                    .ToDictionary(f => f.Name);
                
                var staticProperties = selectedClassType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToDictionary(p => p.Name);
                
                // Update fields
                foreach (var item in data)
                {
                    if (staticFields.TryGetValue(item.Key, out var field))
                    {
                        try
                        {
                            var value = ConvertValue(item.Value, field.FieldType);
                            field.SetValue(null, value);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error setting field {item.Key}: {ex.Message}");
                        }
                    }
                    else if (staticProperties.TryGetValue(item.Key, out var property))
                    {
                        try
                        {
                            var value = ConvertValue(item.Value, property.PropertyType);
                            property.SetValue(null, value);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error setting property {item.Key}: {ex.Message}");
                        }
                    }
                }
                
                // Refresh via type deselection/reselection
                RefreshData();
                EditorUtility.DisplayDialog("Success", "Changes saved successfully!", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deserializing or applying changes: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to save changes: {ex.Message}", "OK");
            }
        }
        
        private object ConvertValue(object value, Type targetType)
        {
            // Handle the case where JSON.NET deserializes numbers as long or double
            if (value is Newtonsoft.Json.Linq.JValue jValue)
            {
                value = jValue.Value;
            }

            // Handle basic type conversions
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
            
            // Convert numeric types appropriately
            if (value is long longValue && targetType == typeof(int))
            {
                return Convert.ToInt32(longValue);
            }
            
            if (value is double doubleValue)
            {
                if (targetType == typeof(float))
                    return Convert.ToSingle(doubleValue);
                if (targetType == typeof(decimal))
                    return Convert.ToDecimal(doubleValue);
            }
            
            // Handle special Unity types that may need conversion
            if (targetType == typeof(Vector2) && value is JObject v2Obj)
            {
                return new Vector2(
                    v2Obj["x"]?.ToObject<float>() ?? 0f,
                    v2Obj["y"]?.ToObject<float>() ?? 0f
                );
            }
            
            if (targetType == typeof(Vector3) && value is JObject v3Obj)
            {
                return new Vector3(
                    v3Obj["x"]?.ToObject<float>() ?? 0f,
                    v3Obj["y"]?.ToObject<float>() ?? 0f,
                    v3Obj["z"]?.ToObject<float>() ?? 0f
                );
            }
            
            if (targetType == typeof(Color) && value is JObject colorObj)
            {
                return new Color(
                    colorObj["r"]?.ToObject<float>() ?? 0f,
                    colorObj["g"]?.ToObject<float>() ?? 0f,
                    colorObj["b"]?.ToObject<float>() ?? 0f,
                    colorObj["a"]?.ToObject<float>() ?? 1f
                );
            }
            
            // For complex objects, try direct deserialization
            if (value is JObject || value is JArray)
            {
                string json = value.ToString();
                return JsonConvert.DeserializeObject(json, targetType);
            }
            
            // Default conversion
            return Convert.ChangeType(value, targetType);
        }

        // Custom AdvancedDropdown for selecting static classes
        private class StaticClassDropdown : AdvancedDropdown
        {
            private Dictionary<string, List<Type>> categoryToTypes;
            private System.Action<Type> onSelectionCallback;

            public StaticClassDropdown(AdvancedDropdownState state, Dictionary<string, List<Type>> categoryToTypes,
                System.Action<Type> callback)
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