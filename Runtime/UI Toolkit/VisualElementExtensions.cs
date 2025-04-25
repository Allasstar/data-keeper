using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    public static class VisualElementExtensions
    {
        #region VisualElement Basics
        public static T SetSize<T>(this T element, float width, float height) where T : VisualElement
        {
            element.style.width = width;
            element.style.height = height;
            return element;
        }

        public static T SetWidth<T>(this T element, float width) where T : VisualElement
        {
            element.style.width = width;
            return element;
        }

        public static T SetHeight<T>(this T element, float height) where T : VisualElement
        {
            element.style.height = height;
            return element;
        }

        public static T SetBackgroundColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.backgroundColor = color;
            return element;
        }

        public static T SetOpacity<T>(this T element, float opacity) where T : VisualElement
        {
            element.style.opacity = opacity;
            return element;
        }

        /// <summary>
        /// Inside space
        /// </summary>
        /// <param name="element"></param>
        /// <param name="allSides"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T SetPadding<T>(this T element, float allSides) where T : VisualElement
        {
            element.style.paddingLeft = allSides;
            element.style.paddingTop = allSides;
            element.style.paddingRight = allSides;
            element.style.paddingBottom = allSides;
            return element;
        }

        /// <summary>
        /// Outside space
        /// </summary>
        /// <param name="element"></param>
        /// <param name="allSides"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T SetMargin<T>(this T element, float allSides) where T : VisualElement
        {
            element.style.marginLeft = allSides;
            element.style.marginTop = allSides;
            element.style.marginRight = allSides;
            element.style.marginBottom = allSides;
            return element;
        }

        public static T SetBorderRadius<T>(this T element, float radius) where T : VisualElement
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }

        public static T SetOverflow<T>(this T element, Overflow overflow) where T : VisualElement
        {
            element.style.overflow = overflow;
            return element;
        }

        public static T SetFlexDirection<T>(this T element, FlexDirection direction) where T : VisualElement
        {
            element.style.flexDirection = direction;
            return element;
        }

        public static T SetFlexGrow<T>(this T element, float grow) where T : VisualElement
        {
            element.style.flexGrow = grow;
            return element;
        }

        public static T SetFlexShrink<T>(this T element, float shrink) where T : VisualElement
        {
            element.style.flexShrink = shrink;
            return element;
        }

        public static T SetAlignItems<T>(this T element, Align align) where T : VisualElement
        {
            element.style.alignItems = align;
            return element;
        }

        public static T SetJustifyContent<T>(this T element, Justify justify) where T : VisualElement
        {
            element.style.justifyContent = justify;
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

        public static T SetPositionAbsolute<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            if (left.HasValue) element.style.left = left.Value;
            if (top.HasValue) element.style.top = top.Value;
            if (right.HasValue) element.style.right = right.Value;
            if (bottom.HasValue) element.style.bottom = top.Value;
            return element;
        }

        public static T SetPositionRelative<T>(this T element) where T : VisualElement
        {
            element.style.position = Position.Relative;
            return element;
        }

        public static T SetFontSize<T>(this T element, float size) where T : VisualElement
        {
            element.style.fontSize = size;
            return element;
        }

        public static T SetColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.color = color;
            return element;
        }

        public static T SetFontStyle<T>(this T element, FontStyle fontStyle) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = fontStyle;
            return element;
        }
        #endregion

        #region Layout & Hierarchy Helpers
        public static T SetStretchToParent<T>(this T element) where T : VisualElement
        {
            element.StretchToParentSize();
            return element;
        }

        public static T SetManipulator<T, TManipulator>(this T element, TManipulator manipulator)
            where T : VisualElement
            where TManipulator : IManipulator
        {
            element.AddManipulator(manipulator);
            return element;
        }
        #endregion

        #region Label/TextElement
        public static T SetText<T>(this T textElement, string text) where T : TextElement
        {
            textElement.text = text;
            return textElement;
        }
        #endregion

        #region Button
        public static Button SetOnClick(this Button button, Action callback)
        {
            button.clicked += callback;
            return button;
        }

        public static Button SetContent(this Button button, string text, Action callback)
        {
            button.text = text;
            button.clicked += callback;
            return button;
        }
        #endregion

        #region Data & Event Binding

        public static T SetOnEvent<T, TEvent>(this T element, EventCallback<TEvent> callback)
            where T : VisualElement
            where TEvent : EventBase<TEvent>, new()
        {
            element.RegisterCallback(callback);
            return element;
        }
        #endregion

        #region Animation & Interaction

        public static T SetTooltip<T>(this T element, string tooltipText) where T : VisualElement
        {
            element.tooltip = tooltipText;
            return element;
        }
        #endregion

        #region Utility

        public static T SetParent<T>(this T element, T parent) where T : VisualElement
        {
            parent.Add(element);
            return element;
        }
        
        public static T AddChild<T>(this T element, params T[] child) where T : VisualElement
        {
            foreach (var c in child)
                element.Add(c);
            
            return element;
        }
        
        public static T AddClasses<T>(this T element, params string[] classNames) where T : VisualElement
        {
            foreach (var c in classNames)
                element.AddToClassList(c);
            
            return element;
        }
        
        public static T RemoveAllUnityClasses<T>(this T element) where T : VisualElement
        {
            foreach (string className in element.GetClasses().Where(className => className.StartsWith("unity")))
                element.RemoveFromClassList(className);

            foreach (VisualElement child in element.hierarchy.Children()) 
                child.RemoveAllUnityClasses();
            
            return element;
        }
        
        public static T DebugLogAllUnityClasses<T>(this T element) where T : VisualElement
        {
            foreach (string className in element.GetClasses().Where(className => className.StartsWith("unity")))
                Debug.Log($"{element.name}<{element.GetType().Name}> :: class: {className}");

            foreach (VisualElement child in element.hierarchy.Children()) 
                child.DebugLogAllUnityClasses();
            
            return element;
        }
        
        #endregion
    }
}