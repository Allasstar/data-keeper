using DataKeeper.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor
{
    [CustomPropertyDrawer(typeof(DataFile<>))]
    public class DataFilePropertyDrawer : PropertyDrawer
    {
        private const float _buttonWidth = 18f;
        private const float _space = 3f;
        
        IDataFile targetProperty;
        private Color _color = new Color(0.31f, 0.15f, 0.15f, 0.3f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("data");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (targetProperty == null)
                targetProperty = (IDataFile)property.GetPropertyInstance();
            
            var valueProperty = property.FindPropertyRelative("data");
            
            label.tooltip = "Data File";
            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUI.DrawRect(new Rect(position.x, position.y + 1, position.width, _buttonWidth), _color);
            
            Rect valuePosition = position;
            valuePosition.width -= _buttonWidth * 2 + _space * 2;
            
            EditorGUI.PropertyField(valuePosition, valueProperty, label, true);

            Rect buttonPosition = position;
            buttonPosition.x += valuePosition.width + _space;
            buttonPosition.width = _buttonWidth;
            buttonPosition.height = _buttonWidth;
            
            GUIContent buttonContent = new GUIContent("S", "Save");
                
            if (GUI.Button(buttonPosition, buttonContent))
            {
                targetProperty.SaveData();
            }

            buttonPosition.x += _buttonWidth + _space;
            GUIContent buttonContent2 = new GUIContent("L", "Load");
                
            if (GUI.Button(buttonPosition, buttonContent2))
            {
                targetProperty.LoadData();
            }

            EditorGUI.EndProperty();
        }
    }
}
