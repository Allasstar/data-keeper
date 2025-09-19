using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for margin and padding with pixel, percentage, auto, and initial support
    /// </summary>
    public static class UTKSpacingExtensions
    {
        #region Padding Extensions

        /// <summary>
        /// Set padding for all sides (inside space)
        /// </summary>
        public static T SetPadding<T>(this T element, float allSides) where T : VisualElement
        {
            element.style.paddingLeft = allSides;
            element.style.paddingTop = allSides;
            element.style.paddingRight = allSides;
            element.style.paddingBottom = allSides;
            return element;
        }
        
        /// <summary>
        /// Set padding for all sides (inside space)
        /// </summary>
        public static T SetPadding<T>(this T element, float allSides, LengthUnit lengthUnit) where T : VisualElement
        {
            var allSidesLength = new Length(allSides, lengthUnit);
            element.style.paddingLeft = allSidesLength;
            element.style.paddingTop = allSidesLength;
            element.style.paddingRight = allSidesLength;
            element.style.paddingBottom = allSidesLength;
            return element;
        }

        /// <summary>
        /// Set padding for specific sides (inside space)
        /// </summary>
        public static T SetPadding<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            if (left.HasValue) element.style.paddingLeft = left.Value;
            if (top.HasValue) element.style.paddingTop = top.Value;
            if (right.HasValue) element.style.paddingRight = right.Value;
            if (bottom.HasValue) element.style.paddingBottom = bottom.Value;
            return element;
        }
        
        /// <summary>
        /// Set padding for specific sides (inside space)
        /// </summary>
        public static T SetPadding<T>(this T element, LengthUnit lengthUnit, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            if (left.HasValue) element.style.paddingLeft = new Length(left.Value, lengthUnit);
            if (top.HasValue) element.style.paddingTop = new Length(top.Value, lengthUnit);
            if (right.HasValue) element.style.paddingRight = new Length(right.Value, lengthUnit);
            if (bottom.HasValue) element.style.paddingBottom = new Length(bottom.Value, lengthUnit);
            return element;
        }

        // Individual padding sides - Pixels
        public static T SetPaddingLeft<T>(this T element, float padding, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.paddingLeft = new Length(padding, lengthUnit);
            return element;
        }

        public static T SetPaddingTop<T>(this T element, float padding, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.paddingTop = new Length(padding, lengthUnit);
            return element;
        }

        public static T SetPaddingRight<T>(this T element, float padding, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.paddingRight = new Length(padding, lengthUnit);
            return element;
        }

        public static T SetPaddingBottom<T>(this T element, float padding, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.paddingBottom = new Length(padding, lengthUnit);
            return element;
        }

        public static T SetPaddingLeft<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.paddingLeft = styleKeyword;
            return element;
        }

        public static T SetPaddingTop<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.paddingTop = styleKeyword;
            return element;
        }

        public static T SetPaddingRight<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.paddingRight = styleKeyword;
            return element;
        }

        public static T SetPaddingBottom<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.paddingBottom = styleKeyword;
            return element;
        }

        #endregion

        #region Margin Extensions

        /// <summary>
        /// Set margin for all sides (outside space)
        /// </summary>
        public static T SetMargin<T>(this T element, float allSides) where T : VisualElement
        {
            element.style.marginLeft = allSides;
            element.style.marginTop = allSides;
            element.style.marginRight = allSides;
            element.style.marginBottom = allSides;
            return element;
        }
        
        /// <summary>
        /// Set margin for all sides (outside space)
        /// </summary>
        public static T SetMargin<T>(this T element, float allSides, LengthUnit lengthUnit) where T : VisualElement
        {
            var allSidesLength = new Length(allSides, lengthUnit);
            element.style.marginLeft = allSidesLength;
            element.style.marginTop = allSidesLength;
            element.style.marginRight = allSidesLength;
            element.style.marginBottom = allSidesLength;
            return element;
        }

        /// <summary>
        /// Set margin for specific sides (outside space)
        /// </summary>
        public static T SetMargin<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            if (left.HasValue) element.style.marginLeft = left.Value;
            if (top.HasValue) element.style.marginTop = top.Value;
            if (right.HasValue) element.style.marginRight = right.Value;
            if (bottom.HasValue) element.style.marginBottom = bottom.Value;
            return element;
        }
        
        /// <summary>
        /// Set margin for specific sides (outside space)
        /// </summary>
        public static T SetMargin<T>(this T element, LengthUnit lengthUnit, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            if (left.HasValue) element.style.marginLeft = new Length(left.Value, lengthUnit);
            if (top.HasValue) element.style.marginTop = new Length(top.Value, lengthUnit);
            if (right.HasValue) element.style.marginRight = new Length(right.Value, lengthUnit);
            if (bottom.HasValue) element.style.marginBottom = new Length(bottom.Value, lengthUnit);
            return element;
        }

        // Margin - Auto variants
        public static T SetMargin<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.marginLeft = styleKeyword;
            element.style.marginTop = styleKeyword;
            element.style.marginRight = styleKeyword;
            element.style.marginBottom = styleKeyword;
            return element;
        }
        
        // Individual margin sides - Pixels
        public static T SetMarginLeft<T>(this T element, float margin, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.marginLeft = new Length(margin, lengthUnit);
            return element;
        }

        public static T SetMarginTop<T>(this T element, float margin, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.marginTop = new Length(margin, lengthUnit);
            return element;
        }

        public static T SetMarginRight<T>(this T element, float margin, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.marginRight = new Length(margin, lengthUnit);
            return element;
        }

        public static T SetMarginBottom<T>(this T element, float margin, LengthUnit lengthUnit = LengthUnit.Pixel) where T : VisualElement
        {
            element.style.marginBottom = new Length(margin, lengthUnit);
            return element;
        }
        
        public static T SetMarginLeft<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.marginLeft = styleKeyword;
            return element;
        }

        public static T SetMarginTop<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.marginTop = styleKeyword;
            return element;
        }

        public static T SetMarginRight<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.marginRight = styleKeyword;
            return element;
        }

        public static T SetMarginBottom<T>(this T element, StyleKeyword styleKeyword) where T : VisualElement
        {
            element.style.marginBottom = styleKeyword;
            return element;
        }

        #endregion
    }
}