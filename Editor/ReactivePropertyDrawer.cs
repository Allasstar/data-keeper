using DataKeeper.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor
{
    [CustomPropertyDrawer(typeof(Reactive<>))]
    public class ReactivePropertyDrawer : PropertyDrawer
    {
        private const float _buttonWidth = 18f;
        private const float _space = 3f;
        
        IReactive targetProperty;
        private Color _color = new Color(0.15f, 0.15f, 0.31f, 0.3f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (targetProperty == null)
                targetProperty = (IReactive)property.GetPropertyInstance();
            
            var valueProperty = property.FindPropertyRelative("value");

            label.tooltip = "Reactive";
            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUI.DrawRect(new Rect(position.x, position.y + 1, position.width, _buttonWidth), _color);

            Rect valuePosition = position;
            valuePosition.width -= _buttonWidth + _space;
            
            EditorGUI.PropertyField(valuePosition, valueProperty, label, true);

            Rect buttonPosition = position;
            buttonPosition.x += valuePosition.width + _space;
            buttonPosition.width = _buttonWidth;
            buttonPosition.height = _buttonWidth;
            
            GUIContent buttonContent = new GUIContent("F", "Fire event.");
            
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUI.Button(buttonPosition, buttonContent))
            {
                targetProperty.Invoke();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }
    }
}
