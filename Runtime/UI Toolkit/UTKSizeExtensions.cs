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

        // Width - Percentage values
        public static T SetWidthPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.width = Length.Percent(percentage);
            return element;
        }

        // Width - Auto
        public static T SetWidthAuto<T>(this T element) where T : VisualElement
        {
            element.style.width = StyleKeyword.Auto;
            return element;
        }

        // Width - Initial
        public static T SetWidthInitial<T>(this T element) where T : VisualElement
        {
            element.style.width = StyleKeyword.Initial;
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

        // Height - Percentage values
        public static T SetHeightPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.height = Length.Percent(percentage);
            return element;
        }

        // Height - Auto
        public static T SetHeightAuto<T>(this T element) where T : VisualElement
        {
            element.style.height = StyleKeyword.Auto;
            return element;
        }

        // Height - Initial
        public static T SetHeightInitial<T>(this T element) where T : VisualElement
        {
            element.style.height = StyleKeyword.Initial;
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
        public static T SetSizePercent<T>(this T element, float widthPercent, float heightPercent) where T : VisualElement
        {
            element.style.width = Length.Percent(widthPercent);
            element.style.height = Length.Percent(heightPercent);
            return element;
        }

        // Size - Auto
        public static T SetSizeAuto<T>(this T element) where T : VisualElement
        {
            element.style.width = StyleKeyword.Auto;
            element.style.height = StyleKeyword.Auto;
            return element;
        }

        #endregion

        #region Min Width Extensions

        public static T SetMinWidth<T>(this T element, float minWidth) where T : VisualElement
        {
            element.style.minWidth = minWidth;
            return element;
        }

        public static T SetMinWidthPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.minWidth = Length.Percent(percentage);
            return element;
        }

        public static T SetMinWidthAuto<T>(this T element) where T : VisualElement
        {
            element.style.minWidth = StyleKeyword.Auto;
            return element;
        }

        #endregion

        #region Min Height Extensions

        public static T SetMinHeight<T>(this T element, float minHeight) where T : VisualElement
        {
            element.style.minHeight = minHeight;
            return element;
        }

        public static T SetMinHeightPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.minHeight = Length.Percent(percentage);
            return element;
        }

        public static T SetMinHeightAuto<T>(this T element) where T : VisualElement
        {
            element.style.minHeight = StyleKeyword.Auto;
            return element;
        }

        #endregion

        #region Max Width Extensions

        public static T SetMaxWidth<T>(this T element, float maxWidth) where T : VisualElement
        {
            element.style.maxWidth = maxWidth;
            return element;
        }

        public static T SetMaxWidthPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.maxWidth = Length.Percent(percentage);
            return element;
        }

        public static T SetMaxWidthNone<T>(this T element) where T : VisualElement
        {
            element.style.maxWidth = StyleKeyword.None;
            return element;
        }

        #endregion

        #region Max Height Extensions

        public static T SetMaxHeight<T>(this T element, float maxHeight) where T : VisualElement
        {
            element.style.maxHeight = maxHeight;
            return element;
        }

        public static T SetMaxHeightPercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.maxHeight = Length.Percent(percentage);
            return element;
        }

        public static T SetMaxHeightNone<T>(this T element) where T : VisualElement
        {
            element.style.maxHeight = StyleKeyword.None;
            return element;
        }

        #endregion

        #region Min/Max Size Combinations

        public static T SetMinSize<T>(this T element, float? width, float? height) where T : VisualElement
        {
            if (width.HasValue) element.style.minWidth = width.Value;
            if (height.HasValue) element.style.minHeight = height.Value;
            return element;
        }

        public static T SetMaxSize<T>(this T element, float? width, float? height) where T : VisualElement
        {
            if (width.HasValue) element.style.maxWidth = width.Value;
            if (height.HasValue) element.style.maxHeight = height.Value;
            return element;
        }

        public static T SetMinSizePercent<T>(this T element, float? widthPercent, float? heightPercent) where T : VisualElement
        {
            if (widthPercent.HasValue) element.style.minWidth = Length.Percent(widthPercent.Value);
            if (heightPercent.HasValue) element.style.minHeight = Length.Percent(heightPercent.Value);
            return element;
        }

        public static T SetMaxSizePercent<T>(this T element, float? widthPercent, float? heightPercent) where T : VisualElement
        {
            if (widthPercent.HasValue) element.style.maxWidth = Length.Percent(widthPercent.Value);
            if (heightPercent.HasValue) element.style.maxHeight = Length.Percent(heightPercent.Value);
            return element;
        }

        #endregion

        #region Utility Size Methods

        public static T SetStretchToParent<T>(this T element) where T : VisualElement
        {
            element.StretchToParentSize();
            return element;
        }

        public static T SetSquareSize<T>(this T element, float size) where T : VisualElement
        {
            element.style.width = size;
            element.style.height = size;
            return element;
        }

        #endregion
    }
}