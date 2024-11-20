using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    public class SpritePreviewDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite sprite)
            {
                SpritePreviewAttribute previewAttribute = (SpritePreviewAttribute)attribute;

                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property, label);

                float aspectRatio = sprite.rect.width / sprite.rect.height;

                float previewHeight = previewAttribute.height;
                float previewWidth = previewHeight * aspectRatio;

                Rect previewRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight + 5,
                    previewWidth,
                    previewHeight
                );

                if (sprite.texture != null)
                {
                    Rect spriteRect = new Rect(
                        sprite.textureRect.x / sprite.texture.width,
                        sprite.textureRect.y / sprite.texture.height,
                        sprite.textureRect.width / sprite.texture.width,
                        sprite.textureRect.height / sprite.texture.height
                    );

                    GUI.DrawTextureWithTexCoords(previewRect, sprite.texture, spriteRect);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue is Sprite)
            {
                SpritePreviewAttribute previewAttribute = (SpritePreviewAttribute)attribute;
                return EditorGUIUtility.singleLineHeight + previewAttribute.height + 10;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}