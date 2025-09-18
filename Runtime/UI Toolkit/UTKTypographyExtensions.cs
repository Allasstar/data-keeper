using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using TextElement = UnityEngine.UIElements.TextElement;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for typography and text styling
    /// </summary>
    public static class UTKTypographyExtensions
    {
        #region Font Size Extensions

        public static T SetFontSize<T>(this T element, float size) where T : VisualElement
        {
            element.style.fontSize = size;
            return element;
        }

        public static T SetFontSizePercent<T>(this T element, float percentage) where T : VisualElement
        {
            element.style.fontSize = Length.Percent(percentage);
            return element;
        }

        public static T SetFontSizeInitial<T>(this T element) where T : VisualElement
        {
            element.style.fontSize = StyleKeyword.Initial;
            return element;
        }

        #endregion

        #region Font Style and Weight Extensions

        public static T SetFontStyle<T>(this T element, FontStyle fontStyle) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = fontStyle;
            return element;
        }

        #endregion

        #region Text Alignment Extensions

        public static T SetTextAlign<T>(this T element, TextAnchor textAnchor) where T : VisualElement
        {
            element.style.unityTextAlign = textAnchor;
            return element;
        }

        #endregion

        #region Text Overflow and Wrap Extensions

        public static T SetTextOverflow<T>(this T element, TextOverflow textOverflow) where T : VisualElement
        {
            element.style.textOverflow = textOverflow;
            return element;
        }
        
        public static T SetTextOverflowEllipsis<T>(this T element) where T : VisualElement
        {
            element.style.textOverflow = TextOverflow.Ellipsis;
            return element;
        }

        public static T SetTextOverflowClip<T>(this T element) where T : VisualElement
        {
            element.style.textOverflow = TextOverflow.Clip;
            return element;
        }
        
        public static T SetWhiteSpace<T>(this T element, WhiteSpace whiteSpace) where T : VisualElement
        {
            element.style.whiteSpace = whiteSpace;
            return element;
        }

        public static T SetTextNoWrap<T>(this T element) where T : VisualElement
        {
            element.style.whiteSpace = WhiteSpace.NoWrap;
            return element;
        }

        public static T SetTextWrapNormal<T>(this T element) where T : VisualElement
        {
            element.style.whiteSpace = WhiteSpace.Normal;
            return element;
        }

        #endregion

        #region Letter and Word Spacing

        public static T SetLetterSpacing<T>(this T element, float spacing) where T : VisualElement
        {
            element.style.letterSpacing = spacing;
            return element;
        }
        
        public static T SetLetterSpacing<T>(this T element, StyleLength spacing) where T : VisualElement
        {
            element.style.letterSpacing = spacing;
            return element;
        }

        public static T SetWordSpacing<T>(this T element, float spacing) where T : VisualElement
        {
            element.style.wordSpacing = spacing;
            return element;
        }
        
        public static T SetWordSpacing<T>(this T element, StyleLength spacing) where T : VisualElement
        {
            element.style.wordSpacing = spacing;
            return element;
        }

        #endregion

        #region Text Content Extensions (for TextElement derived classes)

        public static T SetText<T>(this T textElement, string text) where T : TextElement
        {
            textElement.text = text;
            return textElement;
        }

        public static T SetTextFormat<T>(this T textElement, string format, params object[] args) where T : TextElement
        {
            textElement.text = string.Format(format, args);
            return textElement;
        }

        public static T AppendText<T>(this T textElement, string text) where T : TextElement
        {
            textElement.text += text;
            return textElement;
        }

        public static T ClearText<T>(this T textElement) where T : TextElement
        {
            textElement.text = string.Empty;
            return textElement;
        }

        #endregion

        #region Font Asset Extensions

        public static T SetFont<T>(this T element, Font font) where T : VisualElement
        {
            element.style.unityFont = font;
            return element;
        }

        public static T SetFontDefinition<T>(this T element, FontAsset fontAsset) where T : VisualElement
        {
            element.style.unityFontDefinition = FontDefinition.FromSDFFont(fontAsset);
            return element;
        }

        #endregion

        #region Text Color Extensions

        public static T SetTextColor<T>(this T element, Color color) where T : VisualElement
        {
            element.style.color = color;
            return element;
        }

        public static T SetTextColorHex<T>(this T element, string hexColor) where T : VisualElement
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                element.style.color = color;
            }

            return element;
        }

        #endregion

        #region Opacity Extensions

        public static T SetOpacity<T>(this T element, float opacity) where T : VisualElement
        {
            element.style.opacity = Mathf.Clamp01(opacity);
            return element;
        }

        #endregion


        #region Focusable and Selectable Extensions

        public static T SetTextSelectable<T>(this T textElement, bool selectable = true) where T : TextElement
        {
            textElement.focusable = selectable;
            textElement.pickingMode = selectable ? PickingMode.Position : PickingMode.Ignore;
            return textElement;
        }

        #endregion
    }
}