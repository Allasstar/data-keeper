using System.Globalization;
using System.Text;
using UnityEngine;

namespace DataKeeper.Utility
{
    /// <summary>
    /// Fluent builder for TextMeshPro rich text tags.
    /// Usage: RichText.Text("Hello").Bold().Color(Color.red).ToString()
    ///
    /// Allocation notes:
    ///   • Every zero-arg wrap (Bold, Italic, …) uses string.Concat(3 strings) — one alloc: the result.
    ///   • Parameterised wraps (Color, Size, …) rent a StringBuilder from the shared pool,
    ///     build the string, release the builder — one alloc: the result.
    ///   • All tag literals are const / static-readonly so they live in the intern pool.
    /// </summary>
    public readonly struct RichText
    {
        private readonly string _text;

        private RichText(string text) => _text = text ?? string.Empty;

        // ── Entry points ──────────────────────────────────────────────────────

        public static RichText Text(string text) => new RichText(text);

        public static implicit operator string(RichText r) => r._text;
        public override string ToString() => _text;

        // ═════════════════════════════════════════════════════════════════════
        // Tag constants
        // ═════════════════════════════════════════════════════════════════════

        // Brackets
        private const string BracketSqOpen    = "[";
        private const string BracketSqClose   = "]";
        private const string BracketCuOpen    = "{";
        private const string BracketCuClose   = "}";
        private const string BracketParOpen   = "(";
        private const string BracketParClose  = ")";
        // angle-bracket wrappers use noparse to survive TMP parsing
        private const string AngleOpen        = "<noparse><</noparse>";
        private const string AngleClose       = "<noparse>></noparse>";

        // Paired tags — open
        private const string TagBoldOpen        = "<b>";
        private const string TagItalicOpen      = "<i>";
        private const string TagStrikeOpen      = "<s>";
        private const string TagUnderlineOpen   = "<u>";
        private const string TagSupOpen         = "<sup>";
        private const string TagSubOpen         = "<sub>";
        private const string TagSmallCapsOpen   = "<smallcaps>";
        private const string TagNoParseOpen     = "<noparse>";
        private const string TagAlignLeftOpen   = "<align=left>";
        private const string TagAlignRightOpen  = "<align=right>";
        private const string TagAlignCenterOpen = "<align=center>";
        private const string TagAlignJustOpen   = "<align=justified>";
        private const string TagAlignFlushOpen  = "<align=flush>";

        // Paired tags — close
        private const string TagBoldClose        = "</b>";
        private const string TagItalicClose      = "</i>";
        private const string TagStrikeClose      = "</s>";
        private const string TagUnderlineClose   = "</u>";
        private const string TagSupClose         = "</sup>";
        private const string TagSubClose         = "</sub>";
        private const string TagSmallCapsClose   = "</smallcaps>";
        private const string TagNoParseClose     = "</noparse>";
        private const string TagAlignClose       = "</align>";
        private const string TagColorClose       = "</color>";
        private const string TagSizeClose        = "</size>";
        private const string TagMarkClose        = "</mark>";
        private const string TagGradientClose    = "</gradient>";
        private const string TagCSpaceClose      = "</cspace>";
        private const string TagSpaceClose       = "</space>";
        private const string TagLineHeightClose  = "</line-height>";
        private const string TagIndentClose      = "</indent>";
        private const string TagVOffsetClose     = "</voffset>";
        private const string TagMarginClose      = "</margin>";
        private const string TagFontClose        = "</font>";
        private const string TagFontWeightClose  = "</font-weight>";
        private const string TagMaterialClose    = "</material>";
        private const string TagLinkClose        = "</link>";

        // Parameterised tag prefixes (value + ">" appended at runtime)
        private const string PfxColor      = "<color=";
        private const string PfxSize       = "<size=";
        private const string PfxMark       = "<mark=";
        private const string PfxGradient   = "<gradient=\"";
        private const string PfxColorB     = "\" colorb=\"";
        private const string PfxCSpace     = "<cspace=";
        private const string PfxSpace      = "<space=";
        private const string PfxLineHeight = "<line-height=";
        private const string PfxIndent     = "<indent=";
        private const string PfxVOffset    = "<voffset=";
        private const string PfxMargin     = "<margin=";
        private const string PfxFont       = "<font=";
        private const string PfxFontWeight = "<font-weight=";
        private const string PfxMaterial   = "<material=";
        private const string PfxLink       = "<link=";
        private const string PfxSpriteIdx  = "<sprite=";
        private const string PfxSpriteName = "<sprite name=\"";

        // Shared symbols
        private const string TagClose      = ">";   // generic ">"
        private const string QuoteClose    = "\">";  // ">
        private const string UnitEm        = "em";
        private const string UnitPct       = "%";
        private const string Hash          = "#";

        // ═════════════════════════════════════════════════════════════════════
        // Style — zero-allocation beyond the result string
        // ═════════════════════════════════════════════════════════════════════

        public RichText Bold()
            => new RichText(string.Concat(TagBoldOpen, _text, TagBoldClose));

        public RichText Italic()
            => new RichText(string.Concat(TagItalicOpen, _text, TagItalicClose));

        public RichText Strikethrough()
            => new RichText(string.Concat(TagStrikeOpen, _text, TagStrikeClose));

        public RichText Underline()
            => new RichText(string.Concat(TagUnderlineOpen, _text, TagUnderlineClose));

        public RichText Superscript()
            => new RichText(string.Concat(TagSupOpen, _text, TagSupClose));

        public RichText Subscript()
            => new RichText(string.Concat(TagSubOpen, _text, TagSubClose));

        public RichText SmallCaps()
            => new RichText(string.Concat(TagSmallCapsOpen, _text, TagSmallCapsClose));

        public RichText NoParseOff()
            => new RichText(string.Concat(TagNoParseOpen, _text, TagNoParseClose));

        // ── Brackets ─────────────────────────────────────────────────────────

        public RichText SquareBrackets() => new RichText(string.Concat(BracketSqOpen,  _text, BracketSqClose));
        public RichText CurlyBrackets()  => new RichText(string.Concat(BracketCuOpen,  _text, BracketCuClose));
        public RichText Parentheses()    => new RichText(string.Concat(BracketParOpen, _text, BracketParClose));
        public RichText AngleBrackets()  => new RichText(string.Concat(AngleOpen,      _text, AngleClose));

        // ── Color ─────────────────────────────────────────────────────────────

        public RichText Color(Color color)   => WrapParam(PfxColor, ColorToHex(color), TagColorClose);
        public RichText Color(string htmlColor) => WrapParam(PfxColor, htmlColor, TagColorClose);

        public RichText Mark(Color color)    => WrapParam(PfxMark, ColorToHex(color), TagMarkClose);

        public RichText GradientH(Color left, Color right)
        {
            // <gradient="LLLLLL" colorb="RRRRRR">text</gradient>
            var sb = StringBuilderPool.Get();
            sb.Append(PfxGradient)
              .Append(ColorToHex(left))
              .Append(PfxColorB)
              .Append(ColorToHex(right))
              .Append(QuoteClose)
              .Append(_text)
              .Append(TagGradientClose);
            return new RichText(StringBuilderPool.ReleaseAndGet(sb));
        }

        // ── Size ──────────────────────────────────────────────────────────────

        /// <param name="px">Absolute size in points.</param>
        public RichText Size(float px)
            => WrapParam(PfxSize, px.ToString(CultureInfo.InvariantCulture), TagSizeClose);

        /// <param name="percent">Size relative to the default, e.g. 120 for 120%.</param>
        public RichText SizePercent(float percent)
            => WrapParamSuffix(PfxSize, percent.ToString(CultureInfo.InvariantCulture), UnitPct, TagSizeClose);

        // ── Spacing ───────────────────────────────────────────────────────────

        public RichText CharSpacing(float em)
            => WrapParamSuffix(PfxCSpace, em.ToString(CultureInfo.InvariantCulture), UnitEm, TagCSpaceClose);

        public RichText WordSpacing(float em)
            => WrapParamSuffix(PfxSpace, em.ToString(CultureInfo.InvariantCulture), UnitEm, TagSpaceClose);

        public RichText LineHeight(float percent)
            => WrapParamSuffix(PfxLineHeight, percent.ToString(CultureInfo.InvariantCulture), UnitPct, TagLineHeightClose);

        // ── Alignment ─────────────────────────────────────────────────────────

        public RichText AlignLeft()    => new RichText(string.Concat(TagAlignLeftOpen,   _text, TagAlignClose));
        public RichText AlignRight()   => new RichText(string.Concat(TagAlignRightOpen,  _text, TagAlignClose));
        public RichText AlignCenter()  => new RichText(string.Concat(TagAlignCenterOpen, _text, TagAlignClose));
        public RichText AlignJustify() => new RichText(string.Concat(TagAlignJustOpen,   _text, TagAlignClose));
        public RichText AlignFlush()   => new RichText(string.Concat(TagAlignFlushOpen,  _text, TagAlignClose));

        // ── Positioning ───────────────────────────────────────────────────────

        public RichText Indent(float em)
            => WrapParamSuffix(PfxIndent, em.ToString(CultureInfo.InvariantCulture), UnitEm, TagIndentClose);

        public RichText Offset(float em)
            => WrapParamSuffix(PfxVOffset, em.ToString(CultureInfo.InvariantCulture), UnitEm, TagVOffsetClose);

        public RichText Margin(float em)
            => WrapParamSuffix(PfxMargin, em.ToString(CultureInfo.InvariantCulture), UnitEm, TagMarginClose);

        // ── Font / Material ───────────────────────────────────────────────────

        public RichText Font(string assetName)     => WrapParam(PfxFont, assetName, TagFontClose);
        public RichText Material(string materialName) => WrapParam(PfxMaterial, materialName, TagMaterialClose);

        public RichText FontWeight(int weight)
            => WrapParam(PfxFontWeight, weight.ToString(), TagFontWeightClose);

        public RichText Sprite(int index)
        {
            // <sprite=N>
            var sb = StringBuilderPool.Get();
            sb.Append(PfxSpriteIdx).Append(index).Append(TagClose);
            return new RichText(StringBuilderPool.ReleaseAndGet(sb));
        }

        public RichText Sprite(string name)
        {
            // <sprite name="NAME">
            var sb = StringBuilderPool.Get();
            sb.Append(PfxSpriteName).Append(name).Append(QuoteClose);
            return new RichText(StringBuilderPool.ReleaseAndGet(sb));
        }

        // ── Link ──────────────────────────────────────────────────────────────

        public RichText Link(string id) => WrapParam(PfxLink, id, TagLinkClose);

        // ── Static helpers (no chaining needed) ───────────────────────────────

        public static string SquareBrackets(string text) => string.Concat(BracketSqOpen,  text, BracketSqClose);
        public static string CurlyBrackets(string text)  => string.Concat(BracketCuOpen,  text, BracketCuClose);
        public static string Parentheses(string text)    => string.Concat(BracketParOpen, text, BracketParClose);
        public static string AngleBrackets(string text)  => string.Concat(AngleOpen,      text, AngleClose);

        public static string Bold(string text)   => string.Concat(TagBoldOpen,   text, TagBoldClose);
        public static string Italic(string text) => string.Concat(TagItalicOpen, text, TagItalicClose);

        public static string Color(string text, Color color)
            => string.Concat(PfxColor, ColorToHex(color), TagClose, text, TagColorClose);

        public static string Color(string text, string htmlColor)
            => string.Concat(PfxColor, htmlColor, TagClose, text, TagColorClose);

        public static string Size(string text, float px)
            => string.Concat(PfxSize, px.ToString(CultureInfo.InvariantCulture), TagClose, text, TagSizeClose);

        // ── Color utilities ───────────────────────────────────────────────────

        public static string TextToHexColor(string value) => ColorToHex(TextToColor(value));

        public static Color TextToColor(string value)
        {
            var hue = StableHash(value) / (float)uint.MaxValue;
            return UnityEngine.Color.HSVToRGB(hue, 0.6f, 1f);
        }
        
        // FNV-1a — deterministic everywhere
        public static uint StableHash(string s)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619u;
            }
            return hash;
        }

        public static string ColorToHex(Color color)
            => string.Concat(Hash, ColorUtility.ToHtmlStringRGB(color));

        // ═════════════════════════════════════════════════════════════════════
        // Private helpers
        // ═════════════════════════════════════════════════════════════════════

        // Builds:  <prefix=value>_text</close>
        private RichText WrapParam(string prefix, string value, string close)
        {
            var sb = StringBuilderPool.Get();
            sb.Append(prefix).Append(value).Append(TagClose)
              .Append(_text)
              .Append(close);
            return new RichText(StringBuilderPool.ReleaseAndGet(sb));
        }

        // Builds:  <prefix=valueSuffix>_text</close>  (e.g. <size=120%>)
        private RichText WrapParamSuffix(string prefix, string value, string suffix, string close)
        {
            var sb = StringBuilderPool.Get();
            sb.Append(prefix).Append(value).Append(suffix).Append(TagClose)
              .Append(_text)
              .Append(close);
            return new RichText(StringBuilderPool.ReleaseAndGet(sb));
        }

        // ── Minimal inline pool — swap for DataKeeper.StringBuilderPool if preferred ──

        private static class StringBuilderPool
        {
            // One cached instance is enough for the single-threaded Unity main thread.
            // The builder is never exposed outside a single method, so re-entrancy
            // only occurs if a caller nests RichText calls (they don't — each method
            // is leaf-level), making this safe without a full stack-based pool.
            [System.ThreadStatic]
            private static StringBuilder _cached;

            internal static StringBuilder Get()
            {
                var sb = _cached;
                if (sb == null)
                    return new StringBuilder(128);
                _cached = null;
                sb.Clear();
                return sb;
            }

            internal static string ReleaseAndGet(StringBuilder sb)
            {
                var result = sb.ToString();
                _cached = sb;   // return to pool
                return result;
            }
        }
    }
}