using System;
using UnityEngine;
using UnityEditor;
using DataKeeper.FSM;

namespace DataKeeper.Editor.FSM
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(FSMHistory<>))]
    public class FSMHistoryPropertyDrawer : PropertyDrawer
    {
        private Color _backgroundLatestColor = new Color(0.3f, 0.6f, 0.3f, 0.3f);
        private Color _backgroundOddColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        private Color _backgroundEvenColor = new Color(0f, 0f, 0f, 0f);
        
        private const float ROW_HEIGHT = 18f;
        private const float HEADER_HEIGHT = 20f;
        private const float SPACING = 2f;
        private const float TIME_WIDTH = 80f;
        private const float STATE_WIDTH = 120f;
        private const int MAX_VISIBLE_ROWS = 10;
        
        private bool _isEvenRow = true;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Get history size property
            var historySizeProp = property.FindPropertyRelative("_historySize");
            if (historySizeProp != null)
            {
                var sizeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(sizeRect, historySizeProp);
            }
            
            // Start drawing the table below the label
            var currentY = position.y + EditorGUIUtility.singleLineHeight + SPACING;
            
            // Draw table header
            DrawTableHeader(position.x, currentY, position.width);
            currentY += HEADER_HEIGHT + SPACING;
            
            // Draw history entries
            DrawHistoryEntries(position.x, currentY, position.width, property);
            
            EditorGUI.EndProperty();
        }
        
        private void DrawTableHeader(float x, float y, float width)
        {
            var headerRect = new Rect(x, y, width, HEADER_HEIGHT);
            EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            
            // Column headers
            var timeRect = new Rect(x + 5, y + 2, TIME_WIDTH, HEADER_HEIGHT - 4);
            var fromRect = new Rect(x + TIME_WIDTH + 10, y + 2, STATE_WIDTH, HEADER_HEIGHT - 4);
            var toRect = new Rect(x + TIME_WIDTH + STATE_WIDTH + 15, y + 2, STATE_WIDTH, HEADER_HEIGHT - 4);
            
            EditorGUI.LabelField(timeRect, "Time", EditorStyles.boldLabel);
            EditorGUI.LabelField(fromRect, "From", EditorStyles.boldLabel);
            EditorGUI.LabelField(toRect, "To", EditorStyles.boldLabel);
        }
        
        private void DrawHistoryEntries(float x, float y, float width, SerializedProperty property)
        {
            try
            {
                // Get the history object directly from the SerializedProperty
                var historyObject = GetTargetObjectOfProperty(property);
                
                if (historyObject != null)
                {
                    // Use reflection to get the history
                    var getHistoryMethod = historyObject.GetType().GetMethod("GetHistory");
                    if (getHistoryMethod != null)
                    {
                        var history = getHistoryMethod.Invoke(historyObject, null) as Array;
                        if (history != null && history.Length > 0)
                        {
                            // Display entries from newest to oldest
                            int visibleRows = Mathf.Min(history.Length, MAX_VISIBLE_ROWS);
                            for (int i = 0; i < visibleRows; i++)
                            {
                                int historyIndex = history.Length - 1 - i; // Start from most recent
                                var record = history.GetValue(historyIndex);
                                DrawHistoryRow(x, y + (i * (ROW_HEIGHT + SPACING)), width, record, i == 0);
                            }
                            
                            // Show "..." if there are more entries
                            if (history.Length > MAX_VISIBLE_ROWS)
                            {
                                var moreRect = new Rect(x + 5, y + (visibleRows * (ROW_HEIGHT + SPACING)), width - 10, ROW_HEIGHT);
                                EditorGUI.LabelField(moreRect, $"... and {history.Length - MAX_VISIBLE_ROWS} more entries", EditorStyles.miniLabel);
                            }
                        }
                        else
                        {
                            // No history entries
                            var noDataRect = new Rect(x + 5, y, width - 10, ROW_HEIGHT);
                            EditorGUI.LabelField(noDataRect, "No history entries", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    else
                    {
                        var errorRect = new Rect(x + 5, y, width - 10, ROW_HEIGHT);
                        EditorGUI.LabelField(errorRect, "GetHistory method not found", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    var errorRect = new Rect(x + 5, y, width - 10, ROW_HEIGHT);
                    EditorGUI.LabelField(errorRect, "History object is null", EditorStyles.miniLabel);
                }
            }
            catch (Exception e)
            {
                // Error handling
                var errorRect = new Rect(x + 5, y, width - 10, ROW_HEIGHT);
                EditorGUI.LabelField(errorRect, $"Error: {e.Message}", EditorStyles.miniLabel);
            }
        }
        
        private void DrawHistoryRow(float x, float y, float width, object record, bool isLatest)
        {
            var rowRect = new Rect(x, y, width, ROW_HEIGHT);
            
            // Alternate row colors, with latest entry highlighted
            Color backgroundColor;
            if (isLatest)
            {
                backgroundColor = _backgroundLatestColor;
            }
            else
            {
                backgroundColor = _isEvenRow ? _backgroundEvenColor : _backgroundOddColor;
                _isEvenRow = !_isEvenRow;
            }
            
            EditorGUI.DrawRect(rowRect, backgroundColor);
            
            // Extract data from the record using reflection
            var recordType = record.GetType();
            var timeStampProp = recordType.GetProperty("TimeStamp");
            var fromStateProp = recordType.GetProperty("FromState");
            var toStateProp = recordType.GetProperty("ToState");
            
            if (timeStampProp != null && fromStateProp != null && toStateProp != null)
            {
                var timeStamp = (float)timeStampProp.GetValue(record);
                var fromState = (string)fromStateProp.GetValue(record);
                var toState = (string)toStateProp.GetValue(record);
                
                // Column rects
                var timeRect = new Rect(x + 5, y + 1, TIME_WIDTH, ROW_HEIGHT - 2);
                var fromRect = new Rect(x + TIME_WIDTH + 10, y + 1, STATE_WIDTH, ROW_HEIGHT - 2);
                var toRect = new Rect(x + TIME_WIDTH + STATE_WIDTH + 15, y + 1, STATE_WIDTH, ROW_HEIGHT - 2);
                
                // Draw the data
                EditorGUI.LabelField(timeRect, $"{timeStamp:F2}s", EditorStyles.label);
                EditorGUI.LabelField(fromRect, fromState ?? "null", EditorStyles.label);
                EditorGUI.LabelField(toRect, toState ?? "null", EditorStyles.label);
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + SPACING; // Main label
            height += HEADER_HEIGHT + SPACING; // Header
            
            try
            {
                // Try to get history count to calculate height
                var historyObject = GetTargetObjectOfProperty(property);
                
                if (historyObject != null)
                {
                    var getHistoryMethod = historyObject.GetType().GetMethod("GetHistory");
                    if (getHistoryMethod != null)
                    {
                        var history = getHistoryMethod.Invoke(historyObject, null) as Array;
                        if (history != null && history.Length > 0)
                        {
                            int visibleRows = Mathf.Min(history.Length, MAX_VISIBLE_ROWS);
                            height += visibleRows * (ROW_HEIGHT + SPACING);
                            
                            // Add space for "..." text if there are more entries
                            if (history.Length > MAX_VISIBLE_ROWS)
                            {
                                height += ROW_HEIGHT;
                            }
                        }
                        else
                        {
                            height += ROW_HEIGHT; // "No entries" text
                        }
                    }
                    else
                    {
                        height += ROW_HEIGHT; // Space for error message
                    }
                }
                else
                {
                    height += ROW_HEIGHT; // Space for "No entries"
                }
            }
            catch
            {
                height += ROW_HEIGHT; // Space for error message
            }
            
            return height;
        }
        
        /// <summary>
        /// Gets the actual object that the SerializedProperty represents
        /// </summary>
        private object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }
        
        private object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
                
            var type = source.GetType();
            
            while (type != null)
            {
                var field = type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    return field.GetValue(source);
                
                var property = type.GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (property != null)
                    return property.GetValue(source, null);
                
                type = type.BaseType;
            }
            return null;
        }
        
        private object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            
            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }
            return enumerator.Current;
        }
    }
#endif
}