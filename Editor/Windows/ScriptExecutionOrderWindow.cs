using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using DataKeeper.UIToolkit;

namespace DataKeeper.Editor.Windows
{
    public class ScriptExecutionOrderWindow : EditorWindow
    {
        private static readonly Color ColorCustom  = new Color(0.35f, 0.65f, 1.00f);
        private static readonly Color ColorAttr    = new Color(1.00f, 0.80f, 0.25f);
        private static readonly Color ColorDefault = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color RowBorder    = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color DirtyBorder  = new Color(0.80f, 0.60f, 0.10f);
        private static readonly Color DirtyBg      = new Color(0.20f, 0.18f, 0.08f);

        private const float ColOrder   = 50f;
        private const float ColSource  = 68f;
        private const float ColField   = 50f;
        private const float ColUnset   = 38f;
        private const float ColRemove  = 20f;
        private const float ColActions = ColField + ColUnset + ColRemove + 8f;

        [SerializeField] private List<MonoScript> _savedScripts = new();

        private readonly List<ScriptEntry> _entries = new();
        private ScrollView _listContainer;
        private string _searchFilter = "";

        [MenuItem("Tools/Windows/Script Execution Order", priority = 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptExecutionOrderWindow>();
            window.titleContent = new GUIContent("Execution Order",
                EditorGUIUtility.IconContent("UnityEditor.HierarchyWindow").image);
            window.minSize = new Vector2(480, 300);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.SetPadding(8);

            var dropZone = CreateDropZone();   dropZone.style.flexShrink   = 0; root.Add(dropZone);
            var toolbar  = CreateToolbar();    toolbar.style.flexShrink    = 0; root.Add(toolbar);
            var search   = CreateSearchBar();  search.style.flexShrink     = 0; root.Add(search);
            var headers  = CreateHeaders();    headers.style.flexShrink    = 0; root.Add(headers);

            _listContainer = new ScrollView(ScrollViewMode.Vertical)
                .SetFlexGrow(1)
                .SetMarginTop(2);
            root.Add(_listContainer);

            var bottomBar = CreateBottomBar(); bottomBar.style.flexShrink  = 0; root.Add(bottomBar);

            foreach (var script in _savedScripts)
            {
                if (script != null && !_entries.Any(e => e.Script == script))
                    _entries.Add(BuildEntry(script));
            }
            RebuildList();
        }

        // ── Drop zone ────────────────────────────────────────────────────

        private VisualElement CreateDropZone()
        {
            var zone = new VisualElement()
                .SetHeight(44)
                .SetMarginBottom(6)
                .SetPadding(6)
                .SetBorderRadius(4)
                .SetBorderWidth(1)
                .SetBorderColor(new Color(0.4f, 0.4f, 0.4f))
                .SetJustifyContent(Justify.Center)
                .SetAlignItems(Align.Center);

            new Label("Drop MonoScript or GameObject here")
                .SetColor(new Color(0.5f, 0.5f, 0.5f))
                .SetFontStyle(FontStyle.Italic)
                .SetChildOf(zone);

            zone.RegisterCallback<DragEnterEvent>(_ => zone.SetBorderColor(ColorCustom));
            zone.RegisterCallback<DragLeaveEvent>(_ => zone.SetBorderColor(new Color(0.4f, 0.4f, 0.4f)));
            zone.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                DragAndDrop.visualMode = IsValidDrag()
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;
            });
            zone.RegisterCallback<DragPerformEvent>(_ =>
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is MonoScript ms) TryAddScript(ms);
                    else if (obj is GameObject go) AddFromGameObject(go);
                }
                zone.SetBorderColor(new Color(0.4f, 0.4f, 0.4f));
                RebuildList();
            });

            return zone;
        }

        private static bool IsValidDrag()
            => DragAndDrop.objectReferences.Any(o => o is MonoScript || o is GameObject);

        // ── Toolbar, search & headers ─────────────────────────────────────

        private VisualElement CreateToolbar()
        {
            var bar = new VisualElement().SetFlexRow().SetMarginBottom(4);

            new Button(SortEntries) { text = "Sort by Order" }.SetHeight(22).SetChildOf(bar);
            new Button(ScanScene)   { text = "From Scene" }   .SetHeight(22).SetMarginLeft(4).SetChildOf(bar);
            new Button(ClearAll)    { text = "Clear All" }    .SetHeight(22).SetMarginLeft(4).SetChildOf(bar);

            return bar;
        }

        private VisualElement CreateSearchBar()
        {
            var bar = new VisualElement().SetFlexRow().SetMarginBottom(4).SetAlignItems(Align.Center);

            var searchField = new ToolbarSearchField();
            searchField.style.flexGrow = 1;
            searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                RebuildList();
            });
            bar.Add(searchField);

            return bar;
        }

        private static VisualElement CreateHeaders()
        {
            var row = new VisualElement()
                .SetFlexRow()
                .SetPadding(left: 4, top: 2, right: 4, bottom: 2)
                .SetBackgroundColor(new Color(0.18f, 0.18f, 0.18f))
                .SetBorderRadius(2)
                .SetMarginBottom(2);

            MakeHeaderLabel("Order",  ColOrder).SetChildOf(row);
            MakeHeaderLabel("Source", ColSource).SetChildOf(row);
            MakeHeaderLabel("Script", 0, flex: true).SetChildOf(row);
            MakeHeaderLabel("",       ColActions).SetChildOf(row);

            return row;
        }

        private static Label MakeHeaderLabel(string text, float width, bool flex = false)
        {
            var l = new Label(text).SetFontStyle(FontStyle.Bold).SetFontSize(11);
            return flex ? l.SetFlexGrow(1) : l.SetWidth(width);
        }

        private VisualElement CreateBottomBar()
        {
            var bar = new VisualElement().SetFlexRow().SetMarginTop(4);

            var applyBtn = new Button(ApplyAll) { text = "Apply All" };
            applyBtn.tooltip = "Apply all pending order changes to Project Settings";
            applyBtn.SetHeight(22).SetChildOf(bar);

            var revertBtn = new Button(RevertAll) { text = "Revert" };
            revertBtn.tooltip = "Revert all pending changes back to current applied values";
            revertBtn.SetHeight(22).SetMarginLeft(4).SetChildOf(bar);

            return bar;
        }

        // ── Data helpers ──────────────────────────────────────────────────

        private void TryAddScript(MonoScript script)
        {
            if (script == null) return;
            var type = script.GetClass();
            if (type == null || !type.IsSubclassOf(typeof(MonoBehaviour))) return;
            if (_entries.Any(e => e.Script == script)) return;
            _entries.Add(BuildEntry(script));
            if (!_savedScripts.Contains(script))
                _savedScripts.Add(script);
        }

        private void AddFromGameObject(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                TryAddScript(MonoScript.FromMonoBehaviour(mb));
            }
        }

        private void ScanScene()
        {
            var behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                TryAddScript(MonoScript.FromMonoBehaviour(mb));
            }
            SortEntries();
        }

        private static ScriptEntry BuildEntry(MonoScript script)
        {
            var entry = new ScriptEntry { Script = script };

            int psOrder = MonoImporter.GetExecutionOrder(script);
            if (psOrder != 0)
            {
                entry.IsInProjectSettings  = true;
                entry.ProjectSettingsOrder = psOrder;
            }

            var attr = script.GetClass()?.GetCustomAttribute<DefaultExecutionOrder>();
            if (attr != null)
            {
                entry.HasAttribute   = true;
                entry.AttributeOrder = attr.order;
            }

            entry.PendingOrder = entry.EffectiveOrder;
            return entry;
        }

        // ── List ──────────────────────────────────────────────────────────

        private void SortEntries()
        {
            var sorted = _entries
                .OrderBy(e => e.EffectiveOrder)
                .ThenBy(e => e.Script.name)
                .ToList();
            _entries.Clear();
            _entries.AddRange(sorted);
            RebuildList();
        }

        private void ClearAll()
        {
            _entries.Clear();
            _savedScripts.Clear();
            _listContainer.Clear();
        }

        private void RebuildList()
        {
            _listContainer.Clear();
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    entry.Script.name.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _listContainer.Add(BuildRow(entry));
            }
        }

        private VisualElement BuildRow(ScriptEntry entry)
        {
            bool dirty = entry.IsDirty;

            var row = new VisualElement()
                .SetFlexRow()
                .SetAlignItems(Align.Center)
                .SetPadding(left: 4, top: 2, right: 4, bottom: 2)
                .SetMarginBottom(2)
                .SetBorderRadius(3)
                .SetBorderWidth(1)
                .SetBorderColor(dirty ? DirtyBorder : RowBorder);

            if (dirty)
                row.SetBackgroundColor(DirtyBg);

            var color = entry.IsInProjectSettings ? ColorCustom
                      : entry.HasAttribute        ? ColorAttr
                      :                             ColorDefault;

            new Label(entry.EffectiveOrder.ToString())
                .SetWidth(ColOrder)
                .SetColor(color)
                .SetFontStyle(FontStyle.Bold)
                .SetChildOf(row);

            new Label(entry.SourceLabel)
                .SetWidth(ColSource)
                .SetFontSize(10)
                .SetColor(color)
                .SetChildOf(row);

            var scriptField = new ObjectField { objectType = typeof(MonoScript) };
            scriptField.SetValueWithoutNotify(entry.Script);
            scriptField.RegisterValueChangedCallback(_ => scriptField.SetValueWithoutNotify(entry.Script));
            scriptField.SetFlexGrow(1).SetHeight(18).SetChildOf(row);

            var actions = new VisualElement()
                .SetFlexRow()
                .SetAlignItems(Align.Center)
                .SetMarginLeft(4)
                .SetWidth(ColActions);

            var orderField = new IntegerField();
            orderField.SetValueWithoutNotify(entry.PendingOrder);
            orderField.SetWidth(ColField).SetHeight(18).SetChildOf(actions);

            orderField.RegisterValueChangedCallback(evt =>
            {
                entry.PendingOrder = evt.newValue;
                bool nowDirty = entry.IsDirty;
                row.SetBorderColor(nowDirty ? DirtyBorder : RowBorder);
                row.SetBackgroundColor(nowDirty ? DirtyBg : new Color(0, 0, 0, 0));
            });

            var unsetBtn = new Button(() =>
            {
                RemoveFromPS(entry);
                orderField.SetValueWithoutNotify(entry.PendingOrder);
            }) { text = "Unset" };
            unsetBtn.tooltip = "Remove custom order — reverts to attribute or default (0)";
            unsetBtn.SetWidth(ColUnset).SetHeight(18).SetMarginLeft(2);
            unsetBtn.SetEnabled(entry.IsInProjectSettings);
            unsetBtn.SetChildOf(actions);

            var removeBtn = new Button(() =>
            {
                _entries.Remove(entry);
                _savedScripts.Remove(entry.Script);
                RebuildList();
            }) { text = "✕" };
            removeBtn.tooltip = "Remove from this list";
            removeBtn.SetWidth(ColRemove).SetHeight(18).SetMarginLeft(2).SetChildOf(actions);

            actions.SetChildOf(row);
            return row;
        }

        // ── Execution order mutations ─────────────────────────────────────

        private void ApplyAll()
        {
            bool any = false;
            foreach (var entry in _entries)
            {
                if (!entry.IsDirty) continue;
                if (entry.PendingOrder == 0)
                {
                    Debug.LogWarning($"[Execution Order] Skipping {entry.Script.name}: 0 is the default — use a non-zero value.");
                    continue;
                }
                MonoImporter.SetExecutionOrder(entry.Script, entry.PendingOrder);
                entry.ProjectSettingsOrder = entry.PendingOrder;
                entry.IsInProjectSettings  = true;
                any = true;
            }
            if (any) RebuildList();
        }

        private void RevertAll()
        {
            foreach (var entry in _entries)
                entry.PendingOrder = entry.EffectiveOrder;
            RebuildList();
        }

        private void RemoveFromPS(ScriptEntry entry)
        {
            MonoImporter.SetExecutionOrder(entry.Script, 0);
            entry.IsInProjectSettings  = false;
            entry.ProjectSettingsOrder = 0;
            entry.PendingOrder         = entry.EffectiveOrder;
            RebuildList();
        }

        // ── Data model ────────────────────────────────────────────────────

        private class ScriptEntry
        {
            public MonoScript Script;
            public int  ProjectSettingsOrder;
            public bool IsInProjectSettings;
            public int  AttributeOrder;
            public bool HasAttribute;
            public int  PendingOrder;

            public int EffectiveOrder =>
                IsInProjectSettings ? ProjectSettingsOrder :
                HasAttribute        ? AttributeOrder       : 0;

            public bool IsDirty => PendingOrder != EffectiveOrder;

            public string SourceLabel =>
                IsInProjectSettings ? "Custom"    :
                HasAttribute        ? "Attribute" : "Default";
        }
    }
}
