using System;
using DataKeeper.Forge;
using DataKeeper.Forge.Render;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    [UxmlElement]
    public partial class LayerStripElement : VisualElement
    {
        public const string UssClassName = "forge-strip";

        private const string LockedFieldClass = UssClassName + "__field--locked";

        // The modes the WARP stepper offers; the renderer plays the others as Off until they exist.
        private static readonly WarpMode[] WarpModes = { WarpMode.Off, WarpMode.Sync };

        private readonly VisualElement _tab;
        private readonly Label _index;
        private readonly Toggle _enabled;
        private readonly TextField _name;
        private readonly EnumField _band;
        private readonly ToolbarToggle _mute;
        private readonly ToolbarToggle _solo;
        private readonly Button _menuButton;

        private readonly StepperElement _sourceType;
        private readonly StepperElement _waveform;
        private readonly StepperElement _noiseColor;
        private readonly StepperElement _wavetableBank;
        private readonly ObjectField _sampleClip;
        private readonly ScopeElement _scope;
        private readonly WaveformElement _wave;

        private readonly KnobElement _pitch;
        private readonly KnobElement _offset;
        private readonly KnobElement _decay;
        private readonly KnobElement _level;
        private readonly KnobElement _pan;
        private readonly StepperElement _filterType;
        private readonly KnobElement _cutoff;
        private readonly KnobElement _resonance;

        private readonly ParamBoxElement _sourceBox;
        private readonly VisualElement _fmGroup;
        private readonly VisualElement _wavetableGroup;
        private readonly VisualElement _sampleGroup;
        private readonly VisualElement _granularGroup;
        private readonly VisualElement _shepardGroup;
        private readonly KnobElement _fmRatio;
        private readonly KnobElement _fmIndex;
        private readonly KnobElement _fmEnvelope;
        private readonly KnobElement _wavetablePosition;
        private readonly KnobElement _sampleStart;
        private readonly ToolbarToggle _sampleReverse;
        private readonly StepperElement _sampleInterpolation;
        private readonly KnobElement _grainSize;
        private readonly KnobElement _grainDensity;
        private readonly KnobElement _grainSpray;
        private readonly KnobElement _grainPitchRandom;
        private readonly KnobElement _shepardRate;
        private readonly KnobElement _shepardWidth;
        private readonly KnobElement _shepardPartials;

        private readonly ParamBoxElement _warpBox;
        private readonly VisualElement _warpModeStepper;
        private readonly Button _warpMode;
        private readonly KnobElement _warpAmount;

        private readonly ParamBoxElement _unisonBox;
        private readonly KnobElement _voices;
        private readonly KnobElement _detune;
        private readonly KnobElement _spread;
        private readonly KnobElement _phase;
        private readonly ToolbarToggle _phaseRandom;

        private readonly (KnobElement Knob, LayerParam Param)[] _lockableKnobs;
        private readonly (KnobElement Knob, ModTarget Target)[] _modKnobs;
        private readonly (VisualElement Field, LayerParam Param)[] _lockableFields;

        private SerializedProperty _layer;
        private int _layerIndex;

        private static readonly string[] CurvePaths = { "AmpCurve", "PitchCurve", "CutoffCurve", "PanCurve" };

        public event Action<int> Selected;
        public event Action<int> RemoveRequested;
        public event Action<int> DuplicateRequested;

        public LayerStripElement()
        {
            AddToClassList(UssClassName);

            _tab = Part(this, "tab");
            _enabled = new Toggle { focusable = false };
            _enabled.AddToClassList(UssClassName + "__enabled");
            _tab.Add(_enabled);
            _index = new Label();
            _index.AddToClassList(UssClassName + "__index");
            _tab.Add(_index);

            var body = Part(this, "body");

            var head = Part(body, "head");
            _name = new TextField();
            _name.AddToClassList(UssClassName + "__name");
            head.Add(_name);
            _band = new EnumField(Band.Body);
            _band.AddToClassList(UssClassName + "__band");
            head.Add(_band);
            _mute = FlagToggle("M", "mute");
            _solo = FlagToggle("S", "solo");
            head.Add(_mute);
            head.Add(_solo);
            _menuButton = new Button(() => ShowMenu(_menuButton.worldBound))
            {
                text = "…",
                focusable = false,
            };
            _menuButton.AddToClassList(UssClassName + "__menu");
            head.Add(_menuButton);

            var main = Part(body, "main");

            var display = Part(main, "display");
            var sourceRow = Part(display, "source");
            _sourceType = Stepper(sourceRow, SourceType.Oscillator, "source-type");
            _waveform = Stepper(sourceRow, Waveform.Sine, "variant");
            _noiseColor = Stepper(sourceRow, NoiseColor.White, "variant");
            _wavetableBank = Stepper(sourceRow, WavetableBank.Basic, "variant");
            _sampleClip = new ObjectField { objectType = typeof(AudioClip), allowSceneObjects = false };
            _sampleClip.AddToClassList(UssClassName + "__clip");
            sourceRow.Add(_sampleClip);

            var views = Part(display, "views");
            _scope = new ScopeElement { pickingMode = PickingMode.Ignore };
            views.Add(_scope);
            _wave = new WaveformElement { ShowTrim = false, Normalize = true, pickingMode = PickingMode.Ignore };
            _wave.AddToClassList(UssClassName + "__wave");
            views.Add(_wave);

            // The boxes wrap as one group, so strips with the same controls always lay out the same.
            var controls = Part(main, "controls");

            var voice = Box(controls, "VOICE");
            _pitch = Knob(voice, new KnobElement("Pitch", ParamRanges.PitchMin, ParamRanges.PitchMax, 0f,
                KnobFormat.Semitones, bipolar: true));
            _offset = Knob(voice, new KnobElement("Offset", 0f, ParamRanges.OffsetMaxMs, 0f, KnobFormat.Milliseconds));
            _decay = Knob(voice, new KnobElement("Decay", ParamRanges.DecayMinMs, ParamRanges.DecayMaxMs, 500f,
                KnobFormat.Milliseconds, KnobScale.Log));

            var amp = Box(controls, "AMP");
            _level = Knob(amp, new KnobElement("Level", ParamRanges.LevelMinDb, ParamRanges.LevelMaxDb, -6f,
                KnobFormat.Decibels));
            _pan = Knob(amp, new KnobElement("Pan", -1f, 1f, 0f, KnobFormat.Pan, bipolar: true));

            // Stacked rows instead of a 100%-wide stepper in a wrapping row, which UI Toolkit
            // measures too short and the strip then clips.
            var filter = Box(controls, "FILTER");
            filter.AddToClassList(ParamBoxElement.UssClassName + "--stacked");
            _filterType = Stepper(filter, FilterType.Off, "filter");
            var filterKnobs = Part(filter, "knob-row");
            _cutoff = Knob(filterKnobs, new KnobElement("Cutoff", ParamRanges.CutoffMin, ParamRanges.CutoffMax, 2000f,
                KnobFormat.Hertz, KnobScale.Log));
            _resonance = Knob(filterKnobs, new KnobElement("Reso", 0f, 1f, 0.1f, KnobFormat.Percent));

            _sourceBox = Box(controls, "SOURCE");
            _fmGroup = Part(_sourceBox, "group");
            _fmRatio = Knob(_fmGroup, new KnobElement("Ratio", FmSettings.MinRatio, FmSettings.MaxRatio, 2f,
                KnobFormat.Ratio, KnobScale.Log));
            _fmIndex = Knob(_fmGroup, new KnobElement("Index", 0f, FmSettings.MaxIndex, 2f));
            _fmEnvelope = Knob(_fmGroup, new KnobElement("Index Env", 0f, 1f, 0.5f, KnobFormat.Percent));

            _wavetableGroup = Part(_sourceBox, "group");
            _wavetablePosition = Knob(_wavetableGroup, new KnobElement("Position", 0f, 1f, 0f, KnobFormat.Percent));

            _sampleGroup = Part(_sourceBox, "group");
            _sampleStart = Knob(_sampleGroup, new KnobElement("Start", 0f, SampleSettings.MaxStartMs, 0f,
                KnobFormat.Milliseconds));
            var sampleOptions = Part(_sampleGroup, "options");
            _sampleReverse = FlagToggle("Rev", "reverse");
            sampleOptions.Add(_sampleReverse);
            _sampleInterpolation = Stepper(sampleOptions, SampleInterpolation.Cubic, "interp");

            _granularGroup = Part(_sourceBox, "group");
            _grainSize = Knob(_granularGroup, new KnobElement("Grain", GranularSettings.MinGrainMs,
                GranularSettings.MaxGrainMs, 60f, KnobFormat.Milliseconds, KnobScale.Log));
            _grainDensity = Knob(_granularGroup, new KnobElement("Density", GranularSettings.MinDensity,
                GranularSettings.MaxDensity, 30f, KnobFormat.Number, KnobScale.Log));
            _grainSpray = Knob(_granularGroup, new KnobElement("Spray", 0f, GranularSettings.MaxSprayMs, 20f,
                KnobFormat.Milliseconds));
            _grainPitchRandom = Knob(_granularGroup, new KnobElement("Pitch Rnd", 0f, GranularSettings.MaxPitchRandom,
                0f, KnobFormat.Semitones));

            _shepardGroup = Part(_sourceBox, "group");
            _shepardRate = Knob(_shepardGroup, new KnobElement("Rate", -ShepardSettings.MaxRateOctaves,
                ShepardSettings.MaxRateOctaves, 1f, KnobFormat.OctavesPerSecond, bipolar: true));
            _shepardWidth = Knob(_shepardGroup, new KnobElement("Width", 0f, 1f, 0.5f, KnobFormat.Percent));
            _shepardPartials = Knob(_shepardGroup, new KnobElement("Partials", ShepardSettings.MinPartials,
                ShepardSettings.MaxPartials, 8f, KnobFormat.Integer) { WholeNumbers = true });
            // Bound by hand like Voices: Partials is an int property.
            _shepardPartials.RegisterValueChangedCallback(evt => SetInt("Source.Shepard.Partials", (int)evt.newValue));

            // Hand-built rather than a StepperElement: an EnumField cannot hide the modes that are
            // not implemented yet.
            _warpBox = Box(controls, "WARP");
            _warpBox.AddToClassList(ParamBoxElement.UssClassName + "--stacked");
            _warpModeStepper = Part(_warpBox, "stepper--warp");
            _warpModeStepper.AddToClassList(StepperElement.UssClassName);
            _warpModeStepper.Add(StepperArrow("<", () => StepWarpMode(-1)));
            _warpMode = new Button(ShowWarpMenu) { focusable = false };
            _warpMode.AddToClassList(StepperElement.UssClassName + "__label");
            _warpModeStepper.Add(_warpMode);
            _warpModeStepper.Add(StepperArrow(">", () => StepWarpMode(1)));
            var warpKnobs = Part(_warpBox, "knob-row");
            _warpAmount = Knob(warpKnobs, new KnobElement("Amount", 0f, 1f, 0f, KnobFormat.Percent));

            _unisonBox = Box(controls, "UNISON");
            _voices = Knob(_unisonBox, new KnobElement("Voices", UnisonSettings.MinVoices, UnisonSettings.MaxVoices,
                UnisonSettings.MinVoices, KnobFormat.Integer) { WholeNumbers = true });
            _detune = Knob(_unisonBox, new KnobElement("Detune", 0f, UnisonSettings.MaxDetuneCents, 0f, KnobFormat.Cents));
            _spread = Knob(_unisonBox, new KnobElement("Spread", 0f, 1f, 0f, KnobFormat.Percent));
            _phase = Knob(_unisonBox, new KnobElement("Phase", 0f, 1f, 0f, KnobFormat.Degrees));
            var phaseOptions = Part(_unisonBox, "options");
            _phaseRandom = FlagToggle("Rnd", "random");
            phaseOptions.Add(_phaseRandom);
            // Bound by hand: the knob is a float field and Voices is an int property.
            _voices.RegisterValueChangedCallback(evt => SetInt("Unison.Voices", (int)evt.newValue));

            _modKnobs = new[]
            {
                (_pitch, ModTarget.Pitch),
                (_cutoff, ModTarget.Cutoff),
                (_level, ModTarget.Level),
                (_pan, ModTarget.Pan),
                (_decay, ModTarget.Decay),
                (_resonance, ModTarget.Resonance),
                (_warpAmount, ModTarget.Warp),
            };
            foreach (var (knob, target) in _modKnobs) knob.ModTarget = target;

            _lockableKnobs = new[]
            {
                (_pitch, LayerParam.Pitch),
                (_cutoff, LayerParam.Cutoff),
                (_resonance, LayerParam.Resonance),
                (_decay, LayerParam.Decay),
                (_level, LayerParam.Level),
                (_pan, LayerParam.Pan),
                (_offset, LayerParam.Offset),
                (_fmRatio, LayerParam.Source),
                (_fmIndex, LayerParam.Source),
                (_fmEnvelope, LayerParam.Source),
                (_wavetablePosition, LayerParam.Source),
                (_sampleStart, LayerParam.Source),
                (_grainSize, LayerParam.Source),
                (_grainDensity, LayerParam.Source),
                (_grainSpray, LayerParam.Source),
                (_grainPitchRandom, LayerParam.Source),
                (_shepardRate, LayerParam.Source),
                (_shepardWidth, LayerParam.Source),
                (_shepardPartials, LayerParam.Source),
                (_warpAmount, LayerParam.Source),
                (_voices, LayerParam.Source),
                (_detune, LayerParam.Source),
                (_spread, LayerParam.Source),
                (_phase, LayerParam.Source),
            };
            foreach (var (knob, param) in _lockableKnobs)
            {
                knob.Lockable = true;
                knob.LockToggled += locked => SetParamLock(param, locked);
            }

            _lockableFields = new (VisualElement, LayerParam)[]
            {
                (_sourceType, LayerParam.Source),
                (_waveform, LayerParam.Source),
                (_noiseColor, LayerParam.Source),
                (_filterType, LayerParam.FilterType),
                (_wavetableBank, LayerParam.Source),
                (_sampleClip, LayerParam.Source),
                (_sampleReverse, LayerParam.Source),
                (_sampleInterpolation, LayerParam.Source),
                (_phaseRandom, LayerParam.Source),
                (_warpModeStepper, LayerParam.Source),
            };
            foreach (var (field, param) in _lockableFields)
                field.AddManipulator(new ContextualMenuManipulator(evt => PopulateFieldMenu(evt, param)));

            var help = ForgeHelp.Layers;
            ForgeHints.Set(_tab, help, "Side tab");
            ForgeHints.Set(_name, help, "Name, band");
            ForgeHints.Set(_band, help, "Name, band");
            ForgeHints.Set(_mute, help, "M  S");
            ForgeHints.Set(_solo, help, "M  S");
            ForgeHints.Set(_menuButton, help, "…");
            ForgeHints.Set(sourceRow, help, "Source");
            ForgeHints.Set(views, help, "Pictures");
            ForgeHints.Set(_pitch, help, "Pitch");
            ForgeHints.Set(_offset, help, "Offset");
            ForgeHints.Set(_decay, help, "Decay");
            ForgeHints.Set(amp, help, "Level / Pan");
            ForgeHints.Set(filter, help, "Filter");
            ForgeHints.Set(_fmGroup, help, "FM");
            ForgeHints.Set(_wavetableGroup, help, "Wavetable");
            ForgeHints.Set(_sampleGroup, help, "Sample");
            ForgeHints.Set(_sampleClip, help, "Sample");
            ForgeHints.Set(_granularGroup, help, "Granular");
            ForgeHints.Set(_shepardGroup, help, "Shepard");
            ForgeHints.Set(_warpBox, help, "Warp");
            ForgeHints.Set(_unisonBox, help, "Unison");

            head.AddManipulator(new ContextualMenuManipulator(PopulateLayerMenu));
            _tab.AddManipulator(new ContextualMenuManipulator(PopulateLayerMenu));

            // Trickle-down so it fires even when a knob or field consumes the click.
            RegisterCallback<PointerDownEvent>(_ => Selected?.Invoke(_layerIndex), TrickleDown.TrickleDown);
        }

        public bool IsSelected
        {
            set => EnableInClassList(UssClassName + "--selected", value);
        }

        public bool Compact
        {
            set => EnableInClassList(UssClassName + "--compact", value);
        }

        public float Playhead
        {
            set => _wave.Playhead = value;
        }

        public void Bind(SerializedProperty layer, int index)
        {
            _layer = layer;
            _layerIndex = index;
            _index.text = (index + 1).ToString();
            foreach (var (knob, _) in _modKnobs) knob.ModLayer = index;

            _enabled.BindProperty(Relative("Enabled"));
            _name.BindProperty(Relative("Name"));
            _band.BindProperty(Relative("Band"));
            _mute.BindProperty(Relative("Mute"));
            _solo.BindProperty(Relative("Solo"));
            _sourceType.BindProperty(Relative("Source.Type"));
            _waveform.BindProperty(Relative("Source.Oscillator.Waveform"));
            _noiseColor.BindProperty(Relative("Source.Noise.Color"));
            _filterType.BindProperty(Relative("Filter.Type"));
            _fmRatio.BindProperty(Relative("Source.Fm.Ratio"));
            _fmIndex.BindProperty(Relative("Source.Fm.Index"));
            _fmEnvelope.BindProperty(Relative("Source.Fm.IndexEnvelope"));
            _wavetableBank.BindProperty(Relative("Source.Wavetable.Bank"));
            _wavetablePosition.BindProperty(Relative("Source.Wavetable.Position"));
            _sampleClip.BindProperty(Relative("Source.Sample.Clip"));
            _sampleStart.BindProperty(Relative("Source.Sample.StartMs"));
            _sampleReverse.BindProperty(Relative("Source.Sample.Reverse"));
            _sampleInterpolation.BindProperty(Relative("Source.Sample.Interpolation"));
            _grainSize.BindProperty(Relative("Source.Granular.GrainMs"));
            _grainDensity.BindProperty(Relative("Source.Granular.Density"));
            _grainSpray.BindProperty(Relative("Source.Granular.SprayMs"));
            _grainPitchRandom.BindProperty(Relative("Source.Granular.PitchRandom"));
            _shepardRate.BindProperty(Relative("Source.Shepard.RateOctaves"));
            _shepardWidth.BindProperty(Relative("Source.Shepard.Width"));
            _detune.BindProperty(Relative("Unison.DetuneCents"));
            _spread.BindProperty(Relative("Unison.Spread"));
            _phase.BindProperty(Relative("Phase.Start"));
            _phaseRandom.BindProperty(Relative("Phase.Random"));
            _warpAmount.BindProperty(Relative("Warp.Amount"));

            _pitch.BindProperty(Relative("Pitch"));
            _cutoff.BindProperty(Relative("Filter.CutoffHz"));
            _resonance.BindProperty(Relative("Filter.Resonance"));
            _decay.BindProperty(Relative("DecayMs"));
            _level.BindProperty(Relative("LevelDb"));
            _pan.BindProperty(Relative("Pan"));
            _offset.BindProperty(Relative("StartOffsetMs"));

            this.TrackPropertyValue(layer, _ => UpdateState());
            UpdateState();
        }

        // A layer the renderer skipped (off, muted, soloed out, missing clip) draws flat.
        public void ShowRender(SfxRenderer renderer)
        {
            if (_layerIndex >= renderer.LayerCount || !renderer.IsLayerAudible(_layerIndex))
            {
                _wave.ClearSamples();
                _scope.ClearTrace();
                return;
            }

            renderer.LayerFrameRange(_layerIndex, out var start, out var end);
            var output = renderer.LayerOutput(_layerIndex);
            _wave.SetSamples(output, SfxRenderer.Channels, start, end);
            _scope.SetCycles(output, SfxRenderer.Channels, start, end, renderer.SampleRate);
        }

        private SerializedProperty Relative(string path) => _layer.FindPropertyRelative(path);

        private void UpdateState()
        {
            if (_layer == null) return;

            var type = (SourceType)Relative("Source.Type").intValue;
            Show(_waveform, type == SourceType.Oscillator);
            Show(_noiseColor, type == SourceType.Noise);
            Show(_wavetableBank, type == SourceType.Wavetable);
            Show(_sampleClip, SourceSettings.UsesClip(type));
            Show(_fmGroup, type == SourceType.FM);
            Show(_wavetableGroup, type == SourceType.Wavetable);
            Show(_sampleGroup, SourceSettings.UsesClip(type));
            Show(_granularGroup, type == SourceType.Granular);
            Show(_shepardGroup, type == SourceType.Shepard);
            Show(_sourceBox, type != SourceType.Oscillator && type != SourceType.Noise);
            Show(_warpBox, WarpSettings.Supports(type));
            Show(_unisonBox, SourceSettings.IsTonal(type));
            _pitch.SetEnabled(type != SourceType.Noise);

            // Old layers deserialize Voices as 0; the renderer plays them as one voice.
            _voices.SetValueWithoutNotify(Mathf.Max(UnisonSettings.MinVoices, Relative("Unison.Voices").intValue));
            // Old layers deserialize Partials as 0 too; the renderer clamps it to the minimum.
            _shepardPartials.SetValueWithoutNotify(Mathf.Clamp(Relative("Source.Shepard.Partials").intValue,
                ShepardSettings.MinPartials, ShepardSettings.MaxPartials));
            _phase.SetEnabled(!Relative("Phase.Random").boolValue);
            var warpMode = (WarpMode)Relative("Warp.Mode").intValue;
            _warpMode.text = warpMode.ToString();
            _warpAmount.SetEnabled(warpMode != WarpMode.Off);

            var filterOn = Relative("Filter.Type").intValue != (int)FilterType.Off;
            _cutoff.SetEnabled(filterOn);
            _resonance.SetEnabled(filterOn);

            EnableInClassList(UssClassName + "--disabled", !Relative("Enabled").boolValue);
            EnableInClassList(UssClassName + "--muted", Relative("Mute").boolValue);
            EnableInClassList(UssClassName + "--locked", Relative("Locked").boolValue);

            var locks = Relative("LockedParams").intValue;
            foreach (var (knob, param) in _lockableKnobs) knob.Locked = (locks & (int)param) != 0;
            foreach (var (field, param) in _lockableFields)
                field.EnableInClassList(LockedFieldClass, (locks & (int)param) != 0);
            _index.EnableInClassList(UssClassName + "__index--curve-locked", AnyCurveLocked());

            var band = Relative("Band").intValue;
            foreach (Band value in Enum.GetValues(typeof(Band)))
            {
                var name = value.ToString().ToLowerInvariant();
                _band.EnableInClassList($"{UssClassName}__band--{name}", (int)value == band);
                EnableInClassList($"{UssClassName}--band-{name}", (int)value == band);
            }
        }

        private void PopulateFieldMenu(ContextualMenuPopulateEvent evt, LayerParam param)
        {
            var locked = (Relative("LockedParams").intValue & (int)param) != 0;
            evt.menu.AppendAction(locked ? "Unlock" : "Lock", _ => SetParamLock(param, !locked));
        }

        private void PopulateLayerMenu(ContextualMenuPopulateEvent evt) =>
            AddLayerMenuItems((name, isChecked, enabled, action) => evt.menu.AppendAction(name, _ => action(),
                !enabled ? DropdownMenuAction.Status.Disabled
                : isChecked ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal));

        private void ShowMenu(Rect anchor)
        {
            var menu = new GenericDropdownMenu();
            AddLayerMenuItems((name, isChecked, enabled, action) =>
            {
                if (enabled) menu.AddItem(name, isChecked, action);
                else menu.AddDisabledItem(name, isChecked);
            });
            menu.DropDown(anchor, this, DropdownMenuSizeMode.Auto);
        }

        // One item list for both the … button and the right-click menu, so they never drift apart.
        private void AddLayerMenuItems(Action<string, bool, bool, Action> add)
        {
            var layerLocked = Relative("Locked").boolValue;
            var curvesLocked = AnyCurveLocked();
            var hasParamLocks = Relative("LockedParams").intValue != 0;

            add("Duplicate", false, true, () => DuplicateRequested?.Invoke(_layerIndex));
            add("Remove", false, true, () => RemoveRequested?.Invoke(_layerIndex));
            add("Lock Layer", layerLocked, true, () => SetBool("Locked", !layerLocked));
            add("Lock Curves", curvesLocked, true, () => SetCurvesLocked(!curvesLocked));
            add("Clear Parameter Locks", false, hasParamLocks, () => SetLockFlags(0));
        }

        private void StepWarpMode(int direction)
        {
            var index = Array.IndexOf(WarpModes, (WarpMode)Relative("Warp.Mode").intValue);
            index = index < 0 ? 0 : (index + direction + WarpModes.Length) % WarpModes.Length;
            SetInt("Warp.Mode", (int)WarpModes[index]);
        }

        private void ShowWarpMenu()
        {
            var current = Relative("Warp.Mode").intValue;
            var menu = new GenericDropdownMenu();
            foreach (var mode in WarpModes)
            {
                var choice = (int)mode;
                menu.AddItem(mode.ToString(), choice == current, () => SetInt("Warp.Mode", choice));
            }

            menu.DropDown(_warpMode.worldBound, this, DropdownMenuSizeMode.Auto);
        }

        private void SetInt(string path, int value)
        {
            var property = Relative(path);
            if (property.intValue == value) return;

            property.intValue = value;
            Apply();
        }

        private void SetParamLock(LayerParam param, bool locked)
        {
            var flags = Relative("LockedParams").intValue;
            SetLockFlags(locked ? flags | (int)param : flags & ~(int)param);
        }

        private void SetLockFlags(int flags)
        {
            Relative("LockedParams").intValue = flags;
            Apply();
        }

        private bool AnyCurveLocked()
        {
            foreach (var path in CurvePaths)
                if (Relative(path + ".Locked").boolValue) return true;
            return false;
        }

        private void SetCurvesLocked(bool locked)
        {
            foreach (var path in CurvePaths) Relative(path + ".Locked").boolValue = locked;
            Apply();
        }

        private void SetBool(string path, bool value)
        {
            Relative(path).boolValue = value;
            Apply();
        }

        private void Apply()
        {
            _layer.serializedObject.ApplyModifiedProperties();
            UpdateState();
        }

        private static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private static VisualElement Part(VisualElement parent, string name)
        {
            var part = new VisualElement();
            part.AddToClassList($"{UssClassName}__{name}");
            parent.Add(part);
            return part;
        }

        private static ParamBoxElement Box(VisualElement parent, string title)
        {
            var box = new ParamBoxElement(title);
            parent.Add(box);
            return box;
        }

        private static KnobElement Knob(VisualElement parent, KnobElement knob)
        {
            knob.AddToClassList(UssClassName + "__knob");
            parent.Add(knob);
            return knob;
        }

        private static StepperElement Stepper(VisualElement parent, Enum initial, string role)
        {
            var stepper = new StepperElement(initial);
            stepper.AddToClassList($"{UssClassName}__stepper--{role}");
            parent.Add(stepper);
            return stepper;
        }

        private static Button StepperArrow(string text, Action step)
        {
            var button = new Button(step) { text = text, focusable = false };
            button.AddToClassList(StepperElement.UssClassName + "__arrow");
            return button;
        }

        private static ToolbarToggle FlagToggle(string text, string flag)
        {
            // Not focusable, so the window's Space hotkey is never swallowed as a toggle press.
            var toggle = new ToolbarToggle { text = text, focusable = false };
            toggle.AddToClassList(UssClassName + "__flag");
            toggle.AddToClassList($"{UssClassName}__flag--{flag}");
            return toggle;
        }
    }
}
