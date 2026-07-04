using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows
{
    public class ColorToolsWindow : EditorWindow
    {
        // ── Constants ─────────────────────────────────────────────────────

        private const float AA_NORMAL = 4.5f;
        private const float AA_LARGE = 3.0f;
        private const float AAA_NORMAL = 7.0f;
        private const float AAA_LARGE = 4.5f;

        private static readonly Color PassColor   = new Color(0.30f, 0.65f, 0.35f);
        private static readonly Color WarnColor   = new Color(0.80f, 0.55f, 0.15f);
        private static readonly Color FailColor   = new Color(0.75f, 0.30f, 0.30f);
        private static readonly Color PanelBg     = new Color(0f, 0f, 0f, 0.12f);
        private static readonly Color SwatchLine  = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color SourceOutline = new Color(0.35f, 0.65f, 1f);
        private static readonly Color HintText    = new Color(0.5f, 0.5f, 0.5f);

        private enum PreviewMode { Light, Dark, Custom }

        private enum PaletteScheme
        {
            Monochromatic,
            Analogous,
            Complementary,
            SplitComplementary,
            Triadic,
            Tetradic,
            Square
        }

        // ── Serialized state (survives domain reload) ─────────────────────

        [SerializeField] private Color _bgColor = Color.white;
        [SerializeField] private Color _fgColor = Color.black;
        [SerializeField] private PreviewMode _previewMode = PreviewMode.Light;
        [SerializeField] private Color _customPreview = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private bool _showSuggestions = true;

        [SerializeField] private Color _paletteBase = new Color(0.20f, 0.55f, 0.85f);
        [SerializeField] private PaletteScheme _paletteScheme = PaletteScheme.Complementary;
        [SerializeField] private bool _paletteShades = false;

        [SerializeField] private Color _hueSource = new Color(0.85f, 0.35f, 0.30f);
        [SerializeField] private float _hueStep = 30f;
        [SerializeField] private int _hueCount = 6;
        [SerializeField] private Color _hueTarget = new Color(0.30f, 0.55f, 0.85f);

        [SerializeField] private int _activeTab;

        // ── UI references ─────────────────────────────────────────────────

        private TabView _tabView;
        private Tab _contrastTab;

        private VisualElement _banner;
        private Label _bannerLabel;
        private ColorField _bgField;
        private ColorField _fgField;
        private ColorField _customPreviewField;
        private VisualElement _previewHost;
        private VisualElement _previewCard;
        private Label _sampleBig;
        private Label _sampleSmall;
        private Label _ratioLabel;
        private readonly List<(Label badge, float threshold)> _wcagBadges = new();
        private Foldout _suggestionsFoldout;
        private VisualElement _suggestionsContent;

        private Label _schemeDescription;
        private VisualElement _paletteContent;

        private VisualElement _hueCorrectedRow;
        private VisualElement _hueNaiveRow;
        private VisualElement _balanceRow;
        private VisualElement _saturationRow;

        [MenuItem("Tools/Windows/Color Tools", priority = 5)]
        public static void ShowWindow()
        {
            var window = GetWindow<ColorToolsWindow>();
            window.titleContent = new GUIContent("Color Tools",
                EditorGUIUtility.IconContent("d_ColorPicker.CycleColor").image);
            window.minSize = new Vector2(560, 460);
        }

        public void CreateGUI()
        {
            _tabView = new TabView().SetFlexGrow(1).SetChildOf(rootVisualElement);

            _contrastTab = new Tab("Contrast Checker");
            var paletteTab = new Tab("Palette Generator");
            var hueTab = new Tab("Hue Shifter");

            _tabView.Add(_contrastTab);
            _tabView.Add(paletteTab);
            _tabView.Add(hueTab);

            BuildContrastTab(_contrastTab);
            BuildPaletteTab(paletteTab);
            BuildHueTab(hueTab);

            var tabs = new[] { _contrastTab, paletteTab, hueTab };
            if (_activeTab > 0 && _activeTab < tabs.Length)
                _tabView.activeTab = tabs[_activeTab];

            _tabView.activeTabChanged += (_, tab) => _activeTab = Array.IndexOf(tabs, tab);

            RefreshContrast();
            RefreshPalette();
            RefreshHue();
        }

        // ══ Tab 1: Contrast Checker ═══════════════════════════════════════

        private void BuildContrastTab(Tab tab)
        {
            var root = new VisualElement().SetFlexGrow(1).SetPadding(10).SetChildOf(tab);

            _banner = new VisualElement()
                .SetHeight(30)
                .SetFlexShrink(0)
                .SetBorderRadius(4)
                .SetJustifyContent(Justify.Center)
                .SetChildOf(root);

            _bannerLabel = new Label()
                .SetFontStyle(FontStyle.Bold)
                .SetTextAlign(TextAnchor.MiddleCenter)
                .SetTextColor(Color.white)
                .SetChildOf(_banner);

            var row = new VisualElement().SetFlexRow().SetFlexShrink(0).SetMarginTop(10).SetChildOf(root);

            BuildContrastLeftColumn(row);
            BuildContrastRightColumn(row);

            _suggestionsFoldout = new Foldout { text = "Suggestions", value = _showSuggestions };
            _suggestionsFoldout.SetMarginTop(10).SetChildOf(root);
            _suggestionsFoldout.RegisterValueChangedCallback(e => _showSuggestions = e.newValue);

            var scroll = new ScrollView(ScrollViewMode.Vertical).SetFlexGrow(1);
            _suggestionsFoldout.Add(scroll);
            _suggestionsContent = scroll.contentContainer;
        }

        private void BuildContrastLeftColumn(VisualElement row)
        {
            var left = new VisualElement().SetWidth(230).SetFlexShrink(0).SetChildOf(row);

            SectionHeader("Colors", left);

            _bgField = new ColorField("Background") { value = _bgColor, showAlpha = false };
            _bgField.RegisterValueChangedCallback(e => { _bgColor = e.newValue; RefreshContrast(); });
            left.Add(CompactLabel(_bgField, 80));

            _fgField = new ColorField("Foreground") { value = _fgColor, showAlpha = false };
            _fgField.RegisterValueChangedCallback(e => { _fgColor = e.newValue; RefreshContrast(); });
            left.Add(CompactLabel(_fgField, 80));

            var buttons = new VisualElement().SetFlexRow().SetMarginTop(6).SetChildOf(left);
            new Button(SwapContrastColors) { text = "Swap" }.SetFlexGrow(1).SetChildOf(buttons);
            new Button(() => SetContrastColors(Color.white, Color.black)) { text = "Reset" }
                .SetFlexGrow(1).SetChildOf(buttons);

            SectionHeader("Preview Background", left).SetMarginTop(14);

            var modeField = new EnumField("Mode", _previewMode);
            modeField.RegisterValueChangedCallback(e =>
            {
                _previewMode = (PreviewMode)e.newValue;
                _customPreviewField.SetDisplay(_previewMode == PreviewMode.Custom ? DisplayStyle.Flex : DisplayStyle.None);
                RefreshContrast();
            });
            left.Add(CompactLabel(modeField, 80));

            _customPreviewField = new ColorField("Custom") { value = _customPreview, showAlpha = false };
            _customPreviewField.RegisterValueChangedCallback(e => { _customPreview = e.newValue; RefreshContrast(); });
            _customPreviewField.SetDisplay(_previewMode == PreviewMode.Custom ? DisplayStyle.Flex : DisplayStyle.None);
            left.Add(CompactLabel(_customPreviewField, 80));
        }

        private void BuildContrastRightColumn(VisualElement row)
        {
            var right = new VisualElement().SetFlexGrow(1).SetMarginLeft(14).SetChildOf(row);

            _previewHost = new VisualElement()
                .SetHeight(110)
                .SetBorderRadius(4)
                .SetTooltip("Click to swap foreground and background")
                .SetOnClick(SwapContrastColors)
                .SetChildOf(right);

            _previewCard = new VisualElement()
                .SetFlexGrow(1)
                .SetMargin(12)
                .SetBorderRadius(4)
                .SetJustifyContent(Justify.Center)
                .SetChildOf(_previewHost);
            _previewCard.pickingMode = PickingMode.Ignore;

            _sampleBig = new Label("Sample Text AaBbCc 123")
                .SetFontSize(16)
                .SetTextAlign(TextAnchor.MiddleCenter)
                .SetChildOf(_previewCard);
            _sampleBig.pickingMode = PickingMode.Ignore;

            _sampleSmall = new Label("The quick brown fox jumps over 0123456789")
                .SetFontSize(10)
                .SetTextAlign(TextAnchor.MiddleCenter)
                .SetChildOf(_previewCard);
            _sampleSmall.pickingMode = PickingMode.Ignore;

            _ratioLabel = new Label()
                .SetFontSize(22)
                .SetFontStyle(FontStyle.Bold)
                .SetTextAlign(TextAnchor.MiddleCenter)
                .SetMarginTop(8)
                .SetTooltip("Click to copy ratio")
                .SetOnClick(() => Copy(_ratioLabel.text))
                .SetChildOf(right);

            var grid = new VisualElement().SetFlexRow().SetFlexWrap(Wrap.Wrap).SetMarginTop(6).SetChildOf(right);

            AddWcagItem(grid, "AA Normal", "4.5:1 — body text", AA_NORMAL);
            AddWcagItem(grid, "AA Large", "3:1 — 18pt+ or 14pt bold", AA_LARGE);
            AddWcagItem(grid, "AAA Normal", "7:1 — enhanced body text", AAA_NORMAL);
            AddWcagItem(grid, "AAA Large", "4.5:1 — enhanced large text", AAA_LARGE);
        }

        private void AddWcagItem(VisualElement grid, string title, string tooltip, float threshold)
        {
            var item = new VisualElement()
                .SetFlexRow()
                .SetAlignItems(Align.Center)
                .SetWidth(50, LengthUnit.Percent)
                .SetPaddingTop(3)
                .SetPaddingBottom(3)
                .SetTooltip(tooltip);
            grid.Add(item);

            var badge = new Label()
                .SetWidth(44)
                .SetFontSize(10)
                .SetFontStyle(FontStyle.Bold)
                .SetTextAlign(TextAnchor.MiddleCenter)
                .SetTextColor(Color.white)
                .SetBorderRadius(3)
                .SetPaddingTop(2)
                .SetPaddingBottom(2)
                .SetChildOf(item);

            new Label(title).SetMarginLeft(6).SetChildOf(item);

            _wcagBadges.Add((badge, threshold));
        }

        private void SwapContrastColors() => SetContrastColors(_fgColor, _bgColor);

        private void SetContrastColors(Color bg, Color fg)
        {
            _bgColor = bg;
            _fgColor = fg;
            _bgField.SetValueWithoutNotify(bg);
            _fgField.SetValueWithoutNotify(fg);
            RefreshContrast();
        }

        private void RefreshContrast()
        {
            float contrast = CalculateContrast(_bgColor, _fgColor);

            if (contrast >= AAA_NORMAL)
            {
                _banner.SetBackgroundColor(PassColor);
                _bannerLabel.SetText($"Excellent Contrast — passes WCAG AAA ({contrast:F2}:1)");
            }
            else if (contrast >= AA_NORMAL)
            {
                _banner.SetBackgroundColor(PassColor);
                _bannerLabel.SetText($"Accessible Contrast — passes WCAG AA ({contrast:F2}:1)");
            }
            else if (contrast >= AA_LARGE)
            {
                _banner.SetBackgroundColor(WarnColor);
                _bannerLabel.SetText($"Large Text Only — passes AA Large ({contrast:F2}:1)");
            }
            else
            {
                _banner.SetBackgroundColor(FailColor);
                _bannerLabel.SetText($"Low Contrast ({contrast:F2}:1)");
            }

            Color previewBg = _previewMode switch
            {
                PreviewMode.Light => Color.white,
                PreviewMode.Dark => new Color(0.07f, 0.07f, 0.07f),
                _ => _customPreview
            };

            _previewHost.SetBackgroundColor(previewBg);
            _previewCard.SetBackgroundColor(_bgColor);
            _sampleBig.SetTextColor(_fgColor);
            _sampleSmall.SetTextColor(_fgColor);

            _ratioLabel.SetText($"{contrast:F2} : 1");

            foreach (var (badge, threshold) in _wcagBadges)
            {
                bool pass = contrast >= threshold;
                badge.SetText(pass ? "PASS" : "FAIL");
                badge.SetBackgroundColor(pass ? PassColor : FailColor);
            }

            RebuildSuggestions(contrast);
        }

        private void RebuildSuggestions(float contrast)
        {
            _suggestionsContent.Clear();

            if (contrast >= AA_NORMAL)
            {
                new Label("Current colors already meet WCAG AA — no changes needed.")
                    .SetTextColor(HintText)
                    .SetFontStyle(FontStyle.Italic)
                    .SetPadding(4)
                    .SetChildOf(_suggestionsContent);
                return;
            }

            AddSuggestionGroup("Background Suggestions (keeps hue, adjusts lightness)", true);
            AddSuggestionGroup("Foreground Suggestions (keeps hue, adjusts lightness)", false);
        }

        private void AddSuggestionGroup(string title, bool adjustBackground)
        {
            var suggestions = GenerateAccessibleVariants(
                adjustBackground ? _bgColor : _fgColor,
                adjustBackground ? _fgColor : _bgColor,
                adjustBackground);

            if (suggestions.Count == 0)
                return;

            SectionHeader(title, _suggestionsContent);

            var grid = new VisualElement().SetFlexRow().SetFlexWrap(Wrap.Wrap).SetMarginBottom(6);
            _suggestionsContent.Add(grid);

            foreach (var (color, ratio) in suggestions)
            {
                var c = color;
                MakeSwatch(c, 32, 32, $"#{Hex(c)} — {ratio:F2}:1 — click to apply", () =>
                {
                    if (adjustBackground) SetContrastColors(c, _fgColor);
                    else SetContrastColors(_bgColor, c);
                }).SetChildOf(grid);
            }
        }

        /// Lightness ladder in OKLCH at the color's own hue/chroma, keeping only AA-passing steps.
        private List<(Color color, float ratio)> GenerateAccessibleVariants(Color adjust, Color other, bool adjustIsBg)
        {
            ColorToOklch(adjust, out float baseL, out float c, out float h);

            var result = new List<(Color color, float ratio, float dist)>();

            for (float l = 0.05f; l <= 0.99f; l += 0.05f)
            {
                Color candidate = OklchToColor(l, c, h);
                float ratio = adjustIsBg
                    ? CalculateContrast(candidate, other)
                    : CalculateContrast(other, candidate);

                if (ratio >= AA_NORMAL)
                    result.Add((candidate, ratio, Mathf.Abs(l - baseL)));
            }

            return result
                .OrderBy(r => r.dist)
                .Take(12)
                .Select(r => (r.color, r.ratio))
                .ToList();
        }

        // ══ Tab 2: Palette Generator ══════════════════════════════════════

        private static readonly Dictionary<PaletteScheme, (float[] hues, string description)> Schemes = new()
        {
            { PaletteScheme.Monochromatic,      (new float[] { 0 }, "One hue, stepped through perceptual lightness.") },
            { PaletteScheme.Analogous,          (new float[] { -30, 0, 30 }, "Neighboring hues — calm, harmonious.") },
            { PaletteScheme.Complementary,      (new float[] { 0, 180 }, "Opposite hues — maximum hue contrast.") },
            { PaletteScheme.SplitComplementary, (new float[] { 0, 150, 210 }, "Base plus the two hues beside its complement.") },
            { PaletteScheme.Triadic,            (new float[] { 0, 120, 240 }, "Three evenly spaced hues — vibrant, balanced.") },
            { PaletteScheme.Tetradic,           (new float[] { 0, 60, 180, 240 }, "Two complementary pairs (rectangle).") },
            { PaletteScheme.Square,             (new float[] { 0, 90, 180, 270 }, "Four evenly spaced hues.") },
        };

        private void BuildPaletteTab(Tab tab)
        {
            var root = new VisualElement().SetFlexGrow(1).SetPadding(10).SetChildOf(tab);

            var controls = new VisualElement().SetFlexRow().SetFlexShrink(0).SetAlignItems(Align.Center).SetChildOf(root);

            var baseField = new ColorField("Base") { value = _paletteBase, showAlpha = false };
            CompactLabel(baseField).SetWidth(160).SetChildOf(controls);
            baseField.RegisterValueChangedCallback(e => { _paletteBase = e.newValue; RefreshPalette(); });

            var schemeField = new EnumField(_paletteScheme);
            schemeField.SetWidth(160).SetMarginLeft(12).SetChildOf(controls);
            schemeField.RegisterValueChangedCallback(e => { _paletteScheme = (PaletteScheme)e.newValue; RefreshPalette(); });

            var shadesToggle = new Toggle("Tints & Shades") { value = _paletteShades };
            CompactLabel(shadesToggle).SetMarginLeft(12).SetChildOf(controls);
            shadesToggle.RegisterValueChangedCallback(e => { _paletteShades = e.newValue; RefreshPalette(); });

            _schemeDescription = new Label()
                .SetTextColor(HintText)
                .SetFontStyle(FontStyle.Italic)
                .SetMarginTop(4)
                .SetFlexShrink(0)
                .SetChildOf(root);

            _paletteContent = new ScrollView(ScrollViewMode.Vertical)
                .SetFlexGrow(1)
                .SetMarginTop(8)
                .SetChildOf(root)
                .contentContainer;

            var footer = new VisualElement().SetFlexRow().SetFlexShrink(0).SetAlignItems(Align.Center).SetMarginTop(6).SetChildOf(root);
            new Button(CopyPaletteHex) { text = "Copy All Hex" }.SetChildOf(footer);
            new Label("Click a swatch to copy its hex • Right-click for more options")
                .SetTextColor(HintText)
                .SetFontSize(10)
                .SetMarginLeft(8)
                .SetChildOf(footer);
        }

        private void RefreshPalette()
        {
            _schemeDescription.SetText(Schemes[_paletteScheme].description);
            _paletteContent.Clear();

            var palette = GeneratePalette();

            SectionHeader("Palette", _paletteContent);
            var mainRow = new VisualElement().SetFlexRow().SetFlexWrap(Wrap.Wrap);
            _paletteContent.Add(mainRow);

            foreach (var color in palette)
                AddPaletteSwatch(mainRow, color, 56, 56);

            if (!_paletteShades)
                return;

            SectionHeader("Tints & Shades", _paletteContent).SetMarginTop(8);

            foreach (var color in palette)
            {
                var row = new VisualElement().SetFlexRow().SetMarginBottom(2);
                _paletteContent.Add(row);

                ColorToOklch(color, out float l, out float c, out float h);

                foreach (float offset in new[] { -0.24f, -0.12f, 0f, 0.12f, 0.24f })
                {
                    Color variant = OklchToColor(Mathf.Clamp(l + offset, 0.05f, 0.97f), c, h);
                    AddPaletteSwatch(row, variant, 44, 36);
                }
            }
        }

        private List<Color> GeneratePalette()
        {
            ColorToOklch(_paletteBase, out float l, out float c, out float h);

            var palette = new List<Color>();

            if (_paletteScheme == PaletteScheme.Monochromatic)
            {
                foreach (float step in new[] { 0.85f, 0.70f, 0.55f, 0.40f, 0.25f })
                    palette.Add(OklchToColor(step, c, h));
            }
            else
            {
                foreach (float offset in Schemes[_paletteScheme].hues)
                    palette.Add(offset == 0 ? _paletteBase : OklchToColor(l, c, h + offset));
            }

            return palette;
        }

        private void AddPaletteSwatch(VisualElement parent, Color color, float width, float height)
        {
            var item = new VisualElement()
                .SetAlignItems(Align.Center)
                .SetMarginRight(6)
                .SetMarginBottom(4)
                .SetChildOf(parent);

            string hex = Hex(color);

            var swatch = MakeSwatch(color, width, height, $"#{hex} — click to copy", () => Copy($"#{hex}"));
            swatch.SetChildOf(item);

            swatch.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Copy Hex", _ => Copy($"#{hex}"));
                evt.menu.AppendAction("Check Contrast/As Background", _ => SendToContrast(color, true));
                evt.menu.AppendAction("Check Contrast/As Foreground", _ => SendToContrast(color, false));
            }));

            new Label($"#{hex}").SetFontSize(9).SetTextColor(HintText).SetChildOf(item);
        }

        private void SendToContrast(Color color, bool asBackground)
        {
            if (asBackground) SetContrastColors(color, _fgColor);
            else SetContrastColors(_bgColor, color);

            _tabView.activeTab = _contrastTab;
        }

        private void CopyPaletteHex()
        {
            var palette = GeneratePalette();
            Copy(string.Join("\n", palette.Select(c => $"#{Hex(c)}")));
        }

        // ══ Tab 3: Hue Shifter (luminance-preserving) ═════════════════════

        private void BuildHueTab(Tab tab)
        {
            var root = new VisualElement().SetFlexGrow(1).SetPadding(10).SetChildOf(tab);

            var controls = new VisualElement().SetFlexRow().SetFlexShrink(0).SetAlignItems(Align.Center).SetChildOf(root);

            var sourceField = new ColorField("Source") { value = _hueSource, showAlpha = false };
            CompactLabel(sourceField).SetWidth(160).SetChildOf(controls);
            sourceField.RegisterValueChangedCallback(e => { _hueSource = e.newValue; RefreshHue(); });

            var stepSlider = new Slider("Step °", -180f, 180f) { value = _hueStep, showInputField = true };
            CompactLabel(stepSlider).SetFlexGrow(1).SetMarginLeft(12).SetChildOf(controls);
            stepSlider.RegisterValueChangedCallback(e => { _hueStep = e.newValue; RefreshHue(); });

            var countSlider = new SliderInt("Count", 1, 12) { value = _hueCount, showInputField = true };
            CompactLabel(countSlider).SetWidth(170).SetMarginLeft(12).SetChildOf(controls);
            countSlider.RegisterValueChangedCallback(e => { _hueCount = e.newValue; RefreshHue(); });

            new Label("Shifting only the hue changes perceived brightness — yellow looks far brighter than blue at the same HSV value. " +
                      "The first row compensates for this (OKLCH); the second shows the plain hue-only shift for comparison. Y = relative luminance.")
                .SetTextColor(HintText)
                .SetFontStyle(FontStyle.Italic)
                .SetTextWrapNormal()
                .SetFlexShrink(0)
                .SetMarginTop(6)
                .SetChildOf(root);

            var scroll = new ScrollView(ScrollViewMode.Vertical).SetFlexGrow(1).SetMarginTop(8).SetChildOf(root);

            SectionHeader("Brightness-corrected shift (use these)", scroll.contentContainer);
            _hueCorrectedRow = new VisualElement().SetFlexRow().SetFlexWrap(Wrap.Wrap);
            scroll.contentContainer.Add(_hueCorrectedRow);

            SectionHeader("Hue-only shift (for comparison — brightness drifts)", scroll.contentContainer).SetMarginTop(10);
            _hueNaiveRow = new VisualElement().SetFlexRow().SetFlexWrap(Wrap.Wrap);
            scroll.contentContainer.Add(_hueNaiveRow);

            BuildBalanceSection(scroll.contentContainer);
        }

        private void BuildBalanceSection(VisualElement parent)
        {
            SectionHeader("Balance a target color", parent).SetMarginTop(14);

            new Label("Pick any target color — it is rebalanced to the same perceived brightness as Source, keeping its own hue and saturation.")
                .SetTextColor(HintText)
                .SetFontStyle(FontStyle.Italic)
                .SetTextWrapNormal()
                .SetChildOf(parent);

            var targetField = new ColorField("Target") { value = _hueTarget, showAlpha = false };
            CompactLabel(targetField).SetWidth(160).SetMarginTop(4).SetChildOf(parent);
            targetField.RegisterValueChangedCallback(e => { _hueTarget = e.newValue; RefreshBalance(); });

            _balanceRow = new VisualElement().SetFlexRow().SetAlignItems(Align.Center).SetMarginTop(6);
            parent.Add(_balanceRow);

            SectionHeader("Saturation variations (same hue, balanced brightness)", parent).SetMarginTop(10);

            _saturationRow = new VisualElement().SetFlexRow().SetFlexWrap(Wrap.Wrap);
            parent.Add(_saturationRow);
        }

        private void RefreshHue()
        {
            _hueCorrectedRow.Clear();
            _hueNaiveRow.Clear();

            ColorToOklch(_hueSource, out float l, out float c, out float h);
            Color.RGBToHSV(_hueSource, out float hsvH, out float hsvS, out float hsvV);

            for (int i = 0; i <= _hueCount; i++)
            {
                float angle = _hueStep * i;

                Color corrected = OklchToColor(l, c, h + angle);
                AddHueSwatch(_hueCorrectedRow, corrected, i == 0);

                Color naive = Color.HSVToRGB(Mathf.Repeat(hsvH + angle / 360f, 1f), hsvS, hsvV);
                AddHueSwatch(_hueNaiveRow, naive, i == 0);
            }

            RefreshBalance();
        }

        private void RefreshBalance()
        {
            _balanceRow.Clear();

            ColorToOklch(_hueSource, out float sourceL, out _, out _);
            ColorToOklch(_hueTarget, out _, out float targetC, out float targetH);
            Color balanced = OklchToColor(sourceL, targetC, targetH);

            AddCaptionedSwatch(_balanceRow, "Source", _hueSource);
            AddCaptionedSwatch(_balanceRow, "Target", _hueTarget);

            new Label("→").SetFontSize(18).SetTextColor(HintText).SetMarginRight(10).SetChildOf(_balanceRow);

            AddCaptionedSwatch(_balanceRow, "Balanced", balanced);

            RefreshSaturationVariations(sourceL, targetC, targetH);
        }

        /// Chroma ramp from gray to the sRGB gamut limit at the balanced lightness and target hue.
        private void RefreshSaturationVariations(float l, float balancedC, float h)
        {
            _saturationRow.Clear();

            const int steps = 8;
            float maxC = MaxChroma(l, h);
            int highlight = maxC > 0f
                ? Mathf.RoundToInt(Mathf.Clamp01(balancedC / maxC) * (steps - 1))
                : 0;

            for (int i = 0; i < steps; i++)
            {
                Color variant = OklchToColor(l, maxC * i / (steps - 1), h);
                AddHueSwatch(_saturationRow, variant, i == highlight);
            }
        }

        private void AddCaptionedSwatch(VisualElement parent, string caption, Color color)
        {
            var item = new VisualElement()
                .SetAlignItems(Align.Center)
                .SetMarginRight(10)
                .SetChildOf(parent);

            new Label(caption).SetFontSize(10).SetTextColor(HintText).SetMarginBottom(2).SetChildOf(item);

            string hex = Hex(color);
            float luminance = GetRelativeLuminance(color);

            MakeSwatch(color, 64, 44, $"#{hex} — Y {luminance:F3} — click to copy", () => Copy($"#{hex}"))
                .SetChildOf(item);

            new Label($"#{hex}").SetFontSize(9).SetTextColor(HintText).SetChildOf(item);
            new Label($"Y {luminance:F2}").SetFontSize(9).SetTextColor(HintText).SetChildOf(item);
        }

        private void AddHueSwatch(VisualElement parent, Color color, bool isSource)
        {
            var item = new VisualElement()
                .SetAlignItems(Align.Center)
                .SetMarginRight(6)
                .SetMarginBottom(4)
                .SetChildOf(parent);

            string hex = Hex(color);
            float luminance = GetRelativeLuminance(color);

            var swatch = MakeSwatch(color, 52, 44, $"#{hex} — Y {luminance:F3} — click to copy", () => Copy($"#{hex}"));
            if (isSource)
            {
                swatch.SetBorderWidth(2).SetBorderColor(SourceOutline);
                swatch.SetOnPointerLeave(_ => swatch.SetBorderColor(SourceOutline));
            }
            swatch.SetChildOf(item);

            new Label($"#{hex}").SetFontSize(9).SetTextColor(HintText).SetChildOf(item);
            new Label($"Y {luminance:F2}").SetFontSize(9).SetTextColor(HintText).SetChildOf(item);
        }

        // ══ Shared UI helpers ═════════════════════════════════════════════

        /// Field labels default to a 150px min-width meant for inspector alignment,
        /// which visually detaches short labels from their control in toolbars.
        private static T CompactLabel<T>(T field, float width = 0) where T : VisualElement
        {
            var label = field.Q<Label>(className: "unity-base-field__label");
            if (label == null)
                return field;

            if (width > 0)
            {
                label.style.minWidth = width;
                label.style.width = width;
            }
            else
            {
                label.style.minWidth = StyleKeyword.Auto;
                label.style.width = StyleKeyword.Auto;
                label.SetMarginRight(6);
            }

            return field;
        }

        private static Label SectionHeader(string text, VisualElement parent)
        {
            return new Label(text)
                .SetFontStyle(FontStyle.Bold)
                .SetMarginBottom(4)
                .SetChildOf(parent);
        }

        private static VisualElement MakeSwatch(Color color, float width, float height, string tooltip, Action onClick)
        {
            var swatch = new VisualElement()
                .SetSize(width, height)
                .SetBackgroundColor(color)
                .SetBorderRadius(3)
                .SetBorderWidth(1)
                .SetBorderColor(SwatchLine)
                .SetTooltip(tooltip)
                .SetOnClick(onClick);

            swatch.SetOnPointerEnter(_ => swatch.SetBorderColor(new Color(1f, 1f, 1f, 0.8f)));
            swatch.SetOnPointerLeave(_ => swatch.SetBorderColor(SwatchLine));

            return swatch;
        }

        private void Copy(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent($"Copied {text.Split('\n')[0]}{(text.Contains('\n') ? " …" : "")}"), 0.7d);
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        // ══ Color math ════════════════════════════════════════════════════

        // WCAG 2.x contrast

        private static float CalculateContrast(Color bg, Color fg)
        {
            float bgL = GetRelativeLuminance(bg);
            float fgL = GetRelativeLuminance(fg);

            float lighter = Mathf.Max(bgL, fgL);
            float darker = Mathf.Min(bgL, fgL);

            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float GetRelativeLuminance(Color c)
        {
            float r = SrgbToLinear(c.r);
            float g = SrgbToLinear(c.g);
            float b = SrgbToLinear(c.b);

            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        private static float SrgbToLinear(float v)
            => v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);

        private static float LinearToSrgb(float v)
            => v <= 0.0031308f ? v * 12.92f : 1.055f * Mathf.Pow(v, 1f / 2.4f) - 0.055f;

        // OKLCH (Björn Ottosson's OKLab, polar form) — perceptually uniform,
        // so hue rotation at constant L keeps colors equally bright to the eye.

        private static void ColorToOklch(Color color, out float l, out float c, out float h)
        {
            float r = SrgbToLinear(color.r);
            float g = SrgbToLinear(color.g);
            float b = SrgbToLinear(color.b);

            float lm = Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
            float mm = Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
            float sm = Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);

            l = 0.2104542553f * lm + 0.7936177850f * mm - 0.0040720468f * sm;
            float a = 1.9779984951f * lm - 2.4285922050f * mm + 0.4505937099f * sm;
            float bb = 0.0259040371f * lm + 0.7827717662f * mm - 0.8086757660f * sm;

            c = Mathf.Sqrt(a * a + bb * bb);
            h = Mathf.Atan2(bb, a) * Mathf.Rad2Deg;
        }

        /// Converts back to sRGB; if the target is out of gamut, chroma is reduced
        /// (binary search) so lightness and hue are preserved exactly.
        private static Color OklchToColor(float l, float c, float h)
        {
            if (TryOklchToRgb(l, c, h, out Color color))
                return color;

            float lo = 0f, hi = c;

            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (TryOklchToRgb(l, mid, h, out color)) lo = mid;
                else hi = mid;
            }

            TryOklchToRgb(l, lo, h, out color);
            return color;
        }

        private static bool TryOklchToRgb(float l, float c, float h, out Color color)
        {
            float hr = h * Mathf.Deg2Rad;
            float a = c * Mathf.Cos(hr);
            float b = c * Mathf.Sin(hr);

            float lm = Cube(l + 0.3963377774f * a + 0.2158037573f * b);
            float mm = Cube(l - 0.1055613458f * a - 0.0638541728f * b);
            float sm = Cube(l - 0.0894841775f * a - 1.2914855480f * b);

            float r = +4.0767416621f * lm - 3.3077115913f * mm + 0.2309699292f * sm;
            float g = -1.2684380046f * lm + 2.6097574011f * mm - 0.3413193965f * sm;
            float bl = -0.0041960863f * lm - 0.7034186147f * mm + 1.7076147010f * sm;

            const float eps = 0.0005f;
            bool inGamut = r >= -eps && r <= 1f + eps
                        && g >= -eps && g <= 1f + eps
                        && bl >= -eps && bl <= 1f + eps;

            color = new Color(
                Mathf.Clamp01(LinearToSrgb(Mathf.Clamp01(r))),
                Mathf.Clamp01(LinearToSrgb(Mathf.Clamp01(g))),
                Mathf.Clamp01(LinearToSrgb(Mathf.Clamp01(bl))));

            return inGamut;
        }

        /// Largest chroma still inside the sRGB gamut at the given lightness and hue.
        private static float MaxChroma(float l, float h)
        {
            float lo = 0f, hi = 0.5f;

            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (TryOklchToRgb(l, mid, h, out _)) lo = mid;
                else hi = mid;
            }

            return lo;
        }

        private static float Cbrt(float v) => Mathf.Pow(v, 1f / 3f);

        private static float Cube(float v) => v * v * v;
    }
}
