
using DataKeeper.Generic.Data;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Drawer
{
    [CustomPropertyDrawer(typeof(Optional<,>))]
    public class OptionalPropertyDrawer : PropertyDrawer
    {
        private const float _buttonWidth = 18f;
        private const float _space = 3f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("localValue");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var modeProperty = property.FindPropertyRelative("mode");
            var localValueProperty = property.FindPropertyRelative("localValue");
            var globalProviderProperty = property.FindPropertyRelative("globalProvider");
            
            var currentMode = (OptionalMode)modeProperty.enumValueIndex;

            // Calculate value field position (main content)
            Rect valuePosition = position;
            valuePosition.width -= _buttonWidth + _space;

            // Draw the appropriate value field based on mode
            switch (currentMode)
            {
                case OptionalMode.Disabled:
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(valuePosition, localValueProperty, label);
                    EditorGUI.EndDisabledGroup();
                    break;
                
                case OptionalMode.LocalValue:
                    EditorGUI.PropertyField(valuePosition, localValueProperty, label);
                    break;
                
                case OptionalMode.GlobalValue:
                    EditorGUI.PropertyField(valuePosition, globalProviderProperty, label);
                    break;
            }

            // Calculate and draw mode button
            Rect buttonPosition = position;
            buttonPosition.x += valuePosition.width + _space;
            buttonPosition.width = _buttonWidth;
            buttonPosition.height = _buttonWidth;

            string buttonLabel = currentMode switch
            {
                OptionalMode.Disabled => "D",
                OptionalMode.LocalValue => "L",
                OptionalMode.GlobalValue => "G",
                _ => "?"
            };
            
            if (GUI.Button(buttonPosition, new GUIContent(buttonLabel, "Change Optional Mode")))
            {
                GenericMenu menu = new GenericMenu();
                foreach (OptionalMode mode in System.Enum.GetValues(typeof(OptionalMode)))
                {
                    menu.AddItem(new GUIContent(mode.ToString()), currentMode == mode, () =>
                    {
                        modeProperty.enumValueIndex = (int)mode;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.DropDown(buttonPosition);
            }

            EditorGUI.EndProperty();
        }
    }
}