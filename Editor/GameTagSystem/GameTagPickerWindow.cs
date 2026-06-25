#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using DataKeeper.GameTagSystem;
using DataKeeper.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.GameTagSystem
{
    // Tree picker over the GameTagRegistry. Nodes are real entries (stable ids); selecting a
    // branch or a leaf returns that node's id. Supports add / rename / move / delete in place.
    public class GameTagPickerWindow : EditorWindow
    {
        // ─── Palette ──────────────────────────────────────────────────────────
        private static readonly Color BgDeep     = new Color(0.155f, 0.155f, 0.155f);
        private static readonly Color BgBar      = new Color(0.205f, 0.205f, 0.205f);
        private static readonly Color BgField    = new Color(0.165f, 0.165f, 0.165f);
        private static readonly Color BgRowHover = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color BgRowSel   = new Color(0.28f, 0.48f, 0.76f, 0.20f);
        private static readonly Color Border     = new Color(0.115f, 0.115f, 0.115f);
        private static readonly Color CheckBorder= new Color(0.46f, 0.46f, 0.46f);
        private static readonly Color Accent     = new Color(0.30f, 0.52f, 0.82f);
        private static readonly Color MoveBar    = new Color(0.55f, 0.42f, 0.18f);
        private static readonly Color MissingBar = new Color(0.52f, 0.22f, 0.24f);
        private static readonly Color WarnBar    = new Color(0.40f, 0.34f, 0.14f);
        private static readonly Color TextHi     = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color TextLo     = new Color(0.78f, 0.78f, 0.78f);
        private static readonly Color TextFaint  = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color CsGreen    = new Color(0.45f, 0.74f, 0.48f);

        private const float IndentWidth = 14f;

        // ─── State ────────────────────────────────────────────────────────────
        private GameTagRegistry _registry;
        private Action<int> _onApply;
        private bool _editorOnly; // standalone "edit tags" mode: no picking/apply, just the registry editor

        private int _selectedId;
        private int _renamingId;
        private int _moveSourceId;
        private readonly HashSet<int> _expanded = new();
        private HashSet<int> _generatedIds = new();
        private readonly List<TagNode> _roots = new();
        private readonly List<RowWidget> _rows = new();
        private string _search = string.Empty;

        // ─── UI refs ──────────────────────────────────────────────────────────
        private VisualElement _moveRow;
        private Label _moveLabel;
        private VisualElement _addRow;
        private TextField _newTagField;
        private VisualElement _missingRow;
        private Label _missingLabel;
        private TextField _missingField;
        private int _missingId; // the dead reference id the banner offers to re-add (independent of tree selection)
        private bool _codeDirty; // a structural edit happened this session — generated GameTags class may be stale
        private VisualElement _treeContainer;
        private VisualElement _emptyState;
        private Label _emptyLabel;
        private ScrollView _scroll;
        private VisualElement _codeWarnRow;
        private Label _footerLabel;
        private Button _renameBtn, _moveBtn, _deleteBtn;

        // ─── Public API ─────────────────────────────────────────────────────────
        public static void Show(GameTagRegistry registry, int currentId, Action<int> onApply)
        {
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No GameTagRegistry found.\nCreate a GameTagRegistry asset in a Resources folder.", "OK");
                return;
            }

            var win = CreateInstance<GameTagPickerWindow>();
            win.titleContent = new GUIContent("Game Tag Picker", EditorGUIUtility.FindTexture("d_FilterByLabel"));
            win.minSize = new Vector2(360, 480);
            win._registry = registry;
            win._onApply = onApply;
            win._selectedId = currentId;
            win.ShowAuxWindow();
        }

        // Standalone editor: open the registry editor without a GameTag field in the inspector.
        [MenuItem("Tools/Windows/Game Tags Editor", priority = 77)]
        public static void ShowEditor()
        {
            var registry = GameTagRegistry.Default;
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No GameTagRegistry found.\nCreate a GameTagRegistry asset in a Resources folder.", "OK");
                return;
            }

            var win = GetWindow<GameTagPickerWindow>();
            win.titleContent = new GUIContent("Game Tags", EditorGUIUtility.FindTexture("d_FilterByLabel"));
            win.minSize = new Vector2(360, 480);
            win._registry = registry;
            win._editorOnly = true;
            win._selectedId = GameTagRegistry.NONE;

            // GetWindow already ran CreateGUI (with the picker defaults) before the fields above were
            // set; rebuild now that we're in editor-only mode.
            win.rootVisualElement.Clear();
            win.CreateGUI();

            if (TryLoadEditorPosition(out var saved)) win.position = saved;
            win.Show();
        }

        // ─── Standalone window position persistence ─────────────────────────────
        private const string PosPrefKey = "DataKeeper.GameTagsEditor.Position";

        private static bool TryLoadEditorPosition(out Rect rect)
        {
            rect = default;
            var s = EditorPrefs.GetString(PosPrefKey, string.Empty);
            var p = s.Split('|');
            if (p.Length != 4) return false;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (!float.TryParse(p[0], System.Globalization.NumberStyles.Float, ci, out var x) ||
                !float.TryParse(p[1], System.Globalization.NumberStyles.Float, ci, out var y) ||
                !float.TryParse(p[2], System.Globalization.NumberStyles.Float, ci, out var w) ||
                !float.TryParse(p[3], System.Globalization.NumberStyles.Float, ci, out var h)) return false;
            rect = new Rect(x, y, w, h);
            return true;
        }

        private void SaveEditorPosition()
        {
            var r = position;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            EditorPrefs.SetString(PosPrefKey,
                $"{r.x.ToString(ci)}|{r.y.ToString(ci)}|{r.width.ToString(ci)}|{r.height.ToString(ci)}");
        }

        private void OnEnable()  => Undo.undoRedoPerformed += OnUndoRedo;
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (_editorOnly) SaveEditorPosition();
        }

        // Re-derive caches and the tree after a serialized-state revert (the baked caches aren't serialized).
        private void OnUndoRedo()
        {
            if (_registry == null) return;
            _registry.Bake();
            _renamingId = GameTagRegistry.NONE;
            if (_selectedId != GameTagRegistry.NONE && _registry.GetNode(_selectedId) == null)
                _selectedId = GameTagRegistry.NONE;
            RebuildTree();
        }

        // ─── Build UI ─────────────────────────────────────────────────────────
        public void CreateGUI()
        {
            // GetWindow() (standalone editor) runs CreateGUI before ShowEditor assigns _registry,
            // so fall back to the default registry here.
            if (_registry == null) _registry = GameTagRegistry.Default;
            if (_registry == null) return;

            _registry.Bake(); // always reflect the current registry state on open
            _generatedIds = GameTagsCodeGen.LoadGeneratedIds(_registry);

            var root = rootVisualElement;
            root.style.backgroundColor = BgDeep;

            root.Add(BuildHeader());
            root.Add(BuildSearchBar());
            root.Add(BuildEditBar());

            _moveRow = BuildMoveRow();
            _moveRow.SetDisplay(DisplayStyle.None);
            root.Add(_moveRow);

            _addRow = BuildAddRow();
            _addRow.SetDisplay(DisplayStyle.None);
            root.Add(_addRow);

            _missingRow = BuildMissingRow();
            _missingRow.SetDisplay(DisplayStyle.None);
            root.Add(_missingRow);

            _scroll = new ScrollView(ScrollViewMode.Vertical).SetFlexGrow(1);
            _scroll.contentContainer.SetPadding(left: 4, top: 4, right: 4, bottom: 4);

            _treeContainer = new VisualElement();
            _scroll.Add(_treeContainer);

            _emptyState = BuildEmptyState();
            _scroll.Add(_emptyState);

            root.Add(_scroll);

            _codeWarnRow = BuildCodeWarnRow();
            _codeWarnRow.SetDisplay(DisplayStyle.None);
            root.Add(_codeWarnRow);

            root.Add(BuildFooter());

            // expand to reveal the initial selection
            for (int p = _registry.GetParentId(_selectedId); p != GameTagRegistry.NONE; p = _registry.GetParentId(p))
                _expanded.Add(p);

            // Opened on a reference whose id no longer exists — offer to re-add it.
            if (_selectedId != GameTagRegistry.NONE && _registry.GetNode(_selectedId) == null)
                ShowMissingBanner(_selectedId);

            RebuildTree();
        }

        private VisualElement BuildHeader()
        {
            var hdr = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 6, right: 6, bottom: 6)
                .SetBackgroundColor(BgBar)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            hdr.Add(new Label("Game Tags").SetFontSize(12).SetFontStyle(FontStyle.Bold).SetColor(TextHi));
            hdr.Add(new VisualElement().SetFlexGrow(1));

            hdr.Add(MakeToolButton("＋ New Tag", "Add a tag under the selection (or at root)", OpenAddRow, accent: true));

            var menuBtn = MakeToolButton("⋮", "More actions", OpenContextMenu);
            menuBtn.style.marginLeft = 4;
            menuBtn.style.width = 24;
            hdr.Add(menuBtn);
            return hdr;
        }

        private VisualElement BuildSearchBar()
        {
            var bar = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 6, top: 4, right: 6, bottom: 4)
                .SetBackgroundColor(BgBar)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            var search = new ToolbarSearchField();
            search.SetFlexGrow(1);
            search.RegisterValueChangedCallback(evt => { _search = evt.newValue; RebuildTree(); });
            bar.Add(search);
            return bar;
        }

        // Selection-scoped node actions (enabled only when a node is selected).
        private VisualElement BuildEditBar()
        {
            var bar = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 4, right: 6, bottom: 4)
                .SetBackgroundColor(BgBar)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            _renameBtn = MakeToolButton("✎ Rename", "Rename the selected tag (refs are kept)", BeginRenameSelected);
            _moveBtn   = MakeToolButton("⤴ Move", "Move the selected tag under another parent", BeginMoveSelected);
            _deleteBtn = MakeToolButton("🗑 Delete", "Delete the selected tag and its children", DeleteSelected);
            _moveBtn.style.marginLeft = 4;
            _deleteBtn.style.marginLeft = 4;

            bar.Add(_renameBtn);
            bar.Add(_moveBtn);
            bar.Add(_deleteBtn);
            return bar;
        }

        private VisualElement BuildMoveRow()
        {
            var row = new VisualElement()
                .SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 5, right: 8, bottom: 5)
                .SetBackgroundColor(MoveBar);

            _moveLabel = new Label().SetFontSize(11).SetColor(Color.white);
            _moveLabel.style.whiteSpace = WhiteSpace.Normal;
            _moveLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _moveLabel.style.marginBottom = 5;
            row.Add(_moveLabel);

            var buttons = new VisualElement().SetFlexRow().SetJustifyContent(Justify.Center);
            buttons.Add(MakeToolButton("To Root", "Move to the top level", () => CommitMove(GameTagRegistry.NONE)));
            var cancel = MakeToolButton("Cancel", "Cancel move", CancelMove);
            cancel.style.marginLeft = 4;
            buttons.Add(cancel);
            row.Add(buttons);
            return row;
        }

        private VisualElement BuildAddRow()
        {
            var row = new VisualElement()
                .SetPadding(left: 6, top: 4, right: 6, bottom: 4)
                .SetBackgroundColor(BgField)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            var line = new VisualElement().SetFlexRow().SetAlignItems(Align.Center);

            _newTagField = new TextField { value = string.Empty };
            _newTagField.SetFlexGrow(1);
            _newTagField.textEdition.placeholder = "Name (or Sub/Path)";
            RestrictToIdentifier(_newTagField, allowSeparator: true);
            _newTagField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) { CommitNewTag(); evt.StopPropagation(); }
                else if (evt.keyCode == KeyCode.Escape) { CloseAddRow(); evt.StopPropagation(); }
            });
            line.Add(_newTagField);

            var add = MakeToolButton("Add", "Create tag", CommitNewTag, accent: true);
            add.style.marginLeft = 4;
            line.Add(add);

            var cancel = MakeToolButton("✕", "Cancel", CloseAddRow);
            cancel.style.marginLeft = 2;
            line.Add(cancel);

            row.Add(line);
            return row;
        }

        private VisualElement BuildEmptyState()
        {
            var wrap = new VisualElement().SetAlignItems(Align.Center).SetJustifyContent(Justify.Center);
            wrap.style.paddingTop = 50;
            wrap.SetDisplay(DisplayStyle.None);

            _emptyLabel = new Label("No tags yet").SetFontSize(12).SetColor(TextFaint);
            _emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            wrap.Add(_emptyLabel);

            var hint = new Label("Use “＋ New Tag” to add one").SetFontSize(11).SetColor(TextFaint);
            hint.style.marginTop = 6;
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            wrap.Add(hint);
            return wrap;
        }

        private VisualElement BuildFooter()
        {
            var bar = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 5, right: 6, bottom: 5)
                .SetBackgroundColor(BgBar)
                .SetBorderWidth(top: 1).SetBorderColor(top: Border);

            _footerLabel = new Label().SetFontSize(11).SetColor(TextLo).SetFlexGrow(1);
            _footerLabel.style.overflow = Overflow.Hidden;
            _footerLabel.SetTextOverflowEllipsis().SetTextNoWrap();
            bar.Add(_footerLabel);

            bar.Add(MakeToolButton("Clear", "Deselect", () => { _selectedId = GameTagRegistry.NONE; RefreshSelectionVisuals(); }));

            // In standalone editor mode there's no field to write back to — edits persist live, so no Apply.
            if (!_editorOnly)
            {
                var apply = MakeToolButton("Apply", "Confirm selection", Apply, accent: true);
                apply.style.marginLeft = 4;
                apply.style.width = 64;
                bar.Add(apply);
            }
            return bar;
        }

        // ─── Tree data ──────────────────────────────────────────────────────────
        private void RebuildTree()
        {
            _roots.Clear();
            foreach (var rootId in _registry.RootIds)
                _roots.Add(BuildNode(rootId));
            _roots.Sort(CompareNodes);
            RenderTree();
        }

        private TagNode BuildNode(int id)
        {
            var n = _registry.GetNode(id);
            var node = new TagNode { Id = id, Label = n.Name, FullPath = n.Path };
            foreach (var childId in n.Children)
            {
                var child = BuildNode(childId);
                child.Parent = node;
                node.Children.Add(child);
            }
            node.Children.Sort(CompareNodes);
            return node;
        }

        private static int CompareNodes(TagNode a, TagNode b)
            => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);

        // ─── Tree rendering ─────────────────────────────────────────────────────
        private void RenderTree()
        {
            _rows.Clear();
            _treeContainer.Clear();

            bool searching = !string.IsNullOrEmpty(_search);
            HashSet<int> visible = null;
            if (searching)
            {
                visible = new HashSet<int>();
                foreach (var r in _roots) Filter(r, _search, visible);
            }

            foreach (var r in _roots)
                RenderNode(r, 0, searching, visible);

            bool empty = _treeContainer.childCount == 0;
            _emptyState.SetDisplay(empty ? DisplayStyle.Flex : DisplayStyle.None);
            _emptyLabel.text = searching ? $"No tags match “{_search}”" : "No tags yet";

            RefreshSelectionVisuals();
        }

        private static bool Filter(TagNode node, string term, HashSet<int> visible)
        {
            bool match = node.FullPath.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            foreach (var c in node.Children)
                match |= Filter(c, term, visible);
            if (match) visible.Add(node.Id);
            return match;
        }

        private void RenderNode(TagNode node, int depth, bool searching, HashSet<int> visible)
        {
            if (visible != null && !visible.Contains(node.Id)) return;

            bool hasChildren = node.Children.Count > 0;
            bool expanded = searching || _expanded.Contains(node.Id);

            var rowEl = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetHeight(22).SetBorderRadius(3);
            rowEl.style.paddingLeft = 4 + depth * IndentWidth;
            rowEl.style.flexShrink = 0;

            var row = new RowWidget { Node = node, Element = rowEl };

            // Foldout arrow (or spacer)
            if (hasChildren)
            {
                var arrow = new Label(expanded ? "▾" : "▸").SetFontSize(10).SetColor(TextLo);
                arrow.SetWidth(IndentWidth);
                arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
                arrow.RegisterCallback<ClickEvent>(evt => { ToggleExpand(node); evt.StopPropagation(); });
                rowEl.Add(arrow);
            }
            else
            {
                rowEl.Add(new VisualElement().SetWidth(IndentWidth));
            }

            // Selection checkbox
            var check = new CheckBox();
            check.style.marginRight = 6;
            check.RegisterCallback<ClickEvent>(evt => { SelectNode(node); evt.StopPropagation(); });
            row.Check = check;
            rowEl.Add(check);

            // Label or inline rename field
            if (_renamingId == node.Id)
            {
                var field = new TextField { value = node.Label };
                field.SetFlexGrow(1);
                RestrictToIdentifier(field, allowSeparator: false);
                field.schedule.Execute(() => { field.Focus(); field.SelectAll(); });
                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) { CommitRename(node.Id, field.value); evt.StopPropagation(); }
                    else if (evt.keyCode == KeyCode.Escape) { _renamingId = GameTagRegistry.NONE; RenderTree(); evt.StopPropagation(); }
                });
                field.RegisterCallback<FocusOutEvent>(_ => { if (_renamingId == node.Id) CommitRename(node.Id, field.value); });
                rowEl.Add(field);
            }
            else
            {
                var label = new Label(node.Label).SetFontSize(12).SetColor(TextHi);
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.SetFlexGrow(1);
                label.style.overflow = Overflow.Hidden;
                label.SetTextOverflowEllipsis().SetTextNoWrap();
                rowEl.Add(label);

                if (_generatedIds.Contains(node.Id))
                {
                    var cs = new Label("C#").SetFontSize(8).SetColor(CsGreen).SetFontStyle(FontStyle.Bold);
                    cs.tooltip = "Has a generated GameTags constant";
                    cs.style.marginRight = 4;
                    cs.pickingMode = PickingMode.Ignore;
                    rowEl.Add(cs);
                }

                var idLabel = new Label($"#{node.Id}").SetFontSize(9).SetColor(TextFaint);
                idLabel.style.marginRight = 6;
                idLabel.style.minWidth = 14;
                idLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                idLabel.pickingMode = PickingMode.Ignore;
                rowEl.Add(idLabel);
            }

            rowEl.RegisterCallback<ClickEvent>(evt =>
            {
                if (_moveSourceId != GameTagRegistry.NONE) { CommitMove(node.Id); return; }
                if (evt.clickCount >= 2 && !hasChildren && !_editorOnly) { _selectedId = node.Id; Apply(); return; }
                if (evt.clickCount >= 2) { BeginRename(node.Id); return; }
                if (hasChildren) ToggleExpand(node);
            });

            rowEl.RegisterCallback<MouseEnterEvent>(_ => { row.Hover = true; ApplyRowBg(row); });
            rowEl.RegisterCallback<MouseLeaveEvent>(_ => { row.Hover = false; ApplyRowBg(row); });

            _treeContainer.Add(rowEl);
            _rows.Add(row);

            if (hasChildren && expanded)
                foreach (var child in node.Children)
                    RenderNode(child, depth + 1, searching, visible);
        }

        private void ToggleExpand(TagNode node)
        {
            if (!_expanded.Remove(node.Id)) _expanded.Add(node.Id);
            RenderTree();
        }

        // ─── Selection ────────────────────────────────────────────────────────
        private void SelectNode(TagNode node)
        {
            _selectedId = _selectedId == node.Id ? GameTagRegistry.NONE : node.Id;
            RefreshSelectionVisuals();
        }

        private void RefreshSelectionVisuals()
        {
            foreach (var row in _rows)
            {
                bool sel = row.Node.Id == _selectedId;
                row.Check.Set(sel);
                row.Selected = sel;
                ApplyRowBg(row);
            }

            bool has = _selectedId != GameTagRegistry.NONE;
            SetEnabled(_renameBtn, has);
            SetEnabled(_moveBtn, has);
            SetEnabled(_deleteBtn, has);

            _footerLabel.text = has ? _registry.GetPath(_selectedId) : "Nothing selected";
        }

        private static void SetEnabled(VisualElement btn, bool on)
        {
            btn.SetEnabled(on);
            btn.style.opacity = on ? 1f : 0.4f;
        }

        private void ApplyRowBg(RowWidget row)
        {
            row.Element.style.backgroundColor = row.Selected ? BgRowSel : row.Hover ? BgRowHover : Color.clear;
        }

        // ─── Add ────────────────────────────────────────────────────────────────
        private void OpenAddRow()
        {
            string basePath = _selectedId == GameTagRegistry.NONE ? null : _registry.GetPath(_selectedId);
            string prefill = string.IsNullOrEmpty(basePath) ? string.Empty : basePath + GameTagRegistry.SEPARATOR;

            _addRow.SetDisplay(DisplayStyle.Flex);
            _newTagField.value = prefill;
            _newTagField.schedule.Execute(() =>
            {
                _newTagField.Focus();
                _newTagField.cursorIndex = _newTagField.selectIndex = prefill.Length;
            });
        }

        private void CommitNewTag()
        {
            var full = (_newTagField.value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(full)) { CloseAddRow(); return; }

            RecordUndo("Add Game Tag");
            int id = _registry.GetOrCreate(full);
            Persist();

            for (int p = _registry.GetParentId(id); p != GameTagRegistry.NONE; p = _registry.GetParentId(p))
                _expanded.Add(p);
            _selectedId = id;

            CloseAddRow();
            RebuildTree();
        }

        private void CloseAddRow()
        {
            _addRow.SetDisplay(DisplayStyle.None);
            _newTagField.value = string.Empty;
        }

        // ─── Missing-reference recovery ───────────────────────────────────────────
        // Built once; populated/toggled by ShowMissingBanner when the picker opens on a dead id.
        private VisualElement BuildMissingRow()
        {
            var row = new VisualElement()
                .SetPadding(left: 8, top: 6, right: 8, bottom: 6)
                .SetBackgroundColor(MissingBar)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            _missingLabel = new Label().SetFontSize(11).SetColor(Color.white);
            _missingLabel.style.whiteSpace = WhiteSpace.Normal;
            _missingLabel.style.marginBottom = 5;
            row.Add(_missingLabel);

            var line = new VisualElement().SetFlexRow().SetAlignItems(Align.Center);

            _missingField = new TextField { value = string.Empty };
            _missingField.SetFlexGrow(1);
            _missingField.textEdition.placeholder = "New name (or Sub/Path)";
            RestrictToIdentifier(_missingField, allowSeparator: true);
            _missingField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) { CommitReAdd(); evt.StopPropagation(); }
                else if (evt.keyCode == KeyCode.Escape) { HideMissingBanner(); evt.StopPropagation(); }
            });
            line.Add(_missingField);

            var reAdd = MakeToolButton("Re-add", "Re-add this id to the registry under the typed name", CommitReAdd, accent: true);
            reAdd.style.marginLeft = 4;
            line.Add(reAdd);

            var dismiss = MakeToolButton("✕", "Dismiss (pick a replacement instead)", HideMissingBanner);
            dismiss.style.marginLeft = 2;
            line.Add(dismiss);

            row.Add(line);
            return row;
        }

        private void ShowMissingBanner(int missingId)
        {
            _missingId = missingId;
            _missingLabel.text = $"Referenced tag #{missingId} is missing. Re-add it under a name/path, or pick a replacement below.";
            _missingField.value = string.Empty;
            _missingRow.SetDisplay(DisplayStyle.Flex);
            _missingField.schedule.Execute(() => _missingField.Focus());
        }

        private void HideMissingBanner()
        {
            _missingId = GameTagRegistry.NONE;
            _missingRow.SetDisplay(DisplayStyle.None);
            _missingField.value = string.Empty;
        }

        // Re-add the dead reference id (tracked in _missingId, independent of the current tree
        // selection) at the typed name/path so the existing reference resolves again.
        private void CommitReAdd()
        {
            if (_missingId == GameTagRegistry.NONE || _registry.GetNode(_missingId) != null) { HideMissingBanner(); return; }

            var path = (_missingField.value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(path)) return;

            int reAddedId = _missingId;
            RecordUndo("Re-add Game Tag");
            bool added = _registry.ReAddId(reAddedId, path);
            Persist();

            HideMissingBanner();
            if (added)
            {
                _generatedIds = GameTagsCodeGen.LoadGeneratedIds(_registry);
                for (int p = _registry.GetParentId(reAddedId); p != GameTagRegistry.NONE; p = _registry.GetParentId(p))
                    _expanded.Add(p);
                _selectedId = reAddedId;
            }
            RebuildTree();
        }

        // ─── Rename ───────────────────────────────────────────────────────────────
        private void BeginRenameSelected() { if (_selectedId != GameTagRegistry.NONE) BeginRename(_selectedId); }

        private void BeginRename(int id)
        {
            _selectedId = id;
            _renamingId = id;
            for (int p = _registry.GetParentId(id); p != GameTagRegistry.NONE; p = _registry.GetParentId(p))
                _expanded.Add(p);
            RenderTree();
        }

        private void CommitRename(int id, string newName)
        {
            _renamingId = GameTagRegistry.NONE;
            newName = (newName ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(newName) && newName != _registry.GetName(id))
            {
                RecordUndo("Rename Game Tag");
                _registry.Rename(id, newName);
                Persist();
            }
            RebuildTree();
        }

        // ─── Move ─────────────────────────────────────────────────────────────────
        private void BeginMoveSelected()
        {
            if (_selectedId == GameTagRegistry.NONE) return;
            _moveSourceId = _selectedId;
            _moveLabel.text = $"Moving  “{_registry.GetPath(_moveSourceId)}”  —  click a new parent";
            _moveRow.SetDisplay(DisplayStyle.Flex);
        }

        private void CommitMove(int newParentId)
        {
            int src = _moveSourceId;
            CancelMove();
            if (src == GameTagRegistry.NONE || src == newParentId) return;

            if (newParentId != GameTagRegistry.NONE && _registry.Matches(newParentId, src))
            {
                EditorUtility.DisplayDialog("Move", "Cannot move a tag into its own subtree.", "OK");
                return;
            }

            RecordUndo("Move Game Tag");
            _registry.Reparent(src, newParentId);
            Persist();
            if (newParentId != GameTagRegistry.NONE) _expanded.Add(newParentId);
            _selectedId = src;
            RebuildTree();
        }

        private void CancelMove()
        {
            _moveSourceId = GameTagRegistry.NONE;
            _moveRow.SetDisplay(DisplayStyle.None);
        }

        // ─── Delete ────────────────────────────────────────────────────────────────
        private void DeleteSelected()
        {
            if (_selectedId == GameTagRegistry.NONE) return;
            var node = _registry.GetNode(_selectedId);
            if (node == null) return;

            int subtree = CountSubtree(_selectedId);
            string msg = subtree > 1
                ? $"Delete “{node.Path}” and its {subtree - 1} descendant tag(s)?\n\nReferences to these ids will show as [missing]."
                : $"Delete “{node.Path}”?\n\nReferences to this id will show as [missing].";

            if (!EditorUtility.DisplayDialog("Delete Tag", msg, "Delete", "Cancel")) return;

            RecordUndo("Delete Game Tag");
            _registry.Delete(_selectedId);
            Persist();
            _selectedId = GameTagRegistry.NONE;
            RebuildTree();
        }

        private int CountSubtree(int id)
        {
            var n = _registry.GetNode(id);
            if (n == null) return 0;
            int total = 1;
            foreach (var c in n.Children) total += CountSubtree(c);
            return total;
        }

        // ─── Actions ──────────────────────────────────────────────────────────────
        private void Apply()
        {
            _onApply?.Invoke(_selectedId);
            Close();
        }

        // Snapshot the registry's serialized state so the next mutation is undoable.
        private void RecordUndo(string action) => Undo.RegisterCompleteObjectUndo(_registry, action);

        private void Persist()
        {
            EditorUtility.SetDirty(_registry);
            AssetDatabase.SaveAssets();
            _codeDirty = true; // any persisted edit can leave the generated GameTags class stale
            RefreshCodeWarning();
        }

        private void OpenContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Select GameTagRegistry"), false, PingRegistry);
            menu.AddItem(new GUIContent("Generate C# Tags Class"), false, GenerateCode);
            menu.ShowAsContext();
        }

        private void PingRegistry()
        {
            Selection.activeObject = _registry;
            EditorGUIUtility.PingObject(_registry);
            Close();
        }

        private void GenerateCode()
        {
            GameTagsCodeGen.Regenerate(_registry);
            Close();
        }

        // ─── Generated-code staleness warning ─────────────────────────────────────
        private VisualElement BuildCodeWarnRow()
        {
            var row = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 5, right: 6, bottom: 5)
                .SetBackgroundColor(WarnBar)
                .SetBorderWidth(top: 1).SetBorderColor(top: Border);

            var label = new Label("⚠ Generated GameTags class is out of date").SetFontSize(11).SetColor(TextHi).SetFlexGrow(1);
            label.style.overflow = Overflow.Hidden;
            label.SetTextOverflowEllipsis().SetTextNoWrap();
            row.Add(label);

            row.Add(MakeToolButton("Regenerate", "Regenerate the GameTags C# class from the registry", RegenerateInline, accent: true));
            return row;
        }

        // Show the warning only when a generated file exists (otherwise there's nothing to keep in sync).
        private void RefreshCodeWarning()
        {
            if (_codeWarnRow == null) return;
            bool show = _codeDirty && _generatedIds.Count > 0;
            _codeWarnRow.SetDisplay(show ? DisplayStyle.Flex : DisplayStyle.None);
        }

        private void RegenerateInline()
        {
            GameTagsCodeGen.Regenerate(_registry);
            _generatedIds = GameTagsCodeGen.LoadGeneratedIds(_registry);
            _codeDirty = false;
            RefreshCodeWarning();
            RebuildTree(); // refresh the C# badges against the freshly generated file
        }

        // ─── Input filtering ─────────────────────────────────────────────────────
        // Strip anything that isn't a valid C# identifier char as the user types, so the generated
        // GameTags class never has to mangle names. The add field keeps '/' for path entry.
        private static void RestrictToIdentifier(TextField field, bool allowSeparator)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                var filtered = FilterIdentifier(evt.newValue, allowSeparator);
                if (filtered != evt.newValue) field.SetValueWithoutNotify(filtered);
            });
        }

        // Allowed: letters, digits, '_' and (for the add field) the '/' separator. Each segment must
        // START with a letter or '_' — no leading digit and no leading/empty separator — so every
        // segment maps straight onto a valid C# identifier.
        private static string FilterIdentifier(string s, bool allowSeparator)
        {
            if (string.IsNullOrEmpty(s)) return s;
            char sep = GameTagRegistry.SEPARATOR[0];
            var sb = new StringBuilder(s.Length);
            bool atSegmentStart = true;

            foreach (var c in s)
            {
                if (allowSeparator && c == sep)
                {
                    if (sb.Length == 0 || sb[sb.Length - 1] == sep) continue; // no leading/empty segment
                    sb.Append(sep);
                    atSegmentStart = true;
                    continue;
                }

                bool ok = atSegmentStart ? char.IsLetter(c) || c == '_'
                                         : char.IsLetterOrDigit(c) || c == '_';
                if (!ok) continue;
                sb.Append(c);
                atSegmentStart = false;
            }
            return sb.ToString();
        }

        // ─── Reusable button ─────────────────────────────────────────────────────
        private static Button MakeToolButton(string text, string tooltip, Action onClick, bool accent = false)
        {
            var btn = new Button(onClick) { text = text, tooltip = tooltip };
            btn.SetFontSize(11).SetColor(accent ? Color.white : TextLo);
            btn.style.height = 22;
            btn.style.paddingLeft = 8; btn.style.paddingRight = 8;
            btn.style.marginLeft = 0; btn.style.marginRight = 0;
            btn.style.marginTop = 0; btn.style.marginBottom = 0;
            btn.SetBorderWidth(0).SetBorderRadius(4);
            btn.SetBackgroundColor(accent ? Accent : new Color(0.27f, 0.27f, 0.27f));
            return btn;
        }

        // ─── Selection checkbox element ────────────────────────────────────────────
        private class CheckBox : VisualElement
        {
            private readonly Label _mark;

            public CheckBox()
            {
                this.SetSize(15, 15).SetBorderRadius(3).SetFlexShrink(0);
                style.alignItems = Align.Center;
                style.justifyContent = Justify.Center;
                style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;

                _mark = new Label { pickingMode = PickingMode.Ignore };
                _mark.SetFontSize(11).SetColor(Color.white).SetFontStyle(FontStyle.Bold);
                _mark.style.unityTextAlign = TextAnchor.MiddleCenter;
                Add(_mark);
                Set(false);
            }

            public void Set(bool on)
            {
                if (on) { this.SetBackgroundColor(Accent).SetBorderColor(Accent); _mark.text = "✓"; }
                else { this.SetBackgroundColor(BgField).SetBorderColor(CheckBorder); _mark.text = string.Empty; }
            }
        }

        // ─── Models ───────────────────────────────────────────────────────────────
        private class TagNode
        {
            public int Id;
            public string Label;
            public string FullPath;
            public TagNode Parent;
            public readonly List<TagNode> Children = new();
        }

        private class RowWidget
        {
            public TagNode Node;
            public VisualElement Element;
            public CheckBox Check;
            public bool Hover;
            public bool Selected;
        }
    }
}
#endif
