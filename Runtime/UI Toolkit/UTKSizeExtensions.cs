using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for size, width, and height styling with pixel, percentage, auto, and initial support
    /// </summary>
    public static class UTKSizeExtensions
    {
        #region Width Extensions

        // Width - Pixel values
        public static T SetWidth<T>(this T element, float width) where T : VisualElement
        {
            element.style.width = width;
            return element;
        }

        public static T SetWidth<T>(this T element, float wight, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.width = new Length(wight, lengthUnit);
            return element;
        }
        
        public static T SetWidth<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.width = styleKeyword;
            return element;
        }

        #endregion

        #region Height Extensions

        // Height - Pixel values
        public static T SetHeight<T>(this T element, float height) where T : VisualElement
        {
            element.style.height = height;
            return element;
        }

        public static T SetHeight<T>(this T element, float height, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.height = new Length(height, lengthUnit);
            return element;
        }
        
        public static T SetHeight<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.height = styleKeyword;
            return element;
        }

        #endregion

        #region Size Combinations

        // Size - Pixel values
        public static T SetSize<T>(this T element, float width, float height) where T : VisualElement
        {
            element.style.width = width;
            element.style.height = height;
            return element;
        }

        // Size - Percentage values
        public static T SetSize<T>(this T element, float width, float height, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.width = new Length(width, lengthUnit);
            element.style.height = new Length(height, lengthUnit);
            return element;
        }

        // Size - Auto
        public static T SetSize<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.width = styleKeyword;
            element.style.height = styleKeyword;
            return element;
        }

        #endregion

        #region Min Width Extensions

        public static T SetMinWidth<T>(this T element, float minWidth) where T : VisualElement
        {
            element.style.minWidth = minWidth;
            return element;
        }

        public static T SetMinWidth<T>(this T element, float minWidth, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.minWidth = new Length(minWidth, lengthUnit);
            return element;
        }

        public static T SetMinWidth<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.minWidth = styleKeyword;
            return element;
        }

        #endregion

        #region Min Height Extensions

        public static T SetMinHeight<T>(this T element, float minHeight) where T : VisualElement
        {
            element.style.minHeight = minHeight;
            return element;
        }

        public static T SetMinHeight<T>(this T element, float minHeight, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.minHeight = new Length(minHeight, lengthUnit);
            return element;
        }

        public static T SetMinHeight<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.minHeight = styleKeyword;
            return element;
        }

        #endregion

        #region Max Width Extensions

        public static T SetMaxWidth<T>(this T element, float maxWidth) where T : VisualElement
        {
            element.style.maxWidth = maxWidth;
            return element;
        }

        public static T SetMaxWidth<T>(this T element, float maxWidth, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.maxWidth = new Length(maxWidth, lengthUnit);
            return element;
        }

        public static T SetMaxWidth<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.maxWidth = styleKeyword;
            return element;
        }

        #endregion

        #region Max Height Extensions

        public static T SetMaxHeight<T>(this T element, float maxHeight) where T : VisualElement
        {
            element.style.maxHeight = maxHeight;
            return element;
        }

        public static T SetMaxHeight<T>(this T element, float maxHeight, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.maxHeight = new Length(maxHeight, lengthUnit);
            return element;
        }

        public static T SetMaxHeight<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.maxHeight = styleKeyword;
            return element;
        }

        #endregion
        
        #region Utility Size Methods

        public static T SetStretchToParent<T>(this T element) where T : VisualElement
        {
            element.StretchToParentSize();
            return element;
        }

        public static T SetSize<T>(this T element, float size) where T : VisualElement
        {
            element.style.width = size;
            element.style.height = size;
            return element;
        }
        
        public static T SetSize<T>(this T element, float size, LengthUnit lengthUnit) where T : VisualElement
        {
            var length = new Length(size, lengthUnit);
            element.style.width = length;
            element.style.height = length;
            return element;
        }
        
        public static T SetSize<T>(this T element, float size, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.width = styleKeyword;
            element.style.height = styleKeyword;
            return element;
        }

        #endregion
    }
}