using System;
using DataKeeper.Forge;
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

        private readonly Label _index;
        private readonly Toggle _enabled;
        private readonly TextField _name;
        private readonly EnumField _band;
        private readonly ToolbarToggle _lock;
        private readonly ToolbarToggle _mute;
        private readonly ToolbarToggle _solo;
        private readonly EnumField _sourceType;
        private readonly EnumField _waveform;
        private readonly EnumField _noiseColor;
        private readonly EnumField _filterType;

        private readonly KnobElement _pitch;
        private readonly KnobElement _cutoff;
        private readonly KnobElement _resonance;
        private readonly KnobElement _decay;
        private readonly KnobElement _level;
        private readonly KnobElement _pan;
        private readonly KnobElement _offset;

        private readonly VisualElement _fmGroup;
        private readonly VisualElement _wavetableGroup;
        private readonly VisualElement _sampleGroup;
        private readonly VisualElement _granularGroup;
        private readonly KnobElement _fmRatio;
        private readonly KnobElement _fmIndex;
        private readonly KnobElement _fmEnvelope;
        private readonly EnumField _wavetableBank;
        private readonly KnobElement _wavetablePosition;
        private readonly ObjectField _sampleClip;
        private readonly KnobElement _sampleStart;
        private readonly ToolbarToggle _sampleReverse;
        private readonly EnumField _sampleInterpolation;
        private readonly KnobElement _grainSize;
        private readonly KnobElement _grainDensity;
        private readonly KnobElement _grainSpray;
        private readonly KnobElement _grainPitchRandom;

        private readonly (KnobElement Knob, LayerParam Param)[] _lockableKnobs;
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

            var header = Row("header");
            _index = new Label();
            _index.AddToClassList(UssClassName + "__index");
            _enabled = new Toggle { focusable = false };
            _enabled.AddToClassList(UssClassName + "__enabled");
            _name = new TextField();
            _name.AddToClassList(UssClassName + "__name");
            _band = new EnumField(Band.Body);
            _band.AddToClassList(UssClassName + "__band");
            _lock = FlagToggle("L", "lock");
            _mute = FlagToggle("M", "mute");
            _solo = FlagToggle("S", "solo");

            header.Add(_index);
            header.Add(_enabled);
            header.Add(_name);
            header.Add(_band);
            header.Add(_lock);
            header.Add(_mute);
            header.Add(_solo);
            header.Add(ActionButton("Dup", () => DuplicateRequested?.Invoke(_layerIndex)));
            header.Add(ActionButton("×", () => RemoveRequested?.Invoke(_layerIndex)));

            var source = Row("source");
            _sourceType = Dropdown(SourceType.Oscillator, "Source");
            _waveform = Dropdown(Waveform.Sine, "Wave");
            _noiseColor = Dropdown(NoiseColor.White, "Color");
            _filterType = Dropdown(FilterType.Off, "Filter");
            source.Add(_sourceType);
            source.Add(_waveform);
            source.Add(_noiseColor);
            source.Add(_filterType);

            var sourceParams = Row("source-params");
            _fmGroup = Group(sourceParams);
            _fmRatio = new KnobElement("Ratio", FmSettings.MinRatio, FmSettings.MaxRatio, 2f, KnobFormat.Ratio, KnobScale.Log);
            _fmIndex = new KnobElement("Index", 0f, FmSettings.MaxIndex, 2f);
            _fmEnvelope = new KnobElement("Index Env", 0f, 1f, 0.5f, KnobFormat.Percent);
            _fmGroup.Add(_fmRatio);
            _fmGroup.Add(_fmIndex);
            _fmGroup.Add(_fmEnvelope);

            _wavetableGroup = Group(sourceParams);
            _wavetableBank = Dropdown(WavetableBank.Basic, "Bank");
            _wavetablePosition = new KnobElement("Position", 0f, 1f, 0f, KnobFormat.Percent);
            _wavetableGroup.Add(_wavetableBank);
            _wavetableGroup.Add(_wavetablePosition);

            _sampleGroup = Group(sourceParams);
            _sampleClip = new ObjectField("Clip") { objectType = typeof(AudioClip), allowSceneObjects = false };
            _sampleClip.AddToClassList(UssClassName + "__clip");
            _sampleStart = new KnobElement("Start", 0f, SampleSettings.MaxStartMs, 0f, KnobFormat.Milliseconds);
            _sampleReverse = FlagToggle("Rev", "reverse");
            _sampleInterpolation = Dropdown(SampleInterpolation.Cubic, "Interp");
            _sampleGroup.Add(_sampleClip);
            _sampleGroup.Add(_sampleStart);
            _sampleGroup.Add(_sampleReverse);
            _sampleGroup.Add(_sampleInterpolation);

            _granularGroup = Group(sourceParams);
            _grainSize = new KnobElement("Grain", GranularSettings.MinGrainMs, GranularSettings.MaxGrainMs, 60f,
                KnobFormat.Milliseconds, KnobScale.Log);
            _grainDensity = new KnobElement("Density", GranularSettings.MinDensity, GranularSettings.MaxDensity, 30f,
                KnobFormat.Number, KnobScale.Log);
            _grainSpray = new KnobElement("Spray", 0f, GranularSettings.MaxSprayMs, 20f, KnobFormat.Milliseconds);
            _grainPitchRandom = new KnobElement("Pitch Rnd", 0f, GranularSettings.MaxPitchRandom, 0f, KnobFormat.Semitones);
            _granularGroup.Add(_grainSize);
            _granularGroup.Add(_grainDensity);
            _granularGroup.Add(_grainSpray);
            _granularGroup.Add(_grainPitchRandom);

            var knobs = Row("knobs");
            _pitch = new KnobElement("Pitch", ParamRanges.PitchMin, ParamRanges.PitchMax, 0f,
                KnobFormat.Semitones, bipolar: true);
            _cutoff = new KnobElement("Cutoff", ParamRanges.CutoffMin, ParamRanges.CutoffMax, 2000f,
                KnobFormat.Hertz, KnobScale.Log);
            _resonance = new KnobElement("Reso", 0f, 1f, 0.1f, KnobFormat.Percent);
            _decay = new KnobElement("Decay", ParamRanges.DecayMinMs, ParamRanges.DecayMaxMs, 500f,
                KnobFormat.Milliseconds, KnobScale.Log);
            _level = new KnobElement("Level", ParamRanges.LevelMinDb, ParamRanges.LevelMaxDb, -6f,
                KnobFormat.Decibels);
            _pan = new KnobElement("Pan", -1f, 1f, 0f, KnobFormat.Pan, bipolar: true);
            _offset = new KnobElement("Offset", 0f, ParamRanges.OffsetMaxMs, 0f, KnobFormat.Milliseconds);
            knobs.Add(_pitch);
            knobs.Add(_cutoff);
            knobs.Add(_resonance);
            knobs.Add(_decay);
            knobs.Add(_level);
            knobs.Add(_pan);
            knobs.Add(_offset);

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
            };
            foreach (var (field, param) in _lockableFields)
                field.AddManipulator(new ContextualMenuManipulator(evt => PopulateFieldMenu(evt, param)));

            header.AddManipulator(new ContextualMenuManipulator(PopulateHeaderMenu));

            // Trickle-down so it fires even when a knob or field consumes the click.
            RegisterCallback<PointerDownEvent>(_ => Selected?.Invoke(_layerIndex), TrickleDown.TrickleDown);
        }

        public bool IsSelected
        {
            set => EnableInClassList(UssClassName + "--selected", value);
        }

        public void Bind(SerializedProperty layer, int index)
        {
            _layer = layer;
            _layerIndex = index;
            _index.text = (index + 1).ToString();

            _enabled.BindProperty(Relative("Enabled"));
            _name.BindProperty(Relative("Name"));
            _band.BindProperty(Relative("Band"));
            _lock.BindProperty(Relative("Locked"));
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

        private SerializedProperty Relative(string path) => _layer.FindPropertyRelative(path);

        private void UpdateState()
        {
            if (_layer == null) return;

            var type = (SourceType)Relative("Source.Type").intValue;
            Show(_waveform, type == SourceType.Oscillator);
            Show(_noiseColor, type == SourceType.Noise);
            Show(_fmGroup, type == SourceType.FM);
            Show(_wavetableGroup, type == SourceType.Wavetable);
            Show(_sampleGroup, SourceSettings.UsesClip(type));
            Show(_granularGroup, type == SourceType.Granular);
            _pitch.SetEnabled(type != SourceType.Noise);

            var filterOn = Relative("Filter.Type").intValue != (int)FilterType.Off;
            _cutoff.SetEnabled(filterOn);
            _resonance.SetEnabled(filterOn);

            EnableInClassList(UssClassName + "--disabled", !Relative("Enabled").boolValue);
            EnableInClassList(UssClassName + "--muted", Relative("Mute").boolValue);
            EnableInClassList(UssClassName + "--locked", Relative("Locked").boolValue);

            var locks = Relative("LockedParams").intValue;
            foreach (var (knob, param) in _lockableKnobs) knob.Locked = (locks & (int)param) != 0;
            foreach (var (field, param) in _lockableFields)
                field.EnableInClassList(UssClassName + "__dropdown--locked", (locks & (int)param) != 0);
            _index.EnableInClassList(UssClassName + "__index--curve-locked", AnyCurveLocked());

            var band = Relative("Band").intValue;
            foreach (Band value in Enum.GetValues(typeof(Band)))
                _band.EnableInClassList($"{UssClassName}__band--{value.ToString().ToLowerInvariant()}", (int)value == band);
        }

        private void PopulateFieldMenu(ContextualMenuPopulateEvent evt, LayerParam param)
        {
            var locked = (Relative("LockedParams").intValue & (int)param) != 0;
            evt.menu.AppendAction(locked ? "Unlock" : "Lock", _ => SetParamLock(param, !locked));
        }

        private void PopulateHeaderMenu(ContextualMenuPopulateEvent evt)
        {
            var layerLocked = Relative("Locked").boolValue;
            evt.menu.AppendAction(layerLocked ? "Unlock Layer" : "Lock Layer", _ => SetBool("Locked", !layerLocked));

            var curvesLocked = AnyCurveLocked();
            evt.menu.AppendAction(curvesLocked ? "Unlock Curves" : "Lock Curves", _ => SetCurvesLocked(!curvesLocked));

            var hasParamLocks = Relative("LockedParams").intValue != 0;
            evt.menu.AppendAction("Clear Parameter Locks", _ => SetLockFlags(0),
                hasParamLocks ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
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

        private static VisualElement Group(VisualElement parent)
        {
            var group = new VisualElement();
            group.AddToClassList(UssClassName + "__group");
            parent.Add(group);
            return group;
        }

        private VisualElement Row(string name)
        {
            var row = new VisualElement();
            row.AddToClassList($"{UssClassName}__{name}");
            Add(row);
            return row;
        }

        private static ToolbarToggle FlagToggle(string text, string flag)
        {
            // Not focusable, so the window's Space hotkey is never swallowed as a toggle press.
            var toggle = new ToolbarToggle { text = text, focusable = false };
            toggle.AddToClassList(UssClassName + "__flag");
            toggle.AddToClassList($"{UssClassName}__flag--{flag}");
            return toggle;
        }

        private static Button ActionButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text, focusable = false };
            button.AddToClassList(UssClassName + "__action");
            return button;
        }

        private static EnumField Dropdown(Enum initial, string label)
        {
            var field = new EnumField(label, initial);
            field.AddToClassList(UssClassName + "__dropdown");
            return field;
        }
    }
}
