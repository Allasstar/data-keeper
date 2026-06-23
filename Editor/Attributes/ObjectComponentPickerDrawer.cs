using System;
using System.Collections.Generic;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Attributes
{
    /// <summary>
    /// Draws an Object field with a small dropdown button that lets you re-pick the stored
    /// reference among the dropped GameObject and its components.
    /// </summary>
    [CustomPropertyDrawer(typeof(ObjectComponentPickerAttribute))]
    public class ObjectComponentPickerDrawer : PropertyDrawer
    {
        private const float BUTTON_W = 20f;
        private const float SPACING = 2f;

        private static readonly GUIContent s_PickContent = new GUIContent(string.Empty, "Pick reference (GameObject / component)");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "[ObjectComponentPicker] only works on Object fields.");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            GameObject go = ResolveGameObject(property.objectReferenceValue);
            bool showButton = go != null && !property.hasMultipleDifferentValues;

            Rect fieldRect = position;
            if (showButton)
                fieldRect.width -= BUTTON_W + SPACING;

            EditorGUI.PropertyField(fieldRect, property, label, true);

            if (showButton)
            {
                Rect btnRect = new Rect(position.xMax - BUTTON_W, position.y, BUTTON_W, EditorGUIUtility.singleLineHeight);
                Type filter = ((ObjectComponentPickerAttribute)attribute).FilterType;

                if (EditorGUI.DropdownButton(btnRect, s_PickContent, FocusType.Keyboard))
                    ShowMenu(property, go, filter);
            }

            EditorGUI.EndProperty();
        }

        private static GameObject ResolveGameObject(Object value)
        {
            return value switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => null
            };
        }

        private static void ShowMenu(SerializedProperty property, GameObject go, Type filter)
        {
            var menu = new GenericMenu();
            Object current = property.objectReferenceValue;

            // GameObject option (only if it passes the filter, or there is no filter)
            if (Matches(go, filter))
                AddItem(menu, "GameObject", go, current, property);

            var components = new List<Component>();
            go.GetComponents(components);

            var typeCounts = new Dictionary<Type, int>();
            foreach (var component in components)
            {
                if (component == null || !Matches(component, filter))
                    continue;

                Type type = component.GetType();
                typeCounts.TryGetValue(type, out int seen);

                // Disambiguate duplicate component types with an index.
                int total = components.FindAll(c => c != null && c.GetType() == type).Count;
                string label = total > 1 ? $"{type.Name} ({seen})" : type.Name;
                typeCounts[type] = seen + 1;

                AddItem(menu, label, component, current, property);
            }

            menu.ShowAsContext();
        }

        private static bool Matches(Object obj, Type filter)
        {
            return filter == null || (obj != null && filter.IsInstanceOfType(obj));
        }

        private static void AddItem(GenericMenu menu, string label, Object target, Object current, SerializedProperty property)
        {
            menu.AddItem(new GUIContent(label), current == target, () =>
            {
                property.objectReferenceValue = target;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
    }
}
