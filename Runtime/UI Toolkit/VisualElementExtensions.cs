using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    public static class VisualElementExtensions
    {
        #region VisualElement Basics
        
        public static T SetEnabledSelf<T>(this T element, bool isEnabled) where T : VisualElement
        {
            element.SetEnabled(isEnabled);
            return element;
        }

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
        
        public static T SetBackgroundColor<T>(this T element, StyleColor color) where T : VisualElement
        {
            element.style.backgroundColor = color;
            return element;
        }
                
        public static T SetBackgroundImage<T>(this T element, Sprite sprite) where T : VisualElement
        {
            element.style.backgroundImage = new StyleBackground(sprite);
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

        public static T SetPadding<T>(this T element, float? left, float? top, float? right, float? bottom) where T : VisualElement
        {
            if (left.HasValue) element.style.paddingLeft = left.Value;
            if (top.HasValue) element.style.paddingTop = top.Value;
            if (right.HasValue) element.style.paddingRight = right.Value;
            if (bottom.HasValue) element.style.paddingBottom = bottom.Value;
            
            return element;
        }

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

        public static T SetMargin<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null)  where T : VisualElement
        {
            if (left.HasValue) element.style.marginLeft = left.Value;
            if (top.HasValue) element.style.marginTop = top.Value;
            if (right.HasValue) element.style.marginRight = right.Value;
            if (bottom.HasValue) element.style.marginBottom = bottom.Value;
            
            return element;
        }

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

        public static T SetOverflow<T>(this T element, Overflow overflow) where T : VisualElement
        {
            element.style.overflow = overflow;
            return element;
        }
        
        public static T SetFlexRow<T>(this T element) where T : VisualElement
        {
            element.style.flexDirection = FlexDirection.Row;
            return element;
        }
        
        public static T SetFlexColumn<T>(this T element) where T : VisualElement
        {
            element.style.flexDirection = FlexDirection.Column;
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

        public static T SetPositionAbsolute<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            if (left.HasValue) element.style.left = left.Value;
            if (top.HasValue) element.style.top = top.Value;
            if (right.HasValue) element.style.right = right.Value;
            if (bottom.HasValue) element.style.bottom = bottom.Value;
            
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
        
        public static T SetTextAlign<T>(this T element, TextAnchor textAnchor) where T : VisualElement
        {
            element.style.unityTextAlign = textAnchor;
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

        #region Image

        public static T SetVectorImage<T>(this T element, VectorImage vectorImage) where T : Image
        {
            element.vectorImage = vectorImage;
            return element;
        }
        
        public static T SetImage<T>(this T element, Sprite sprite) where T : Image
        {
            element.sprite = sprite;
            return element;
        }

        #endregion

        #region Button

        public static T SetOnClick<T>(this T button, Action callback) where T : Button
        {
            button.clicked += callback;
            return button;
        }

        public static T SetContent<T>(this T button, string text, Action callback) where T : Button
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

        public static T SetSafeArea<T>(this T element, bool isApply = true) where T : VisualElement
        {
            Rect safeArea = isApply ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);

            element.style.borderLeftWidth = safeArea.x;
            element.style.borderBottomWidth = safeArea.y;
            element.style.borderRightWidth = Screen.width - safeArea.x;
            element.style.borderTopWidth = Screen.height - safeArea.y;

            return element;
        }

        public static T SetChildOf<T>(this T element, VisualElement parent) where T : VisualElement
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
        
        public static T RemoveClasses<T>(this T element, params string[] classNames) where T : VisualElement
        {
            foreach (var c in classNames)
                element.RemoveFromClassList(c);

            return element;
        }

        public static T RemoveAllUnityClasses<T>(this T element) where T : VisualElement
        {
            var list = element.GetClasses()
                .Where(className => className.StartsWith("unity"))
                .ToList();
            
            foreach (string className in list)
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

        public static T ForceUpdate<T>(this T element) where T : VisualElement
        {
            element.schedule.Execute(() =>
            {
                var fakeOldRect = Rect.zero;
                var fakeNewRect = element.layout;

                using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = element.contentContainer;
                element.contentContainer.SendEvent(evt);
            });

            return element;
        }

        #endregion
    }
}

#if !UNITY_6000_0_OR_NEWER
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class UxmlAttributeAttribute : Attribute
{
    public string Name { get; }
    public UxmlAttributeAttribute(string name = null)
    {
        Name = name;
    }
}

public class UxmlElementAttribute : Attribute
{
}
#endif