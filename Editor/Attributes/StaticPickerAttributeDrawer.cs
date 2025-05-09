using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using DataKeeper.Attributes;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(StaticPickerAttribute))]
    public class StaticPickerAttributeDrawer : PropertyDrawer
    {
        private static AdvancedDropdownState dropdownState = new AdvancedDropdownState();
        private const float _buttonWidth = 18f;
        private const float _space = 3f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            var propertyRect = position;
            propertyRect.width -= (_buttonWidth + _space);

            var buttonRect = position;
            buttonRect.x = propertyRect.xMax + _space;
            buttonRect.width = _buttonWidth;
            buttonRect.height = _buttonWidth;

            // Draw the default property field
            EditorGUI.PropertyField(propertyRect, property, label, true);

            // Draw the dropdown button
            if (GUI.Button(buttonRect, "⌵")) // ⌵ ⌄ ≡ ☰ ⋮ ⋯ … 
            {
                var fieldType = fieldInfo.FieldType;
                ShowDropdown(buttonRect, fieldType, property);
            }

            EditorGUI.EndProperty();
        }

        private void ShowDropdown(Rect buttonRect, Type targetType, SerializedProperty property)
        {
            // Get all static members (fields and properties)
            var staticMembers = new List<StaticMemberInfo>();
            
            // Get static fields
            var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.FieldType == targetType);
            foreach (var field in fields)
            {
                staticMembers.Add(new StaticMemberInfo(field.Name, () => field.GetValue(null)));
            }

            // Get static properties
            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(p => p.PropertyType == targetType && p.GetGetMethod() != null);
            foreach (var prop in properties)
            {
                staticMembers.Add(new StaticMemberInfo(prop.Name, () => prop.GetValue(null)));
            }

            // Create and show dropdown
            var dropdown = new StaticMemberDropdown(dropdownState, staticMembers, selectedValue =>
            {
                if (selectedValue != null)
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "Set Static Member");
                    
                    fieldInfo.SetValue(property.serializedObject.targetObject, selectedValue);
                    property.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            });

            dropdown.Show(buttonRect);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    internal class StaticMemberInfo
    {
        public string Name { get; }
        public Func<object> GetValue { get; }

        public StaticMemberInfo(string name, Func<object> getValue)
        {
            Name = name;
            GetValue = getValue;
        }
    }

    internal class StaticMemberDropdown : AdvancedDropdown
    {
        private readonly List<StaticMemberInfo> staticMembers;
        private readonly Action<object> onSelected;

        public StaticMemberDropdown(AdvancedDropdownState state, List<StaticMemberInfo> staticMembers, Action<object> onSelected)
            : base(state)
        {
            this.staticMembers = staticMembers;
            this.onSelected = onSelected;
            minimumSize = new Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Static Members");

            foreach (var member in staticMembers)
            {
                var item = new AdvancedDropdownItem(member.Name)
                {
                    id = staticMembers.IndexOf(member)
                };
                root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item.id >= 0 && item.id < staticMembers.Count)
            {
                var selectedMember = staticMembers[item.id];
                var value = selectedMember.GetValue();
                onSelected?.Invoke(value);
            }
        }
    }
}