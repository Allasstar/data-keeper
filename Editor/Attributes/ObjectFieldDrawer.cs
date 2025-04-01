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
                property.objectReferenceValue = EditorGUILayout.ObjectField(label, property.objectReferenceValue, property?.GetPropertyInstance()?.GetType(), previewAttribute.AllowSceneObjects);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}