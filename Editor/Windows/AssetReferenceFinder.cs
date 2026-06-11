using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using DataKeeper.UIToolkit;

namespace DataKeeper.Editor.Windows
{
    public class AssetReferenceFinder : EditorWindow
    {
        // ─── State ────────────────────────────────────────────────────────────
        private Object _targetAsset;
        private string _targetGuid;
        private string _targetPath;
        private readonly List<ResultEntry> _results = new List<ResultEntry>();
        private int _selectedFilter;
        private bool _hasSearched;
        private string _searchText = "";

        // ─── UI refs ──────────────────────────────────────────────────────────
        private ObjectField _objectField;
        private Label _countLabel;
        private VisualElement _statusBar;
        private VisualElement _searchBar;
        private VisualElement _resultsContainer;
        private VisualElement _emptyState;
        private Label _emptyTitle;
        private Label _emptySub;
        private Button[] _filterChips;

        // ─── Palette ──────────────────────────────────────────────────────────
        private static readonly Color BgDeep    = new Color(0.155f, 0.155f, 0.155f);
        private static readonly Color BgCard    = new Color(0.210f, 0.210f, 0.210f);
        private static readonly Color BgHover   = new Color(0.255f, 0.255f, 0.255f);
        private static readonly Color BgDetail  = new Color(0.175f, 0.175f, 0.175f);
        private static readonly Color Border    = new Color(0.115f, 0.115f, 0.115f);
        private static readonly Color Divider   = new Color(0.150f, 0.150f, 0.150f);
        private static readonly Color TextHi    = new Color(0.92f,  0.92f,  0.92f);
        private static readonly Color TextLo    = new Color(0.82f,  0.82f,  0.82f);
        private static readonly Color TextPath  = new Color(0.72f,  0.72f,  0.72f);
        private static readonly Color TextBlue  = new Color(0.70f,  0.82f,  1.00f);
        private static readonly Color Connector = new Color(0.28f,  0.28f,  0.28f);
        private static readonly Color CScene    = new Color(0.28f,  0.66f,  1.00f);
        private static readonly Color CPrefab   = new Color(0.45f,  0.86f,  0.45f);
        private static readonly Color CAsset    = new Color(1.00f,  0.73f,  0.25f);
        private static readonly Color COther    = new Color(0.58f,  0.58f,  0.58f);

        private static readonly string[] Kinds = { "All", "Scene", "Prefab", "Asset", "Other" };

        // ─── Data types ───────────────────────────────────────────────────────
        private class ResultEntry
        {
            public string Path;
            public string Kind;
            public bool IsExpanded;
            public bool DetailsLoaded;
            public readonly List<DetailItem> Details = new List<DetailItem>();
            public VisualElement DetailsContainer;
            public VisualElement HeaderRow;
            public Label Arrow;
        }

        private struct DetailItem
        {
            public string ObjectPath;
            public string ComponentType;
            public string FieldName;
            public GlobalObjectId? ObjId;
            public string ScenePath;
        }

        // ─── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/Windows/Asset Reference Finder", priority = 5)]
        public static void ShowWindow() => OpenWindow(null);

        [MenuItem("Assets/Find References in Project", priority = 25)]
        static void FindFromContext() => OpenWindow(Selection.activeObject);

        [MenuItem("Assets/Find References in Project", true)]
        static bool FindFromContextValidate() => Selection.activeObject != null;

        public static void OpenWindow(Object target)
        {
            var w = GetWindow<AssetReferenceFinder>();
            w.titleContent = new GUIContent("Reference Finder",
                EditorGUIUtility.FindTexture("d_Search Icon"));
            w.minSize = new Vector2(520, 320);
            if (target == null) return;
            w._targetAsset = target;
            w._objectField?.SetValueWithoutNotify(target);
            w.FindReferences();
        }

        // ─── CreateGUI ────────────────────────────────────────────────────────
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.backgroundColor = BgDeep;

            root.Add(BuildHeader());

            _statusBar = BuildStatusBar();
            _statusBar.SetDisplay(DisplayStyle.None);
            root.Add(_statusBar);

            _searchBar = BuildSearchBar();
            _searchBar.SetDisplay(DisplayStyle.None);
            root.Add(_searchBar);

            var scroll = new ScrollView(ScrollViewMode.Vertical).SetFlexGrow(1);
            scroll.contentContainer.SetPadding(8);
            _emptyState = BuildEmptyState();
            scroll.Add(_emptyState);
            _resultsContainer = new VisualElement();
            scroll.Add(_resultsContainer);
            root.Add(scroll);

