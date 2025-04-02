using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(ObjectFieldAttribute))]
    public class ObjectFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                ObjectFieldAttribute previewAttribute = (ObjectFieldAttribute)attribute;
                property.objectReferenceValue = EditorGUILayout.ObjectField(label, property.objectReferenceValue, fieldInfo.FieldType, previewAttribute.AllowSceneObjects);
            }
            else
            {
                PropertyGUI.DrawGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}