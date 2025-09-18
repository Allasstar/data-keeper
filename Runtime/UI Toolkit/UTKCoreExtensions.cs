using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Core VisualElement extensions for basic functionality
    /// </summary>
    public static class UTKCoreExtensions
    {
        public static T SetEnabledSelf<T>(this T element, bool isEnabled) where T : VisualElement
        {
            element.SetEnabled(isEnabled);
            return element;
        }

        public static T SetDisplay<T>(this T element, DisplayStyle style) where T : VisualElement
        {
            element.style.display = style;
            return element;
        }

        public static T SetVisibility<T>(this T element, Visibility visibility) where T : VisualElement
        {
            element.style.visibility = visibility;
            return element;
        }

        public static T SetOpacity<T>(this T element, float opacity) where T : VisualElement
        {
            element.style.opacity = opacity;
            return element;
        }

        public static T SetOverflow<T>(this T element, Overflow overflow) where T : VisualElement
        {
            element.style.overflow = overflow;
            return element;
        }

        public static T SetTooltip<T>(this T element, string tooltipText) where T : VisualElement
        {
            element.tooltip = tooltipText;
            return element;
        }

        public static T SetName<T>(this T element, string name) where T : VisualElement
        {
            element.name = name;
            return element;
        }
    }
}