using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataKeeper.Forge;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Export;
using DataKeeper.Forge.Render;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    public class SfxForgeWindow : EditorWindow
    {
        private const double RenderIntervalMs = 30.0;
        private const long PlayheadIntervalMs = 16;
        private const int VariationCount = 8;
        private const string PreviewVolumeKey = "DataKeeper.Forge.PreviewVolume";
        private const string AutoplayKey = "DataKeeper.Forge.Autoplay";
        private const long AutoplayDelayMs = 300;
        private const float DefaultPreviewVolume = 0.8f;
        private const string IdleStatus = "Hover a control to see what it does. Right-click a knob or stepper to lock it. " +
                                          "R randomizes, M mutates, Space plays.";

        private enum Page
        {
            Sound,
            Fx,
            Mod,
            Export,
        }

        [SerializeField] private SfxRecipe _recipe;
        [SerializeField] private ExportSettings _export = new();
        [SerializeField] private string _presetFolder = ForgePresets.DefaultFolder;
        [SerializeField] private string _presetPath;
        [SerializeField] private Page _page;
        [SerializeField] private bool _leftCollapsed;
        [SerializeField] private float _trimStart;
        [SerializeField] private float _trimEnd = 1f;
        [SerializeField] private List<string> _variations = new();
        [SerializeField] private int _selectedVariation = -1;
        [SerializeField] private CurveTarget _curveTarget = CurveTarget.Amp;
        [SerializeField] private int _curveLayer;
        [SerializeField] private bool _curveLinked;
        [SerializeField] private bool _drawMode;
        [SerializeField] private bool _gridSnap;
        [SerializeField] private bool _harmonySnap = true;
        [SerializeField] private bool _compactStrips;
        [SerializeField] private ModSource _modTab = ModSource.Lfo;
        [SerializeField] private bool _envDrawMode;
        [SerializeField] private bool _envGridSnap;

        private readonly List<LayerStripElement> _strips = new();
        private readonly Dictionary<Page, Button> _pageTabs = new();
        private readonly Dictionary<Page, VisualElement> _pages = new();
        private readonly Dictionary<HarmonyMode, Button> _harmonyButtons = new();
        private readonly VariationThumbElement[] _thumbs = new VariationThumbElement[VariationCount];
        private readonly Dictionary<CurveTarget, Button> _curveTabs = new();
        private readonly List<(BindableElement Element, string Path)> _fxBindings = new();
        private readonly List<(ParamBoxElement Box, Func<FxChain, bool> IsOn)> _fxModules = new();
        private readonly List<string> _presetPaths = new();
        private readonly List<ModRouteElement> _routeElements = new();
        private readonly KnobElement[] _macroKnobs = new KnobElement[4];
        private static readonly string[] MacroNames = { nameof(Macros.Size), nameof(Macros.Energy), nameof(Macros.Tone), nameof(Macros.Motion) };
        private static readonly ModSource[] LfoSources = { ModSource.Lfo, ModSource.Lfo2, ModSource.Lfo3 };
        private static readonly string[] LfoPaths = { nameof(SfxRecipe.Lfo), nameof(SfxRecipe.Lfo2), nameof(SfxRecipe.Lfo3) };
        private static readonly ModSource[] RandomSources = { ModSource.Random, ModSource.Random2, ModSource.Random3 };
        private readonly LfoPanelElement[] _lfoPanels = new LfoPanelElement[3];
        private readonly RandomPanelElement[] _randomPanels = new RandomPanelElement[3];
        private readonly Dictionary<ModSource, Button> _modTabs = new();
        private readonly Dictionary<ModSource, VisualElement> _modPanels = new();

        private SfxRenderer _renderer;
        private SfxRenderer _thumbRenderer;
        private SfxAnalyzer _analyzer;
        private VariationGenerator _generator;
        private BatchExporter _batchExporter;
        private FxGraphBuilder _fxGraphBuilder;
        private ForgeModulation _modulation;
        private ModSourceBarElement _modBar;
        private SfxRecipe _scratch;
        private PreviewPlayer _player;
        private SerializedObject _serializedRecipe;
        private SerializedObject _serializedWindow;

        private ObjectField _recipeField;
        private StepperElement _category;
        private VisualElement _main;
        private VisualElement _leftColumn;
        private Button _leftToggle;
        private Label _placeholder;
        private UnsignedIntegerField _seed;
        private KnobElement _length;
        private PianoElement _piano;
        private Label _rootNoteLabel;
        private KnobElement _variationAmount;
        private KnobElement _coupling;
        private IntegerField _candidates;
        private WaveformElement _waveform;
        private VisualElement _readout;
        private Label _readoutLength;
        private Label _readoutPeak;
        private Label _readoutLoudness;
        private Label _readoutCentroid;
        private Label _readoutCrest;
        private Label _readoutRender;
        private Label _readoutWarnings;
        private Button _exportButton;
        private Button _presetName;
        private MeterElement _meter;
        private ParamBoxElement _envPanel;
        private CurveEditorElement _envEditor;
        private Label _routesTitle;
        private VisualElement _routeContainer;
        private VisualElement _peakTarget;
        private VisualElement _loudnessTarget;
        private VisualElement _stripContainer;
        private Button _addLayer;
        private VisualElement _tracker;
        private Label _statusName;
        private Label _statusText;
        private string _statusMessage = IdleStatus;
        private HelpOverlay _help;
        private IVisualElementScheduledItem _playheadUpdater;
        private IVisualElementScheduledItem _autoplayer;
        private bool _autoplay;
        private string _playedState;
        private CurveEditorElement _curveEditor;
        private Label _curveLayerLabel;
        private ToolbarToggle _harmonyToggle;
        private ToolbarToggle _curveLockToggle;
        private int _curveUndoGroup;
        private Label _fxTitle;
        private StepperElement _distortionMode;
        private KnobElement _driveKnob;
        private FxGraphElement _transientGraph;
        private FxGraphElement _distortionGraph;
        private FxGraphElement _compressorGraph;
        private FxGraphElement _delayGraph;
        private FxGraphElement _reverbGraph;
        private FxGraphElement _limiterGraph;

        private double _lastRenderTime = double.MinValue;
        private bool _renderPending;
        private int _trimFirstFrame;
        private uint _seedCounter;

        private static double NowMs => EditorApplication.timeSinceStartup * 1000.0;

        [MenuItem("Tools/Windows/SFX Forge", priority = 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<SfxForgeWindow>();
            window.titleContent = new GUIContent("SFX Forge", EditorGUIUtility.FindTexture("d_AudioSource Icon"));
            window.minSize = new Vector2(900, 560);
        }

        private void OnEnable()
        {
            _renderer = new SfxRenderer();
            _thumbRenderer = new SfxRenderer();
            _analyzer = new SfxAnalyzer();
            _generator = new VariationGenerator();
            _batchExporter = new BatchExporter(_generator);
            _fxGraphBuilder = new FxGraphBuilder();
            _player = new PreviewPlayer();
            _scratch = CreateInstance<SfxRecipe>();
            _scratch.hideFlags = HideFlags.HideAndDontSave;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        // Runs before domain reload, so native buffers and the hidden objects never leak.
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.projectChanged -= OnProjectChanged;
            _player?.Dispose();
            _renderer?.Dispose();
            _thumbRenderer?.Dispose();
            _analyzer?.Dispose();
            _generator?.Dispose();
            _batchExporter?.Dispose();
            _fxGraphBuilder?.Dispose();
            if (_scratch != null) DestroyImmediate(_scratch);
            _player = null;
            _renderer = null;
            _thumbRenderer = null;
            _analyzer = null;
            _generator = null;
            _batchExporter = null;
            _fxGraphBuilder = null;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            var style = ForgeAssets.LoadUss("SfxForgeWindow");
            if (style != null) root.styleSheets.Add(style);
            root.AddToClassList("forge-root");
            _serializedWindow = new SerializedObject(this);

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            // Clicking non-focusable controls (knobs, waveform) would otherwise leave nothing
            // focused, and the hotkeys would stop reaching the window.
            root.RegisterCallback<PointerUpEvent>(_ =>
            {
                if (root.focusController?.focusedElement == null) root.Focus();
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerOverEvent>(OnPointerOver);
            root.RegisterCallback<PointerLeaveEvent>(_ => ShowStatus());

            _modulation = new ForgeModulation(root, SetStatus, OnRoutesChanged);
            root.Add(BuildTopBar());

            _placeholder = new Label("Pick a recipe or click New to start.");
            _placeholder.AddToClassList("forge-placeholder");
            root.Add(_placeholder);

            _main = new VisualElement();
            _main.AddToClassList("forge-main");
            _main.Add(BuildLeftColumn());
            _main.Add(BuildCenterColumn());
            root.Add(_main);

            root.Add(BuildStatusBar());

            _help = new HelpOverlay();
            root.Add(_help);
            root.Add(_modBar.Ghost);

            _playheadUpdater = root.schedule.Execute(UpdatePlayhead).Every(PlayheadIntervalMs);
            _playheadUpdater.Pause();
            _autoplayer = root.schedule.Execute(Autoplay);
            _autoplayer.Pause();

            SetRecipe(_recipe, false);
            root.Focus();
        }

        private VisualElement BuildTopBar()
        {
            var bar = Bar("forge-top-bar");
            var topic = ForgeHelp.TopBar;

            _recipeField = new ObjectField { objectType = typeof(SfxRecipe), allowSceneObjects = false };
            _recipeField.AddToClassList("forge-recipe-field");
            _recipeField.RegisterValueChangedCallback(e => SetRecipe(e.newValue as SfxRecipe, true));
            bar.Add(Hint(_recipeField, topic, "Recipe"));
            bar.Add(Hint(MakeButton("New", CreateRecipe), topic, "New"));
            bar.Add(Spacer());

            _category = new StepperElement(SfxCategory.Impact);
            _category.AddToClassList("forge-category");
            _category.Changed += value => SelectCategory((SfxCategory)value);
            bar.Add(Hint(_category, topic, "Category"));

            var preset = new VisualElement();
            preset.AddToClassList(StepperElement.UssClassName);
            preset.AddToClassList("forge-preset");
            preset.Add(StepperArrow("<", () => StepPreset(-1)));
            _presetName = new Button(ShowPresetMenu) { text = ForgePresets.DisplayName(_presetPath), focusable = false };
            _presetName.AddToClassList("forge-preset__name");
            preset.Add(_presetName);
            preset.Add(StepperArrow(">", () => StepPreset(1)));
            bar.Add(Hint(preset, topic, "Preset"));
            bar.Add(Hint(IconButton(ForgeIcon.Save, SavePreset), topic, "Save Preset"));
            bar.Add(Spacer());

            bar.Add(Hint(IconButton(ForgeIcon.Undo, Undo.PerformUndo), topic, "Undo / Redo"));
            bar.Add(Hint(IconButton(ForgeIcon.Redo, Undo.PerformRedo), topic, "Undo / Redo"));
            bar.Add(Separator());
            bar.Add(Hint(IconButton(ForgeIcon.Play, Play, "forge-primary"), topic, "Play / Stop  (Space)"));
            bar.Add(Hint(IconButton(ForgeIcon.Stop, Stop), topic, "Play / Stop  (Space)"));
            bar.Add(Hint(BuildAutoplayToggle(), topic, "Auto"));
            bar.Add(Hint(BuildMeter(), topic, "Meter"));
            bar.Add(HelpButton(topic));
            return bar;
        }

        // The meter is also the preview volume fader. Monitoring only: the render, the meter
        // reading and exports stay at full level.
        private MeterElement BuildMeter()
        {
            _meter = new MeterElement
            {
                DefaultVolume = DefaultPreviewVolume,
                Volume = EditorPrefs.GetFloat(PreviewVolumeKey, DefaultPreviewVolume),
            };
            _player.Volume = PreviewGain(_meter.Volume);
            _meter.VolumeChanged += SetPreviewVolume;
            return _meter;
        }

        private void SetPreviewVolume(float position)
        {
            EditorPrefs.SetFloat(PreviewVolumeKey, position);
            var gain = PreviewGain(position);
            _player.Volume = gain;
            SetStatus(gain > 0f ? $"Preview volume {AudioMath.LinearToDb(gain):0.0} dB" : "Preview muted");
        }

        // Squared so the fader travel feels even to the ear instead of crowding at the top.
        private static float PreviewGain(float position) => position * position;

        private ToolbarToggle BuildAutoplayToggle()
        {
            _autoplay = EditorPrefs.GetBool(AutoplayKey, true);
            var toggle = new ToolbarToggle { value = _autoplay, focusable = false };
            toggle.AddToClassList("forge-curve-toggle");
            toggle.AddToClassList("forge-icon-toggle");
            toggle.Add(new IconElement(ForgeIcon.Autoplay));
            toggle.RegisterValueChangedCallback(e =>
            {
                _autoplay = e.newValue;
                EditorPrefs.SetBool(AutoplayKey, _autoplay);
                if (!_autoplay) _autoplayer.Pause();
            });
            return toggle;
        }

        private VisualElement BuildLeftColumn()
        {
            var outer = Column("forge-left");
            _leftColumn = outer;
            var column = new ScrollView(ScrollViewMode.Vertical);
            column.AddToClassList("forge-left__scroll");
            outer.Add(column);

            var randomizer = ForgeHelp.Randomizer;
            column.Add(SectionHeader("Randomizer", randomizer));
            var generate = new VisualElement();
            generate.AddToClassList("forge-generate");
            generate.Add(Hint(MakeButton("Randomize", () => GenerateVariations(false), "forge-primary"), randomizer, "Randomize  (R)"));
            generate.Add(Hint(MakeButton("Mutate", () => GenerateVariations(true)), randomizer, "Mutate  (M)"));
            column.Add(generate);
            column.Add(SubTitle("Harmony"));
            var pills = new VisualElement();
            pills.AddToClassList("forge-pills");
            foreach (HarmonyMode mode in Enum.GetValues(typeof(HarmonyMode)))
            {
                var pill = MakeButton(mode.ToString(), () => SetRecipeInt("Randomizer.Harmony", (int)mode));
                pill.AddToClassList("forge-pill");
                _harmonyButtons[mode] = pill;
                pills.Add(pill);
            }
            column.Add(Hint(pills, randomizer, "Harmony"));

            var knobs = new VisualElement();
            knobs.AddToClassList("forge-randomizer-knobs");
            _variationAmount = new KnobElement("Variation", 0f, 1f, 0.3f, KnobFormat.Percent);
            _coupling = new KnobElement("Physics", 0f, 1f, 0.7f, KnobFormat.Percent);
            knobs.Add(Hint(_variationAmount, randomizer, "Variation"));
            knobs.Add(Hint(_coupling, randomizer, "Physics"));
            column.Add(knobs);

            _candidates = new IntegerField("Candidates");
            _candidates.AddToClassList("forge-candidates");
            column.Add(Hint(_candidates, randomizer, "Candidates"));

            var variations = ForgeHelp.Variations;
            column.Add(SectionHeader("Variations", variations));
            var grid = new VisualElement();
            grid.AddToClassList("forge-variation-grid");
            for (var i = 0; i < VariationCount; i++)
            {
                var thumb = new VariationThumbElement();
                thumb.SetIndex(i);
                thumb.ClearSamples();
                thumb.Clicked += OnThumbClicked;
                _thumbs[i] = thumb;
                grid.Add(Hint(thumb, variations, "Thumbnail"));
            }
            column.Add(grid);

            _seed = new UnsignedIntegerField("Seed");
            _seed.AddToClassList("forge-seed");
            column.Add(Hint(_seed, variations, "Seed"));
            return outer;
        }

        private VisualElement BuildCenterColumn()
        {
            var column = Column("forge-center");

            var tabs = Bar("forge-page-bar");
            column.Add(tabs);
            _leftToggle = MakeButton(string.Empty, () => SetLeftCollapsed(!_leftCollapsed), "forge-left-toggle");
            tabs.Add(Hint(_leftToggle, ForgeHelp.Variations, "«  »"));
            SetLeftCollapsed(_leftCollapsed);
            _modBar = new ModSourceBarElement(_modulation, SetStatus);
            _modBar.Clicked += OnSourceClicked;
            column.Add(_modBar);
            AddPage(column, tabs, Page.Sound, "Sound", BuildSoundPage());
            AddPage(column, tabs, Page.Fx, "FX", BuildFxPage());
            AddPage(column, tabs, Page.Mod, "Mod", BuildModPage());
            AddPage(column, tabs, Page.Export, "Export", BuildExportPage());
            ShowPage(_page);
            return column;
        }

        private void AddPage(VisualElement column, VisualElement tabs, Page page, string title, VisualElement content)
        {
            var tab = MakeButton(title, () => ShowPage(page), "forge-page-tab");
            _pageTabs[page] = tab;
            tabs.Add(tab);

            content.AddToClassList("forge-page");
            _pages[page] = content;
            column.Add(content);
        }

        private void SetLeftCollapsed(bool collapsed)
        {
            _leftCollapsed = collapsed;
            _leftColumn.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            _leftToggle.text = collapsed ? "»" : "«";
        }

        private void ShowPage(Page page)
        {
            _page = page;
            foreach (var pair in _pages) pair.Value.style.display = pair.Key == page ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var pair in _pageTabs) pair.Value.EnableInClassList("forge-page-tab--selected", pair.Key == page);
            if (page == Page.Fx) RefreshFxGraphs();
        }

        private static ScrollView ScrollPage()
        {
            var page = new ScrollView(ScrollViewMode.Vertical);
            page.AddToClassList("forge-page--scroll");
            return page;
        }

        private VisualElement BuildSoundPage()
        {
            var page = new VisualElement();

            var globals = Bar("forge-globals");
            _length = new KnobElement("Length", SfxRecipe.MinLengthMs, SfxRecipe.MaxLengthMs, 500f,
                KnobFormat.Milliseconds, KnobScale.Log) { Lockable = true, ModTarget = ModTarget.Length };
            _length.AddToClassList("forge-knob--inline");
            _length.LockToggled += locked => SetRecipeBool("Randomizer.LockLength", locked);
            globals.Add(Hint(_length, ForgeHelp.Waveform, "Length knob"));
            _piano = new PianoElement();
            _piano.NoteClicked += SetRootNote;
            globals.Add(Hint(_piano, ForgeHelp.Waveform, "Root note"));
            _rootNoteLabel = new Label();
            _rootNoteLabel.AddToClassList("forge-root-note");
            globals.Add(Hint(_rootNoteLabel, ForgeHelp.Waveform, "Root note"));
            globals.Add(Spacer());
            globals.Add(HelpButton(ForgeHelp.Waveform));
            page.Add(globals);

            var wavePanel = new VisualElement();
            wavePanel.AddToClassList("forge-wave-panel");
            _waveform = new WaveformElement();
            _waveform.SetTrim(_trimStart, _trimEnd);
            _waveform.TrimChanged += OnTrimChanged;
            wavePanel.Add(Hint(_waveform, ForgeHelp.Waveform, "Waveform"));
            wavePanel.Add(BuildReadout());
            page.Add(wavePanel);
            page.Add(BuildCurvePanel());

            var layersHeader = SectionHeader("Layers", ForgeHelp.Layers);
            layersHeader.AddToClassList("forge-layers-header");
            layersHeader.Insert(layersHeader.childCount - 1, BuildCompactToggle());
            page.Add(layersHeader);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("forge-layers");
            _stripContainer = new VisualElement();
            scroll.Add(_stripContainer);
            _addLayer = MakeButton("+ Add Layer", AddLayer);
            _addLayer.AddToClassList("forge-add-layer");
            scroll.Add(_addLayer);
            page.Add(scroll);
            return page;
        }

        private ToolbarToggle BuildCompactToggle()
        {
            var toggle = new ToolbarToggle { text = "Compact", value = _compactStrips, focusable = false };
            toggle.AddToClassList("forge-curve-toggle");
            toggle.RegisterValueChangedCallback(e =>
            {
                _compactStrips = e.newValue;
                foreach (var strip in _strips) strip.Compact = _compactStrips;
            });
            return Hint(toggle, ForgeHelp.Layers, "Compact");
        }

        private VisualElement BuildCurvePanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("forge-curve-panel");

            var bar = Bar("forge-curve-bar");
            AddCurveTab(bar, CurveTarget.Pitch, "Pitch");
            AddCurveTab(bar, CurveTarget.Cutoff, "Filter");
            AddCurveTab(bar, CurveTarget.Amp, "Amp");
            AddCurveTab(bar, CurveTarget.Pan, "Pan");

            _curveLayerLabel = new Label();
            _curveLayerLabel.AddToClassList("forge-curve-layer");
            bar.Add(_curveLayerLabel);
            bar.Add(CurveToggle("Link Layers", _curveLinked, value => _curveLinked = value));

            bar.Add(Spacer());
            bar.Add(CurveToggle("Draw", _drawMode, value => _drawMode = value));
            bar.Add(CurveToggle("Grid", _gridSnap, value => _gridSnap = value));
            _harmonyToggle = CurveToggle("Harmony", _harmonySnap, value => _harmonySnap = value);
            bar.Add(_harmonyToggle);
            _curveLockToggle = CurveToggle("Lock", false, SetCurveLocked);
            bar.Add(_curveLockToggle);
            bar.Add(Hint(MakeButton("Reset", ResetCurve), ForgeHelp.Curves, "Reset"));
            bar.Add(HelpButton(ForgeHelp.Curves));
            panel.Add(bar);

            _curveEditor = new CurveEditorElement();
            _curveEditor.EditStarted += OnCurveEditStarted;
            _curveEditor.Changed += OnCurveChanged;
            _curveEditor.EditFinished += OnCurveEditFinished;
            panel.Add(Hint(_curveEditor, ForgeHelp.Curves, "Mouse"));
            return panel;
        }

        private void AddCurveTab(VisualElement bar, CurveTarget target, string label)
        {
            var tab = MakeButton(label, () =>
            {
                _curveTarget = target;
                RefreshCurveEditor();
            });
            tab.AddToClassList("forge-tab");
            _curveTabs[target] = tab;
            bar.Add(Hint(tab, ForgeHelp.Curves, label));
        }

        private ToolbarToggle CurveToggle(string text, bool initial, Action<bool> onChanged)
        {
            var toggle = new ToolbarToggle { text = text, value = initial, focusable = false };
            toggle.AddToClassList("forge-curve-toggle");
            toggle.RegisterValueChangedCallback(e =>
            {
                onChanged(e.newValue);
                RefreshCurveEditor();
            });
            return Hint(toggle, ForgeHelp.Curves, text);
        }

        private VisualElement BuildModPage()
        {
            var page = ScrollPage();
            var topic = ForgeHelp.Modulation;
            page.Add(SectionHeader("Macros and sources", topic));

            var row = new VisualElement();
            row.AddToClassList("forge-mod-row");
            var macros = new ParamBoxElement("MACROS");
            for (var i = 0; i < _macroKnobs.Length; i++)
            {
                _macroKnobs[i] = new KnobElement(MacroNames[i], 0f, 1f, 0.5f, KnobFormat.Percent);
                macros.Add(Hint(_macroKnobs[i], topic, MacroNames[i]));
            }
            row.Add(macros);
            page.Add(row);
            page.Add(BuildModSources());

            var routesHeader = new VisualElement();
            routesHeader.AddToClassList("forge-section-header");
            routesHeader.AddToClassList("forge-routes-header");
            _routesTitle = SectionTitle("Routes");
            routesHeader.Add(_routesTitle);
            routesHeader.Add(Spacer());
            routesHeader.Add(Hint(MakeButton("+ Route", AddRoute), topic, "+ Route / Defaults"));
            routesHeader.Add(Hint(MakeButton("Defaults", ResetRoutes), topic, "+ Route / Defaults"));
            page.Add(routesHeader);

            _routeContainer = new VisualElement();
            _routeContainer.AddToClassList("forge-routes");
            page.Add(Hint(_routeContainer, topic, "Routes"));
            return page;
        }

        // Vital's left column: a tab per continuous source beside the selected source's panel.
        private VisualElement BuildModSources()
        {
            var sources = new VisualElement();
            sources.AddToClassList("forge-mod-sources");
            var tabs = new VisualElement();
            tabs.AddToClassList("forge-mod-tabs");
            sources.Add(tabs);
            var panels = new VisualElement();
            panels.AddToClassList("forge-mod-panels");
            sources.Add(panels);

            for (var i = 0; i < _lfoPanels.Length; i++)
            {
                var source = LfoSources[i];
                // LFO 1's Rate stays a drop target: the LfoRate route target acts on LFO 1 only.
                _lfoPanels[i] = new LfoPanelElement(TabName(source), ForgeModulation.SourceColor(source),
                    i == 0 ? ModTarget.LfoRate : (ModTarget?)null);
                AddModTab(tabs, panels, source, _lfoPanels[i], "LFO");
            }

            AddModTab(tabs, panels, ModSource.Envelope, BuildEnv1Panel(), "ENV 1");
            _envPanel = BuildEnvPanel();
            AddModTab(tabs, panels, ModSource.Env2, _envPanel, "ENV 2 / 3");
            AddModTab(tabs, panels, ModSource.Env3, _envPanel, "ENV 2 / 3");
            for (var i = 0; i < _randomPanels.Length; i++)
            {
                var source = RandomSources[i];
                _randomPanels[i] = new RandomPanelElement(TabName(source), ForgeModulation.SourceColor(source), source);
                AddModTab(tabs, panels, source, _randomPanels[i], "RND");
            }

            ShowModTab(_modTab);
            return sources;
        }

        private static string TabName(ModSource source) => ForgeModulation.SourceName(source).ToUpperInvariant();

        private void AddModTab(VisualElement tabs, VisualElement panels, ModSource source, VisualElement panel, string hint)
        {
            var tab = MakeButton(TabName(source), () => ShowModTab(source), "forge-mod-tab");
            tab.style.borderLeftColor = ForgeModulation.SourceColor(source);
            _modTabs[source] = tab;
            tabs.Add(Hint(tab, ForgeHelp.Modulation, hint));

            _modPanels[source] = panel;
            if (panel.parent == null) panels.Add(Hint(panel, ForgeHelp.Modulation, hint));
        }

        private VisualElement BuildEnv1Panel()
        {
            var box = new ParamBoxElement("ENV 1");
            box.AddToClassList(LfoPanelElement.PanelClassName);
            var text = new Label("Env 1 is each layer's own Amp curve, so a route from it follows every layer's " +
                                 "volume envelope. Edit it as the Amp curve on the Sound page.");
            text.AddToClassList(LfoPanelElement.PanelClassName + "__text");
            box.Add(text);
            box.Add(MakeButton("Edit Amp Curve", EditAmpCurve));
            return box;
        }

        private ParamBoxElement BuildEnvPanel()
        {
            var box = new ParamBoxElement();
            box.AddToClassList(LfoPanelElement.PanelClassName);
            box.AddToClassList(ParamBoxElement.UssClassName + "--stacked");

            var bar = new VisualElement();
            bar.AddToClassList(LfoPanelElement.PanelClassName + "__bar");
            bar.Add(CurveToggle("Draw", _envDrawMode, value =>
            {
                _envDrawMode = value;
                RefreshEnvEditor();
            }));
            bar.Add(CurveToggle("Grid", _envGridSnap, value =>
            {
                _envGridSnap = value;
                RefreshEnvEditor();
            }));
            bar.Add(Hint(MakeButton("Reset", ResetEnvCurve), ForgeHelp.Curves, "Reset"));
            box.Add(bar);

            _envEditor = new CurveEditorElement();
            _envEditor.AddToClassList(LfoPanelElement.PanelClassName + "__curve");
            _envEditor.EditStarted += OnCurveEditStarted;
            _envEditor.Changed += OnEnvCurveChanged;
            _envEditor.EditFinished += OnEnvCurveEditFinished;
            box.Add(Hint(_envEditor, ForgeHelp.Curves, "Mouse"));
            return box;
        }

        private void ShowModTab(ModSource source)
        {
            if (!_modPanels.ContainsKey(source)) source = ModSource.Lfo;
            _modTab = source;
            foreach (var pair in _modPanels) pair.Value.style.display = DisplayStyle.None;
            _modPanels[source].style.display = DisplayStyle.Flex;
            foreach (var pair in _modTabs) pair.Value.EnableInClassList("forge-mod-tab--selected", pair.Key == source);
            _envPanel.Title = TabName(source == ModSource.Env3 ? ModSource.Env3 : ModSource.Env2);
            RefreshEnvEditor();
        }

        // A chip click without a drag opens that source's panel, if it has one.
        private void OnSourceClicked(ModSource source)
        {
            if (_modPanels.ContainsKey(source)) ShowModTab(source);
        }

        private void EditAmpCurve()
        {
            _curveTarget = CurveTarget.Amp;
            ShowPage(Page.Sound);
            RefreshCurveEditor();
        }

        // Rows refresh here too: the Rnd modes decide whether a row's route is supported.
        private void RefreshModPanels()
        {
            _lfoPanels[0].Show(_recipe.Lfo);
            _lfoPanels[1].Show(_recipe.Lfo2);
            _lfoPanels[2].Show(_recipe.Lfo3);
            foreach (var panel in _randomPanels) panel.Show(_recipe);
            RefreshEnvEditor();

            // A variation can bring a different route count, which would leave rows on stale
            // array elements until the tracker catches up.
            if (_routeElements.Count != _recipe.Routes.Count) RebuildRoutes();
            else foreach (var route in _routeElements) route.Refresh();
        }

        private VisualElement BuildFxPage()
        {
            var page = ScrollPage();
            var topic = ForgeHelp.Fx;

            var header = new VisualElement();
            header.AddToClassList("forge-section-header");
            _fxTitle = SectionTitle("FX");
            header.Add(_fxTitle);
            header.Add(Spacer());
            header.Add(Hint(BindFx(FxToggle("Lock"), "Randomizer.LockFx"), topic, "Lock"));
            header.Add(HelpButton(topic));
            page.Add(header);

            // Stacked in the order the chain runs, so the page itself shows the order (FUI-O1).
            var transient = FxModule(page, "TRANSIENT", "Transient", "Fx.Transient", fx => fx.Transient.Enabled, out _transientGraph);
            transient.Add(FxKnob(new KnobElement("Attack", -1f, 1f, 0f, KnobFormat.Percent, bipolar: true)
                { ModTarget = ModTarget.TransientAttack }, "Fx.Transient.Attack"));
            transient.Add(FxKnob(new KnobElement("Sustain", -1f, 1f, 0f, KnobFormat.Percent, bipolar: true), "Fx.Transient.Sustain"));

            var distortion = FxModule(page, "DISTORTION", "Distortion", "Fx.Distortion", fx => fx.Distortion.Enabled, out _distortionGraph);
            _distortionMode = new StepperElement(DistortionMode.Tanh);
            _distortionMode.AddToClassList("forge-fx__mode");
            distortion.Add(_distortionMode);
            _driveKnob = FxKnob(new KnobElement("Drive", 0f, DistortionSettings.MaxDriveDb, 6f, KnobFormat.Decibels)
                { ModTarget = ModTarget.Drive }, "Fx.Distortion.DriveDb");
            distortion.Add(_driveKnob);
            distortion.Add(FxKnob(new KnobElement("Mix", 0f, 1f, 1f, KnobFormat.Percent), "Fx.Distortion.Mix"));

            var compressor = FxModule(page, "COMPRESSOR", "Compressor", "Fx.Compressor", fx => fx.Compressor.Enabled, out _compressorGraph);
            compressor.Add(FxKnob(new KnobElement("Depth", 0f, 1f, 1f, KnobFormat.Percent)
                { ModTarget = ModTarget.CompressorDepth }, "Fx.Compressor.Depth"));
            compressor.Add(FxKnob(new KnobElement("Time", 0f, 1f, 0.5f, KnobFormat.Percent), "Fx.Compressor.Time"));
            compressor.Add(FxKnob(new KnobElement("Upward", 0f, 1f, 1f, KnobFormat.Percent), "Fx.Compressor.Upward"));
            compressor.Add(FxKnob(new KnobElement("Downward", 0f, 1f, 1f, KnobFormat.Percent), "Fx.Compressor.Downward"));
            compressor.Add(FxKnob(new KnobElement("Gain", -CompressorSettings.MaxGainDb, CompressorSettings.MaxGainDb, 0f,
                KnobFormat.Decibels, bipolar: true), "Fx.Compressor.GainDb"));

            var delay = FxModule(page, "DELAY", "Delay", "Fx.Delay", fx => fx.Delay.Enabled, out _delayGraph);
            delay.Add(FxKnob(new KnobElement("Time", DelaySettings.MinTimeMs, DelaySettings.MaxTimeMs, 180f,
                KnobFormat.Milliseconds, KnobScale.Log), "Fx.Delay.TimeMs"));
            delay.Add(FxKnob(new KnobElement("Feedback", 0f, DelaySettings.MaxFeedback, 0.35f, KnobFormat.Percent), "Fx.Delay.Feedback"));
            delay.Add(FxKnob(new KnobElement("Mix", 0f, 1f, 0.25f, KnobFormat.Percent)
                { ModTarget = ModTarget.DelayMix }, "Fx.Delay.Mix"));
            delay.Add(BindFx(FxToggle("Ping-Pong"), "Fx.Delay.PingPong"));

            var reverb = FxModule(page, "REVERB", "Reverb", "Fx.Reverb", fx => fx.Reverb.Enabled, out _reverbGraph);
            reverb.Add(FxKnob(new KnobElement("Size", 0f, 1f, 0.5f, KnobFormat.Percent), "Fx.Reverb.Size"));
            reverb.Add(FxKnob(new KnobElement("Damping", 0f, 1f, 0.5f, KnobFormat.Percent), "Fx.Reverb.Damping"));
            reverb.Add(FxKnob(new KnobElement("Mix", 0f, 1f, 0.2f, KnobFormat.Percent)
                { ModTarget = ModTarget.ReverbMix }, "Fx.Reverb.Mix"));

            var limiter = FxModule(page, "LIMITER", "Limiter", "Fx.Limiter", fx => fx.Limiter.Enabled, out _limiterGraph);
            limiter.Add(FxKnob(new KnobElement("Ceiling", LimiterSettings.MinCeilingDb, 0f, -1f, KnobFormat.Decibels), "Fx.Limiter.CeilingDb"));
            limiter.Add(FxKnob(new KnobElement("Release", LimiterSettings.MinReleaseMs, LimiterSettings.MaxReleaseMs, 60f,
                KnobFormat.Milliseconds, KnobScale.Log), "Fx.Limiter.ReleaseMs"));

            return page;
        }

        // Returns the row that holds the effect's controls, to the right of its graph.
        private VisualElement FxModule(VisualElement page, string title, string hint, string path,
            Func<FxChain, bool> isOn, out FxGraphElement graph)
        {
            var box = new ParamBoxElement(title);
            box.AddToClassList("forge-fx");
            BindFx(box.AddPower(), path + ".Enabled");

            graph = new FxGraphElement();
            box.Add(graph);

            var controls = new VisualElement();
            controls.AddToClassList("forge-fx__controls");
            box.Add(controls);

            page.Add(Hint(box, ForgeHelp.Fx, hint));
            _fxModules.Add((box, isOn));
            return controls;
        }

        private void RefreshFxModules()
        {
            foreach (var (box, isOn) in _fxModules) box.EnableInClassList("forge-fx--off", !isOn(_recipe.Fx));
        }

        // Drive stays one stored dB amount in every mode, so the Drive target works in all of
        // them; only its name and readout follow what the mode turns it into.
        private void RefreshDriveKnob()
        {
            var mode = _recipe.Fx.Distortion.Mode;
            _driveKnob.Label = mode switch
            {
                DistortionMode.BitCrush => "Bits",
                DistortionMode.Downsample => "Hold",
                _ => "Drive",
            };
            _driveKnob.Format = mode switch
            {
                DistortionMode.BitCrush => KnobFormat.Bits,
                DistortionMode.Downsample => KnobFormat.Hold,
                _ => KnobFormat.Decibels,
            };
        }

        private void RefreshFxGraphs()
        {
            if (_recipe == null || _fxGraphBuilder == null) return;
            _fxGraphBuilder.Fill(_recipe.Fx, _recipe.LengthMs,
                _transientGraph, _distortionGraph, _compressorGraph, _delayGraph, _reverbGraph, _limiterGraph);
        }

        private static ToolbarToggle FxToggle(string text)
        {
            var toggle = new ToolbarToggle { text = text, focusable = false };
            toggle.AddToClassList("forge-curve-toggle");
            return toggle;
        }

        private KnobElement FxKnob(KnobElement knob, string path)
        {
            knob.AddToClassList("forge-knob--small");
            return BindFx(knob, path);
        }

        private T BindFx<T>(T element, string path) where T : BindableElement
        {
            _fxBindings.Add((element, path));
            return element;
        }

        private void OnProjectChanged()
        {
            // A reimported clip would otherwise keep playing its old cached data.
            _renderer?.ClearSampleCache();
            _thumbRenderer?.ClearSampleCache();
            _generator?.ClearSampleCache();
            _batchExporter?.ClearSampleCache();
            RequestRender(false);
        }

        private VisualElement BuildReadout()
        {
            _readout = new VisualElement();
            _readout.AddToClassList("forge-readout");
            _readoutLength = ReadoutItem("Length");
            _readoutPeak = ReadoutItem("Peak");
            _readoutLoudness = ReadoutItem("Loudness");
            _readoutCentroid = ReadoutItem("Centroid");
            _readoutCrest = ReadoutItem("Crest");
            _readoutRender = ReadoutItem("Render");
            _readoutWarnings = new Label();
            _readoutWarnings.AddToClassList("forge-readout__warning");
            _readout.Add(Hint(_readoutWarnings, ForgeHelp.Waveform, "Warnings"));
            return _readout;
        }

        private Label ReadoutItem(string name)
        {
            var item = new VisualElement();
            item.AddToClassList("forge-readout__item");
            var label = new Label(name);
            label.AddToClassList("forge-readout__name");
            item.Add(label);
            var value = new Label();
            value.AddToClassList("forge-readout__value");
            item.Add(value);
            _readout.Add(Hint(item, ForgeHelp.Waveform, name));
            return value;
        }

        private VisualElement BuildExportPage()
        {
            var page = ScrollPage();
            page.Add(SectionHeader("Export", ForgeHelp.Export));

            var form = new VisualElement();
            form.AddToClassList("forge-export");
            page.Add(form);

            var folderRow = new VisualElement();
            folderRow.AddToClassList("forge-export__row");
            var folder = ExportField(new TextField("Folder"), nameof(ExportSettings.Folder));
            folder.AddToClassList("forge-export__folder");
            folderRow.Add(folder);
            folderRow.Add(MakeButton("…", PickExportFolder, "forge-button--icon"));
            form.Add(Hint(folderRow, ForgeHelp.Export, "Folder  …"));

            form.Add(ExportField(new TextField("Name"), nameof(ExportSettings.NameTemplate), "Name"));
            var count = ExportField(new IntegerField("Count"), nameof(ExportSettings.Count), "Count");
            count.RegisterValueChangedCallback(e => UpdateExportButton(e.newValue));
            form.Add(count);
            form.Add(ExportField(new EnumField("Format", WavFormat.Pcm16), nameof(ExportSettings.Format), "Format"));
            form.Add(ExportField(new EnumField("Channels", ExportChannels.Auto), nameof(ExportSettings.Channels), "Channels"));
            var normalize = ExportField(new EnumField("Normalize", NormalizeMode.None), nameof(ExportSettings.Normalize), "Normalize");
            normalize.RegisterValueChangedCallback(e => UpdateNormalizeFields((NormalizeMode)e.newValue));
            form.Add(normalize);
            _loudnessTarget = ExportField(new FloatField("LUFS"), nameof(ExportSettings.LoudnessTargetLufs), "LUFS / dBTP");
            form.Add(_loudnessTarget);
            _peakTarget = ExportField(new FloatField("dBTP"), nameof(ExportSettings.PeakTargetDb), "LUFS / dBTP");
            form.Add(_peakTarget);
            form.Add(ExportField(new Toggle("Trim"), nameof(ExportSettings.TrimSilence), "Trim"));
            form.Add(ExportField(new FloatField("Fade ms"), nameof(ExportSettings.FadeOutMs), "Fade ms"));

            _exportButton = MakeButton("Export WAV", Export, "forge-primary");
            _exportButton.AddToClassList("forge-export__button");
            form.Add(Hint(_exportButton, ForgeHelp.Export, "Export"));

            UpdateNormalizeFields(_export.Normalize);
            UpdateExportButton(_export.Count);
            return page;
        }

        private T ExportField<T>(T field, string name, string hint = null) where T : BindableElement
        {
            field.AddToClassList("forge-export__field");
            field.BindProperty(_serializedWindow.FindProperty($"{nameof(_export)}.{name}"));
            return hint == null ? field : Hint(field, ForgeHelp.Export, hint);
        }

        private void UpdateNormalizeFields(NormalizeMode mode)
        {
            // Loudness mode keeps the peak target as a ceiling, so both fields apply there.
            _peakTarget.style.display = mode == NormalizeMode.None ? DisplayStyle.None : DisplayStyle.Flex;
            _loudnessTarget.style.display = mode == NormalizeMode.Loudness ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateExportButton(int count)
        {
            count = Mathf.Clamp(count, 1, ExportSettings.MaxCount);
            _exportButton.text = count == 1 ? "Export WAV" : $"Export {count} WAVs";
        }

        private void PickExportFolder()
        {
            var folder = PickAssetsFolder("Export Folder", _export.Folder);
            if (folder == null) return;

            _serializedWindow.FindProperty($"{nameof(_export)}.{nameof(ExportSettings.Folder)}").stringValue = folder;
            _serializedWindow.ApplyModifiedProperties();
        }

        // Returns a project-relative folder under Assets, or null when cancelled or outside it.
        private string PickAssetsFolder(string title, string current)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath)!.Replace('\\', '/');
            var start = $"{projectRoot}/{WavExporter.NormalizeFolder(current)}";
            var picked = EditorUtility.OpenFolderPanel(title, Directory.Exists(start) ? start : Application.dataPath, "");
            if (string.IsNullOrEmpty(picked)) return null;

            picked = WavExporter.NormalizeFolder(picked);
            var relative = picked.Length > projectRoot.Length ? picked.Substring(projectRoot.Length + 1) : string.Empty;
            if (picked.StartsWith(projectRoot + "/") && WavExporter.IsAssetsFolder(relative)) return relative;

            SetStatus("Pick a folder inside this project's Assets folder.");
            return null;
        }

        // ── Presets ─────────────────────────────────────────────────────────────────

        private void StepPreset(int direction)
        {
            if (_recipe == null) return;

            ForgePresets.Find(_presetFolder, _presetPaths);
            if (_presetPaths.Count == 0)
            {
                SetStatus($"No presets in {_presetFolder}. Save one first.");
                return;
            }

            var count = _presetPaths.Count;
            var index = _presetPaths.IndexOf(_presetPath);
            index = index < 0 ? (direction > 0 ? 0 : count - 1) : (index + direction + count) % count;
            LoadPreset(_presetPaths[index]);
        }

        private void LoadPreset(string path)
        {
            if (_recipe == null) return;

            ForgePresets.Load(_recipe, path);
            SetPresetPath(path);
            SetStatus($"Loaded preset {path}");
            OnRecipeReplaced();
            Play();
        }

        private void SavePreset()
        {
            if (_recipe == null) return;

            if (WavExporter.IsAssetsFolder(_presetFolder)) WavExporter.EnsureFolder(_presetFolder);
            var path = EditorUtility.SaveFilePanelInProject("Save Preset", _recipe.name, "json",
                "Save the recipe as a JSON preset.", _presetFolder);
            if (string.IsNullOrEmpty(path)) return;

            ForgePresets.Save(_recipe, path);
            _presetFolder = Path.GetDirectoryName(path)!.Replace('\\', '/');
            SetPresetPath(path);
            SetStatus($"Saved preset {path}");
        }

        private void ShowPresetMenu()
        {
            ForgePresets.Find(_presetFolder, _presetPaths);
            var menu = new GenericDropdownMenu();
            foreach (var path in _presetPaths)
                menu.AddItem(ForgePresets.DisplayName(path), path == _presetPath, () => LoadPreset(path));
            if (_presetPaths.Count == 0) menu.AddDisabledItem($"No presets in {_presetFolder}", false);

            menu.AddSeparator(string.Empty);
            menu.AddItem("Choose Preset Folder…", false, () =>
            {
                var folder = PickAssetsFolder("Preset Folder", _presetFolder);
                if (folder != null) _presetFolder = folder;
            });
            menu.DropDown(_presetName.worldBound, _presetName, DropdownMenuSizeMode.Auto);
        }

        private void SetPresetPath(string path)
        {
            _presetPath = path;
            _presetName.text = ForgePresets.DisplayName(path);
        }

        // ── Routes ──────────────────────────────────────────────────────────────────

        private void RebuildRoutes()
        {
            _routeContainer.Clear();
            _routeElements.Clear();

            var routes = _serializedRecipe.FindProperty(nameof(SfxRecipe.Routes));
            for (var i = 0; i < routes.arraySize; i++)
            {
                var element = new ModRouteElement();
                _routeContainer.Add(element);
                element.Bind(routes.GetArrayElementAtIndex(i), i);
                element.RemoveRequested += RemoveRoute;
                _routeElements.Add(element);
            }

            _routesTitle.text = $"Routes ({routes.arraySize})";
        }

        private void AddRoute()
        {
            Undo.RecordObject(_recipe, "Add Route");
            _recipe.Routes.Add(new ModRoute(ModSource.Lfo, ModTarget.Pitch, 0f));
            CommitRouteChange();
        }

        private void RemoveRoute(int index)
        {
            Undo.RecordObject(_recipe, "Remove Route");
            _recipe.Routes.RemoveAt(index);
            CommitRouteChange();
        }

        private void ResetRoutes()
        {
            Undo.RecordObject(_recipe, "Reset Routes");
            _recipe.Routes = ModRoute.Defaults();
            CommitRouteChange();
        }

        private void CommitRouteChange()
        {
            EditorUtility.SetDirty(_recipe);
            _serializedRecipe.Update();
            OnRoutesChanged();
            _modulation.Refresh();
        }

        private void OnRoutesChanged()
        {
            RebuildRoutes();
            RequestRender();
        }

        private void SetRecipe(SfxRecipe recipe, bool clearVariations)
        {
            _recipe = recipe;
            _recipeField.SetValueWithoutNotify(recipe);
            _player.Stop();

            _tracker?.RemoveFromHierarchy();
            _tracker = null;
            _serializedRecipe = recipe != null ? new SerializedObject(recipe) : null;
            _modulation.Bind(recipe, _serializedRecipe);

            if (clearVariations)
            {
                _variations.Clear();
                _selectedVariation = -1;
            }

            var hasRecipe = recipe != null;
            _main.style.display = hasRecipe ? DisplayStyle.Flex : DisplayStyle.None;
            _placeholder.style.display = hasRecipe ? DisplayStyle.None : DisplayStyle.Flex;

            if (!hasRecipe)
            {
                _stripContainer.Clear();
                _strips.Clear();
                _routeContainer.Clear();
                _routeElements.Clear();
                _waveform.ClearSamples();
                return;
            }

            _seed.BindProperty(_serializedRecipe.FindProperty(nameof(SfxRecipe.Seed)));
            _length.BindProperty(_serializedRecipe.FindProperty(nameof(SfxRecipe.LengthMs)));
            _variationAmount.BindProperty(_serializedRecipe.FindProperty("Randomizer.VariationAmount"));
            _coupling.BindProperty(_serializedRecipe.FindProperty("Randomizer.PhysicsCoupling"));
            _candidates.BindProperty(_serializedRecipe.FindProperty("Randomizer.CandidateCount"));
            for (var i = 0; i < _macroKnobs.Length; i++)
                _macroKnobs[i].BindProperty(_serializedRecipe.FindProperty($"{nameof(SfxRecipe.Macros)}.{MacroNames[i]}"));
            for (var i = 0; i < _lfoPanels.Length; i++) _lfoPanels[i].Bind(_serializedRecipe.FindProperty(LfoPaths[i]));
            for (var i = 0; i < _randomPanels.Length; i++)
                _randomPanels[i].Bind(_serializedRecipe.FindProperty(ForgeModulation.RandomPath(RandomSources[i])));
            _distortionMode.BindProperty(_serializedRecipe.FindProperty("Fx.Distortion.Mode"));
            foreach (var (element, path) in _fxBindings) element.BindProperty(_serializedRecipe.FindProperty(path));
            RebuildStrips();
            RebuildRoutes();
            RenderThumbnails();
            RefreshSelectors();

            // A dedicated element so switching recipes drops the old subscription with it.
            _tracker = new VisualElement { style = { display = DisplayStyle.None } };
            rootVisualElement.Add(_tracker);
            _tracker.TrackSerializedObjectValue(_serializedRecipe, _ => OnRecipeChanged());

            _playedState = JsonUtility.ToJson(recipe);
            RequestRender(false);
        }

        private void RebuildStrips()
        {
            _serializedRecipe.Update();
            _stripContainer.Clear();
            _strips.Clear();

            var layers = _serializedRecipe.FindProperty(nameof(SfxRecipe.Layers));
            for (var i = 0; i < layers.arraySize; i++)
            {
                var strip = new LayerStripElement();
                _stripContainer.Add(strip);
                strip.Bind(layers.GetArrayElementAtIndex(i), i);
                strip.RemoveRequested += RemoveLayer;
                strip.DuplicateRequested += DuplicateLayer;
                strip.Selected += SelectCurveLayer;
                strip.Compact = _compactStrips;
                if (_renderer.FrameCount > 0) strip.ShowRender(_renderer);
                _strips.Add(strip);
            }

            RefreshCurveEditor();
            _modulation.Refresh();

            _addLayer.SetEnabled(layers.arraySize < SfxRecipe.MaxLayers);
        }

        private void RefreshSelectors()
        {
            _category.SetValueWithoutNotify(_recipe.Category);
            _piano.RootNote = _recipe.RootNote;
            _rootNoteLabel.text = PianoElement.NoteName(_recipe.RootNote);
            foreach (var pair in _harmonyButtons)
                pair.Value.EnableInClassList("forge-selected", pair.Key == _recipe.Randomizer.Harmony);
            _length.Locked = _recipe.Randomizer.LockLength;
            _fxTitle.EnableInClassList("forge-locked-title", _recipe.Randomizer.LockFx);
            RefreshFxModules();
            RefreshDriveKnob();
            RefreshCurveEditor();
            RefreshModPanels();
            _modulation.Refresh();
        }

        private void OnRecipeChanged()
        {
            if (_recipe == null) return;
            if (_recipe.Layers.Count != _strips.Count) RebuildStrips();
            if (_recipe.Routes.Count != _routeElements.Count) RebuildRoutes();
            RefreshSelectors();
            RequestRender();
        }

        private void OnUndoRedo()
        {
            if (_serializedRecipe == null || _recipe == null) return;
            OnRecipeReplaced();
        }

        private void OnRecipeReplaced()
        {
            _serializedRecipe.Update();
            RebuildStrips();
            RebuildRoutes();
            RefreshSelectors();
            RequestRender();
        }

        // ── Curves ──────────────────────────────────────────────────────────────────

        private void SelectCurveLayer(int index)
        {
            if (_curveLayer == index) return;
            _curveLayer = index;
            RefreshCurveEditor();
        }

        private void RefreshCurveEditor()
        {
            if (_recipe == null || _recipe.Layers.Count == 0 || _curveEditor == null) return;

            _curveLayer = Mathf.Clamp(_curveLayer, 0, _recipe.Layers.Count - 1);
            var layer = _recipe.Layers[_curveLayer];
            var curve = layer.GetCurve(_curveTarget);
            var shown = IsUsable(curve) ? curve : Curve.Default(_curveTarget);
            var isPitch = _curveTarget == CurveTarget.Pitch;

            _curveEditor.Harmony = _recipe.Randomizer.Harmony;
            _curveEditor.HarmonySnap = _harmonySnap && isPitch;
            _curveEditor.GridSnap = _gridSnap;
            _curveEditor.FreehandMode = _drawMode;
            _curveEditor.SetCurve(shown.Points, shown.Min, shown.Max, shown.Unit, curve.Locked);

            _harmonyToggle.style.display = isPitch ? DisplayStyle.Flex : DisplayStyle.None;
            _curveLockToggle.SetValueWithoutNotify(curve.Locked);
            _curveLayerLabel.text = _curveLinked ? "All layers" : $"Layer {_curveLayer + 1}: {layer.Name}";

            foreach (var pair in _curveTabs) pair.Value.EnableInClassList("forge-selected", pair.Key == _curveTarget);
            for (var i = 0; i < _strips.Count; i++) _strips[i].IsSelected = !_curveLinked && i == _curveLayer;
        }

        // Curves saved before pitch/cutoff/pan curves existed deserialize empty with no range.
        private static bool IsUsable(Curve curve) => curve.Points.Count >= 2 && curve.Max > curve.Min;

        private void OnCurveEditStarted()
        {
            Undo.IncrementCurrentGroup();
            _curveUndoGroup = Undo.GetCurrentGroup();
        }

        private void OnCurveChanged(List<Breakpoint> points)
        {
            EditCurves("Edit Curve", curve =>
            {
                if (!IsUsable(curve)) CopyRange(Curve.Default(_curveTarget), curve);
                curve.Points.Clear();
                curve.Points.AddRange(points);
            });
        }

        private void OnCurveEditFinished()
        {
            Undo.CollapseUndoOperations(_curveUndoGroup);
            RefreshCurveEditor();
        }

        private void ResetCurve()
        {
            EditCurves("Reset Curve", curve =>
            {
                var fresh = Curve.Default(_curveTarget);
                CopyRange(fresh, curve);
                curve.Points.Clear();
                curve.Points.AddRange(fresh.Points);
            });
            RefreshCurveEditor();
        }

        private void SetCurveLocked(bool locked) =>
            EditCurves(locked ? "Lock Curve" : "Unlock Curve", curve => curve.Locked = locked);

        // Curve locks only guard against the randomizer, like knob locks, so edits still apply.
        private void EditCurves(string undoName, Action<Curve> edit)
        {
            if (_recipe == null) return;

            Undo.RecordObject(_recipe, undoName);
            for (var i = 0; i < _recipe.Layers.Count; i++)
            {
                if (!_curveLinked && i != _curveLayer) continue;
                edit(_recipe.Layers[i].GetCurve(_curveTarget));
            }

            EditorUtility.SetDirty(_recipe);
            _serializedRecipe.Update();
            RequestRender();
        }

        private static void CopyRange(Curve from, Curve to)
        {
            to.Min = from.Min;
            to.Max = from.Max;
            to.Unit = from.Unit;
        }

        // ── Mod envelopes (Env 2/3) ─────────────────────────────────────────────────

        private Curve SelectedEnvCurve => _modTab == ModSource.Env3 ? _recipe.Env3 : _recipe.Env2;

        private void RefreshEnvEditor()
        {
            if (_recipe == null || _envEditor == null) return;

            var curve = SelectedEnvCurve;
            var shown = IsUsable(curve) ? curve : Curve.DefaultModEnvelope();
            _envEditor.GridSnap = _envGridSnap;
            _envEditor.FreehandMode = _envDrawMode;
            _envEditor.SetCurve(shown.Points, shown.Min, shown.Max, shown.Unit, false);
        }

        // The drag opens its undo group in OnCurveEditStarted, shared with the layer curves.
        private void OnEnvCurveChanged(List<Breakpoint> points)
        {
            EditEnvCurve("Edit Envelope", curve =>
            {
                if (!IsUsable(curve)) CopyRange(Curve.DefaultModEnvelope(), curve);
                curve.Points.Clear();
                curve.Points.AddRange(points);
            });
        }

        private void OnEnvCurveEditFinished()
        {
            Undo.CollapseUndoOperations(_curveUndoGroup);
            RefreshEnvEditor();
        }

        private void ResetEnvCurve()
        {
            EditEnvCurve("Reset Envelope", curve =>
            {
                var fresh = Curve.DefaultModEnvelope();
                CopyRange(fresh, curve);
                curve.Points.Clear();
                curve.Points.AddRange(fresh.Points);
            });
            RefreshEnvEditor();
        }

        private void EditEnvCurve(string undoName, Action<Curve> edit)
        {
            if (_recipe == null) return;

            Undo.RecordObject(_recipe, undoName);
            edit(SelectedEnvCurve);
            EditorUtility.SetDirty(_recipe);
            _serializedRecipe.Update();
            RequestRender();
        }

        // ── Randomizer ──────────────────────────────────────────────────────────────

        private void SelectCategory(SfxCategory category)
        {
            if (_recipe == null) return;

            Undo.RecordObject(_recipe, "Change Category");
            _recipe.Category = category;
            EditorUtility.SetDirty(_recipe);
            GenerateVariations(false);
        }

        private void GenerateVariations(bool mutate)
        {
            if (_recipe == null) return;

            CategoryTemplate template = null;
            if (!mutate)
            {
                template = ForgeTemplates.Get(_recipe.Category);
                if (template == null)
                {
                    SetStatus($"No template for {_recipe.Category} in {ForgeAssets.TemplatesFolder}.");
                    return;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            var baseSeed = math.hash(new uint2((uint)Environment.TickCount, ++_seedCounter));
            _generator.Generate(_recipe, template, baseSeed, _recipe.Randomizer.CandidateCount, VariationCount, _variations);
            stopwatch.Stop();

            SetStatus($"{(mutate ? "Mutate" : "Randomize")}: kept {_variations.Count} of {_generator.CandidateCount} " +
                      $"candidates, {_generator.Rejected} rejected as defective or outliers " +
                      $"({stopwatch.Elapsed.TotalMilliseconds:0} ms).");

            RenderThumbnails();
            ApplyVariation(0, mutate ? "Mutate" : "Randomize");
        }

        private void RenderThumbnails()
        {
            for (var i = 0; i < VariationCount; i++)
            {
                var thumb = _thumbs[i];
                thumb.Selected = i == _selectedVariation;

                if (i >= _variations.Count)
                {
                    thumb.ClearSamples();
                    continue;
                }

                JsonUtility.FromJsonOverwrite(_variations[i], _scratch);
                _thumbRenderer.Render(_scratch);
                thumb.SetSamples(_thumbRenderer.Output, SfxRenderer.Channels);

                var analysis = _analyzer.Analyze(_thumbRenderer.Output, SfxRenderer.Channels, _thumbRenderer.SampleRate);
                thumb.tooltip = $"{FormatDb(analysis.LoudnessLufs)} LUFS  ·  {FormatHz(analysis.SpectralCentroidHz)}  ·  " +
                                $"{analysis.EffectiveLengthMs:0} ms";
            }
        }

        private void OnThumbClicked(int index)
        {
            if (index < _variations.Count) ApplyVariation(index, "Select Variation");
        }

        private void ApplyVariation(int index, string undoName)
        {
            Undo.RecordObject(_recipe, undoName);
            JsonUtility.FromJsonOverwrite(_variations[index], _recipe);
            EditorUtility.SetDirty(_recipe);

            _selectedVariation = index;
            for (var i = 0; i < VariationCount; i++) _thumbs[i].Selected = i == index;

            RebuildStrips();
            RefreshSelectors();
            Play();
        }

        // ── Layers ──────────────────────────────────────────────────────────────────

        private void AddLayer()
        {
            if (_recipe.Layers.Count >= SfxRecipe.MaxLayers) return;

            Undo.RecordObject(_recipe, "Add Layer");
            _recipe.Layers.Add(new Layer { Name = $"Layer {_recipe.Layers.Count + 1}" });
            CommitLayerChange();
        }

        private void RemoveLayer(int index)
        {
            if (_recipe.Layers.Count <= 1) return;

            Undo.RecordObject(_recipe, "Remove Layer");
            _recipe.Layers.RemoveAt(index);
            CommitLayerChange();
        }

        private void DuplicateLayer(int index)
        {
            if (_recipe.Layers.Count >= SfxRecipe.MaxLayers) return;

            Undo.RecordObject(_recipe, "Duplicate Layer");
            var copy = JsonUtility.FromJson<Layer>(JsonUtility.ToJson(_recipe.Layers[index]));
            copy.Name += " Copy";
            _recipe.Layers.Insert(index + 1, copy);
            CommitLayerChange();
        }

        private void CommitLayerChange()
        {
            EditorUtility.SetDirty(_recipe);
            RebuildStrips();
            RequestRender();
        }

        private void SetRecipeBool(string path, bool value)
        {
            _serializedRecipe.FindProperty(path).boolValue = value;
            _serializedRecipe.ApplyModifiedProperties();
            RefreshSelectors();
        }

        private void SetRootNote(int note)
        {
            SetRecipeInt(nameof(SfxRecipe.RootNote), note);
            Play();
        }

        private void SetRecipeInt(string path, int value)
        {
            if (_serializedRecipe == null) return;
            _serializedRecipe.FindProperty(path).intValue = value;
            _serializedRecipe.ApplyModifiedProperties();
            RefreshSelectors();
        }

        // ── Rendering and playback ──────────────────────────────────────────────────

        // Throttles to one render per RenderIntervalMs; the deferred render picks up whatever
        // the recipe looks like when it fires, so no edit is lost while dragging.
        private void RequestRender(bool autoplay = true)
        {
            // Restarting the delay on every change waits for a drag to settle instead of
            // retriggering the sound each frame.
            if (autoplay && _autoplay && _recipe != null) _autoplayer.ExecuteLater(AutoplayDelayMs);
            if (_renderPending || _recipe == null) return;

            _renderPending = true;
            var delay = (long)Math.Max(0.0, RenderIntervalMs - (NowMs - _lastRenderTime));
            rootVisualElement.schedule.Execute(() =>
            {
                _renderPending = false;
                RenderNow();
            }).ExecuteLater(delay);
        }

        private void RenderNow()
        {
            if (_recipe == null || _renderer == null) return;

            var stopwatch = Stopwatch.StartNew();
            _renderer.Render(_recipe);
            stopwatch.Stop();
            _lastRenderTime = NowMs;

            var output = _renderer.Output;
            _waveform.SetSamples(output, SfxRenderer.Channels);
            foreach (var strip in _strips) strip.ShowRender(_renderer);
            if (_page == Page.Fx) RefreshFxGraphs();
            LoadPreview();
            UpdateReadout(_analyzer.Analyze(output, SfxRenderer.Channels, _renderer.SampleRate), stopwatch.Elapsed.TotalMilliseconds);
        }

        private void UpdateReadout(SfxAnalysis analysis, double renderMs)
        {
            _readoutLength.text = $"{analysis.EffectiveLengthMs:0} / {analysis.LengthMs:0} ms";
            _readoutPeak.text = $"{FormatDb(analysis.TruePeakDb)} dBTP";
            _readoutLoudness.text = $"{FormatDb(analysis.LoudnessLufs)} LUFS";
            _readoutCentroid.text = FormatHz(analysis.SpectralCentroidHz);
            _readoutCrest.text = $"{analysis.CrestFactorDb:0.0} dB";
            _readoutRender.text = $"{renderMs:0.00} ms";

            var warnings = string.Empty;
            if (analysis.IsClipping) warnings += $"Clipping ({analysis.ClippedSamples} samples)   ";
            if (analysis.DcOffset > 0.01f) warnings += $"DC offset {analysis.DcOffset:0.000}   ";
            if (!analysis.IsSilent && analysis.EffectiveLengthMs >= analysis.LengthMs * 0.98f) warnings += "Tail cut off";
            _readoutWarnings.text = warnings;
        }

        private void LoadPreview() =>
            _player.Load(TrimmedOutput(), SfxRenderer.Channels, _renderer.SampleRate);

        private NativeArray<float> TrimmedOutput()
        {
            var frames = _renderer.FrameCount;
            _trimFirstFrame = Mathf.Clamp(Mathf.RoundToInt(_trimStart * frames), 0, frames - 1);
            var last = Mathf.Clamp(Mathf.RoundToInt(_trimEnd * frames), _trimFirstFrame + 1, frames);
            return _renderer.Output.GetSubArray(_trimFirstFrame * SfxRenderer.Channels,
                (last - _trimFirstFrame) * SfxRenderer.Channels);
        }

        private void OnTrimChanged(float start, float end)
        {
            _trimStart = start;
            _trimEnd = end;
            if (_renderer.FrameCount > 0) LoadPreview();
        }

        // Compares against the last played state, so changes that already played (randomize,
        // variations, presets) are not played a second time.
        private void Autoplay()
        {
            _autoplayer.Pause();
            if (_recipe == null || JsonUtility.ToJson(_recipe) == _playedState) return;
            Play();
        }

        private void Play()
        {
            if (_recipe == null) return;

            _playedState = JsonUtility.ToJson(_recipe);
            RenderNow();
            _player.Play();
            _playheadUpdater.Resume();
        }

        private void Stop()
        {
            _player.Stop();
            SetPlayhead(-1f);
            _meter.ResetLevels(EditorApplication.timeSinceStartup);
            _playheadUpdater.Pause();
        }

        private void UpdatePlayhead()
        {
            if (!_player.IsPlaying || _renderer.FrameCount == 0)
            {
                Stop();
                return;
            }

            var position = _trimFirstFrame + _player.TimeSamples;
            SetPlayhead(position / (float)_renderer.FrameCount);
            UpdateMeter(position);
        }

        private void SetPlayhead(float playhead)
        {
            _waveform.Playhead = playhead;
            foreach (var strip in _strips) strip.Playhead = playhead;
        }

        // Peak of the rendered audio the player went through since the last tick.
        private void UpdateMeter(int position)
        {
            var output = _renderer.Output;
            var frames = _renderer.FrameCount;
            var window = Mathf.Max(1, (int)(_renderer.SampleRate * PlayheadIntervalMs / 1000));
            var first = Mathf.Clamp(position - window, 0, frames);
            var last = Mathf.Clamp(position, first, frames);

            var left = 0f;
            var right = 0f;
            for (var f = first; f < last; f++)
            {
                left = Mathf.Max(left, Mathf.Abs(output[f * SfxRenderer.Channels]));
                right = Mathf.Max(right, Mathf.Abs(output[f * SfxRenderer.Channels + 1]));
            }

            _meter.SetLevels(left, right, EditorApplication.timeSinceStartup);
        }

        private void Export()
        {
            if (_recipe == null) return;
            SetStatus(_batchExporter.Export(_recipe, _export, _trimStart, _trimEnd));
        }

        private void CreateRecipe()
        {
            var path = EditorUtility.SaveFilePanelInProject("New SFX Recipe", "SFX Recipe", "asset",
                "Choose where to save the recipe.");
            if (string.IsNullOrEmpty(path)) return;

            var recipe = CreateInstance<SfxRecipe>();
            AssetDatabase.CreateAsset(recipe, path);
            AssetDatabase.SaveAssets();
            SetRecipe(recipe, true);
        }

        // ── Status bar ──────────────────────────────────────────────────────────────

        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("forge-status");
            _statusName = new Label();
            _statusName.AddToClassList("forge-status__name");
            bar.Add(_statusName);
            _statusText = new Label();
            _statusText.AddToClassList("forge-status__text");
            bar.Add(_statusText);
            ShowStatus();
            return bar;
        }

        // Shown straight away even under the cursor, so the result of the button just pressed is
        // not hidden behind that button's hint; the next element hovered brings hints back.
        private void SetStatus(string message)
        {
            _statusMessage = message;
            ShowStatus();
        }

        private void ShowStatus()
        {
            _statusName.style.display = DisplayStyle.None;
            _statusText.text = _statusMessage;
        }

        private void OnPointerOver(PointerOverEvent evt)
        {
            if (!ForgeHints.TryFind(evt.target as VisualElement, out var name, out var text))
            {
                ShowStatus();
                return;
            }

            _statusName.text = name;
            _statusName.style.display = DisplayStyle.Flex;
            _statusText.text = text;
        }

        // ── Hotkeys ─────────────────────────────────────────────────────────────────

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey || evt.altKey || IsInsideTextInput(evt.target as VisualElement)) return;

            switch (evt.keyCode)
            {
                case KeyCode.Space:
                    if (_player.IsPlaying) Stop();
                    else Play();
                    break;
                case KeyCode.R:
                    GenerateVariations(false);
                    break;
                case KeyCode.M:
                    GenerateVariations(true);
                    break;
                case KeyCode.Escape:
                    if (!_help.IsOpen) return;
                    _help.Hide();
                    break;
                default:
                    return;
            }

            evt.StopPropagation();
        }

        private static bool IsInsideTextInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(TextInputBaseField<string>.ussClassName)) return true;
            }

            return false;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private static string FormatDb(float db) => db <= AudioMath.SilenceDb ? "-inf" : db.ToString("0.0");

        private static string FormatHz(float hz) => hz >= 1000f ? $"{hz / 1000f:0.00} kHz" : $"{hz:0} Hz";

        private static VisualElement Bar(string className)
        {
            var bar = new VisualElement();
            bar.AddToClassList("forge-bar");
            bar.AddToClassList(className);
            return bar;
        }

        private static VisualElement Column(string className)
        {
            var column = new VisualElement();
            column.AddToClassList("forge-column");
            column.AddToClassList(className);
            return column;
        }

        private static Label SectionTitle(string text)
        {
            var label = new Label(text);
            label.AddToClassList("forge-section-title");
            return label;
        }

        private VisualElement SectionHeader(string title, HelpTopic topic)
        {
            var header = new VisualElement();
            header.AddToClassList("forge-section-header");
            header.Add(SectionTitle(title));
            header.Add(Spacer());
            header.Add(HelpButton(topic));
            return header;
        }

        private Button HelpButton(HelpTopic topic)
        {
            var button = MakeButton("?", null, "forge-help-button");
            button.clicked += () => _help.Show(topic, button);
            return button;
        }

        private static Label SubTitle(string text)
        {
            var label = new Label(text);
            label.AddToClassList("forge-subtitle");
            return label;
        }

        private static VisualElement Spacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList("forge-spacer");
            return spacer;
        }

        private static VisualElement Separator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("forge-separator");
            return separator;
        }

        // Not focusable, so Space reaches the window hotkey instead of re-clicking the last button.
        private static Button MakeButton(string text, Action onClick, string extraClass = null)
        {
            var button = new Button(onClick) { text = text, focusable = false };
            button.AddToClassList("forge-button");
            if (extraClass != null) button.AddToClassList(extraClass);
            return button;
        }

        private static Button IconButton(ForgeIcon icon, Action onClick, string extraClass = null)
        {
            var button = MakeButton(string.Empty, onClick, "forge-button--icon");
            if (extraClass != null) button.AddToClassList(extraClass);
            button.Add(new IconElement(icon));
            return button;
        }

        private static Button StepperArrow(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text, focusable = false };
            button.AddToClassList(StepperElement.UssClassName + "__arrow");
            return button;
        }

        private static T Hint<T>(T element, HelpTopic topic, string item) where T : VisualElement =>
            ForgeHints.Set(element, topic, item);
    }
}
