using DataKeeper.Editor.Drawer;
using DataKeeper.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor
{
    [CustomPropertyDrawer(typeof(Optional<>))]
    public class OptionalPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return PropertyGUI.GetPropertyHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PropertyGUI.DrawGUI(position, property, label);
        }
    }
}