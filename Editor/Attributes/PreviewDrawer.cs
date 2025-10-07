using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(PreviewAttribute))]
    public class PreviewDrawer : PropertyDrawer
    {
        private float _previewSize = 64f;
        private float _padding = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                PreviewAttribute previewAttribute = (PreviewAttribute)attribute;
                _previewSize = previewAttribute.PreviewSize;
                _padding = previewAttribute.Padding;
                
                EditorGUI.BeginProperty(position, label, property);
                
                // Calculate rects
                bool hasPreview = HasPreview(property.objectReferenceValue);
                float objectFieldHeight = EditorGUIUtility.singleLineHeight;
                Rect objectFieldRect = new Rect(position.x, position.y, position.width, objectFieldHeight);
                
                // Draw object field
                EditorGUI.BeginChangeCheck();
                Object newValue = EditorGUI.ObjectField(
                    objectFieldRect,
                    label,
                    property.objectReferenceValue,
                    fieldInfo.FieldType,
                    previewAttribute.AllowSceneObjects
                );
                
                if (EditorGUI.EndChangeCheck())
                {
                    property.objectReferenceValue = newValue;
                }
                
                // Draw preview if applicable
                if (hasPreview && property.objectReferenceValue != null)
                {
                    Vector2 previewDimensions = GetPreviewDimensions(property.objectReferenceValue);
                    
                    Rect previewRect = new Rect(
                        position.x + EditorGUIUtility.labelWidth,
                        position.y + objectFieldHeight + _padding,
                        previewDimensions.x,
                        previewDimensions.y
                    );
                    
                    DrawPreview(previewRect, property.objectReferenceValue);
                }
                
                EditorGUI.EndProperty();
            }
            else
            {
                PropertyGUI.DrawGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                float height = EditorGUIUtility.singleLineHeight;
                
                if (HasPreview(property.objectReferenceValue) && property.objectReferenceValue != null)
                {
                    Vector2 previewDimensions = GetPreviewDimensions(property.objectReferenceValue);
                    height += previewDimensions.y + _padding;
                }
                
                return height;
            }
            
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool HasPreview(Object obj)
        {
            return obj is Sprite || obj is Texture2D || obj is Texture;
        }

        private Vector2 GetPreviewDimensions(Object obj)
        {
            Vector2 textureDimensions = GetTextureDimensions(obj);
            
            if (textureDimensions.x <= 0 || textureDimensions.y <= 0)
            {
                return new Vector2(_previewSize, _previewSize);
            }
            
            float aspectRatio = textureDimensions.x / textureDimensions.y;
            float width, height;
            
            // The larger dimension should be _previewSize, smaller should be scaled by aspect ratio
            if (textureDimensions.x > textureDimensions.y)
            {
                // Landscape: width is larger
                width = _previewSize;
                height = _previewSize / aspectRatio;
            }
            else
            {
                // Portrait or square: height is larger or equal
                height = _previewSize;
                width = _previewSize * aspectRatio;
            }
            
            return new Vector2(width, height);
        }

        private Vector2 GetTextureDimensions(Object obj)
        {
            if (obj is Sprite sprite)
            {
                return new Vector2(sprite.rect.width, sprite.rect.height);
            }
            else if (obj is Texture texture)
            {
                return new Vector2(texture.width, texture.height);
            }
            
            return Vector2.zero;
        }

        private void DrawPreview(Rect rect, Object obj)
        {
            // Draw transparent background (checkerboard pattern)
            DrawTransparentBackground(rect);
            
            // Draw the texture
            if (obj is Sprite sprite)
            {
                Texture2D texture = sprite.texture;
                Rect spriteRect = sprite.rect;
                Rect uvRect = new Rect(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height
                );
                GUI.DrawTextureWithTexCoords(rect, texture, uvRect);
            }
            else if (obj is Texture texture)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            }
        }

        private void DrawTransparentBackground(Rect rect)
        {
            // Create checkerboard pattern for transparent background
            Color lightGray = new Color(0.8f, 0.8f, 0.8f, 1f);
            Color darkGray = new Color(0.6f, 0.6f, 0.6f, 1f);
            
            int checkerSize = 8;
            int xSquares = Mathf.CeilToInt(rect.width / checkerSize);
            int ySquares = Mathf.CeilToInt(rect.height / checkerSize);
            
            for (int y = 0; y < ySquares; y++)
            {
                for (int x = 0; x < xSquares; x++)
                {
                    Rect squareRect = new Rect(
                        rect.x + x * checkerSize,
                        rect.y + y * checkerSize,
                        checkerSize,
                        checkerSize
                    );
                    
                    // Clip to preview bounds
                    squareRect.xMax = Mathf.Min(squareRect.xMax, rect.xMax);
                    squareRect.yMax = Mathf.Min(squareRect.yMax, rect.yMax);
                    
                    Color color = (x + y) % 2 == 0 ? lightGray : darkGray;
                    EditorGUI.DrawRect(squareRect, color);
                }
            }
        }
    }
}