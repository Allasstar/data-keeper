using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for visual appearance: colors, backgrounds, borders, and border radius
    /// </summary>
    public static class UTKAppearanceExtensions
    {
        #region Color Extensions

        public static T SetColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.color = color;
            return element;
        }

        public static T SetColor<T>(this T element, StyleColor color) where T : VisualElement
        {
            element.style.color = color;
            return element;
        }

        public static T SetColor<T>(this T element, string hexColor) where T : VisualElement
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                element.style.color = color;
            }
            return element;
        }

        #endregion

        #region Background Extensions

        public static T SetBackgroundColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.backgroundColor = color;
            return element;
        }

        public static T SetBackgroundColor<T>(this T element, StyleColor color) where T : VisualElement
        {
            element.style.backgroundColor = color;
            return element;
        }

        public static T SetBackgroundColor<T>(this T element, string hexColor) where T : VisualElement
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                element.style.backgroundColor = color;
            }
            return element;
        }

        public static T SetBackgroundImage<T>(this T element, Sprite sprite) where T : VisualElement
        {
            element.style.backgroundImage = new StyleBackground(sprite);
            return element;
        }

        public static T SetBackgroundImage<T>(this T element, Texture2D texture) where T : VisualElement
        {
            element.style.backgroundImage = new StyleBackground(texture);
            return element;
        }

        public static T SetBackgroundImage<T>(this T element, VectorImage vectorImage) where T : VisualElement
        {
            element.style.backgroundImage = new StyleBackground(vectorImage);
            return element;
        }

        public static T SetBackgroundImage<T>(this T element, StyleBackground styleBackground) where T : VisualElement
        {
            element.style.backgroundImage = styleBackground;
            return element;
        }

        public static T SetBackgroundSize<T>(this T element, BackgroundSize size) where T : VisualElement
        {
            element.style.backgroundSize = size;
            return element;
        }

        #endregion

        #region Border Extensions

        public static T SetBorder<T>(this T element, float width, Color color) where T : VisualElement
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;

            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;

            return element;
        }

        public static T SetBorderWidth<T>(this T element, float width) where T : VisualElement
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            return element;
        }

        public static T SetBorderWidth<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            if (left.HasValue) element.style.borderLeftWidth = left.Value;
            if (top.HasValue) element.style.borderTopWidth = top.Value;
            if (right.HasValue) element.style.borderRightWidth = right.Value;
            if (bottom.HasValue) element.style.borderBottomWidth = bottom.Value;
            return element;
        }

        public static T SetBorderColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.borderLeftColor = color;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            return element;
        }

        public static T SetBorderColor<T>(this T element, Color? left = null, Color? top = null, Color? right = null, Color? bottom = null) where T : VisualElement
        {
            if (left.HasValue) element.style.borderLeftColor = left.Value;
            if (top.HasValue) element.style.borderTopColor = top.Value;
            if (right.HasValue) element.style.borderRightColor = right.Value;
            if (bottom.HasValue) element.style.borderBottomColor = bottom.Value;
            return element;
        }

        public static T SetBorderColor<T>(this T element, string hexColor) where T : VisualElement
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                element.style.borderLeftColor = color;
                element.style.borderTopColor = color;
                element.style.borderRightColor = color;
                element.style.borderBottomColor = color;
            }
            return element;
        }

        // Individual border sides
        public static T SetBorderTop<T>(this T element, float width, Color color) where T : VisualElement
        {
            element.style.borderTopWidth = width;
            element.style.borderTopColor = color;
            return element;
        }

        public static T SetBorderRight<T>(this T element, float width, Color color) where T : VisualElement
        {
            element.style.borderRightWidth = width;
            element.style.borderRightColor = color;
            return element;
        }

        public static T SetBorderBottom<T>(this T element, float width, Color color) where T : VisualElement
        {
            element.style.borderBottomWidth = width;
            element.style.borderBottomColor = color;
            return element;
        }

        public static T SetBorderLeft<T>(this T element, float width, Color color) where T : VisualElement
        {
            element.style.borderLeftWidth = width;
            element.style.borderLeftColor = color;
            return element;
        }

        #endregion

        #region Border Radius Extensions

        public static T SetBorderRadius<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }
        
        public static T SetBorderRadius<T>(this T element, float radius, LengthUnit lengthUnit) where T : VisualElement
        {
            var radiusLength = new Length(radius, lengthUnit);
            element.style.borderTopLeftRadius = radiusLength;
            element.style.borderTopRightRadius = radiusLength;
            element.style.borderBottomLeftRadius = radiusLength;
            element.style.borderBottomRightRadius = radiusLength;
            return element;
        }

        public static T SetBorderRadius<T>(this T element, float? topLeft, float? topRight, float? bottomRight, float? bottomLeft) where T : VisualElement
        {
            if (topLeft.HasValue) element.style.borderTopLeftRadius = topLeft.Value;
            if (topRight.HasValue) element.style.borderTopRightRadius = topRight.Value;
            if (bottomRight.HasValue) element.style.borderBottomRightRadius = bottomRight.Value;
            if (bottomLeft.HasValue) element.style.borderBottomLeftRadius = bottomLeft.Value;
            return element;
        }
        
        public static T SetBorderRadius<T>(this T element, float? topLeft, float? topRight, float? bottomRight, float? bottomLeft, LengthUnit lengthUnit) where T : VisualElement
        {
            if (topLeft.HasValue) element.style.borderTopLeftRadius = new Length(topLeft.Value, lengthUnit);
            if (topRight.HasValue) element.style.borderTopRightRadius = new Length(topRight.Value, lengthUnit);
            if (bottomRight.HasValue) element.style.borderBottomRightRadius = new Length(bottomRight.Value, lengthUnit);
            if (bottomLeft.HasValue) element.style.borderBottomLeftRadius = new Length(bottomLeft.Value, lengthUnit);
            return element;
        }

        // Individual border radius corners
        public static T SetBorderRadiusTopLeft<T>(this T element, float radius, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.borderTopLeftRadius = new Length(radius, lengthUnit);
            return element;
        }

        public static T SetBorderRadiusTopRight<T>(this T element, float radius, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.borderTopRightRadius = new Length(radius, lengthUnit);
            return element;
        }

        public static T SetBorderRadiusBottomRight<T>(this T element, float radius, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.borderBottomRightRadius = new Length(radius, lengthUnit);
            return element;
        }

        public static T SetBorderRadiusBottomLeft<T>(this T element, float radius, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.borderBottomLeftRadius = new Length(radius, lengthUnit);
            return element;
        }

        // Border radius percentage variants
        public static T SetBorderRadiusPercent<T>(this T element, float percentage) where T : VisualElement
        {
            var radius = Length.Percent(percentage);
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }

        // Utility border radius methods
        public static T SetRoundedCorners<T>(this T element) where T : VisualElement
        {
            // Creates fully rounded corners (50% radius)
            element.style.borderTopLeftRadius = Length.Percent(50);
            element.style.borderTopRightRadius = Length.Percent(50);
            element.style.borderBottomLeftRadius = Length.Percent(50);
            element.style.borderBottomRightRadius = Length.Percent(50);
            return element;
        }

        #endregion
    }
}