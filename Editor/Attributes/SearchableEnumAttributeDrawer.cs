using System;
using System.Linq;
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
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.LabelField(position, "SearchableEnum only works with enums.");
                EditorGUI.EndProperty();
                return;
            }

            var enumType = fieldInfo.FieldType;
            
            // Workaround if enum is inside an array or list
            if (enumType.IsArray) enumType = enumType.GetElementType();

            var enumValues = Enum.GetValues(enumType).Cast<object>().ToArray();
            var enumNames = Enum.GetNames(enumType);
            var underlyingValues = enumValues.Select(v => Convert.ToInt32(v)).ToArray();

            var attr = (SearchableEnumAttribute)attribute;

            int currentValue = property.intValue; // actual underlying int
            int currentIndex = Array.IndexOf(underlyingValues, currentValue);
            if (currentIndex < 0) currentIndex = 0; // failsafe for invalid state

            string buttonLabel = SearchableEnumDropdown.GenerateEnumLabel(attr.ShowValue, currentIndex, enumNames, underlyingValues);

            if (GUI.Button(position, buttonLabel, EditorStyles.popup))
            {
                var dropdown = new SearchableEnumDropdown(
                    dropdownState,
                    enumNames,
                    underlyingValues,
                    attr.ShowValue,
                    selectedValue =>
                    {
                        property.intValue = selectedValue;
                        property.serializedObject.ApplyModifiedProperties();
                    });

                dropdown.Show(position);
            }

            EditorGUI.EndProperty();
        }
    }

    public class SearchableEnumDropdown : AdvancedDropdown
    {
        private readonly string[] names;
        private readonly int[] values;
        private readonly bool showValue;
        private readonly Action<int> onSelected;

        public SearchableEnumDropdown(
            AdvancedDropdownState state,
            string[] names,
            int[] values,
            bool showValue,
            Action<int> onSelected)
            : base(state)
        {
            this.names = names;
            this.values = values;
            this.showValue = showValue;
            this.onSelected = onSelected;
            minimumSize = new Vector2(240, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select an Option");

            for (int i = 0; i < names.Length; i++)
            {
                string display = GenerateEnumLabel(showValue, i, names, values);

                var item = new AdvancedDropdownItem(display) { id = values[i] };
                root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            onSelected?.Invoke(item.id);
        }
        
        public static string GenerateEnumLabel(bool showIndex, int currentIndex, string[] enumNames,  int[] enumValues)
        {
            return showIndex
                ? $"{enumNames[currentIndex]} = {enumValues[currentIndex]}"
                : enumNames[currentIndex];
        }
    }
}
