using DataKeeper.Generic.Data;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Drawer
{
    [CustomPropertyDrawer(typeof(Optional<,>))]
    public class OptionalPropertyDrawer : PropertyDrawer
    {
        private const float DROPDOWN_HEIGHT = 20f;
        private const float SPACING = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get serialized properties
            var modeProperty = property.FindPropertyRelative("mode");
            var localValueProperty = property.FindPropertyRelative("localValue");
            var globalProviderProperty = property.FindPropertyRelative("globalProvider");

            // Calculate rectangles
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, DROPDOWN_HEIGHT);
            var dropdownRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 
                position.width - EditorGUIUtility.labelWidth, DROPDOWN_HEIGHT);

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            // Draw mode dropdown
            var currentMode = (OptionalMode)modeProperty.enumValueIndex;
            var newMode = (OptionalMode)EditorGUI.EnumPopup(dropdownRect, currentMode);
        
            if (newMode != currentMode)
            {
                modeProperty.enumValueIndex = (int)newMode;
            }

            // Draw value field based on mode
            var valueRect = new Rect(position.x, position.y + DROPDOWN_HEIGHT + SPACING, 
                position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.indentLevel++;
        
            switch (newMode)
            {
                case OptionalMode.Disabled:
                    // Don't draw anything for disabled mode
                    break;
                
                case OptionalMode.LocalValue:
                    EditorGUI.PropertyField(valueRect, localValueProperty, new GUIContent("Local Value"));
                    break;
                
                case OptionalMode.GlobalValue:
                    EditorGUI.PropertyField(valueRect, globalProviderProperty, new GUIContent("Global Provider"));
                    break;
            }
        
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var modeProperty = property.FindPropertyRelative("mode");
            var mode = (OptionalMode)modeProperty.enumValueIndex;
        
            float height = DROPDOWN_HEIGHT;
        
            if (mode != OptionalMode.Disabled)
            {
                height += SPACING + EditorGUIUtility.singleLineHeight;
            
                // Add extra height for complex property types if needed
                if (mode == OptionalMode.LocalValue)
                {
                    var localValueProperty = property.FindPropertyRelative("localValue");
                    height += EditorGUI.GetPropertyHeight(localValueProperty, true) - EditorGUIUtility.singleLineHeight;
                }
                else if (mode == OptionalMode.GlobalValue)
                {
                    var globalProviderProperty = property.FindPropertyRelative("globalProvider");
                    height += EditorGUI.GetPropertyHeight(globalProviderProperty, true) - EditorGUIUtility.singleLineHeight;
                }
            }
        
            return height;
        }
    }
}
