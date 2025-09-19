using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for layout, flexbox, and positioning
    /// </summary>
    public static class UTKLayoutExtensions
    {
        #region Flex Direction Extensions

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

        public static T SetFlexRowReverse<T>(this T element) where T : VisualElement
        {
            element.style.flexDirection = FlexDirection.RowReverse;
            return element;
        }

        public static T SetFlexColumnReverse<T>(this T element) where T : VisualElement
        {
            element.style.flexDirection = FlexDirection.ColumnReverse;
            return element;
        }

        public static T SetFlexDirection<T>(this T element, FlexDirection direction) where T : VisualElement
        {
            element.style.flexDirection = direction;
            return element;
        }

        #endregion

        #region Flex Properties

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
        
        public static T SetFlexBasis<T>(this T element, float basis) where T : VisualElement
        {
            element.style.flexBasis = basis;
            return element;
        }

        public static T SetFlexBasis<T>(this T element, float basis, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.flexBasis = new Length(basis, lengthUnit);
            return element;
        }

        public static T SetFlexBasisAuto<T>(this T element) where T : VisualElement
        {
            element.style.flexBasis = StyleKeyword.Auto;
            return element;
        }

        public static T SetFlex<T>(this T element, float grow, float shrink, float basis) where T : VisualElement
        {
            element.style.flexGrow = grow;
            element.style.flexShrink = shrink;
            element.style.flexBasis = basis;
            return element;
        }

        public static T SetFlexExpand<T>(this T element) where T : VisualElement
        {
            element.style.flexGrow = 1;
            element.style.flexShrink = 1;
            return element;
        }

        public static T SetFlexNone<T>(this T element) where T : VisualElement
        {
            element.style.flexGrow = StyleKeyword.None;
            element.style.flexShrink = StyleKeyword.None;
            return element;
        }

        #endregion

        #region Flex Wrap

        public static T SetFlexWrap<T>(this T element, Wrap wrap) where T : VisualElement
        {
            element.style.flexWrap = wrap;
            return element;
        }

        #endregion

        #region Alignment Extensions

        public static T SetAlignItems<T>(this T element, Align align) where T : VisualElement
        {
            element.style.alignItems = align;
            return element;
        }

        public static T SetAlignSelf<T>(this T element, Align align) where T : VisualElement
        {
            element.style.alignSelf = align;
            return element;
        }

        #endregion

        #region Justify Content Extensions

        public static T SetJustifyContent<T>(this T element, Justify justify) where T : VisualElement
        {
            element.style.justifyContent = justify;
            return element;
        }

        #endregion

        #region Positioning Extensions

        public static T SetPositionAbsolute<T>(this T element, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            if (left.HasValue) element.style.left = left.Value;
            if (top.HasValue) element.style.top = top.Value;
            if (right.HasValue) element.style.right = right.Value;
            if (bottom.HasValue) element.style.bottom = bottom.Value;
            return element;
        }

        public static T SetPosition<T>(this T element, Position position) where T : VisualElement
        {
            element.style.position = position;
            return element;
        }

        // Individual position properties
        public static T SetLeft<T>(this T element, float left) where T : VisualElement
        {
            element.style.left = left;
            return element;
        }

        public static T SetTop<T>(this T element, float top) where T : VisualElement
        {
            element.style.top = top;
            return element;
        }

        public static T SetRight<T>(this T element, float right) where T : VisualElement
        {
            element.style.right = right;
            return element;
        }

        public static T SetBottom<T>(this T element, float bottom) where T : VisualElement
        {
            element.style.bottom = bottom;
            return element;
        }

        // Position with percentage
        public static T SetLeft<T>(this T element, float left, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.left = new Length(left, lengthUnit);
            return element;
        }

        public static T SetTop<T>(this T element, float top, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.top = new Length(top, lengthUnit);
            return element;
        }

        public static T SetRight<T>(this T element, float right, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.right = new Length(right, lengthUnit);
            return element;
        }

        public static T SetBottom<T>(this T element, float bottom, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.bottom = new Length(bottom, lengthUnit);
            return element;
        }

        #endregion

        #region Utility Layout Methods

        public static T CenterInParent<T>(this T element) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(50);
            element.style.top = Length.Percent(50);
            element.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            return element;
        }

        public static T CenterHorizontally<T>(this T element) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(50);
            element.style.translate = new Translate(Length.Percent(-50), 0);
            return element;
        }

        public static T CenterVertically<T>(this T element) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            element.style.top = Length.Percent(50);
            element.style.translate = new Translate(0, Length.Percent(-50));
            return element;
        }

        public static T SetFullSize<T>(this T element) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
            return element;
        }

        #endregion

        #region Transform Extensions

        public static T SetTranslate<T>(this T element, float x, float y) where T : VisualElement
        {
            element.style.translate = new Translate(x, y);
            return element;
        }

        public static T SetTranslate<T>(this T element, float x, float y, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.translate = new Translate(new Length(x, lengthUnit), new Length(y, lengthUnit));
            return element;
        }

        public static T SetRotate<T>(this T element, float angle) where T : VisualElement
        {
            element.style.rotate = new Rotate(angle);
            return element;
        }

        public static T SetScale<T>(this T element, float scale) where T : VisualElement
        {
            element.style.scale = new Scale(Vector2.one * scale);
            return element;
        }

        public static T SetScale<T>(this T element, float x, float y) where T : VisualElement
        {
            element.style.scale = new Scale(new Vector2(x, y));
            return element;
        }

        public static T SetTransformOrigin<T>(this T element, float x, float y) where T : VisualElement
        {
            element.style.transformOrigin = new TransformOrigin(x, y);
            return element;
        }

        public static T SetTransformOrigin<T>(this T element, float x, float y, LengthUnit lengthUnit) where T : VisualElement
        {
            element.style.transformOrigin = new TransformOrigin(new Length(x, lengthUnit), new Length(y, lengthUnit));
            return element;
        }

        #endregion
    }
}