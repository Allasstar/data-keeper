#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.GameTagSystem;
using DataKeeper.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.GameTagSystem
{
    public class GameTagPickerWindow : EditorWindow
    {
        // ─── Palette ──────────────────────────────────────────────────────────
        private static readonly Color BgDeep      = new Color(0.155f, 0.155f, 0.155f);
        private static readonly Color BgBar        = new Color(0.205f, 0.205f, 0.205f);
        private static readonly Color BgField      = new Color(0.165f, 0.165f, 0.165f);
        private static readonly Color BgRowHover   = new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color BgRowSel     = new Color(0.28f, 0.48f, 0.76f, 0.20f);
        private static readonly Color Border       = new Color(0.115f, 0.115f, 0.115f);
        private static readonly Color CheckBorder  = new Color(0.46f, 0.46f, 0.46f);
        private static readonly Color Accent       = new Color(0.30f, 0.52f, 0.82f);
        private static readonly Color TextHi        = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color TextLo        = new Color(0.78f, 0.78f, 0.78f);
        private static readonly Color TextFaint     = new Color(0.55f, 0.55f, 0.55f);

        private const float IndentWidth = 14f;

        // ─── State ────────────────────────────────────────────────────────────
        private GameTagRegistry _registry;
        private Action<string> _onApply;

        private string _selected;
        private readonly HashSet<string> _expanded = new();
        private readonly List<TagNode> _roots = new();
        private readonly List<RowWidget> _rows = new();
        private string _search = string.Empty;

        // ─── UI refs ──────────────────────────────────────────────────────────
        private VisualElement _addRow;
        private TextField _newTagField;
        private VisualElement _treeContainer;
        private VisualElement _emptyState;
        private Label _emptyLabel;
        private ScrollView _scroll;
        private Label _footerLabel;

        // ─── Public API ─────────────────────────────────────────────────────────
        public static void Show(GameTagRegistry registry, string currentTag, Action<string> onApply)
        {
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No GameTagRegistry found.\nCreate new GameTagRegistry in Resources folder.", "OK");
                return;
            }

            var win = CreateInstance<GameTagPickerWindow>();
            win.titleContent = new GUIContent("Game Tag Picker",
                EditorGUIUtility.FindTexture("d_FilterByLabel"));
            win.minSize = new Vector2(360, 480);
            win._registry = registry;
            win._onApply = onApply;
            win._selected = string.IsNullOrEmpty(currentTag) ? null : currentTag;
            win.ShowAuxWindow();
        }

        // ─── Build UI ─────────────────────────────────────────────────────────
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.backgroundColor = BgDeep;

            root.Add(BuildHeader());
            root.Add(BuildSearchBar());

            _addRow = BuildAddRow();
            _addRow.SetDisplay(DisplayStyle.None);
            root.Add(_addRow);

            _scroll = new ScrollView(ScrollViewMode.Vertical).SetFlexGrow(1);
            _scroll.contentContainer.SetPadding(left: 4, top: 4, right: 4, bottom: 4);

            _treeContainer = new VisualElement();
            _scroll.Add(_treeContainer);

            _emptyState = BuildEmptyState();
            _scroll.Add(_emptyState);

            root.Add(_scroll);
            root.Add(BuildFooter());

            RebuildTree();
        }

        private VisualElement BuildHeader()
        {
            var hdr = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 6, right: 6, bottom: 6)
                .SetBackgroundColor(BgBar)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            var titleLabel = new Label("Game Tags")
                .SetFontSize(12).SetFontStyle(FontStyle.Bold).SetColor(TextHi);
            hdr.Add(titleLabel);

            hdr.Add(new VisualElement().SetFlexGrow(1));

            hdr.Add(MakeToolButton("＋ New Tag", "Add a new tag to the registry", () =>
            {
                bool show = _addRow.style.display == DisplayStyle.None;
                _addRow.SetDisplay(show ? DisplayStyle.Flex : DisplayStyle.None);
                if (show)
                {
                    // Seed with the search text, else the current tag (handy when the
                    // field holds a tag that's missing from the registry — one click to add it).
                    _newTagField.value = !string.IsNullOrEmpty(_search) ? _search : (_selected ?? string.Empty);
                    _newTagField.schedule.Execute(() => _newTagField.Focus());
                }
            }, accent: true));

            var editBtn = MakeToolButton("Edit", "Select the GameTagRegistry asset", PingRegistry);
            editBtn.style.marginLeft = 4;
            hdr.Add(editBtn);

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
            search.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue;
                RebuildTree();
            });
            bar.Add(search);
            return bar;
        }

        private VisualElement BuildAddRow()
        {
            var row = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 6, top: 4, right: 6, bottom: 4)
                .SetBackgroundColor(BgField)
                .SetBorderWidth(bottom: 1).SetBorderColor(bottom: Border);

            _newTagField = new TextField { value = string.Empty };
            _newTagField.SetFlexGrow(1);
            _newTagField.textEdition.placeholder = "Parent/Child/Tag";
            _newTagField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    CommitNewTag();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    CloseAddRow();
                    evt.StopPropagation();
                }
            });
            row.Add(_newTagField);

            var add = MakeToolButton("Add", "Create tag", CommitNewTag, accent: true);
            add.style.marginLeft = 4;
            row.Add(add);

            var cancel = MakeToolButton("✕", "Cancel", CloseAddRow);
            cancel.style.marginLeft = 2;
            row.Add(cancel);

            return row;
        }

        private VisualElement BuildEmptyState()
        {
            var wrap = new VisualElement()
                .SetAlignItems(Align.Center).SetJustifyContent(Justify.Center);
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

            var clear = MakeToolButton("Clear", "Deselect", () =>
            {
                _selected = null;
                RefreshSelectionVisuals();
            });
            bar.Add(clear);

            var apply = MakeToolButton("Apply", "Confirm selection", Apply, accent: true);
            apply.style.marginLeft = 4;
            apply.style.width = 64;
            bar.Add(apply);
            return bar;
        }

        // ─── Tree data ──────────────────────────────────────────────────────────
        private void RebuildTree()
        {
            _roots.Clear();
            var dict = new Dictionary<string, TagNode>();

            IEnumerable<string> source = _registry.Tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(_search))
                source = source.Where(t => t.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var tag in source)
            {
                var parts = tag.Split(GameTag.SEPARATOR);
                TagNode parent = null;

                for (int i = 0; i < parts.Length; i++)
                {
                    var path = string.Join(GameTag.SEPARATOR, parts[..(i + 1)]);
                    if (!dict.TryGetValue(path, out var node))
                    {
                        node = new TagNode { Label = parts[i], FullPath = path };
                        dict[path] = node;
                        if (parent == null) _roots.Add(node);
                        else parent.Children.Add(node);
                    }

                    if (i == parts.Length - 1)
                        node.ExistsInRegistry = true;

                    parent = node;
                }
            }

            RenderTree();
        }

        // ─── Tree rendering ─────────────────────────────────────────────────────
        private void RenderTree()
        {
            _rows.Clear();
            _treeContainer.Clear();

            bool searching = !string.IsNullOrEmpty(_search);
            foreach (var root in _roots)
                RenderNode(root, 0, searching);

            bool empty = _roots.Count == 0;
            _emptyState.SetDisplay(empty ? DisplayStyle.Flex : DisplayStyle.None);
            _emptyLabel.text = searching ? $"No tags match “{_search}”" : "No tags yet";

            RefreshSelectionVisuals();
        }

        private void RenderNode(TagNode node, int depth, bool forceExpand)
        {
            bool hasChildren = node.Children.Count > 0;
            bool expanded = forceExpand || _expanded.Contains(node.FullPath);

            var row = new RowWidget { Node = node };

            var rowEl = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetHeight(22).SetBorderRadius(3);
            rowEl.style.paddingLeft = 4 + depth * IndentWidth;
            rowEl.style.flexShrink = 0;
            row.Element = rowEl;

            // Foldout arrow (or spacer to keep alignment)
            if (hasChildren)
            {
                var arrow = new Label(expanded ? "▾" : "▸").SetFontSize(10).SetColor(TextLo);
                arrow.SetWidth(IndentWidth);
                arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
                arrow.RegisterCallback<ClickEvent>(evt =>
                {
                    ToggleExpand(node);
                    evt.StopPropagation();
                });
                rowEl.Add(arrow);
            }
            else
            {
                rowEl.Add(new VisualElement().SetWidth(IndentWidth));
            }

            // Selection checkbox
            var check = new CheckBox();
            check.style.marginRight = 6;
            check.RegisterCallback<ClickEvent>(evt =>
            {
                ToggleNode(node);
                RefreshSelectionVisuals();
                evt.StopPropagation();
            });
            row.Check = check;
            rowEl.Add(check);

            // Label
            var label = new Label(node.Label).SetFontSize(12);
            label.SetColor(node.ExistsInRegistry ? TextHi : TextFaint);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.SetFlexGrow(1);
            label.style.overflow = Overflow.Hidden;
            label.SetTextOverflowEllipsis().SetTextNoWrap();
            rowEl.Add(label);

            // Child count hint for parents
            if (hasChildren)
            {
                var count = new Label(node.Children.Count.ToString()).SetFontSize(9).SetColor(TextFaint);
                count.style.marginRight = 6;
                count.style.minWidth = 14;
                count.style.unityTextAlign = TextAnchor.MiddleRight;
                rowEl.Add(count);
            }

            // Row interaction: parents expand, leaves select; double-click a leaf applies
            rowEl.RegisterCallback<ClickEvent>(evt =>
            {
                if (hasChildren)
                {
                    ToggleExpand(node);
                }
                else if (evt.clickCount >= 2)
                {
                    _selected = node.FullPath;
                    Apply();
                }
                else
                {
                    ToggleNode(node);
                    RefreshSelectionVisuals();
                }
            });

            rowEl.RegisterCallback<MouseEnterEvent>(_ => { row.Hover = true; ApplyRowBg(row); });
            rowEl.RegisterCallback<MouseLeaveEvent>(_ => { row.Hover = false; ApplyRowBg(row); });

            _treeContainer.Add(rowEl);
            _rows.Add(row);

            if (hasChildren && expanded)
                foreach (var child in node.Children)
                    RenderNode(child, depth + 1, forceExpand);
        }

        private void ToggleExpand(TagNode node)
        {
            if (_expanded.Contains(node.FullPath)) _expanded.Remove(node.FullPath);
            else _expanded.Add(node.FullPath);
            RenderTree();
        }

        // ─── Selection ────────────────────────────────────────────────────────
        private void ToggleNode(TagNode node)
        {
            _selected = _selected == node.FullPath ? null : node.FullPath;
        }

        private void RefreshSelectionVisuals()
        {
            foreach (var row in _rows)
            {
                bool sel = row.Node.FullPath == _selected;
                row.Check.Set(sel);
                row.Selected = sel;
                ApplyRowBg(row);
            }
            UpdateFooter();
        }

        private void ApplyRowBg(RowWidget row)
        {
            Color bg = row.Selected ? BgRowSel
                : row.Hover ? BgRowHover
                : Color.clear;
            row.Element.style.backgroundColor = bg;
        }

        private void UpdateFooter()
        {
            _footerLabel.text = string.IsNullOrEmpty(_selected) ? "Nothing selected" : _selected;
        }

        // ─── New tag ──────────────────────────────────────────────────────────
        private void CommitNewTag()
        {
            var value = (_newTagField.value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value) || _registry.Tags.Contains(value))
            {
                CloseAddRow();
                return;
            }

            var so = new SerializedObject(_registry);
            var list = so.FindProperty("_tags");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = value;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            // Reveal and select the freshly created tag
            foreach (var seg in EnumeratePrefixes(value))
                _expanded.Add(seg);
            _selected = value;

            CloseAddRow();
            RebuildTree();
        }

        private static IEnumerable<string> EnumeratePrefixes(string tag)
        {
            var parts = tag.Split(GameTag.SEPARATOR);
            for (int i = 1; i < parts.Length; i++)
                yield return string.Join(GameTag.SEPARATOR, parts[..i]);
        }

        private void CloseAddRow()
        {
            _addRow.SetDisplay(DisplayStyle.None);
            _newTagField.value = string.Empty;
        }

        // ─── Actions ──────────────────────────────────────────────────────────
        private void Apply()
        {
            _onApply?.Invoke(_selected);
            Close();
        }

        private void PingRegistry()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(GameTagRegistry)}");
            if (guids.Length == 0) return;
            var asset = AssetDatabase.LoadAssetAtPath<GameTagRegistry>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Close();
        }

        // ─── Reusable button ────────────────────────────────────────────────────
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

        // ─── Selection checkbox element ───────────────────────────────────────────
        private class CheckBox : VisualElement
        {
            private readonly Label _mark;

            public CheckBox()
            {
                this.SetSize(15, 15).SetBorderRadius(3).SetFlexShrink(0);
                style.alignItems = Align.Center;
                style.justifyContent = Justify.Center;
                style.borderTopWidth = style.borderBottomWidth =
                    style.borderLeftWidth = style.borderRightWidth = 1;

                _mark = new Label { pickingMode = PickingMode.Ignore };
                _mark.SetFontSize(11).SetColor(Color.white).SetFontStyle(FontStyle.Bold);
                _mark.style.unityTextAlign = TextAnchor.MiddleCenter;
                Add(_mark);
                Set(false);
            }

            public void Set(bool on)
            {
                if (on)
                {
                    this.SetBackgroundColor(Accent).SetBorderColor(Accent);
                    _mark.text = "✓";
                }
                else
                {
                    this.SetBackgroundColor(BgField).SetBorderColor(CheckBorder);
                    _mark.text = string.Empty;
                }
            }
        }

        // ─── Models ───────────────────────────────────────────────────────────
        private class TagNode
        {
            public readonly List<TagNode> Children = new();
            public bool ExistsInRegistry;
            public string FullPath;
            public string Label;
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
