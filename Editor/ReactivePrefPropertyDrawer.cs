using DataKeeper.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor
{
    [CustomPropertyDrawer(typeof(ReactivePref<>))]
    public class ReactivePrefPropertyDrawer : PropertyDrawer
    {
        private const float _buttonWidth = 18f;
        private const float _space = 3f;
        
        IReactivePref targetProperty;
        private Color _color = new Color(0.15f, 0.31f, 0.15f, 0.3f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (targetProperty == null)
                targetProperty = (IReactivePref)property.GetPropertyInstance();
            
            var valueProperty = property.FindPropertyRelative("value");

            label.tooltip = "Reactive Pref";
            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUI.DrawRect(new Rect(position.x, position.y + 1, position.width, _buttonWidth), _color);

            Rect valuePosition = position;
            valuePosition.width -= _buttonWidth * 3 + _space * 3;
            
            EditorGUI.PropertyField(valuePosition, valueProperty, label, true);

            Rect buttonPosition = position;
            buttonPosition.x += valuePosition.width + _space;
            buttonPosition.width = _buttonWidth;
            buttonPosition.height = _buttonWidth;
            
            GUIContent buttonContent = new GUIContent("S", "Save");
                
            if (GUI.Button(buttonPosition, buttonContent))
            {
                targetProperty.Save();
            }

            buttonPosition.x += _buttonWidth + _space;
            GUIContent buttonContent2 = new GUIContent("L", "Load");
                
            if (GUI.Button(buttonPosition, buttonContent2))
            {
                targetProperty.Load();
            }
            
            buttonPosition.x += _buttonWidth + _space;
            GUIContent buttonContent3 = new GUIContent("F", "Fire event.");
            
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUI.Button(buttonPosition, buttonContent3))
            {
                targetProperty.Invoke();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.EndProperty();
        }
    }
}
