using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for margin and padding with pixel, percentage, auto, and initial support
    /// </summary>
    public static class VisualElementSpacingExtensions
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
        /// Set padding for specific sides (inside space)
        /// </summary>
        public static T SetPadding<T>(this T element, float? left, float? top, float? right, float? bottom) where T : VisualElement
        {
            if (left.HasValue) element.style.paddingLeft = left.Value;
            if (top.HasValue) element.style.paddingTop = top.Value;
            if (right.HasValue) element.style.paddingRight = right.Value;
            if (bottom.HasValue) element.style.paddingBottom = bottom.Value;
            return element;
        }

        // Padding - Percentage variants
        public static T SetPaddingPercent<T>(this T element, float allSidesPercent) where T : VisualElement
        {
            var padding = Length.Percent(allSidesPercent);
            element.style.paddingLeft = padding;
            element.style.paddingTop = padding;
            element.style.paddingRight = padding;
            element.style.paddingBottom = padding;
            return element;
        }

        public static T SetPaddingPercent<T>(this T element, float? leftPercent, float? topPercent, float? rightPercent, float? bottomPercent) where T : VisualElement
        {
            if (leftPercent.HasValue) element.style.paddingLeft = Length.Percent(leftPercent.Value);
            if (topPercent.HasValue) element.style.paddingTop = Length.Percent(topPercent.Value);
            if (rightPercent.HasValue) element.style.paddingRight = Length.Percent(rightPercent.Value);
            if (bottomPercent.HasValue) element.style.paddingBottom = Length.Percent(bottomPercent.Value);
            return element;
        }

        // Individual padding sides - Pixels
        public static T SetPaddingLeft<T>(this T element, float padding) where T : VisualElement
        {
            element.style.paddingLeft = padding;
            return element;
        }

        public static T SetPaddingTop<T>(this T element, float padding) where T : VisualElement
        {
            element.style.paddingTop = padding;
            return element;
        }

        public static T SetPaddingRight<T>(this T element, float padding) where T : VisualElement
        {
            element.style.paddingRight = padding;
            return element;
        }

        public static T SetPaddingBottom<T>(this T element, float padding) where T : VisualElement
        {
            element.style.paddingBottom = padding;
            return element;
        }

        // Individual padding sides - Percentage
        public static T SetPaddingLeftPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.paddingLeft = Length.Percent(percentage);
            return element;
        }

        public static T SetPaddingTopPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.paddingTop = Length.Percent(percentage);
            return element;
        }

        public static T SetPaddingRightPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.paddingRight = Length.Percent(percentage);
            return element;
        }

        public static T SetPaddingBottomPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.paddingBottom = Length.Percent(percentage);
            return element;
        }

        // Horizontal and Vertical padding shortcuts
        public static T SetPaddingHorizontal<T>(this T element, float horizontal) where T : VisualElement
        {
            element.style.paddingLeft = horizontal;
            element.style.paddingRight = horizontal;
            return element;
        }

        public static T SetPaddingVertical<T>(this T element, float vertical) where T : VisualElement
        {
            element.style.paddingTop = vertical;
            element.style.paddingBottom = vertical;
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

        // Margin - Percentage variants
        public static T SetMarginPercent<T>(this T element, float allSidesPercent) where T : VisualElement
        {
            var margin = Length.Percent(allSidesPercent);
            element.style.marginLeft = margin;
            element.style.marginTop = margin;
            element.style.marginRight = margin;
            element.style.marginBottom = margin;
            return element;
        }

        public static T SetMarginPercent<T>(this T element, float? leftPercent, float? topPercent, float? rightPercent, float? bottomPercent) where T : VisualElement
        {
            if (leftPercent.HasValue) element.style.marginLeft = Length.Percent(leftPercent.Value);
            if (topPercent.HasValue) element.style.marginTop = Length.Percent(topPercent.Value);
            if (rightPercent.HasValue) element.style.marginRight = Length.Percent(rightPercent.Value);
            if (bottomPercent.HasValue) element.style.marginBottom = Length.Percent(bottomPercent.Value);
            return element;
        }

        // Margin - Auto variants
        public static T SetMarginAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginLeft = StyleKeyword.Auto;
            element.style.marginTop = StyleKeyword.Auto;
            element.style.marginRight = StyleKeyword.Auto;
            element.style.marginBottom = StyleKeyword.Auto;
            return element;
        }

        public static T SetMarginHorizontalAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginLeft = StyleKeyword.Auto;
            element.style.marginRight = StyleKeyword.Auto;
            return element;
        }

        public static T SetMarginVerticalAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginTop = StyleKeyword.Auto;
            element.style.marginBottom = StyleKeyword.Auto;
            return element;
        }

        // Individual margin sides - Pixels
        public static T SetMarginLeft<T>(this T element, float margin) where T : VisualElement
        {
            element.style.marginLeft = margin;
            return element;
        }

        public static T SetMarginTop<T>(this T element, float margin) where T : VisualElement
        {
            element.style.marginTop = margin;
            return element;
        }

        public static T SetMarginRight<T>(this T element, float margin) where T : VisualElement
        {
            element.style.marginRight = margin;
            return element;
        }

        public static T SetMarginBottom<T>(this T element, float margin) where T : VisualElement
        {
            element.style.marginBottom = margin;
            return element;
        }

        // Individual margin sides - Percentage
        public static T SetMarginLeftPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.marginLeft = Length.Percent(percentage);
            return element;
        }

        public static T SetMarginTopPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.marginTop = Length.Percent(percentage);
            return element;
        }

        public static T SetMarginRightPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.marginRight = Length.Percent(percentage);
            return element;
        }

        public static T SetMarginBottomPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.marginBottom = Length.Percent(percentage);
            return element;
        }

        // Individual margin sides - Auto
        public static T SetMarginLeftAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginLeft = StyleKeyword.Auto;
            return element;
        }

        public static T SetMarginTopAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginTop = StyleKeyword.Auto;
            return element;
        }

        public static T SetMarginRightAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginRight = StyleKeyword.Auto;
            return element;
        }

        public static T SetMarginBottomAuto<T>(this T element) where T : VisualElement
        {
            element.style.marginBottom = StyleKeyword.Auto;
            return element;
        }

        // Horizontal and Vertical margin shortcuts
        public static T SetMarginHorizontal<T>(this T element, float horizontal) where T : VisualElement
        {
            element.style.marginLeft = horizontal;
            element.style.marginRight = horizontal;
            return element;
        }

        public static T SetMarginVertical<T>(this T element, float vertical) where T : VisualElement
        {
            element.style.marginTop = vertical;
            element.style.marginBottom = vertical;
            return element;
        }

        public static T SetMarginHorizontalPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.marginLeft = Length.Percent(percentage);
            element.style.marginRight = Length.Percent(percentage);
            return element;
        }

        public static T SetMarginVerticalPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.marginTop = Length.Percent(percentage);
            element.style.marginBottom = Length.Percent(percentage);
            return element;
        }

        #endregion
    }
}