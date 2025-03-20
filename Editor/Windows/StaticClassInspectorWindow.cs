using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.Editor.Settings;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class StaticClassInspectorWindow : EditorWindow
    {
        private bool showPrivate;
        private Vector2 scrollPosition;
        private AdvancedDropdown dropdown;
        private Dictionary<string, List<Type>> categoryToTypes = new Dictionary<string, List<Type>>();
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        private Dictionary<string, KeyValuePair<object, object>> dictionaryNewKeyValues =
            new Dictionary<string, KeyValuePair<object, object>>();

        [MenuItem("Tools/Windows/Static Class Inspector (Beta)", priority = 2)]
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
            if (GUILayout.Button(
                    DataKeeperEditorPref.SelectedStaticClassPref.Value != null
                        ? DataKeeperEditorPref.SelectedStaticClassPref.Value.Name
                        : "Select a class",
                    EditorStyles.toolbarPopup, GUILayout.Width(200)))
            {
                new StaticClassDropdown(new AdvancedDropdownState(), categoryToTypes, OnStaticClassSelected)
                    .Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }

            GUILayout.FlexibleSpace();

            showPrivate = GUILayout.Toggle(showPrivate, "Show Private", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (DataKeeperEditorPref.SelectedStaticClassPref.Value == null)
            {
                EditorGUILayout.HelpBox(
                    "How to use:\n• Apply \"StaticClassInspector\" attribute to a static class. \n• Select a static class to inspect its members.",
                    MessageType.Info);
                return;
            }

            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Get all fields and properties of the selected type
            var fields = showPrivate
                ? DataKeeperEditorPref.SelectedStaticClassPref.Value.GetFields(BindingFlags.NonPublic |
                                                                               BindingFlags.Public |
                                                                               BindingFlags.Static)
                : DataKeeperEditorPref.SelectedStaticClassPref.Value.GetFields(
                    BindingFlags.Public | BindingFlags.Static);

            var properties = DataKeeperEditorPref.SelectedStaticClassPref.Value
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanRead &&
                            p.GetIndexParameters().Length == 0); // Only properties that we can read and aren't indexers

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
            if (properties.Any())
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

        private object DrawPropertyEditor(string name, Type type, object value, string path = null)
        {
            string fullPath = string.IsNullOrEmpty(path) ? name : $"{type}:{path}.{name}";

            // Handle different property types using switch for better readability
            switch (true)
            {
                case var _ when value is Type:
                    EditorGUILayout.LabelField(name, value.ToString());
                    return value;

                case var _ when type == typeof(int):
                    return EditorGUILayout.IntField(name, (int) value);

                case var _ when type == typeof(float):
                    return EditorGUILayout.FloatField(name, (float) value);

                case var _ when type == typeof(bool):
                    return EditorGUILayout.Toggle(name, (bool) value);

                case var _ when type == typeof(string):
                    return EditorGUILayout.TextField(name, (string) value);

                case var _ when type == typeof(Vector2):
                    return EditorGUILayout.Vector2Field(name, (Vector2) value);

                case var _ when type == typeof(Vector3):
                    return EditorGUILayout.Vector3Field(name, (Vector3) value);

                case var _ when type == typeof(Vector4):
                    return EditorGUILayout.Vector4Field(name, (Vector4) value);

                case var _ when type == typeof(Color):
                    return EditorGUILayout.ColorField(name, (Color) value);

                case var _ when type == typeof(Rect):
                    return EditorGUILayout.RectField(name, (Rect) value);

                case var _ when type == typeof(Bounds):
                    return EditorGUILayout.BoundsField(name, (Bounds) value);

                case var _ when type == typeof(AnimationCurve):
                    return EditorGUILayout.CurveField(name, (AnimationCurve) value);

                case var _ when type.IsEnum:
                    return EditorGUILayout.EnumPopup(name, (Enum) value);

                case var _ when typeof(UnityEngine.Object).IsAssignableFrom(type):
                    return EditorGUILayout.ObjectField(name, (UnityEngine.Object) value, type, false);

                // Collections
                case var _ when type.IsArray || (type.IsGenericType && (
                    type.GetGenericTypeDefinition() == typeof(List<>) ||
                    type.GetGenericTypeDefinition() == typeof(Dictionary<,>))):
                    return DrawCollectionProperty(name, type, value, fullPath);

                // Custom classes (non-primitive types)
                case var _ when !type.IsPrimitive && !type.IsEnum && type != typeof(string) && value != null:
                    return DrawCustomClassProperty(name, type, value, fullPath);

                default:
                    // For types we can't edit, at least display the value as string
                    EditorGUILayout.LabelField(name, value != null ? value.ToString() : "null");
                    return value;
            }
        }

        // Separated method for drawing collections (arrays, lists, dictionaries)
        private object DrawCollectionProperty(string name, Type type, object value, string fullPath)
        {
            if (!foldoutStates.ContainsKey(fullPath))
                foldoutStates[fullPath] = false;

            EditorGUILayout.BeginHorizontal();

            // Foldout header
            foldoutStates[fullPath] = EditorGUILayout.Foldout(foldoutStates[fullPath], name, true);

            // Show collection details
            int count = 0;
            bool isNull = value == null;

            if (!isNull)
            {
                if (type.IsArray) count = ((Array) value).Length;
                else if (type.GetGenericTypeDefinition() == typeof(List<>))
                    count = (int) type.GetProperty("Count").GetValue(value);
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    count = (int) type.GetProperty("Count").GetValue(value);

                EditorGUILayout.LabelField($"Size: {count}", GUILayout.Width(80));
            }
            else
            {
                EditorGUILayout.LabelField("null", GUILayout.Width(80));
            }

            // Add buttons for collection manipulation
            if (!isNull)
            {
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    AddElementToCollection(type, value);
                }

                if (count > 0 && GUILayout.Button("-", GUILayout.Width(25)))
                {
                    RemoveElementFromCollection(type, value);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Draw collection contents if expanded and not null
            if (foldoutStates[fullPath] && !isNull)
            {
                EditorGUI.indentLevel++;

                if (type.IsArray)
                {
                    DrawArrayElements(type, (Array) value, fullPath);
                }
                else if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    DrawListElements(type, value, fullPath);
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    DrawDictionaryElements(type, value, fullPath);
                }

                EditorGUI.indentLevel--;
            }

            return value;
        }

        // Draw array elements with reorderable list functionality
        private void DrawArrayElements(Type type, Array array, string fullPath)
        {
            Type elementType = type.GetElementType();

            // Create a reorderable list-like interface
            for (int i = 0; i < array.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Element index
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(EditorGUI.indentLevel * 15 + 30));

                // Element editor
                object element = array.GetValue(i);
                EditorGUI.BeginChangeCheck();

                // Get appropriate width for the element field
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                object newElement = DrawInlineElement(elementType, element, $"{fullPath}[{i}]");
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    array.SetValue(newElement, i);
                }

                // Move up/down buttons
                if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
                {
                    SwapArrayElements(array, i, i - 1);
                }

                if (GUILayout.Button("↓", GUILayout.Width(25)) && i < array.Length - 1)
                {
                    SwapArrayElements(array, i, i + 1);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // Draw list elements with reorderable list functionality
        private void DrawListElements(Type type, object list, string fullPath)
        {
            Type elementType = type.GetGenericArguments()[0];
            int count = (int) type.GetProperty("Count").GetValue(list);
            var getItemMethod = type.GetMethod("get_Item");
            var setItemMethod = type.GetMethod("set_Item");

            // Create a reorderable list-like interface
            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Element index
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(EditorGUI.indentLevel * 15 + 30));

                // Element editor
                object element = getItemMethod.Invoke(list, new object[] {i});
                EditorGUI.BeginChangeCheck();

                // Get appropriate width for the element field
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                object newElement = DrawInlineElement(elementType, element, $"{fullPath}[{i}]");
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    setItemMethod.Invoke(list, new object[] {i, newElement});
                }

                // Move up/down buttons
                if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
                {
                    SwapListElements(list, i, i - 1);
                }

                if (GUILayout.Button("↓", GUILayout.Width(25)) && i < count - 1)
                {
                    SwapListElements(list, i, i + 1);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDictionaryElements(Type type, object dictionary, string fullPath)
        {
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            var keysProperty = type.GetProperty("Keys");
            // Fix ambiguous matches by specifying parameter types
            var getItemMethod = type.GetMethod("get_Item", new Type[] {keyType});
            var setItemMethod = type.GetMethod("set_Item", new Type[] {keyType, valueType});
            var removeMethod = type.GetMethod("Remove", new Type[] {keyType});
            var containsKeyMethod = type.GetMethod("ContainsKey", new Type[] {keyType});
            var addMethod = type.GetMethod("Add", new Type[] {keyType, valueType});

            var keys = keysProperty.GetValue(dictionary, null);
            var keysList = new List<object>();

            // Convert keys to a list for easier manipulation
            var enumerator =
                (System.Collections.IEnumerator) keys.GetType().GetMethod("GetEnumerator").Invoke(keys, null);
            while (enumerator.MoveNext())
            {
                keysList.Add(enumerator.Current);
            }

            // Create headers for dictionary display
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Draw key-value pairs
            for (int i = 0; i < keysList.Count; i++)
            {
                var key = keysList[i];
                object dictValue = getItemMethod.Invoke(dictionary, new object[] {key});

                EditorGUILayout.BeginHorizontal();

                // Display the key (read-only for now, as changing keys would be complicated)
                EditorGUI.BeginDisabledGroup(true);
                DrawInlineElement(keyType, key, $"{fullPath}.keys[{i}]", GUILayout.Width(150));
                EditorGUI.EndDisabledGroup();

                // Edit the value
                EditorGUI.BeginChangeCheck();
                object newValue = DrawInlineElement(valueType, dictValue, $"{fullPath}[{key}]");

                if (EditorGUI.EndChangeCheck())
                {
                    setItemMethod.Invoke(dictionary, new object[] {key, newValue});
                }

                // Remove button
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    removeMethod.Invoke(dictionary, new object[] {key});
                    // Break the loop since we've modified the collection
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Add new key-value pair section
            if (!dictionaryNewKeyValues.ContainsKey(fullPath))
            {
                dictionaryNewKeyValues[fullPath] = new KeyValuePair<object, object>(
                    CreateDefaultValue(keyType),
                    CreateDefaultValue(valueType)
                );
            }

            var kvp = dictionaryNewKeyValues[fullPath];

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // New key editor
            EditorGUI.BeginChangeCheck();
            object newKey = DrawInlineElement(keyType, kvp.Key, $"{fullPath}.newKey", GUILayout.Width(150));

            if (EditorGUI.EndChangeCheck())
            {
                dictionaryNewKeyValues[fullPath] = new KeyValuePair<object, object>(newKey, kvp.Value);
            }

            // New value editor
            EditorGUI.BeginChangeCheck();
            object newDictValue = DrawInlineElement(valueType, kvp.Value, $"{fullPath}.newValue");

            if (EditorGUI.EndChangeCheck())
            {
                dictionaryNewKeyValues[fullPath] = new KeyValuePair<object, object>(kvp.Key, newDictValue);
            }

            // Add button
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                try
                {
                    // Check if key already exists
                    bool keyExists = (bool) containsKeyMethod.Invoke(dictionary, new object[] {newKey});

                    if (!keyExists)
                    {
                        // Add the new key-value pair
                        addMethod.Invoke(dictionary, new object[] {newKey, newDictValue});

                        // Reset the input fields with new default values
                        dictionaryNewKeyValues[fullPath] = new KeyValuePair<object, object>(
                            CreateDefaultValue(keyType),
                            CreateDefaultValue(valueType)
                        );
                    }
                    else
                    {
                        Debug.LogWarning($"Key already exists in dictionary: {newKey}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error adding dictionary entry: {e.Message}");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Draw custom class properties
        private object DrawCustomClassProperty(string name, Type type, object value, string fullPath)
        {
            if (!foldoutStates.ContainsKey(fullPath))
                foldoutStates[fullPath] = false;

            foldoutStates[fullPath] = EditorGUILayout.Foldout(foldoutStates[fullPath], name, true);

            if (foldoutStates[fullPath])
            {
                EditorGUI.indentLevel++;

                // Get instance fields
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    object fieldValue = field.GetValue(value);
                    EditorGUI.BeginChangeCheck();
                    object newFieldValue = DrawPropertyEditor(field.Name, field.FieldType, fieldValue, fullPath);

                    if (EditorGUI.EndChangeCheck())
                    {
                        field.SetValue(value, newFieldValue);
                    }
                }

                // Get instance properties that have both getter and setter
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

                foreach (var property in properties)
                {
                    object propertyValue = property.GetValue(value);
                    EditorGUI.BeginChangeCheck();
                    object newPropertyValue = DrawPropertyEditor(property.Name, property.PropertyType,
                        propertyValue, fullPath);

                    if (EditorGUI.EndChangeCheck())
                    {
                        try
                        {
                            property.SetValue(value, newPropertyValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error setting property {property.Name}: {e.Message}");
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            return value;
        }

        // Helper method for inline element rendering (simpler version for array/list elements)
        private object DrawInlineElement(Type type, object value, string path, params GUILayoutOption[] options)
        {
            // For simple types, draw them inline
            if (type == typeof(int))
            {
                return EditorGUILayout.IntField((int) value, options);
            }
            else if (type == typeof(float))
            {
                return EditorGUILayout.FloatField((float) value, options);
            }
            else if (type == typeof(bool))
            {
                return EditorGUILayout.Toggle((bool) value, options);
            }
            else if (type == typeof(string))
            {
                return EditorGUILayout.TextField((string) value, options);
            }
            else if (type == typeof(Vector2))
            {
                return EditorGUILayout.Vector2Field("", (Vector2) value, options);
            }
            else if (type == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field("", (Vector3) value, options);
            }
            else if (type == typeof(Color))
            {
                return EditorGUILayout.ColorField((Color) value, options);
            }
            else if (type.IsEnum)
            {
                return EditorGUILayout.EnumPopup((Enum) value, options);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return EditorGUILayout.ObjectField((UnityEngine.Object) value, type, false, options);
            }
            else
            {
                // For complex types, just display a button that will expand the object when clicked
                if (GUILayout.Button(value != null ? value.ToString() : "null", EditorStyles.miniButton, options))
                {
                    // Toggle the foldout state of this object in its parent
                    if (foldoutStates.ContainsKey(path))
                        foldoutStates[path] = !foldoutStates[path];
                    else
                        foldoutStates[path] = true;
                }

                return value;
            }
        }

        // Helper methods for collection manipulation
        private void AddElementToCollection(Type type, object collection)
        {
            if (type.IsArray)
            {
                // For arrays, we need to create a new array with one more element
                Array array = (Array) collection;
                Type elementType = type.GetElementType();
                Array newArray = Array.CreateInstance(elementType, array.Length + 1);
                Array.Copy(array, newArray, array.Length);

                // Set the last element to a default value
                newArray.SetValue(CreateDefaultValue(elementType), array.Length);

                // We need to update the reference in the parent object
                // This is complex and would require additional code and context
            }
            else if (type.GetGenericTypeDefinition() == typeof(List<>))
            {
                // For lists, we can just add a new element
                Type elementType = type.GetGenericArguments()[0];
                object defaultValue = CreateDefaultValue(elementType);
                type.GetMethod("Add").Invoke(collection, new object[] {defaultValue});
            }
            else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Adding to dictionaries is handled in the DrawDictionaryElements method
            }
        }

        private void RemoveElementFromCollection(Type type, object collection)
        {
            if (type.IsArray)
            {
                // For arrays, we need to create a new array with one less element
                Array array = (Array) collection;
                if (array.Length > 0)
                {
                    Type elementType = type.GetElementType();
                    Array newArray = Array.CreateInstance(elementType, array.Length - 1);
                    Array.Copy(array, newArray, array.Length - 1);

                    // We need to update the reference in the parent object
                    // This is complex and would require additional code and context
                }
            }
            else if (type.GetGenericTypeDefinition() == typeof(List<>))
            {
                // For lists, remove the last element
                int count = (int) type.GetProperty("Count").GetValue(collection);
                if (count > 0)
                {
                    type.GetMethod("RemoveAt").Invoke(collection, new object[] {count - 1});
                }
            }
            else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Removing from dictionaries is handled in the DrawDictionaryElements method
            }
        }

        // Helper methods for reordering elements
        private void SwapArrayElements(Array array, int index1, int index2)
        {
            object temp = array.GetValue(index1);
            array.SetValue(array.GetValue(index2), index1);
            array.SetValue(temp, index2);
        }

        private void SwapListElements(object list, int index1, int index2)
        {
            Type type = list.GetType();
            var getItemMethod = type.GetMethod("get_Item");
            var setItemMethod = type.GetMethod("set_Item");

            object temp = getItemMethod.Invoke(list, new object[] {index1});
            setItemMethod.Invoke(list, new object[] {index1, getItemMethod.Invoke(list, new object[] {index2})});
            setItemMethod.Invoke(list, new object[] {index2, temp});
        }

        // Helper to create default values for types
        private object CreateDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else if (type == typeof(string))
            {
                return "";
            }
            else if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), 0);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return Activator.CreateInstance(type);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return Activator.CreateInstance(type);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return null;
            }

            // Try to create an instance for other reference types
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private void OnStaticClassSelected(Type selectedType)
        {
            DataKeeperEditorPref.SelectedStaticClassPref.Value = selectedType;
            Repaint();
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