using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using TextElement = UnityEngine.UIElements.TextElement;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for typography and text styling
    /// </summary>
    public static class VisualElementTypographyExtensions
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

        // Predefined font sizes
        public static T SetFontSizeSmall<T>(this T element) where T : VisualElement
        {
            element.style.fontSize = 12;
            return element;
        }

        public static T SetFontSizeMedium<T>(this T element) where T : VisualElement
        {
            element.style.fontSize = 16;
            return element;
        }

        public static T SetFontSizeLarge<T>(this T element) where T : VisualElement
        {
            element.style.fontSize = 24;
            return element;
        }

        public static T SetFontSizeExtraLarge<T>(this T element) where T : VisualElement
        {
            element.style.fontSize = 32;
            return element;
        }

        #endregion

        #region Font Style and Weight Extensions

        public static T SetFontStyle<T>(this T element, FontStyle fontStyle) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = fontStyle;
            return element;
        }

        public static T SetFontBold<T>(this T element) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = FontStyle.Bold;
            return element;
        }

        public static T SetFontItalic<T>(this T element) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = FontStyle.Italic;
            return element;
        }

        public static T SetFontBoldItalic<T>(this T element) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
            return element;
        }

        public static T SetFontNormal<T>(this T element) where T : VisualElement
        {
            element.style.unityFontStyleAndWeight = FontStyle.Normal;
            return element;
        }

        #endregion

        #region Text Alignment Extensions

        public static T SetTextAlign<T>(this T element, TextAnchor textAnchor) where T : VisualElement
        {
            element.style.unityTextAlign = textAnchor;
            return element;
        }

        public static T SetTextAlignLeft<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.UpperLeft;
            return element;
        }

        public static T SetTextAlignCenter<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.UpperCenter;
            return element;
        }

        public static T SetTextAlignRight<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.UpperRight;
            return element;
        }

        public static T SetTextAlignMiddleLeft<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.MiddleLeft;
            return element;
        }

        public static T SetTextAlignMiddleCenter<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.MiddleCenter;
            return element;
        }

        public static T SetTextAlignMiddleRight<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.MiddleRight;
            return element;
        }

        public static T SetTextAlignLowerLeft<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.LowerLeft;
            return element;
        }

        public static T SetTextAlignLowerCenter<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.LowerCenter;
            return element;
        }

        public static T SetTextAlignLowerRight<T>(this T element) where T : VisualElement
        {
            element.style.unityTextAlign = TextAnchor.LowerRight;
            return element;
        }

        #endregion

        #region Text Overflow and Wrap Extensions

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

        #endregion

        #region Letter and Word Spacing

        public static T SetLetterSpacing<T>(this T element, float spacing) where T : VisualElement
        {
            element.style.letterSpacing = spacing;
            return element;
        }

        public static T SetWordSpacing<T>(this T element, float spacing) where T : VisualElement
        {
            element.style.wordSpacing = spacing;
            return element;
        }

        #endregion

        #region Text Shadow Extensions (for future Unity support)

        // Note: These may not work in current Unity versions but are prepared for future updates
        public static T SetTextShadow<T>(this T element, float offsetX, float offsetY, float blurRadius, Color color)
            where T : VisualElement
        {
            // Placeholder for future Unity text shadow support
            // Currently not directly supported in Unity UI Toolkit
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

        public static T SetFontAsset<T>(this T element, FontAsset fontAsset) where T : VisualElement
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

        public static T SetTextColorWhite<T>(this T element) where T : VisualElement
        {
            element.style.color = Color.white;
            return element;
        }

        public static T SetTextColorBlack<T>(this T element) where T : VisualElement
        {
            element.style.color = Color.black;
            return element;
        }

        public static T SetTextColorGray<T>(this T element) where T : VisualElement
        {
            element.style.color = Color.gray;
            return element;
        }

        public static T SetTextColorRed<T>(this T element) where T : VisualElement
        {
            element.style.color = Color.red;
            return element;
        }

        public static T SetTextColorGreen<T>(this T element) where T : VisualElement
        {
            element.style.color = Color.green;
            return element;
        }

        public static T SetTextColorBlue<T>(this T element) where T : VisualElement
        {
            element.style.color = Color.blue;
            return element;
        }

        #endregion

        #region Utility Typography Methods

        public static T SetHeadingStyle<T>(this T textElement, int level = 1) where T : TextElement
        {
            float[] headingSizes = { 32f, 28f, 24f, 20f, 18f, 16f };
            int index = Mathf.Clamp(level - 1, 0, headingSizes.Length - 1);

            textElement.style.fontSize = headingSizes[index];
            textElement.style.unityFontStyleAndWeight = FontStyle.Bold;

            return textElement;
        }

        public static T SetBodyTextStyle<T>(this T textElement) where T : TextElement
        {
            textElement.style.fontSize = 16f;
            textElement.style.unityFontStyleAndWeight = FontStyle.Normal;
            textElement.style.whiteSpace = WhiteSpace.Normal;

            return textElement;
        }

        // public static T SetCaptionTextStyle<T>(
        
        #endregion
    }
}