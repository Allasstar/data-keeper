using System;
using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.Editor.Windows;
using DataKeeper.Utility;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(CSVTableAttribute))]
    public class CSVTablePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            CSVTableAttribute csvTableAttribute = attribute as CSVTableAttribute;
            
            // Calculate rects
            float buttonWidth = 68f;
            float padding = 3f;
            float textAreaHeight = position.height - EditorGUIUtility.singleLineHeight - padding;
            
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect toCSVButtonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect fromCSVButtonRect = new Rect(toCSVButtonRect.x + buttonWidth + padding, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect editButtonRect = new Rect(fromCSVButtonRect.x + buttonWidth + padding, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            Rect textAreaRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + padding, position.width, textAreaHeight);

            // Draw components
            EditorGUI.LabelField(labelRect, label);

            if (GUI.Button(toCSVButtonRect, "To CSV"))
            {
                SerializedObject targetObject = property.serializedObject;
                object target = targetObject.targetObject;
                
                // Get the list using reflection
                FieldInfo listField = target.GetType().GetField(csvTableAttribute.ListPropertyName);
                if (listField != null)
                {
                    object listValue = listField.GetValue(target);
                    
                    // Get the generic type of the list
                    Type listElementType = listField.FieldType.GetGenericArguments()[0];
                    
                    // Get the generic ListToCSV method and make it specific to our element type
                    MethodInfo listToCsvMethod = typeof(CSVUtility).GetMethod("ListToCSV").MakeGenericMethod(listElementType);
                    
                    // Invoke the method with the list value
                    string csvData = (string)listToCsvMethod.Invoke(null, new object[] { listValue });
                    
                    property.stringValue = csvData;
                    targetObject.ApplyModifiedProperties();
                }
            }

            if (GUI.Button(fromCSVButtonRect, "From CSV"))
            {
                SerializedObject targetObject = property.serializedObject;
                object target = targetObject.targetObject;
                
                // Get the list using reflection
                FieldInfo listField = target.GetType().GetField(csvTableAttribute.ListPropertyName);
                if (listField != null)
                {
                    Type listElementType = listField.FieldType.GetGenericArguments()[0];
                    MethodInfo csvToListMethod = typeof(CSVUtility).GetMethod("CSVToList").MakeGenericMethod(listElementType);
                    
                    object listValue = csvToListMethod.Invoke(null, new object[] { property.stringValue });
                    listField.SetValue(target, listValue);
                    
                    targetObject.Update();
                }
            }

            if (GUI.Button(editButtonRect, "Edit"))
            {
                // Open CSV Editor window
                CSVEditorWindow.OpenWindow(property.stringValue, csvData => {
                    property.stringValue = csvData;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            // Draw the text area
            property.stringValue = EditorGUI.TextArea(textAreaRect, property.stringValue);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 6 + 10f; // Text area height + button height + padding
        }
    }
}
