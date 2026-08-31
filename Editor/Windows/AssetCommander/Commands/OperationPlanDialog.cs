using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The one thing standing between a command and the project. Every mutating command goes
    // through here: the rows shown are the rows that will run, and changing an option rebuilds
    // the plan rather than reinterpreting it at execution time.
    public sealed class OperationPlanDialog : EditorWindow
    {
        private OperationPlan _plan;
        private OperationPlan _result;

        private Label _summary;
        private Label _caveat;
        private ListView _rows;
        private Button _confirm;

        public static OperationPlan Confirm(OperationPlan plan)
        {
            if (plan == null) return null;

            if (plan.IsBlocked)
            {
                EditorUtility.DisplayDialog(plan.Title, plan.Blocked, "OK");
                return null;
            }

            if (!plan.CanRun) return null;

            var dialog = CreateInstance<OperationPlanDialog>();
            dialog._plan = plan;
            dialog.titleContent = new GUIContent(plan.Title);
            dialog.minSize = new Vector2(520, 260);
            dialog.position = new Rect(Screen.currentResolution.width * 0.5f - 320f,
                Screen.currentResolution.height * 0.5f - 220f, 640f, 440f);

            dialog.ShowModalUtility();

            return dialog._result;
        }

        public void CreateGUI()
        {
            var style = UxmlLoader.LoadUss("AssetCommander");
            if (style != null) rootVisualElement.styleSheets.Add(style);

            rootVisualElement.AddToClassList("ac-plan");

            _caveat = new Label();
            _caveat.AddToClassList("ac-mode-notice");
            rootVisualElement.Add(_caveat);

            BuildOptions();

            _rows = new ListView
            {
                fixedItemHeight = 20f,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.None,
                makeItem = () => new PlanRow(),
                bindItem = (element, index) => ((PlanRow)element).Bind(_plan.Operations[index]),
            };
            _rows.AddToClassList("ac-plan-rows");
            rootVisualElement.Add(_rows);

            _summary = new Label();
            _summary.AddToClassList("ac-plan-summary");
            rootVisualElement.Add(_summary);

            var buttons = new VisualElement();
            buttons.AddToClassList("ac-plan-buttons");

            var cancel = new Button(Close) { text = "Cancel" };
            _confirm = new Button(Accept) { text = _plan.Verb };
            _confirm.AddToClassList("ac-plan-confirm");

            buttons.Add(new VisualElement { style = { flexGrow = 1f } });
            buttons.Add(cancel);
            buttons.Add(_confirm);
            rootVisualElement.Add(buttons);

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            Bind();
        }

        private void BuildOptions()
        {
            var options = new VisualElement();
            options.AddToClassList("ac-plan-options");

            if (!string.IsNullOrEmpty(_plan.PatternLabel))
            {
                var pattern = new TextField(_plan.PatternLabel) { value = _plan.Options.Pattern };
                pattern.AddToClassList("ac-plan-pattern");
                pattern.RegisterValueChangedCallback(evt => Rebuild(_plan.Options.WithPattern(evt.newValue)));
                options.Add(pattern);

                var help = new Label("{name}  {n}  {n:000}");
                help.AddToClassList("ac-plan-hint");
                options.Add(help);
            }

            if (_plan.ShowStructureOption)
            {
                var structure = new EnumField("Structure", _plan.Options.Structure);
                structure.AddToClassList("ac-plan-option");
                structure.RegisterValueChangedCallback(evt =>
                    Rebuild(_plan.Options.With((FolderStructure)evt.newValue)));
                options.Add(structure);
            }

            if (_plan.ShowConflictOption)
            {
                var conflict = new EnumField("On collision", _plan.Options.Conflict);
                conflict.AddToClassList("ac-plan-option");
                conflict.RegisterValueChangedCallback(evt =>
                    Rebuild(_plan.Options.With((ConflictResolution)evt.newValue)));
                options.Add(conflict);
            }

            if (options.childCount > 0) rootVisualElement.Add(options);
        }

        // Options do not reinterpret the rows — they rebuild them, so what is listed is always
        // what the current options produce.
        private void Rebuild(PlanOptions options)
        {
            if (_plan.Rebuild == null) return;

            var rebuilt = _plan.Rebuild(options);
            if (rebuilt == null) return;

            rebuilt.Options = options;
            _plan = rebuilt;

            Bind();
        }

        private void Bind()
        {
            bool hasCaveat = !string.IsNullOrEmpty(_plan.Caveat);
            _caveat.EnableInClassList("ac-hidden", !hasCaveat);
            if (hasCaveat) _caveat.text = _plan.Caveat;

            _rows.itemsSource = _plan.Operations;
            _rows.Rebuild();

            _summary.text = _plan.IsBlocked ? _plan.Blocked : _plan.Summary;
            _confirm.SetEnabled(_plan.CanRun);
        }

        private void Accept()
        {
            if (!_plan.CanRun) return;

            _result = _plan;
            Close();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
                return;
            }

            // Return inside the pattern field would otherwise commit a half-typed pattern.
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
            if (evt.target is VisualElement element && element.GetFirstAncestorOfType<TextField>() != null)
                return;

            Accept();
            evt.StopPropagation();
        }

        private sealed class PlanRow : VisualElement
        {
            private readonly Label _source;
            private readonly Label _arrow;
            private readonly Label _destination;
            private readonly Label _note;

            public PlanRow()
            {
                AddToClassList("ac-plan-row");

                _source = new Label();
                _source.AddToClassList("ac-plan-source");
                Add(_source);

                _arrow = new Label("→");
                _arrow.AddToClassList("ac-plan-arrow");
                Add(_arrow);

                _destination = new Label();
                _destination.AddToClassList("ac-plan-destination");
                Add(_destination);

                _note = new Label();
                _note.AddToClassList("ac-plan-note");
                Add(_note);
            }

            public void Bind(PlannedOperation operation)
            {
                _source.text = operation.Source;

                bool hasDestination = !string.IsNullOrEmpty(operation.Destination);
                _arrow.EnableInClassList("ac-hidden", !hasDestination);
                _destination.EnableInClassList("ac-hidden", !hasDestination);
                _destination.text = hasDestination ? operation.Destination : "";

                bool hasNote = !string.IsNullOrEmpty(operation.Note);
                _note.EnableInClassList("ac-hidden", !hasNote);
                _note.text = hasNote ? operation.Note : "";
                _note.EnableInClassList("ac-plan-note--alert", operation.Alert);
            }
        }
    }
}
