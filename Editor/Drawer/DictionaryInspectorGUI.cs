using System.Collections.Generic;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Drawer
{
    /// <summary>
    /// Static utility class for drawing dictionaries in the inspector
    /// </summary>
    public static class DictionaryInspectorGUI
    {
        private static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        /// <summary>
        /// Draws all dictionaries marked with [ShowInInspector] attribute for the given target object
        /// </summary>
        /// <param name="target">The object to inspect for dictionaries</param>
        public static void DrawDictionaries(Object target)
        {
            // Only show dictionaries in Play mode
            if (!Application.isPlaying)
            {
                return;
            }

            bool foundDictionary = false;
            var visitedObjects = new HashSet<object>();
            
            // Recursively search for dictionaries
            SearchAndDrawDictionaries(target, target, "", ref foundDictionary, visitedObjects);

            // Clear old foldout states for destroyed objects
            if (foundDictionary)
            {
                CleanupFoldoutStates();
            }
        }

        /// <summary>
        /// Recursively searches for dictionaries in an object and its fields
        /// </summary>
        private static void SearchAndDrawDictionaries(Object rootTarget, object currentObject, string pathPrefix, ref bool foundDictionary, HashSet<object> visitedObjects)
        {
            if (currentObject == null) return;
            
            // Prevent infinite recursion
            if (visitedObjects.Contains(currentObject)) return;
            visitedObjects.Add(currentObject);

            var type = currentObject.GetType();
            
            // Skip Unity built-in types to avoid deep recursion
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine") && type != currentObject.GetType())
                return;

            var fields = type.GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public
            );

            foreach (var field in fields)
            {
                var value = field.GetValue(currentObject);
                if (value == null) continue;

                var fieldType = value.GetType();
                string fullPath = string.IsNullOrEmpty(pathPrefix) ? field.Name : $"{pathPrefix}.{field.Name}";

                // Check if this field has the ShowInInspector attribute
                var attr = field.GetCustomAttributes(typeof(PreviewDictionaryAttribute), true);
                
                if (attr.Length > 0)
                {
                    // Check if it's a dictionary
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        foundDictionary = true;
                        DrawDictionary(rootTarget, value as System.Collections.IDictionary, fullPath);
                    }
                }
                
                // Recursively search in custom class fields (not primitives or Unity types)
                if (!fieldType.IsPrimitive && 
                    !fieldType.IsEnum && 
                    fieldType != typeof(string) &&
                    !typeof(Object).IsAssignableFrom(fieldType))
                {
                    SearchAndDrawDictionaries(rootTarget, value, fullPath, ref foundDictionary, visitedObjects);
                }
            }
        }

        /// <summary>
        /// Draws a single dictionary
        /// </summary>
        private static void DrawDictionary(Object rootTarget, System.Collections.IDictionary dict, string fieldPath)
        {
            // Create unique key for foldout state
            string foldoutKey = $"{rootTarget.GetInstanceID()}_{fieldPath}";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = false;

            // Draw the dictionary with foldout
            EditorGUILayout.Space(10);

            string label = $"{fieldPath} [{dict.Count}]";

            foldoutStates[foldoutKey] = EditorGUILayout.Foldout(
                foldoutStates[foldoutKey],
                label,
                true,
                EditorStyles.foldoutHeader
            );

            if (foldoutStates[foldoutKey])
            {
                EditorGUI.indentLevel++;

                if (dict.Count == 0)
                {
                    EditorGUILayout.LabelField("Empty", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true); // Make read-only

                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Display key
                        string keyStr = entry.Key != null ? entry.Key.ToString() : "null";
                        EditorGUILayout.LabelField(keyStr, GUILayout.Width(150));

                        // Display value based on type
                        DrawValue(entry.Value);

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.EndDisabledGroup();
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draws a value field based on its type
        /// </summary>
        private static void DrawValue(object value)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField("null");
                return;
            }

            var type = value.GetType();

            if (type == typeof(int))
                EditorGUILayout.IntField((int)value);
            else if (type == typeof(float))
                EditorGUILayout.FloatField((float)value);
            else if (type == typeof(string))
                EditorGUILayout.TextField((string)value);
            else if (type == typeof(bool))
                EditorGUILayout.Toggle((bool)value);
            else if (type == typeof(Vector2))
                EditorGUILayout.Vector2Field("", (Vector2)value);
            else if (type == typeof(Vector3))
                EditorGUILayout.Vector3Field("", (Vector3)value);
            else if (type == typeof(Vector4))
                EditorGUILayout.Vector4Field("", (Vector4)value);
            else if (type == typeof(Color))
                EditorGUILayout.ColorField((Color)value);
            else if (typeof(Object).IsAssignableFrom(type))
                EditorGUILayout.ObjectField((Object)value, type, true);
            else
                EditorGUILayout.LabelField(value.ToString());
        }

        /// <summary>
        /// Cleans up foldout states for objects that no longer exist
        /// </summary>
        private static void CleanupFoldoutStates()
        {
            // This is called occasionally to prevent memory leaks
            // In a real implementation, you might want to make this more sophisticated
            if (foldoutStates.Count > 100)
            {
                foldoutStates.Clear();
            }
        }
    }
}

