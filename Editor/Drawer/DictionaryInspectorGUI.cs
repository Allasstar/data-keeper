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

            // Get all fields with ShowInInspector attribute
            var fields = target.GetType().GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public
            );

            bool foundDictionary = false;

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttributes(typeof(PreviewDictionaryAttribute), true);
                if (attr.Length == 0) continue;

                var value = field.GetValue(target);
                if (value == null) continue;

                // Check if it's a dictionary
                var type = value.GetType();
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                    continue;

                foundDictionary = true;

                // Create unique key for foldout state
                string foldoutKey = $"{target.GetInstanceID()}_{field.Name}";
                if (!foldoutStates.ContainsKey(foldoutKey))
                    foldoutStates[foldoutKey] = false;

                // Draw the dictionary with foldout
                EditorGUILayout.Space(10);

                var dict = value as System.Collections.IDictionary;
                string label = $"{field.Name} [{dict.Count}]";

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

            // Clear old foldout states for destroyed objects
            if (foundDictionary)
            {
                CleanupFoldoutStates();
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

