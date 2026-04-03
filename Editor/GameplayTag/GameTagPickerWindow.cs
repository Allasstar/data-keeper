#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GameTagPickerWindow : EditorWindow
{
    // ── Public API ────────────────────────────────────────────────────────────
    public static void Show(
        GameTagRegistry registry,
        IEnumerable<string> currentTags,
        bool multiSelect,
        Action<IReadOnlyList<string>> onApply)
    {
        var win = CreateInstance<GameTagPickerWindow>();
        win.titleContent = new GUIContent("Game Tag Picker");
        win.minSize      = new Vector2(340, 460);
        win._registry    = registry;
        win._multiSelect = multiSelect;
        win._onApply     = onApply;
        win._selected    = new HashSet<string>(currentTags ?? Enumerable.Empty<string>());
        win.RebuildTree();
        win.ShowAuxWindow();
    }

    // ── State ─────────────────────────────────────────────────────────────────
    private GameTagRegistry               _registry;
    private bool                          _multiSelect;
    private Action<IReadOnlyList<string>> _onApply;

    private HashSet<string>  _selected    = new();
    private HashSet<string>  _expanded    = new();
    private string           _search      = string.Empty;
    private bool             _addingNew;
    private string           _newTagInput = string.Empty;

    private List<TagNode>    _roots  = new();
    private Vector2          _scroll;

    // ── Tree model ────────────────────────────────────────────────────────────
    private class TagNode
    {
        public string        Label;
        public string        FullPath;
        public bool          ExistsInRegistry; // true if this exact path is a registered tag
        public List<TagNode> Children = new();
    }

    private void RebuildTree()
    {
        _roots.Clear();
        var dict = new Dictionary<string, TagNode>();

        IEnumerable<string> source = _registry.Tags.OrderBy(t => t);
        if (!string.IsNullOrEmpty(_search))
            source = source.Where(t =>
                t.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);

        foreach (var tag in source)
        {
            var parts  = tag.Split(GameTag.SEPARASTOR);
            TagNode parent = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var path = string.Join(GameTag.SEPARASTOR, parts[..(i + 1)]);
                if (!dict.TryGetValue(path, out var node))
                {
                    node = new TagNode { Label = parts[i], FullPath = path };
                    dict[path] = node;
                    if (parent == null) _roots.Add(node);
                    else                parent.Children.Add(node);
                }
                // Mark the terminal segment as an explicit registry entry
                if (i == parts.Length - 1)
                    node.ExistsInRegistry = true;
                parent = node;
            }
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────
    private enum CheckState { None, Partial, All }

    // Every node is independently selectable regardless of children.
    // Parent checkbox shows Partial when SOME nodes in its subtree are selected.
    private CheckState GetCheckState(TagNode node)
    {
        var all = CollectAllNodes(node);
        int total  = all.Count;
        int ticked = all.Count(p => _selected.Contains(p));

        if (ticked == 0)     return CheckState.None;
        if (ticked == total) return CheckState.All;
        return CheckState.Partial;
    }

    // Collects this node + every descendant path (all are independently selectable)
    private List<string> CollectAllNodes(TagNode node)
    {
        var result = new List<string>();
        CollectAll(node, result);
        return result;
    }

    private void CollectAll(TagNode node, List<string> result)
    {
        result.Add(node.FullPath);
        foreach (var child in node.Children)
            CollectAll(child, result);
    }

    // Toggle this single node's path only — no cascade to children
    private void ToggleNode(TagNode node)
    {
        if (_multiSelect)
        {
            if (_selected.Contains(node.FullPath)) _selected.Remove(node.FullPath);
            else                                   _selected.Add(node.FullPath);
        }
        else
        {
            bool wasSelected = _selected.Contains(node.FullPath);
            _selected.Clear();
            if (!wasSelected) _selected.Add(node.FullPath);
        }
    }

    // Checkbox click on a parent: bulk-toggle this node + all descendants
    private void ToggleSubtree(TagNode node)
    {
        if (!_multiSelect)
        {
            // In single-select, subtree bulk-toggle doesn't make sense — treat as single toggle
            ToggleNode(node);
            return;
        }

        var all   = CollectAllNodes(node);
        bool allOn = all.All(p => _selected.Contains(p));
        if (allOn) all.ForEach(p => _selected.Remove(p));
        else       all.ForEach(p => _selected.Add(p));
    }

    // ── GUI ───────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        DrawToolbar();
        if (_addingNew) DrawAddTagRow();
        DrawTree();
        DrawFooter();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField,
            GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck()) RebuildTree();

        if (GUILayout.Button("+ New Tag", EditorStyles.toolbarButton, GUILayout.Width(72)))
        {
            _addingNew   = !_addingNew;
            _newTagInput = string.Empty;
            if (_addingNew)
                EditorApplication.delayCall += () => GUI.FocusControl("NewTagField");
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAddTagRow()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUI.SetNextControlName("NewTagField");
        _newTagInput = EditorGUILayout.TextField(_newTagInput, GUILayout.ExpandWidth(true));

        bool confirm = GUILayout.Button("Add", GUILayout.Width(40))
                    || (Event.current.type   == EventType.KeyDown
                     && Event.current.keyCode == KeyCode.Return);
        if (confirm) CommitNewTag();

        if (GUILayout.Button("✕", GUILayout.Width(22)))
        {
            _addingNew   = false;
            _newTagInput = string.Empty;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void CommitNewTag()
    {
        var value = _newTagInput.Trim();
        if (string.IsNullOrEmpty(value) || _registry.Tags.Contains(value))
        {
            _addingNew   = false;
            _newTagInput = string.Empty;
            return;
        }

        var so   = new SerializedObject(_registry);
        var list = so.FindProperty("_tags");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = value;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        _addingNew   = false;
        _newTagInput = string.Empty;
        RebuildTree();
    }

    private void DrawTree()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        foreach (var root in _roots)
            DrawNode(root, 0);
        EditorGUILayout.EndScrollView();
    }

    private static readonly Color kRowHover = new(0.5f, 0.5f, 0.5f, 0.08f);

    private void DrawNode(TagNode node, int depth)
    {
        bool hasChildren = node.Children.Count > 0;
        bool isExpanded  = _expanded.Contains(node.FullPath) || !string.IsNullOrEmpty(_search);
        var  state       = GetCheckState(node);

        var rowRect = GUILayoutUtility.GetRect(
            GUIContent.none, EditorStyles.label,
            GUILayout.Height(20), GUILayout.ExpandWidth(true));

        if (rowRect.Contains(Event.current.mousePosition))
        {
            EditorGUI.DrawRect(rowRect, kRowHover);
            Repaint();
        }

        float x = rowRect.x + depth * 16f;
        float y = rowRect.y + 1f;

        // ── Foldout arrow ─────────────────────────────────────────────────────
        if (hasChildren)
        {
            var arrowRect = new Rect(x, y, 16, 18);
            EditorGUI.BeginChangeCheck();
            bool nowOpen = EditorGUI.Foldout(arrowRect, isExpanded, GUIContent.none, true);
            if (EditorGUI.EndChangeCheck())
            {
                if (nowOpen) _expanded.Add(node.FullPath);
                else         _expanded.Remove(node.FullPath);
            }
        }

        // ── Checkbox ──────────────────────────────────────────────────────────
        // Checkbox toggles ONLY this node's own path (not children).
        // To bulk-select children, user must individually check each row,
        // or click the checkbox while holding Shift (handled via ToggleSubtree).
        var cbRect = new Rect(x + 18, y + 2, 14, 14);
        EditorGUI.BeginChangeCheck();
        DrawCheckbox(cbRect, state);
        if (EditorGUI.EndChangeCheck())
        {
            bool shift = Event.current.shift;
            if (hasChildren && (shift || !_multiSelect))
                ToggleSubtree(node);
            else if (hasChildren)
                ToggleNode(node);   // tick just this path, children unaffected
            else
                ToggleNode(node);
            Repaint();
        }

        // ── Label ─────────────────────────────────────────────────────────────
        var labelRect = new Rect(x + 36, y, rowRect.xMax - (x + 36), 18);

        // Dim the label if this path isn't an explicit registry entry
        // (it's a synthesized intermediate node)
        var labelStyle = new GUIStyle(EditorStyles.label) { richText = true };
        if (!node.ExistsInRegistry)
            labelStyle.normal.textColor = new Color(
                labelStyle.normal.textColor.r,
                labelStyle.normal.textColor.g,
                labelStyle.normal.textColor.b,
                0.5f);

        string display = node.Label;

        if (GUI.Button(labelRect, display, labelStyle))
        {
            if (hasChildren)
            {
                // Clicking label toggles expand; also toggles own selection
                if (isExpanded) _expanded.Remove(node.FullPath);
                else            _expanded.Add(node.FullPath);
            }
            else
            {
                ToggleNode(node);
            }
        }

        // ── Recurse ───────────────────────────────────────────────────────────
        if (hasChildren && isExpanded)
            foreach (var child in node.Children)
                DrawNode(child, depth + 1);
    }

    private void DrawCheckbox(Rect rect, CheckState state)
    {
        if (state == CheckState.Partial)
        {
            EditorGUI.showMixedValue = true;
            EditorGUI.Toggle(rect, false);
            EditorGUI.showMixedValue = false;
        }
        else
        {
            EditorGUI.Toggle(rect, state == CheckState.All);
        }
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        string label = _selected.Count == 0 ? "Nothing selected"
                     : _selected.Count == 1 ? _selected.First()
                     : $"{_selected.Count} tags selected";
        GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(44)))
        {
            _selected.Clear();
            Repaint();
        }

        if (GUILayout.Button("Apply", EditorStyles.toolbarButton, GUILayout.Width(48)))
        {
            _onApply?.Invoke(_selected.ToList());
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif