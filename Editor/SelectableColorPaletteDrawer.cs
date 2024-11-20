using UnityEngine;
using UnityEditor;

// [CustomPropertyDrawer(typeof(SelectableColorPalette), true)]
public class SelectableColorPaletteDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw the object field for the ScriptableObject
        Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(objectFieldRect, property, label);
        
        if (property.objectReferenceValue != null)
        {
            // Get the ScriptableObject
            ScriptableObject scriptableObject = (ScriptableObject)property.objectReferenceValue;
            SerializedObject serializedObject = new SerializedObject(scriptableObject);
            SerializedProperty prop = serializedObject.GetIterator();

            // Calculate the position for the fields
            Rect fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

            prop.NextVisible(true); // Skip script field
            while (prop.NextVisible(false))
            {
                // Draw each property field
                EditorGUI.PropertyField(fieldRect, prop, true);
                fieldRect.y += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            // Apply any changes to the SerializedObject
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight;

        if (property.objectReferenceValue != null)
        {
            ScriptableObject scriptableObject = (ScriptableObject)property.objectReferenceValue;
            SerializedObject serializedObject = new SerializedObject(scriptableObject);
            SerializedProperty prop = serializedObject.GetIterator();

            prop.NextVisible(true); // Skip script field
            while (prop.NextVisible(false))
            {
                totalHeight += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        return totalHeight;
    }
}
