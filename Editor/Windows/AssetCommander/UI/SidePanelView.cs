using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public class SidePanelView : IDisposable
    {
        // Mode ids are declared here in Phase 1 so the chip row and its persistence exist;
        // the ICommanderMode implementations behind them land in Phase 5.
        private static readonly (string Id, string Label)[] Modes =
        {
            ("search", "Search"),
            ("broken-refs", "Broken Refs"),
            ("missing-scripts", "Missing Scripts"),
            ("cross-side", "Cross-Side"),
            ("unused", "Unused"),
            ("duplicates", "Duplicates"),
        };

        private const string HiddenClass = "ac-hidden";

        // Alt-clicking the expand arrow asks for the whole subtree. A pathological folder would
        // otherwise read the entire project from disk in one callback.
        private const int ExpandAllBudget = 20000;

        private readonly SidePanelState _state;
        private readonly FolderSideSource _folderSource = new FolderSideSource();
        private readonly CommanderSelection _selection;

        private readonly VisualElement _panel;
        private readonly ObjectField _slot;
        private readonly VisualElement _breadcrumb;
        private readonly VisualElement _modeChips;
        private readonly VisualElement _viewChips;
        private readonly ToolbarSearchField _search;
        private readonly TreeView _tree;
        private readonly MultiColumnListView _list;
        private readonly VisualElement _placeholder;
        private readonly Label _placeholderText;
        private readonly Label _status;

        private readonly Dictionary<string, Button> _modeButtons = new Dictionary<string, Button>();
        private readonly Dictionary<SideViewMode, Button> _viewButtons = new Dictionary<SideViewMode, Button>();

        private List<ICommanderItem> _flat = new List<ICommanderItem>();

        private string _sortColumn;
        private bool _sortAscending = true;
        private bool _suppressSelection;

        public event Action Activated;

        public SidePanelView(SidePanelState state, VisualElement host, VisualTreeAsset template)
        {
            _state = state;
            _selection = new CommanderSelection(state.Id);

            _panel = template.Instantiate();
            _panel.style.flexGrow = 1;
            host.Add(_panel);

            _slot = _panel.Q<ObjectField>("slot");
            _breadcrumb = _panel.Q<VisualElement>("breadcrumb");
            _modeChips = _panel.Q<VisualElement>("modes");
            _viewChips = _panel.Q<VisualElement>("view-toggle");
            _search = _panel.Q<ToolbarSearchField>("search");
            _tree = _panel.Q<TreeView>("tree");
            _list = _panel.Q<MultiColumnListView>("list");
            _placeholder = _panel.Q<VisualElement>("placeholder");
            _placeholderText = _panel.Q<Label>("placeholder-text");
            _status = _panel.Q<Label>("status");
            _panel.Q<Label>("side-label").text = state.Id.ToString();

            _slot.objectType = typeof(Object);
            _slot.allowSceneObjects = false;
            _slot.RegisterValueChangedCallback(OnSlotChanged);
            _search.RegisterValueChangedCallback(evt => _state.Filter = evt.newValue);

            BuildModeChips();
            BuildViewChips();
            SetupTree();
            SetupList();

            _panel.RegisterCallback<PointerDownEvent>(_ => Activated?.Invoke(), TrickleDown.TrickleDown);

            _state.OnRootChanged.AddListener(SyncRoot);
            _state.OnViewChanged.AddListener(SyncChips);
            _state.OnFilterChanged.AddListener(OnFilterChanged);
            ProjectIndex.OnIndexChanged.AddListener(OnProjectChanged);
            EditorApplication.projectChanged += OnProjectChanged;

            SyncRoot();
            SyncChips();
        }

        public VisualElement Root => _panel;

        public CommanderSelection Selection => _selection;

        public void SetActive(bool active) =>
            _panel.Q<VisualElement>("side-panel").EnableInClassList("ac-panel--active", active);

        public void Focus()
        {
            if (_state.ViewMode == SideViewMode.Tree) _tree.Focus();
            else _list.Focus();
        }

        public void Dispose()
        {
            _state.OnRootChanged.RemoveListener(SyncRoot);
            _state.OnViewChanged.RemoveListener(SyncChips);
            _state.OnFilterChanged.RemoveListener(OnFilterChanged);
            ProjectIndex.OnIndexChanged.RemoveListener(OnProjectChanged);
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void BuildModeChips()
        {
            foreach (var (id, label) in Modes)
            {
                var chip = new Button(() => _state.ModeId = id) { text = label };
                chip.AddToClassList("ac-chip");
                _modeChips.Add(chip);
                _modeButtons[id] = chip;
            }
        }

        private void BuildViewChips()
        {
            foreach (SideViewMode mode in Enum.GetValues(typeof(SideViewMode)))
            {
                var captured = mode;
                var chip = new Button(() => _state.ViewMode = captured) { text = mode.ToString() };
                chip.AddToClassList("ac-chip");
                _viewChips.Add(chip);
                _viewButtons[mode] = chip;
            }
        }

        private void SetupTree()
        {
            _tree.fixedItemHeight = 20f;
            _tree.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _tree.selectionType = SelectionType.Multiple;
            _tree.horizontalScrollingEnabled = false;

            _tree.makeItem = () => new CommanderItemRow();
            _tree.bindItem = (element, index) =>
                ((CommanderItemRow)element).Bind(_tree.GetItemDataForIndex<ICommanderItem>(index));
            _tree.unbindItem = (element, _) => ((CommanderItemRow)element).Unbind();

            _tree.itemExpandedChanged += OnItemExpandedChanged;
            _tree.selectionChanged += OnSelectionChanged;
            _tree.itemsChosen += OnItemsChosen;
        }

        private void SetupList()
        {
            _list.fixedItemHeight = 20f;
            _list.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _list.selectionType = SelectionType.Multiple;
            _list.sortingMode = ColumnSortingMode.Custom;
            _list.itemsSource = _flat;

            // Columns are built here rather than in the UXML: every cell factory and width is
            // C# anyway, and a column list in markup only splits the definition in two.
            var name = new Column
            {
                name = "name",
                title = "Name",
                width = 240f,
                minWidth = 120f,
                stretchable = true,
                sortable = true,
                makeCell = () => new CommanderItemRow(false),
                unbindCell = (element, _) => ((CommanderItemRow)element).Unbind(),
            };
            name.bindCell = (element, index) => ((CommanderItemRow)element).Bind(ItemAt(index));
            _list.columns.Add(name);

            AddTextColumn("type", "Type", 90f, 60f, item => item.SubLabel);
            AddTextColumn("size", "Size", 80f, 60f, item => (item as AssetItem)?.SizeLabel ?? "");
            AddTextColumn("modified", "Modified", 120f, 90f, item => (item as AssetItem)?.ModifiedLabel ?? "");

            _list.columnSortingChanged += OnColumnSortingChanged;
            _list.selectionChanged += OnSelectionChanged;
            _list.itemsChosen += OnItemsChosen;
        }

        private void AddTextColumn(string columnName, string title, float width, float minWidth,
            Func<ICommanderItem, string> text)
        {
            var column = new Column
            {
                name = columnName,
                title = title,
                width = width,
                minWidth = minWidth,
                sortable = true,
                makeCell = () =>
                {
                    var label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("ac-cell");
                    return label;
                },
                unbindCell = (element, _) => ((Label)element).text = "",
            };

            column.bindCell = (element, index) =>
            {
                var item = ItemAt(index);
                ((Label)element).text = item == null ? "" : text(item);
            };

            _list.columns.Add(column);
        }

        private ICommanderItem ItemAt(int index) =>
            index >= 0 && index < _flat.Count ? _flat[index] : null;

        private void OnSlotChanged(ChangeEvent<Object> evt)
        {
            if (SidePanelState.IsAcceptableRoot(evt.newValue))
            {
                _state.SetRoot(AssetDatabase.GetAssetPath(evt.newValue));
                return;
            }

            // Reject files and non-scene assets without firing another change event.
            _slot.SetValueWithoutNotify(_state.RootAsset);
            _status.text = evt.newValue == null
                ? "Cleared — drop a folder or a scene."
                : $"'{evt.newValue.name}' is not a folder or a scene.";
        }

        private void SyncRoot()
        {
            _slot.SetValueWithoutNotify(_state.RootAsset);
            BuildBreadcrumb();

            _folderSource.SetRoot(_state.RootPath);
            _folderSource.Filter = _state.Filter;
            _selection.Clear();
            RefreshContent(false);
        }

        private void SyncChips()
        {
            foreach (var pair in _modeButtons)
                pair.Value.EnableInClassList("ac-chip--selected", pair.Key == _state.ModeId);

            foreach (var pair in _viewButtons)
                pair.Value.EnableInClassList("ac-chip--selected", pair.Key == _state.ViewMode);

            ShowActiveView();

            // Tree and list keep separate selection state, so the one being switched to has to
            // be told what is selected.
            if (_selection.Count > 0) RestoreSelection(CollectSelectedIds());
        }

        private void OnFilterChanged()
        {
            _folderSource.Filter = _state.Filter;
            RefreshContent(true);
        }

        // Both the index signal and projectChanged land here; the disk cache is dropped so the
        // next build sees the new files, and expansion/selection survive by id.
        private void OnProjectChanged()
        {
            if (_state.Kind != SideKind.Folder) return;

            RefreshContent(true, true);
        }

        // The expansion/selection snapshot has to be taken before anything is invalidated —
        // it is read out of the source's own item tree, which the rebuild replaces.
        private void RefreshContent(bool preserveState, bool reread = false)
        {
            if (_state.Kind != SideKind.Folder)
            {
                _flat = new List<ICommanderItem>();
                _list.itemsSource = _flat;
                _placeholderText.text = _state.Kind == SideKind.Scene
                    ? $"{_state.RootPath}\nThe live scene hierarchy lands in Phase 4."
                    : "Drop a folder or a scene here.";
                ShowActiveView();
                UpdateStatus();
                return;
            }

            var expanded = preserveState ? CollectExpandedIds() : null;
            var selected = preserveState ? CollectSelectedIds() : null;

            if (reread) _folderSource.Invalidate();

            _tree.SetRootItems(_folderSource.BuildRoot());
            _tree.Rebuild();
            if (expanded != null && expanded.Count > 0) RestoreExpansion(_folderSource.RootItems, expanded);

            _flat = _folderSource.BuildFlat();
            _list.itemsSource = _flat;
            ApplySort();
            _list.Rebuild();

            if (selected != null && selected.Count > 0) RestoreSelection(selected);

            ShowActiveView();
            UpdateStatus();
        }

        private void ShowActiveView()
        {
            bool folder = _state.Kind == SideKind.Folder;
            bool tree = _state.ViewMode == SideViewMode.Tree;

            _tree.EnableInClassList(HiddenClass, !folder || !tree);
            _list.EnableInClassList(HiddenClass, !folder || tree);
            _placeholder.EnableInClassList(HiddenClass, folder);
        }

        private void UpdateStatus()
        {
            if (_state.Kind != SideKind.Folder)
            {
                _status.text = $"{_state.Kind} · {_state.RootPath}";
                return;
            }

            var count = _flat.Count;
            _status.text = _selection.Count > 0
                ? $"{_state.RootPath} · {count} items · {_selection.Count} selected"
                : $"{_state.RootPath} · {count} items";
        }

        private void OnItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            if (!args.isExpanded) return;

            int loaded = EnsureLoaded(args.id) ? 1 : 0;
            if (args.isAppliedToAllChildren) LoadDescendants(args.id, ref loaded);

            if (loaded > 0) _tree.RefreshItems();
        }

        private bool EnsureLoaded(int id)
        {
            if (!_folderSource.TryLoadChildren(id, out var placeholderId, out var children)) return false;

            _tree.TryRemoveItem(placeholderId, false);
            for (int i = 0; i < children.Count; i++)
                _tree.AddItem(children[i], id, -1, i == children.Count - 1);

            if (children.Count == 0) _tree.Rebuild();
            return true;
        }

        private void LoadDescendants(int id, ref int loaded)
        {
            if (loaded > ExpandAllBudget) return;
            if (!_folderSource.TryGetLoadedChildren(id, out var children)) return;

            foreach (var child in children)
            {
                if (child.data.Kind != CommanderItemKind.Folder) continue;
                if (EnsureLoaded(child.id)) loaded++;

                LoadDescendants(child.id, ref loaded);
                if (loaded > ExpandAllBudget) return;
            }
        }

        private HashSet<int> CollectExpandedIds()
        {
            var expanded = new HashSet<int>();
            CollectExpandedIds(_folderSource.RootItems, expanded);
            return expanded;
        }

        private void CollectExpandedIds(IReadOnlyList<TreeViewItemData<ICommanderItem>> level, HashSet<int> into)
        {
            foreach (var node in level)
            {
                if (node.data.Kind != CommanderItemKind.Folder) continue;
                if (!_tree.IsExpanded(node.id)) continue;

                into.Add(node.id);
                if (_folderSource.TryGetLoadedChildren(node.id, out var children))
                    CollectExpandedIds(children, into);
            }
        }

        // Parents have to be filled and expanded before their children exist in the tree, so
        // this walks the source top-down rather than replaying a flat id list.
        private void RestoreExpansion(IReadOnlyList<TreeViewItemData<ICommanderItem>> level, HashSet<int> expanded)
        {
            foreach (var node in level)
            {
                if (!expanded.Contains(node.id)) continue;

                EnsureLoaded(node.id);
                _tree.ExpandItem(node.id, false, false);

                if (_folderSource.TryGetLoadedChildren(node.id, out var children))
                    RestoreExpansion(children, expanded);
            }
        }

        private List<int> CollectSelectedIds()
        {
            var ids = new List<int>(_selection.Count);
            foreach (var item in _selection.Items) ids.Add(item.Id);
            return ids;
        }

        private void RestoreSelection(List<int> ids)
        {
            _suppressSelection = true;

            _tree.SetSelectionByIdWithoutNotify(ids);

            var indices = new List<int>(ids.Count);
            for (int i = 0; i < _flat.Count; i++)
                if (ids.Contains(_flat[i].Id))
                    indices.Add(i);

            _list.SetSelectionWithoutNotify(indices);
            _suppressSelection = false;
        }

        private void OnSelectionChanged(IEnumerable<object> items)
        {
            if (_suppressSelection) return;

            Activated?.Invoke();
            _selection.Set(items);
            UpdateStatus();

            if (_selection.Count == 1) Ping(_selection.First);
        }

        private void OnItemsChosen(IEnumerable<object> items)
        {
            foreach (var entry in items)
            {
                if (!(entry is ICommanderItem item)) continue;

                if (item.Kind == CommanderItemKind.Folder)
                {
                    _state.SetRoot(item.AssetPath);
                    return;
                }

                var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                if (asset != null) AssetDatabase.OpenAsset(asset);
                return;
            }
        }

        private static void Ping(ICommanderItem item)
        {
            if (item?.AssetPath == null) return;

            var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
            if (asset != null) EditorGUIUtility.PingObject(asset);
        }

        private void OnColumnSortingChanged()
        {
            ApplySort();
            _list.RefreshItems();
        }

        private void ApplySort()
        {
            _sortColumn = null;

            foreach (var description in _list.sortedColumns)
            {
                _sortColumn = description.columnName;
                _sortAscending = description.direction == SortDirection.Ascending;
                break;
            }

            if (_sortColumn == null) return;

            _flat.Sort(CompareItems);
        }

        // Folders stay pinned above files whatever the column — the commander convention, and
        // it keeps double-click-to-descend targets where the eye expects them.
        private int CompareItems(ICommanderItem a, ICommanderItem b)
        {
            bool folderA = a.Kind == CommanderItemKind.Folder;
            if (folderA != (b.Kind == CommanderItemKind.Folder)) return folderA ? -1 : 1;

            int result;
            switch (_sortColumn)
            {
                case "type":
                    result = string.Compare(a.SubLabel, b.SubLabel, StringComparison.OrdinalIgnoreCase);
                    break;
                case "size":
                    result = a.Size.CompareTo(b.Size);
                    break;
                case "modified":
                    result = a.ModifiedTicks.CompareTo(b.ModifiedTicks);
                    break;
                default:
                    result = 0;
                    break;
            }

            if (result == 0) result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

            return _sortAscending ? result : -result;
        }

        private void BuildBreadcrumb()
        {
            _breadcrumb.Clear();

            var segments = _state.RootPath.Split('/');
            var prefix = "";

            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0)
                {
                    var sep = new Label("›");
                    sep.AddToClassList("ac-crumb-sep");
                    _breadcrumb.Add(sep);
                }

                prefix = i == 0 ? segments[i] : prefix + "/" + segments[i];

                var crumb = new Label(segments[i]);
                crumb.AddToClassList("ac-crumb");

                bool last = i == segments.Length - 1;
                if (last) crumb.AddToClassList("ac-crumb--last");

                // "Packages" alone is not a folder the AssetDatabase knows, so it stays inert.
                if (!last && AssetDatabase.IsValidFolder(prefix))
                {
                    var target = prefix;
                    crumb.AddToClassList("ac-crumb--link");
                    crumb.RegisterCallback<PointerDownEvent>(_ => _state.SetRoot(target));
                }

                _breadcrumb.Add(crumb);
            }
        }
    }
}
