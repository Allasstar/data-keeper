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

        [SerializeField] private SfxRecipe _recipe;
        [SerializeField] private ExportSettings _export = new();
        [SerializeField] private string _presetFolder = ForgePresets.DefaultFolder;
        [SerializeField] private string _presetPath;
        [SerializeField] private bool _routesExpanded;
        [SerializeField] private bool _exportExpanded = true;
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

        private readonly List<LayerStripElement> _strips = new();
        private readonly Dictionary<SfxCategory, Button> _categoryButtons = new();
        private readonly Dictionary<HarmonyMode, Button> _harmonyButtons = new();
        private readonly VariationThumbElement[] _thumbs = new VariationThumbElement[VariationCount];
        private readonly Dictionary<CurveTarget, Button> _curveTabs = new();
        private readonly List<(BindableElement Element, string Path)> _fxBindings = new();
        private readonly List<string> _presetPaths = new();
        private readonly List<ModRouteElement> _routeElements = new();
        private readonly KnobElement[] _macroKnobs = new KnobElement[4];
        private static readonly string[] MacroNames = { nameof(Macros.Size), nameof(Macros.Energy), nameof(Macros.Tone), nameof(Macros.Motion) };

        private SfxRenderer _renderer;
        private SfxRenderer _thumbRenderer;
        private SfxAnalyzer _analyzer;
        private VariationGenerator _generator;
        private BatchExporter _batchExporter;
        private SfxRecipe _scratch;
        private PreviewPlayer _player;
        private SerializedObject _serializedRecipe;
        private SerializedObject _serializedWindow;

        private ObjectField _recipeField;
        private VisualElement _main;
        private Label _placeholder;
        private UnsignedIntegerField _seed;
        private KnobElement _length;
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
        private EnumField _lfoShape;
        private KnobElement _lfoRate;
        private Foldout _routesFoldout;
        private VisualElement _routeContainer;
        private VisualElement _peakTarget;
        private VisualElement _loudnessTarget;
        private VisualElement _stripContainer;
        private Button _addLayer;
        private VisualElement _tracker;
        private Label _status;
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
            if (_scratch != null) DestroyImmediate(_scratch);
            _player = null;
            _renderer = null;
            _thumbRenderer = null;
            _analyzer = null;
            _generator = null;
            _batchExporter = null;
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

            root.Add(BuildTopBar());

            _placeholder = new Label("Pick a recipe or click New to start.");
            _placeholder.AddToClassList("forge-placeholder");
            root.Add(_placeholder);

            _main = new VisualElement();
            _main.AddToClassList("forge-main");
            _main.Add(BuildLeftColumn());
            _main.Add(BuildCenterColumn());
            _main.Add(BuildRightColumn());
            root.Add(_main);

            _status = new Label();
            _status.AddToClassList("forge-status");
            root.Add(_status);

            _help = new HelpOverlay();
            root.Add(_help);

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

            _recipeField = new ObjectField { objectType = typeof(SfxRecipe), allowSceneObjects = false };
            _recipeField.AddToClassList("forge-recipe-field");
            _recipeField.RegisterValueChangedCallback(e => SetRecipe(e.newValue as SfxRecipe, true));
            bar.Add(_recipeField);
            bar.Add(MakeButton("New", CreateRecipe));

            bar.Add(Separator());
            bar.Add(MakeButton("◀", () => StepPreset(-1), "forge-button--icon"));
            _presetName = MakeButton(ForgePresets.DisplayName(_presetPath), ShowPresetMenu);
            _presetName.AddToClassList("forge-preset-name");
            bar.Add(_presetName);
            bar.Add(MakeButton("▶", () => StepPreset(1), "forge-button--icon"));
            bar.Add(MakeButton("Save Preset", SavePreset));

            bar.Add(Separator());
            bar.Add(MakeButton("Randomize", () => GenerateVariations(false), "forge-primary"));
            bar.Add(MakeButton("Mutate", () => GenerateVariations(true)));
            bar.Add(MakeButton("Undo", Undo.PerformUndo));
            bar.Add(MakeButton("Redo", Undo.PerformRedo));

            bar.Add(Spacer());
            _meter = new MeterElement();
            bar.Add(_meter);
            bar.Add(BuildVolumeSlider());
            bar.Add(BuildAutoplayToggle());
            bar.Add(MakeButton("Play", Play, "forge-primary"));
            bar.Add(MakeButton("Stop", Stop));
            bar.Add(HelpButton(ForgeHelp.TopBar));
            return bar;
        }

        // Monitoring only: the render, meter and exports stay at full level.
        private Slider BuildVolumeSlider()
        {
            var slider = new Slider("Vol", 0f, 1f) { focusable = false };
            slider.AddToClassList("forge-volume");
            slider.RegisterValueChangedCallback(e => SetPreviewVolume(slider, e.newValue));
            var volume = EditorPrefs.GetFloat(PreviewVolumeKey, DefaultPreviewVolume);
            slider.SetValueWithoutNotify(volume);
            SetPreviewVolume(slider, volume);
            return slider;
        }

        private void SetPreviewVolume(Slider slider, float position)
        {
            EditorPrefs.SetFloat(PreviewVolumeKey, position);

            // Squared so the slider travel feels even to the ear instead of crowding at the top.
            var gain = position * position;
            _player.Volume = gain;
            slider.tooltip = gain > 0f ? $"Preview volume {AudioMath.LinearToDb(gain):0.0} dB" : "Preview muted";
        }

        private ToolbarToggle BuildAutoplayToggle()
        {
            _autoplay = EditorPrefs.GetBool(AutoplayKey, true);
            var toggle = new ToolbarToggle { text = "Auto", value = _autoplay, focusable = false };
            toggle.AddToClassList("forge-curve-toggle");
            toggle.tooltip = "Play automatically after each edit";
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
            var column = new ScrollView(ScrollViewMode.Vertical);
            column.AddToClassList("forge-left__scroll");
            outer.Add(column);

            column.Add(SectionHeader("Category", ForgeHelp.Category));
            var categories = new VisualElement();
            categories.AddToClassList("forge-category-list");
            foreach (SfxCategory category in Enum.GetValues(typeof(SfxCategory)))
            {
                var button = MakeButton(ObjectNames.NicifyVariableName(category.ToString()),
                    () => SelectCategory(category));
                button.AddToClassList("forge-category");
                _categoryButtons[category] = button;
                categories.Add(button);
            }
            column.Add(categories);

            column.Add(SectionHeader("Variations", ForgeHelp.Variations));
            var grid = new VisualElement();
            grid.AddToClassList("forge-variation-grid");
            for (var i = 0; i < VariationCount; i++)
            {
                var thumb = new VariationThumbElement();
                thumb.SetIndex(i);
                thumb.ClearSamples();
                thumb.Clicked += OnThumbClicked;
                _thumbs[i] = thumb;
                grid.Add(thumb);
            }
            column.Add(grid);

            _seed = new UnsignedIntegerField("Seed");
            _seed.AddToClassList("forge-seed");
            column.Add(_seed);

            column.Add(BuildExportPanel());
            return outer;
        }

        private VisualElement BuildCenterColumn()
        {
            var column = Column("forge-center");

            var globals = Bar("forge-globals");
            _length = new KnobElement("Length", SfxRecipe.MinLengthMs, SfxRecipe.MaxLengthMs, 500f,
                KnobFormat.Milliseconds, KnobScale.Log) { Lockable = true };
            _length.AddToClassList("forge-knob--inline");
            _length.LockToggled += locked => SetRecipeBool("Randomizer.LockLength", locked);
            globals.Add(_length);
            globals.Add(Spacer());
            globals.Add(HelpButton(ForgeHelp.Waveform));
            column.Add(globals);

            var wavePanel = new VisualElement();
            wavePanel.AddToClassList("forge-wave-panel");
            _waveform = new WaveformElement();
            _waveform.SetTrim(_trimStart, _trimEnd);
            _waveform.TrimChanged += OnTrimChanged;
            wavePanel.Add(_waveform);
            wavePanel.Add(BuildReadout());
            column.Add(wavePanel);
            column.Add(BuildCurvePanel());

            var layersHeader = SectionHeader("Layers", ForgeHelp.Layers);
            layersHeader.AddToClassList("forge-layers-header");
            column.Add(layersHeader);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("forge-layers");
            _stripContainer = new VisualElement();
            scroll.Add(_stripContainer);
            _addLayer = MakeButton("+ Add Layer", AddLayer);
            _addLayer.AddToClassList("forge-add-layer");
            scroll.Add(_addLayer);
            column.Add(scroll);
            return column;
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
            bar.Add(MakeButton("Reset", ResetCurve));
            bar.Add(HelpButton(ForgeHelp.Curves));
            panel.Add(bar);

            _curveEditor = new CurveEditorElement();
            _curveEditor.EditStarted += OnCurveEditStarted;
            _curveEditor.Changed += OnCurveChanged;
            _curveEditor.EditFinished += OnCurveEditFinished;
            panel.Add(_curveEditor);

            var hint = new Label("Double-click adds a point, right-click or Delete removes it, Alt-drag bends a segment.");
            hint.AddToClassList("forge-curve-hint");
            panel.Add(hint);
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
            bar.Add(tab);
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
            return toggle;
        }

        private VisualElement BuildRightColumn()
        {
            var outer = Column("forge-right");
            var column = new ScrollView(ScrollViewMode.Vertical);
            column.AddToClassList("forge-right__scroll");
            outer.Add(column);
            column.Add(SectionHeader("Macros", ForgeHelp.Modulation));
            var macros = new VisualElement();
            macros.AddToClassList("forge-macros");
            for (var i = 0; i < _macroKnobs.Length; i++)
            {
                _macroKnobs[i] = new KnobElement(MacroNames[i], 0f, 1f, 0.5f, KnobFormat.Percent);
                macros.Add(_macroKnobs[i]);
            }
            column.Add(macros);
            column.Add(BuildModulationPanel());

            column.Add(SectionHeader("Randomizer", ForgeHelp.Randomizer));

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
            column.Add(pills);

            var knobs = new VisualElement();
            knobs.AddToClassList("forge-randomizer-knobs");
            _variationAmount = new KnobElement("Variation", 0f, 1f, 0.3f, KnobFormat.Percent);
            _coupling = new KnobElement("Physics", 0f, 1f, 0.7f, KnobFormat.Percent);
            knobs.Add(_variationAmount);
            knobs.Add(_coupling);
            column.Add(knobs);

            _candidates = new IntegerField("Candidates");
            _candidates.AddToClassList("forge-candidates");
            column.Add(_candidates);

            column.Add(BuildFxPanel());

            var hint = new Label("Right-click a knob or dropdown to lock it. R randomizes, M mutates, Space plays.");
            hint.AddToClassList("forge-hint");
            column.Add(hint);
            return outer;
        }

        private VisualElement BuildModulationPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("forge-mod-panel");

            var lfo = new VisualElement();
            lfo.AddToClassList("forge-lfo");
            lfo.Add(SubTitle("LFO"));
            _lfoShape = new EnumField(Waveform.Sine);
            _lfoShape.AddToClassList("forge-lfo__shape");
            lfo.Add(_lfoShape);
            _lfoRate = new KnobElement("Rate", LfoSettings.MinRateHz, LfoSettings.MaxRateHz, 4f, KnobFormat.Hertz, KnobScale.Log);
            _lfoRate.AddToClassList("forge-knob--small");
            lfo.Add(_lfoRate);
            panel.Add(lfo);

            _routesFoldout = new Foldout { text = "Routes", value = _routesExpanded };
            _routesFoldout.AddToClassList("forge-routes");
            // Child toggles raise bool change events that bubble up to the foldout.
            _routesFoldout.RegisterValueChangedCallback(e =>
            {
                if (e.target == _routesFoldout) _routesExpanded = e.newValue;
            });
            _routeContainer = new VisualElement();
            _routesFoldout.Add(_routeContainer);

            var buttons = new VisualElement();
            buttons.AddToClassList("forge-routes__buttons");
            buttons.Add(MakeButton("+ Route", AddRoute));
            buttons.Add(MakeButton("Defaults", ResetRoutes));
            _routesFoldout.Add(buttons);
            panel.Add(_routesFoldout);
            return panel;
        }

        private VisualElement BuildFxPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("forge-fx-panel");

            var header = new VisualElement();
            header.AddToClassList("forge-fx-header");
            _fxTitle = SectionTitle("FX");
            header.Add(_fxTitle);
            header.Add(Spacer());
            header.Add(BindFx(FxToggle("Lock"), "Randomizer.LockFx"));
            header.Add(HelpButton(ForgeHelp.Fx));
            panel.Add(header);

            var transient = FxSection(panel, "Transient", "Fx.Transient");
            transient.Add(FxKnob(new KnobElement("Attack", -1f, 1f, 0f, KnobFormat.Percent, bipolar: true), "Fx.Transient.Attack"));
            transient.Add(FxKnob(new KnobElement("Sustain", -1f, 1f, 0f, KnobFormat.Percent, bipolar: true), "Fx.Transient.Sustain"));

            var distortion = FxSection(panel, "Distortion", "Fx.Distortion");
            var mode = new EnumField(DistortionMode.Tanh);
            mode.AddToClassList("forge-fx__mode");
            distortion.Add(BindFx(mode, "Fx.Distortion.Mode"));
            distortion.Add(FxKnob(new KnobElement("Drive", 0f, DistortionSettings.MaxDriveDb, 6f, KnobFormat.Decibels), "Fx.Distortion.DriveDb"));
            distortion.Add(FxKnob(new KnobElement("Mix", 0f, 1f, 1f, KnobFormat.Percent), "Fx.Distortion.Mix"));

            var delay = FxSection(panel, "Delay", "Fx.Delay");
            delay.Add(FxKnob(new KnobElement("Time", DelaySettings.MinTimeMs, DelaySettings.MaxTimeMs, 180f,
                KnobFormat.Milliseconds, KnobScale.Log), "Fx.Delay.TimeMs"));
            delay.Add(FxKnob(new KnobElement("Feedback", 0f, DelaySettings.MaxFeedback, 0.35f, KnobFormat.Percent), "Fx.Delay.Feedback"));
            delay.Add(FxKnob(new KnobElement("Mix", 0f, 1f, 0.25f, KnobFormat.Percent), "Fx.Delay.Mix"));
            delay.Add(BindFx(FxToggle("Ping-Pong"), "Fx.Delay.PingPong"));

            var reverb = FxSection(panel, "Reverb", "Fx.Reverb");
            reverb.Add(FxKnob(new KnobElement("Size", 0f, 1f, 0.5f, KnobFormat.Percent), "Fx.Reverb.Size"));
            reverb.Add(FxKnob(new KnobElement("Damping", 0f, 1f, 0.5f, KnobFormat.Percent), "Fx.Reverb.Damping"));
            reverb.Add(FxKnob(new KnobElement("Mix", 0f, 1f, 0.2f, KnobFormat.Percent), "Fx.Reverb.Mix"));

            var limiter = FxSection(panel, "Limiter", "Fx.Limiter");
            limiter.Add(FxKnob(new KnobElement("Ceiling", LimiterSettings.MinCeilingDb, 0f, -1f, KnobFormat.Decibels), "Fx.Limiter.CeilingDb"));
            limiter.Add(FxKnob(new KnobElement("Release", LimiterSettings.MinReleaseMs, LimiterSettings.MaxReleaseMs, 60f,
                KnobFormat.Milliseconds, KnobScale.Log), "Fx.Limiter.ReleaseMs"));

            return panel;
        }

        // Returns the row that holds the effect's controls; the header carries the enable toggle.
        private VisualElement FxSection(VisualElement panel, string title, string path)
        {
            var section = new VisualElement();
            section.AddToClassList("forge-fx");

            var toggle = FxToggle(title);
            toggle.AddToClassList("forge-fx__enable");
            section.Add(BindFx(toggle, path + ".Enabled"));

            var controls = new VisualElement();
            controls.AddToClassList("forge-fx__controls");
            section.Add(controls);

            panel.Add(section);
            return controls;
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
            _readout.Add(_readoutWarnings);
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
            _readout.Add(item);
            return value;
        }

        private VisualElement BuildExportPanel()
        {
            var panel = new Foldout { text = "Export", value = _exportExpanded };
            panel.AddToClassList("forge-export");
            // Child toggles raise bool change events that bubble up to the foldout.
            panel.RegisterValueChangedCallback(e =>
            {
                if (e.target == panel) _exportExpanded = e.newValue;
            });

            var folderRow = new VisualElement();
            folderRow.AddToClassList("forge-export__row");
            var folder = ExportField(new TextField("Folder"), nameof(ExportSettings.Folder));
            folder.AddToClassList("forge-export__folder");
            folderRow.Add(folder);
            folderRow.Add(MakeButton("…", PickExportFolder, "forge-button--icon"));
            panel.Add(folderRow);

            panel.Add(ExportField(new TextField("Name"), nameof(ExportSettings.NameTemplate)));
            var count = ExportField(new IntegerField("Count"), nameof(ExportSettings.Count));
            count.RegisterValueChangedCallback(e => UpdateExportButton(e.newValue));
            panel.Add(count);
            panel.Add(ExportField(new EnumField("Format", WavFormat.Pcm16), nameof(ExportSettings.Format)));
            panel.Add(ExportField(new EnumField("Channels", ExportChannels.Auto), nameof(ExportSettings.Channels)));
            var normalize = ExportField(new EnumField("Normalize", NormalizeMode.None), nameof(ExportSettings.Normalize));
            normalize.RegisterValueChangedCallback(e => UpdateNormalizeFields((NormalizeMode)e.newValue));
            panel.Add(normalize);
            _loudnessTarget = ExportField(new FloatField("LUFS"), nameof(ExportSettings.LoudnessTargetLufs));
            panel.Add(_loudnessTarget);
            _peakTarget = ExportField(new FloatField("dBTP"), nameof(ExportSettings.PeakTargetDb));
            panel.Add(_peakTarget);
            panel.Add(ExportField(new Toggle("Trim"), nameof(ExportSettings.TrimSilence)));
            panel.Add(ExportField(new FloatField("Fade ms"), nameof(ExportSettings.FadeOutMs)));

            _exportButton = MakeButton("Export WAV", Export, "forge-primary");
            _exportButton.AddToClassList("forge-export__button");
            panel.Add(_exportButton);

            // Beside the toggle rather than inside it, so clicking it doesn't fold the panel.
            var help = HelpButton(ForgeHelp.Export);
            help.AddToClassList("forge-help-button--corner");
            panel.hierarchy.Add(help);

            UpdateNormalizeFields(_export.Normalize);
            UpdateExportButton(_export.Count);
            return panel;
        }

        private T ExportField<T>(T field, string name) where T : BindableElement
        {
            field.AddToClassList("forge-export__field");
            field.BindProperty(_serializedWindow.FindProperty($"{nameof(_export)}.{name}"));
            return field;
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

            _status.text = "Pick a folder inside this project's Assets folder.";
            return null;
        }

        // ── Presets ─────────────────────────────────────────────────────────────────

        private void StepPreset(int direction)
        {
            if (_recipe == null) return;

            ForgePresets.Find(_presetFolder, _presetPaths);
            if (_presetPaths.Count == 0)
            {
                _status.text = $"No presets in {_presetFolder}. Save one first.";
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
            _status.text = $"Loaded preset {path}";
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
            _status.text = $"Saved preset {path}";
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

            _routesFoldout.text = $"Routes ({routes.arraySize})";
        }

        private void AddRoute()
        {
            Undo.RecordObject(_recipe, "Add Route");
            _recipe.Routes.Add(new ModRoute(ModSource.Lfo, ModTarget.Pitch, 0f));
            _routesFoldout.value = true;
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
            _lfoShape.BindProperty(_serializedRecipe.FindProperty("Lfo.Shape"));
            _lfoRate.BindProperty(_serializedRecipe.FindProperty("Lfo.RateHz"));
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
                _strips.Add(strip);
            }

            RefreshCurveEditor();

            _addLayer.SetEnabled(layers.arraySize < SfxRecipe.MaxLayers);
        }

        private void RefreshSelectors()
        {
            foreach (var pair in _categoryButtons)
                pair.Value.EnableInClassList("forge-selected", pair.Key == _recipe.Category);
            foreach (var pair in _harmonyButtons)
                pair.Value.EnableInClassList("forge-selected", pair.Key == _recipe.Randomizer.Harmony);
            _length.Locked = _recipe.Randomizer.LockLength;
            _fxTitle.EnableInClassList("forge-locked-title", _recipe.Randomizer.LockFx);
            RefreshCurveEditor();
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
                    _status.text = $"No template for {_recipe.Category} in {ForgeAssets.TemplatesFolder}.";
                    return;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            var baseSeed = math.hash(new uint2((uint)Environment.TickCount, ++_seedCounter));
            _generator.Generate(_recipe, template, baseSeed, _recipe.Randomizer.CandidateCount, VariationCount, _variations);
            stopwatch.Stop();

            _status.text = $"{(mutate ? "Mutate" : "Randomize")}: kept {_variations.Count} of {_generator.CandidateCount} " +
                           $"candidates, {_generator.Rejected} rejected as defective or outliers " +
                           $"({stopwatch.Elapsed.TotalMilliseconds:0} ms).";

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
            _waveform.Playhead = -1f;
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
            _waveform.Playhead = position / (float)_renderer.FrameCount;
            UpdateMeter(position);
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
            _status.text = _batchExporter.Export(_recipe, _export, _trimStart, _trimEnd);
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
    }
}