            // apply deferred target (e.g. opened via context menu before CreateGUI ran)
            if (_targetAsset != null)
            {
                _objectField.SetValueWithoutNotify(_targetAsset);
                if (_hasSearched) RefreshView();
            }
        }

        // ─── Header ───────────────────────────────────────────────────────────
        private VisualElement BuildHeader()
        {
            var hdr = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center).SetPadding(8)
                .SetBackgroundColor(new Color(0.22f, 0.22f, 0.22f));
            hdr.style.borderBottomWidth = 1;
            hdr.style.borderBottomColor = Border;

            _objectField = new ObjectField { label = "", objectType = typeof(Object) };
            _objectField.SetFlexGrow(1).SetMarginRight(6);
            _objectField.RegisterValueChangedCallback(evt =>
            {
                _targetAsset = evt.newValue;
                _results.Clear();
                _targetGuid = null;
                _hasSearched = false;
                RefreshView();
            });
            hdr.Add(_objectField);

            var btn = new Button(FindReferences) { text = "Find" };
            btn.style.backgroundColor = new Color(0.22f, 0.44f, 0.76f);
            btn.style.color = Color.white;
            btn.style.width = 64;
            btn.style.height = 24;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = 4; btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4; btn.style.borderBottomRightRadius = 4;
            hdr.Add(btn);
            return hdr;
        }

        // ─── Status bar ───────────────────────────────────────────────────────
        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 10, top: 5, right: 10, bottom: 5)
                .SetBackgroundColor(new Color(0.18f, 0.18f, 0.18f));
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = Border;

            _countLabel = new Label().SetFontSize(11).SetColor(TextLo).SetFlexGrow(1);
            bar.Add(_countLabel);

            var chips = new VisualElement().SetFlexRow();
            _filterChips = new Button[Kinds.Length];
            for (int i = 0; i < Kinds.Length; i++)
            {
                int idx = i;
                var chip = new Button(() => SetFilter(idx)) { text = Kinds[i] };
                chip.style.marginLeft = 3;
                chip.style.paddingLeft = 8;  chip.style.paddingRight = 8;
                chip.style.paddingTop = 2;   chip.style.paddingBottom = 2;
                chip.style.fontSize = 10;
                chip.style.borderTopLeftRadius = 10;     chip.style.borderTopRightRadius = 10;
                chip.style.borderBottomLeftRadius = 10;  chip.style.borderBottomRightRadius = 10;
                StyleChip(chip, i == 0);
                _filterChips[i] = chip;
                chips.Add(chip);
            }
            bar.Add(chips);
            return bar;
        }

        // ─── Search bar ───────────────────────────────────────────────────────
        private VisualElement BuildSearchBar()
        {
            var bar = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetPadding(left: 8, top: 4, right: 8, bottom: 4)
                .SetBackgroundColor(new Color(0.18f, 0.18f, 0.18f));
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = Border;

            var field = new ToolbarSearchField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RefreshView();
            });
            bar.Add(field);
            return bar;
        }

        // ─── Empty state ──────────────────────────────────────────────────────
        private VisualElement BuildEmptyState()
        {
            var wrap = new VisualElement()
                .SetAlignItems(Align.Center).SetJustifyContent(Justify.Center).SetFlexGrow(1);
            wrap.style.paddingTop = 50;

            var iconTex = EditorGUIUtility.FindTexture("d_Search Icon") as Texture2D;
            if (iconTex != null)
            {
                var ic = new VisualElement();
                ic.style.width = 44; ic.style.height = 44;
                ic.style.backgroundImage = iconTex;
                ic.style.opacity = 0.15f;
                ic.style.marginBottom = 14;
                ic.style.alignSelf = Align.Center;
                wrap.Add(ic);
            }

            _emptyTitle = new Label("Drop an asset and click Find")
                .SetFontSize(13).SetColor(TextLo);
            _emptyTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _emptyTitle.style.marginBottom = 5;
            wrap.Add(_emptyTitle);

            _emptySub = new Label("Searches scenes, prefabs and ScriptableObjects")
                .SetFontSize(11).SetColor(TextPath);
            _emptySub.style.unityTextAlign = TextAnchor.MiddleCenter;
            wrap.Add(_emptySub);
            return wrap;
        }

        // ─── Filter chips ─────────────────────────────────────────────────────
        private void StyleChip(Button c, bool active)
        {
            c.style.backgroundColor = active
                ? new Color(0.28f, 0.48f, 0.76f)
                : new Color(0.22f, 0.22f, 0.22f);
            c.style.color = active ? Color.white : TextLo;
            float bw = active ? 0f : 1f;
            c.style.borderTopWidth = bw;    c.style.borderBottomWidth = bw;
            c.style.borderLeftWidth = bw;   c.style.borderRightWidth = bw;
            c.style.borderTopColor = Border;    c.style.borderBottomColor = Border;
            c.style.borderLeftColor = Border;   c.style.borderRightColor = Border;
        }

        private void SetFilter(int i)
        {
            _selectedFilter = i;
            for (int j = 0; j < _filterChips.Length; j++) StyleChip(_filterChips[j], j == i);
            RefreshView();
        }

        // ─── View refresh ─────────────────────────────────────────────────────
        private void RefreshView()
        {
            if (_emptyState == null || _resultsContainer == null) return;
            _resultsContainer.Clear();

            bool empty = _results.Count == 0;
            _emptyState.SetDisplay(empty ? DisplayStyle.Flex : DisplayStyle.None);
            _statusBar.SetDisplay(empty ? DisplayStyle.None : DisplayStyle.Flex);
            _searchBar.SetDisplay(empty ? DisplayStyle.None : DisplayStyle.Flex);

            if (empty)
            {
                if (_hasSearched && _targetPath != null)
                {
                    _emptyTitle.text = "No references found";
                    _emptySub.text = $"{Path.GetFileName(_targetPath)} is not referenced by any tracked asset";
                }
                else
                {
                    _emptyTitle.text = "Drop an asset and click Find";
                    _emptySub.text = "Searches scenes, prefabs and ScriptableObjects";
                }
                return;
            }

            int total = _results.Count;
            int visible = 0;
            foreach (var e in _results)
                if (MatchesFilters(e)) visible++;

            string assetName = Path.GetFileName(_targetPath);
            bool isFiltered = visible != total;
            _countLabel.text = isFiltered
                ? $"{visible} / {total} file(s)  ·  {assetName}"
                : $"{total} file(s)  ·  {assetName}";

            foreach (var entry in _results)
            {
                if (!MatchesFilters(entry)) continue;
                _resultsContainer.Add(BuildCard(entry));
            }
        }

        private bool MatchesFilters(ResultEntry e)
        {
            if (_selectedFilter != 0 && e.Kind != Kinds[_selectedFilter]) return false;
            if (!string.IsNullOrEmpty(_searchText) &&
                e.Path.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return true;
        }

        // ─── Result card ──────────────────────────────────────────────────────
        private VisualElement BuildCard(ResultEntry entry)
        {
            Color kc = KindColor(entry.Kind);

            var card = new VisualElement();
            card.style.marginBottom = 3;
            card.style.overflow = Overflow.Hidden;
            card.style.borderTopLeftRadius = 5;     card.style.borderTopRightRadius = 5;
            card.style.borderBottomLeftRadius = 5;  card.style.borderBottomRightRadius = 5;
            card.style.borderTopWidth = 1;     card.style.borderTopColor = Border;
            card.style.borderBottomWidth = 1;  card.style.borderBottomColor = Border;
            card.style.borderLeftWidth = 1;    card.style.borderLeftColor = Border;
            card.style.borderRightWidth = 1;   card.style.borderRightColor = Border;

            // Header row
            entry.HeaderRow = new VisualElement()
                .SetFlexRow().SetAlignItems(Align.Center)
                .SetBackgroundColor(BgCard);
            entry.HeaderRow.style.height = 28;

            // Left accent bar
            var accent = new VisualElement();
            accent.style.width = 3;
            accent.style.backgroundColor = kc;
            accent.style.alignSelf = Align.Stretch;
            entry.HeaderRow.Add(accent);

            // Arrow
            entry.Arrow = new Label(entry.IsExpanded ? "▾" : "▸").SetFontSize(11).SetColor(TextLo);
            entry.Arrow.style.width = 18;
            entry.Arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
            entry.Arrow.style.marginLeft = 4;
            entry.HeaderRow.Add(entry.Arrow);

            // Asset icon
            var iconTex = AssetDatabase.GetCachedIcon(entry.Path) as Texture2D;
            if (iconTex != null)
            {
                var ic = new VisualElement();
                ic.style.width = 15; ic.style.height = 15;
                ic.style.backgroundImage = iconTex;
                ic.style.flexShrink = 0;
                ic.style.marginRight = 5;
                ic.style.alignSelf = Align.Center;
                entry.HeaderRow.Add(ic);
            }

            // File name
            var fname = new Label(Path.GetFileName(entry.Path))
                .SetFontSize(12).SetFontStyle(FontStyle.Bold).SetColor(TextHi);
            fname.style.unityTextAlign = TextAnchor.MiddleLeft;
            fname.style.flexShrink = 0;
            fname.style.marginRight = 8;
            entry.HeaderRow.Add(fname);

            // Kind pill
            var pill = new Label(entry.Kind).SetFontSize(9);
            pill.style.color = kc;
            pill.style.backgroundColor = new Color(kc.r, kc.g, kc.b, 0.15f);
            pill.style.borderTopColor = new Color(kc.r, kc.g, kc.b, 0.5f);
            pill.style.borderBottomColor = new Color(kc.r, kc.g, kc.b, 0.5f);
            pill.style.borderLeftColor = new Color(kc.r, kc.g, kc.b, 0.5f);
            pill.style.borderRightColor = new Color(kc.r, kc.g, kc.b, 0.5f);
            pill.style.borderTopWidth = 1; pill.style.borderBottomWidth = 1;
            pill.style.borderLeftWidth = 1; pill.style.borderRightWidth = 1;
            pill.style.borderTopLeftRadius = 8;     pill.style.borderTopRightRadius = 8;
            pill.style.borderBottomLeftRadius = 8;  pill.style.borderBottomRightRadius = 8;
            pill.style.paddingLeft = 5; pill.style.paddingRight = 5;
            pill.style.paddingTop = 1;  pill.style.paddingBottom = 1;
            pill.style.unityTextAlign = TextAnchor.MiddleCenter;
            pill.style.flexShrink = 0;
            pill.style.marginRight = 8;
            entry.HeaderRow.Add(pill);

            // Path (grows, truncates)
            var pathLbl = new Label(entry.Path).SetFontSize(10).SetColor(TextPath);
            pathLbl.style.flexGrow = 1;
            pathLbl.style.flexShrink = 1;
            pathLbl.style.minWidth = 0;
            pathLbl.style.overflow = Overflow.Hidden;
            pathLbl.style.textOverflow = TextOverflow.Ellipsis;
            pathLbl.style.whiteSpace = WhiteSpace.NoWrap;
            pathLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            entry.HeaderRow.Add(pathLbl);

            // Select button
            var pingBtn = new Button(() =>
            {
                var a = AssetDatabase.LoadAssetAtPath<Object>(entry.Path);
                if (a) { EditorGUIUtility.PingObject(a); Selection.activeObject = a; }
            }) { text = "Select", tooltip = "Select and ping in Project window" };
            pingBtn.style.height = 18; pingBtn.style.paddingLeft = 6; pingBtn.style.paddingRight = 6;
            pingBtn.style.paddingTop = 0; pingBtn.style.paddingBottom = 0; pingBtn.style.fontSize = 10;
            pingBtn.style.backgroundColor = new Color(0.22f, 0.35f, 0.55f);
            pingBtn.style.color = TextBlue;
            pingBtn.style.borderTopWidth = 0; pingBtn.style.borderBottomWidth = 0;
            pingBtn.style.borderLeftWidth = 0; pingBtn.style.borderRightWidth = 0;
            pingBtn.style.borderTopLeftRadius = 3;    pingBtn.style.borderTopRightRadius = 3;
            pingBtn.style.borderBottomLeftRadius = 3; pingBtn.style.borderBottomRightRadius = 3;
            pingBtn.style.flexShrink = 0;
            pingBtn.style.marginRight = 6;
            entry.HeaderRow.Add(pingBtn);

            // Details panel
            entry.DetailsContainer = new VisualElement()
                .SetBackgroundColor(BgDetail)
                .SetPadding(left: 28, top: 4, right: 8, bottom: 6)
                .SetDisplay(entry.IsExpanded ? DisplayStyle.Flex : DisplayStyle.None);
            entry.DetailsContainer.style.borderTopWidth = 1;
            entry.DetailsContainer.style.borderTopColor = Divider;

            if (entry.IsExpanded && !entry.DetailsLoaded) LoadAndBuild(entry);
            else if (entry.DetailsLoaded) BuildDetailRows(entry);

            // Expand toggle (skip ping button area)
            entry.HeaderRow.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                entry.IsExpanded = !entry.IsExpanded;
                entry.Arrow.text = entry.IsExpanded ? "▾" : "▸";
                entry.DetailsContainer.style.display =
                    entry.IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                if (entry.IsExpanded && !entry.DetailsLoaded) LoadAndBuild(entry);
            });

            entry.HeaderRow.RegisterCallback<MouseEnterEvent>(_ =>
                entry.HeaderRow.style.backgroundColor = BgHover);
            entry.HeaderRow.RegisterCallback<MouseLeaveEvent>(_ =>
                entry.HeaderRow.style.backgroundColor = BgCard);

            card.Add(entry.HeaderRow);
            card.Add(entry.DetailsContainer);
            return card;
        }

        // ─── Detail rows ──────────────────────────────────────────────────────
        private void LoadAndBuild(ResultEntry entry)
        {
            entry.DetailsLoaded = true;
            switch (entry.Kind)
            {
                case "Prefab": LoadPrefabDetails(entry); break;
                case "Scene":  LoadSceneDetails(entry);  break;
                default:       LoadAssetDetails(entry);  break;
            }
            BuildDetailRows(entry);
        }

        private void BuildDetailRows(ResultEntry entry)
        {
            entry.DetailsContainer.Clear();
            if (entry.Details.Count == 0)
            {
                var lbl = new Label("No component references found")
                    .SetFontSize(11).SetColor(TextLo);
                lbl.style.paddingTop = 3;
                entry.DetailsContainer.Add(lbl);
                return;
            }
            for (int i = 0; i < entry.Details.Count; i++)
                entry.DetailsContainer.Add(BuildDetailRow(entry.Details[i], i == entry.Details.Count - 1));
        }

        private VisualElement BuildDetailRow(DetailItem d, bool last)
        {
            var row = new VisualElement().SetFlexRow().SetAlignItems(Align.Center);
            row.style.paddingTop = 3; row.style.paddingBottom = 3;
            row.SetBorderRadius(3);
            if (!last)
            {
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            }

            // Connector line
            var line = new VisualElement();
            line.style.width = 2;
            line.style.backgroundColor = Connector;
            line.style.alignSelf = Align.Stretch;
            line.style.marginRight = 8; line.style.marginLeft = 2;
            line.style.borderTopLeftRadius = 1; line.style.borderBottomLeftRadius = 1;
            row.Add(line);

            // Transform path
            var pathLbl = new Label(d.ObjectPath).SetFontSize(11).SetColor(TextHi);
            pathLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            pathLbl.style.flexShrink = 0;
            pathLbl.style.maxWidth = 180;
            pathLbl.style.overflow = Overflow.Hidden;
            pathLbl.style.textOverflow = TextOverflow.Ellipsis;
            pathLbl.style.whiteSpace = WhiteSpace.NoWrap;
            row.Add(pathLbl);

            // Component type
            var compLbl = new Label($"[{d.ComponentType}]").SetFontSize(10);
            compLbl.style.color = TextBlue;
            compLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            compLbl.style.marginLeft = 6;
            compLbl.style.flexShrink = 0;
            row.Add(compLbl);

            // Arrow separator
            var sep = new Label("→").SetFontSize(10).SetColor(TextLo);
            sep.style.marginLeft = 6; sep.style.marginRight = 6;
            sep.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(sep);

            // Field name
            var fieldLbl = new Label(d.FieldName).SetFontSize(11).SetColor(TextLo);
            fieldLbl.style.flexGrow = 1;
            fieldLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(fieldLbl);

            if (d.ObjId.HasValue)
            {
                var nav = new Button(() => NavigateTo(d))
                    { text = "Select", tooltip = "Select and ping in hierarchy" };
                nav.style.height = 16; nav.style.paddingLeft = 6; nav.style.paddingRight = 6;
                nav.style.paddingTop = 0; nav.style.paddingBottom = 0; nav.style.fontSize = 10;
                nav.style.backgroundColor = new Color(0.22f, 0.35f, 0.55f);
                nav.style.color = TextBlue;
                nav.style.borderTopWidth = 0; nav.style.borderBottomWidth = 0;
                nav.style.borderLeftWidth = 0; nav.style.borderRightWidth = 0;
                nav.style.borderTopLeftRadius = 3;     nav.style.borderTopRightRadius = 3;
                nav.style.borderBottomLeftRadius = 3;  nav.style.borderBottomRightRadius = 3;
                nav.style.flexShrink = 0;
                row.Add(nav);

                row.RegisterCallback<MouseEnterEvent>(_ =>
                    row.style.backgroundColor = new Color(0.25f, 0.30f, 0.42f));
                row.RegisterCallback<MouseLeaveEvent>(_ =>
                    row.style.backgroundColor = Color.clear);
            }
            return row;
        }

        // ─── Data loading ─────────────────────────────────────────────────────
        private void LoadPrefabDetails(ResultEntry entry)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Path);
            if (root == null) return;
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                CollectFromComponent(comp, _targetAsset, entry.Details, null);
            }
        }

        private void LoadSceneDetails(ResultEntry entry)
        {
            Scene open = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == entry.Path) { open = s; break; }
            }
            bool wasOpen = open.IsValid() && open.isLoaded;
            if (!wasOpen) open = EditorSceneManager.OpenScene(entry.Path, OpenSceneMode.Additive);
            try
            {
                foreach (var go in open.GetRootGameObjects())
                foreach (var comp in go.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;
                    CollectFromComponent(comp, _targetAsset, entry.Details, entry.Path);
                }
            }
            finally { if (!wasOpen) EditorSceneManager.CloseScene(open, true); }
        }

        private void LoadAssetDetails(ResultEntry entry)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(entry.Path);
            if (asset == null) return;
            CollectFromSO(asset, _targetAsset, entry.Details);
        }

        private static void CollectFromComponent(Component comp, Object target,
            List<DetailItem> results, string scenePath)
        {
            try
            {
                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != target) continue;
                    results.Add(new DetailItem
                    {
                        ObjectPath    = GetTransformPath(comp.transform),
                        ComponentType = comp.GetType().Name,
                        FieldName     = prop.displayName,
                        ObjId         = GlobalObjectId.GetGlobalObjectIdSlow(comp),
                        ScenePath     = scenePath,
                    });
                }
            }
            catch { /* skip problematic components */ }
        }

        private static void CollectFromSO(Object obj, Object target, List<DetailItem> results)
        {
            try
            {
                var so = new SerializedObject(obj);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != target) continue;
                    results.Add(new DetailItem
                    {
                        ObjectPath    = obj.name,
                        ComponentType = obj.GetType().Name,
                        FieldName     = prop.displayName,
                        ObjId         = GlobalObjectId.GetGlobalObjectIdSlow(obj),
                        ScenePath     = null,
                    });
                }
            }
            catch { /* skip problematic assets */ }
        }

        // ─── Navigation ───────────────────────────────────────────────────────
        private static void NavigateTo(DetailItem d)
        {
            if (!d.ObjId.HasValue) return;
            if (!string.IsNullOrEmpty(d.ScenePath))
            {
                bool isOpen = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).path == d.ScenePath) { isOpen = true; break; }
                if (!isOpen)
                    EditorSceneManager.OpenScene(d.ScenePath, OpenSceneMode.Additive);
            }
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(d.ObjId.Value);
            if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
        }

        // ─── Search ───────────────────────────────────────────────────────────
        private void FindReferences()
        {
            if (_targetAsset == null) return;
            _targetPath = AssetDatabase.GetAssetPath(_targetAsset);
            _targetGuid = AssetDatabase.AssetPathToGUID(_targetPath);
            _results.Clear();
            _hasSearched = true;

            var all = AssetDatabase.GetAllAssetPaths();
            try
            {
                for (int i = 0; i < all.Length; i++)
                {
                    string p = all[i];
                    if (p == _targetPath || !IsSerializedFile(p)) continue;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Finding References", Path.GetFileName(p), (float)i / all.Length)) break;
                    try { if (FileContainsGuid(p, _targetGuid)) _results.Add(new ResultEntry { Path = p, Kind = GetKind(p) }); }
                    catch { }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            RefreshView();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────
        private static bool FileContainsGuid(string path, string guid)
        {
            using (var reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    if (line.Contains(guid)) return true;
            }
            return false;
        }

        private static bool IsSerializedFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".unity" || ext == ".prefab" || ext == ".asset" ||
                   ext == ".controller" || ext == ".overridecontroller" ||
                   ext == ".anim" || ext == ".playable" || ext == ".mat" || ext == ".spriteatlas";
        }

        private static string GetKind(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".unity"  => "Scene",
                ".prefab" => "Prefab",
                ".asset"  => "Asset",
                _ => "Other"
            };
        }

        private static Color KindColor(string kind)
        {
            return kind switch
            {
                "Scene"  => CScene,
                "Prefab" => CPrefab,
                "Asset"  => CAsset,
                _ => COther,
            };
        }

        private static string GetTransformPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts);
        }
    }
}
