using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
public class SearchableEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        // Ensure the property is an enum
        if (property.propertyType == SerializedPropertyType.Enum)
        {
            if (GUI.Button(position, property.enumDisplayNames[property.enumValueIndex], EditorStyles.popup))
            {
                // Open the dropdown window
                SearchableEnumDropdown.Show(position, property.enumDisplayNames, property.enumValueIndex, selectedIndex =>
                {
                    property.enumValueIndex = selectedIndex;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
        }
        else
        {
            EditorGUI.LabelField(position, "SearchableEnum only works with enums.");
        }

        EditorGUI.EndProperty();
    }
}

public class SearchableEnumDropdown : EditorWindow
{
    private string[] enumNames;
    private Action<int> onSelected;
    private string searchText = "";
    private Vector2 scrollPos;

    public static void Show(Rect buttonRect, string[] enumNames, int selectedIndex, Action<int> onSelected)
    {
        // Create the window
        var window = ScriptableObject.CreateInstance<SearchableEnumDropdown>();
        window.enumNames = enumNames;
        window.onSelected = onSelected;
        window.searchText = "";
        
        // Adjust the position
        buttonRect.position = GUIUtility.GUIToScreenPoint(buttonRect.position);
        window.ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, 250));
    }

    private void OnGUI()
    {
        // Search box
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchText = EditorGUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSearchTextField"));
        EditorGUILayout.EndHorizontal();

        // Display filtered results
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < enumNames.Length; i++)
        {
            if (string.IsNullOrEmpty(searchText) || enumNames[i].ToLower().Contains(searchText.ToLower()))
            {
                if (GUILayout.Button(enumNames[i], EditorStyles.toolbarButton))
                {
                    onSelected?.Invoke(i);
                    Close();
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }
}