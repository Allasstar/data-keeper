using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using DataKeeper.Attributes;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
    public class SearchableEnumAttributeDrawer : PropertyDrawer
    {
        private static AdvancedDropdownState dropdownState = new AdvancedDropdownState();

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
                    // Open the dropdown with state
                    var dropdown = new SearchableEnumDropdown(dropdownState, property.enumDisplayNames,
                        selectedIndex =>
                        {
                            property.enumValueIndex = selectedIndex;
                            property.serializedObject.ApplyModifiedProperties();
                        });

                    dropdown.Show(position);
                }
            }
            else
            {
                EditorGUI.LabelField(position, "SearchableEnum only works with enums.");
            }

            EditorGUI.EndProperty();
        }
    }
    
    public class SearchableEnumDropdown : AdvancedDropdown
    {
        private string[] enumNames;
        private Action<int> onSelected;

        public SearchableEnumDropdown(AdvancedDropdownState state, string[] enumNames, Action<int> onSelected)
            : base(state)
        {
            this.enumNames = enumNames;
            this.onSelected = onSelected;
            minimumSize = new Vector2(200, 200);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select an Option");

            for (int i = 0; i < enumNames.Length; i++)
            {
                var item = new AdvancedDropdownItem(enumNames[i]) { id = i };
                root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            onSelected?.Invoke(item.id);
        }
    }
}