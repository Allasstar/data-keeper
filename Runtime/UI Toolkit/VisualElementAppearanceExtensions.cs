using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for visual appearance: colors, backgrounds, borders, and border radius
    /// </summary>
    public static class VisualElementAppearanceExtensions
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

        public static T SetColorHex<T>(this T element, string hexColor) where T : VisualElement
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

        public static T SetBackgroundColorHex<T>(this T element, string hexColor) where T : VisualElement
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

        public static T SetBackgroundPosition<T>(this T element, BackgroundPosition position) where T : VisualElement
        {
            element.style.backgroundPositionX = position;
            element.style.backgroundPositionY = position;
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

        public static T SetBorderColorHex<T>(this T element, string hexColor) where T : VisualElement
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

        public static T SetBorderRadius<T>(this T element, float? topLeft, float? topRight, float? bottomRight, float? bottomLeft) where T : VisualElement
        {
            if (topLeft.HasValue) element.style.borderTopLeftRadius = topLeft.Value;
            if (topRight.HasValue) element.style.borderTopRightRadius = topRight.Value;
            if (bottomRight.HasValue) element.style.borderBottomRightRadius = bottomRight.Value;
            if (bottomLeft.HasValue) element.style.borderBottomLeftRadius = bottomLeft.Value;
            return element;
        }

        // Individual border radius corners
        public static T SetBorderRadiusTopLeft<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopLeftRadius = radius;
            return element;
        }

        public static T SetBorderRadiusTopRight<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopRightRadius = radius;
            return element;
        }

        public static T SetBorderRadiusBottomRight<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderBottomRightRadius = radius;
            return element;
        }

        public static T SetBorderRadiusBottomLeft<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderBottomLeftRadius = radius;
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
        public static T SetBorderRadiusTop<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            return element;
        }

        public static T SetBorderRadiusBottom<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }

        public static T SetBorderRadiusLeft<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            return element;
        }

        public static T SetBorderRadiusRight<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }

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

        #region Shadow Extensions (if supported in future Unity versions)

        // Note: These may not work in current Unity versions but are prepared for future updates
        public static T SetBoxShadow<T>(this T element, float offsetX, float offsetY, float blurRadius, Color color) where T : VisualElement
        {
            // This is a placeholder for when Unity adds box-shadow support
            // Currently not supported in Unity UI Toolkit
            return element;
        }

        #endregion

        #region Gradient Extensions

        public static T SetBackgroundGradient<T>(this T element, Color startColor, Color endColor, float angle = 0f) where T : VisualElement
        {
            // Create a simple linear gradient using Unity's built-in capabilities
            // Note: This is a simplified approach - Unity UI Toolkit has limited gradient support
            element.style.backgroundColor = Color.Lerp(startColor, endColor, 0.5f);
            return element;
        }

        #endregion

        #region Utility Appearance Methods

        public static T SetTransparent<T>(this T element) where T : VisualElement
        {
            element.style.backgroundColor = Color.clear;
            return element;
        }

        public static T SetSemiTransparent<T>(this T element, float alpha = 0.5f) where T : VisualElement
        {
            var currentColor = element.resolvedStyle.backgroundColor;
            currentColor.a = alpha;
            element.style.backgroundColor = currentColor;
            return element;
        }

        public static T SetWhiteBackground<T>(this T element) where T : VisualElement
        {
            element.style.backgroundColor = Color.white;
            return element;
        }

        public static T SetBlackBackground<T>(this T element) where T : VisualElement
        {
            element.style.backgroundColor = Color.black;
            return element;
        }

        #endregion
    }
}