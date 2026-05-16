using System.Globalization;
using UnityEngine;

namespace DataKeeper.Utility
{
    /// <summary>
    /// Fluent builder for TextMeshPro rich text tags.
    /// Usage: RichText.Text("Hello").Bold().Color(Color.red).ToString()
    /// </summary>
    public readonly struct RichText
    {
        private readonly string _text;

        private RichText(string text) => _text = text ?? string.Empty;

        // ── Entry points ─────────────────────────────────────────────────────

        public static RichText Text(string text) => new RichText(text);

        public static implicit operator string(RichText r) => r._text;
        public override string ToString() => _text;

        // ── Style ────────────────────────────────────────────────────────────

        public RichText Bold()
            => Wrap("b");

        public RichText Italic()
            => Wrap("i");

        public RichText Strikethrough()
            => Wrap("s");

        public RichText Underline()
            => Wrap("u");

        public RichText Superscript()
            => Wrap("sup");

        public RichText Subscript()
            => Wrap("sub");

        public RichText SmallCaps()
            => Wrap("smallcaps");

        public RichText Mark(Color color)
            => Wrap("mark", ColorToHex(color));
        
        // ── Brackets ─────────────────────────────────────────────────────────

        public RichText SquareBrackets() => new RichText($"[{_text}]");
        public RichText AngleBrackets()  => new RichText($"<noparse><</noparse>{_text}<noparse>></noparse>");
        public RichText CurlyBrackets()  => new RichText($"{{{_text}}}");
        public RichText Parentheses()    => new RichText($"({_text})");

        // ── Color ────────────────────────────────────────────────────────────

        public RichText Color(Color color)
            => Wrap("color", ColorToHex(color));

        public RichText Color(string htmlColor)
            => Wrap("color", htmlColor);

        public RichText GradientH(Color left, Color right)
        {
            string l = ColorToHex(left), r = ColorToHex(right);
            return Raw($"<gradient=\"{l}\" colorb=\"{r}\">{_text}</gradient>");
        }

        // ── Size ─────────────────────────────────────────────────────────────

        /// <param name="px">Absolute size in points.</param>
        public RichText Size(float px)
            => Wrap("size", $"{px.ToString(CultureInfo.InvariantCulture)}");

        /// <param name="percent">Size relative to the default, e.g. 120 for 120%.</param>
        public RichText SizePercent(float percent)
            => Wrap("size", $"{percent.ToString(CultureInfo.InvariantCulture)}%");

        // ── Spacing ──────────────────────────────────────────────────────────

        public RichText CharSpacing(float em)
            => Wrap("cspace", $"{em.ToString(CultureInfo.InvariantCulture)}em");

        public RichText WordSpacing(float em)
            => Wrap("space", $"{em.ToString(CultureInfo.InvariantCulture)}em");

        public RichText LineHeight(float percent)
            => Wrap("line-height", $"{percent.ToString(CultureInfo.InvariantCulture)}%");

        // ── Alignment ────────────────────────────────────────────────────────

        public RichText AlignLeft()     => Wrap("align", "left");
        public RichText AlignRight()    => Wrap("align", "right");
        public RichText AlignCenter()   => Wrap("align", "center");
        public RichText AlignJustify()  => Wrap("align", "justified");
        public RichText AlignFlush()    => Wrap("align", "flush");

        // ── Positioning ──────────────────────────────────────────────────────

        public RichText Indent(float em)
            => Wrap("indent", $"{em.ToString(CultureInfo.InvariantCulture)}em");

        public RichText Offset(float em)
            => Wrap("voffset", $"{em.ToString(CultureInfo.InvariantCulture)}em");

        public RichText Margin(float em)
            => Wrap("margin", $"{em.ToString(CultureInfo.InvariantCulture)}em");

        // ── Font / Material ──────────────────────────────────────────────────

        public RichText Font(string assetName)
            => Wrap("font", assetName);

        public RichText FontWeight(int weight)
            => Wrap("font-weight", weight.ToString());

        public RichText Material(string materialName)
            => Wrap("material", materialName);

        public RichText Sprite(int index)
            => Raw($"<sprite={index}>");

        public RichText Sprite(string name)
            => Raw($"<sprite name=\"{name}\">");

        // ── No-parse ─────────────────────────────────────────────────────────

        public RichText NoParseOff()
            => Wrap("noparse");

        // ── Link ─────────────────────────────────────────────────────────────

        public RichText Link(string id)
            => Wrap("link", id);

        // ── Static helpers (no chaining needed) ──────────────────────────────
        public static string SquareBrackets(string text) => $"[{text}]";
        public static string AngleBrackets(string text)  => $"<noparse><</noparse>{text}<noparse>></noparse>";
        public static string CurlyBrackets(string text)  => $"{{{text}}}";
        public static string Parentheses(string text)    => $"({text})";
        
        public static string Bold(string text)     => $"<b>{text}</b>";
        public static string Italic(string text)   => $"<i>{text}</i>";
        public static string Color(string text, Color color) => $"<color={ColorToHex(color)}>{text}</color>";
        public static string Color(string text, string htmlColor) => $"<color={htmlColor}>{text}</color>";
        public static string Size(string text, float px) => $"<size={px.ToString(CultureInfo.InvariantCulture)}>{text}</size>";
        
        public static string TextToHexColor(string value)
        {
            return ColorToHex(TextToColor(value));
        }
        
        public static Color TextToColor(string value)
        {
            var hue = (uint)value.GetHashCode() / (float)uint.MaxValue;
            return UnityEngine.Color.HSVToRGB(hue, 0.6f, 1f);
        }
        
        public static string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        // ── Internals ────────────────────────────────────────────────────────

        private RichText Wrap(string tag, string value = null)
        {
            string open  = value != null ? $"<{tag}={value}>" : $"<{tag}>";
            string close = $"</{tag}>";
            return new RichText($"{open}{_text}{close}");
        }

        private RichText Raw(string markup)
            => new RichText(markup);
    }
}