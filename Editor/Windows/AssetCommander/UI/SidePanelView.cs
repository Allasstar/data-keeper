using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public class SidePanelView : IDisposable
    {
        private const string HiddenClass = "ac-hidden";

        // Alt-clicking the expand arrow asks for the whole subtree. A pathological folder would
        // otherwise read the entire project from disk in one callback.
        private const int ExpandAllBudget = 20000;

        // Only this window's own drags are answered — an asset dragged in from the Project window
        // has no source panel to be moved out of, so it is refused rather than half-handled.
        private const string DragKey = "DataKeeper.AssetCommander";

        // Far enough that a click that wobbles is still a click.
        private const float DragThreshold = 6f;

        // Id sentinel meaning "the cursor is over the panel, not over a row". Item ids are
        // AssetDatabase-derived and never int.MinValue.
        private const int NoDropTarget = int.MinValue;

        private readonly SidePanelState _state;
        private readonly FolderSideSource _folderSource = new FolderSideSource();
        private readonly SceneSideSource _sceneSource = new SceneSideSource();
        private readonly ModeResultSource _modeSource = new ModeResultSource();
        private readonly SceneSlot _sceneSlot = new SceneSlot();
        private readonly CommanderSelection _selection;

        private readonly VisualElement _panel;
        private readonly VisualElement _panelRoot;
        private readonly ObjectField _slot;
        private readonly VisualElement _breadcrumb;
        private readonly VisualElement _modeChips;
        private readonly VisualElement _viewChips;
        private readonly Button _componentsChip;
        private readonly Button _reverseChip;
        private readonly Label _modeNotice;
        private readonly ToolbarSearchField _search;
        private readonly TreeView _tree;
        private readonly MultiColumnListView _list;
        private readonly VisualElement _placeholder;
        private readonly Label _placeholderText;
        private readonly VisualElement _sceneNotice;
        private readonly Label _sceneNoticeText;
        private readonly Button _sceneOpenButton;
        private readonly Label _status;

        private readonly Dictionary<string, Button> _modeButtons = new Dictionary<string, Button>();
        private readonly Dictionary<SideViewMode, Button> _viewButtons = new Dictionary<SideViewMode, Button>();

        private readonly Action _sceneEventHandler;

        private ISideSource _source;
        private SidePanelView _peer;
        private List<ICommanderItem> _flat = new List<ICommanderItem>();

        private ModeResult _modeResult;
        private string _modeUnavailable;
        private long _modeMilliseconds;

        private string _sortColumn;
        private bool _sortAscending = true;
        private bool _suppressSelection;
        private bool _refreshQueued;

        private Vector2 _dragOrigin;
        private bool _dragArmed;

        private CommanderItemRow _dropRow;
        private ICommanderCommand _dropCommand;
        private int _dropOverId = NoDropTarget;
        private bool _dropCopy;

        public event Action Activated;

        // The window appends the command entries; the side appends the ones that are about the
        // clicked item rather than about the selection.
        public event Action<DropdownMenu, ICommanderItem> ContextMenuRequested;

        public SidePanelView(SidePanelState state, VisualElement host, VisualTreeAsset template)
        {
            _state = state;
            _selection = new CommanderSelection(state.Id);

            _panel = template.Instantiate();
            _panel.style.flexGrow = 1;
            host.Add(_panel);

            _panelRoot = _panel.Q<VisualElement>("side-panel");

            _slot = _panel.Q<ObjectField>("slot");
            _breadcrumb = _panel.Q<VisualElement>("breadcrumb");
            _modeChips = _panel.Q<VisualElement>("modes");
            _modeNotice = _panel.Q<Label>("mode-notice");
            _viewChips = _panel.Q<VisualElement>("view-toggle");
            _search = _panel.Q<ToolbarSearchField>("search");
            _tree = _panel.Q<TreeView>("tree");
            _list = _panel.Q<MultiColumnListView>("list");
            _placeholder = _panel.Q<VisualElement>("placeholder");
            _placeholderText = _panel.Q<Label>("placeholder-text");
            _sceneNotice = _panel.Q<VisualElement>("scene-notice");
            _sceneNoticeText = _panel.Q<Label>("scene-notice-text");
            _sceneOpenButton = _panel.Q<Button>("scene-open");
            _status = _panel.Q<Label>("status");
            _panel.Q<Label>("side-label").text = state.Id.ToString();

            _slot.objectType = typeof(Object);
            _slot.allowSceneObjects = false;
            _slot.RegisterValueChangedCallback(OnSlotChanged);
            _search.RegisterValueChangedCallback(evt => _state.Filter = evt.newValue);
            _sceneOpenButton.clicked += OpenPreviewedScene;

            BuildModeChips();
            _reverseChip = BuildReverseChip();
            BuildViewChips();
            _componentsChip = BuildComponentsChip();
            SetupTree();
            SetupList();

            _panel.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _panel.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _panel.RegisterCallback<PointerUpEvent>(_ => _dragArmed = false, TrickleDown.TrickleDown);
            _panel.RegisterCallback<KeyDownEvent>(OnKeyDown);

            _panel.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            _panel.RegisterCallback<DragPerformEvent>(OnDragPerform);
            _panel.RegisterCallback<DragLeaveEvent>(_ => ClearDropFeedback());
            _panel.RegisterCallback<DragExitedEvent>(_ => ClearDropFeedback());

            _sceneEventHandler = QueueSceneRefresh;

            _state.OnRootChanged.AddListener(SyncRoot);
            _state.OnViewChanged.AddListener(SyncChips);
            _state.OnModeChanged.AddListener(OnModeSwitched);
            _state.OnFilterChanged.AddListener(OnFilterChanged);
            _state.OnComponentsChanged.AddListener(OnComponentsToggled);
            ProjectIndex.OnIndexChanged.AddListener(OnIndexChanged);
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.hierarchyChanged += QueueSceneRefresh;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneEvent.SubscribeToEvents(_sceneEventHandler);

            SyncRoot();
        }

        public VisualElement Root => _panel;

        public CommanderSelection Selection => _selection;

        // Phase 6's mutating commands ask for this before touching a scene item: a preview-backed
        // side has to become a real open scene first.
        public SceneSlot SceneSlot => _sceneSlot;

        // What a command sees of this panel. Promotion and refresh are handed over as delegates
        // because they are the only two things a command is allowed to do to the view.
        // The selection is copied rather than handed over: a command holds its context across the
        // refresh that follows it, and the live list is emptied by that refresh.
        public CommanderSide Side => new CommanderSide(_state.Id, _state.Kind, _state.RootPath,
            new List<ICommanderItem>(_selection.Items), _sceneSlot.Scene,
            _state.Kind == SideKind.Scene && _sceneSlot.IsPreview,
            () => _sceneSlot.PromoteToOpen(true), RefreshAfterCommand);

        // A command destroys or moves the very objects the rows are bound to, so the selection
        // goes and the source is re-read rather than patched.
        public void RefreshAfterCommand()
        {
            if (_state.Kind == SideKind.Scene) _sceneSlot.Rebind();

            _selection.Clear();
            RefreshContent(false, true);
        }

        // What the modes are told about this side. The scene has to travel with the state
        // because a preview scene exists nowhere the other side could look it up.
        public SideContext Context => new SideContext(_state, _sceneSlot.Scene);

        // Cross-Side is the one mode that reads the other panel, so the two views know about
        // each other — and each has to re-evaluate when the other's root moves.
        public void SetPeer(SidePanelView peer)
        {
            if (_peer != null) _peer._state.OnRootChanged.RemoveListener(OnPeerRootChanged);

            _peer = peer;
            if (_peer != null) _peer._state.OnRootChanged.AddListener(OnPeerRootChanged);
        }

        public void SetActive(bool active) =>
            _panelRoot.EnableInClassList("ac-panel--active", active);

        public void Focus()
        {
            if (_state.ViewMode == SideViewMode.Tree) _tree.Focus();
            else _list.Focus();
        }

        public void Dispose()
        {
            _state.OnRootChanged.RemoveListener(SyncRoot);
            _state.OnViewChanged.RemoveListener(SyncChips);
            _state.OnModeChanged.RemoveListener(OnModeSwitched);
            _state.OnFilterChanged.RemoveListener(OnFilterChanged);
            _state.OnComponentsChanged.RemoveListener(OnComponentsToggled);
            ProjectIndex.OnIndexChanged.RemoveListener(OnIndexChanged);
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.hierarchyChanged -= QueueSceneRefresh;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneEvent.UnsubscribeFromEvents(_sceneEventHandler);

            if (_peer != null) _peer._state.OnRootChanged.RemoveListener(OnPeerRootChanged);
            _peer = null;

            // The window must not leave a preview scene loaded behind it.
            _sceneSlot.Dispose();
        }

        // The tooltip is set in SyncChips rather than here: what a mode is depends on the side
        // it is asked about, and an unavailable one has to say so.
        private void BuildModeChips()
        {
            foreach (var mode in CommanderModes.All)
            {
                var captured = mode;
                var chip = new Button(() => SelectMode(captured)) { text = mode.DisplayName };

                chip.AddToClassList("ac-chip");
                _modeChips.Add(chip);
                _modeButtons[mode.Id] = chip;
            }
        }

        // A mode chip that does not apply to this side stays enabled so that hovering it still
        // explains itself, so the refusal lives here instead of in SetEnabled.
        private void SelectMode(ICommanderMode mode)
        {
            if (!mode.Supports(_state.Kind)) return;

            _state.ModeId = mode.Id;
        }

        // Part of Cross-Side, not a seventh mode: it is inserted directly after that chip rather
        // than appended to the row, so the control and the mode it belongs to read as one thing.
        private Button BuildReverseChip()
        {
            var chip = new Button(ToggleReverse)
            {
                text = "Reversed",
                tooltip = "Ask what the other side references here, instead of what this side "
                          + "references there.",
            };

            chip.AddToClassList("ac-chip");
            chip.AddToClassList("ac-chip--toggle");

            var crossSide = _modeButtons[CommanderModes.CrossSideId];
            _modeChips.Insert(_modeChips.IndexOf(crossSide) + 1, chip);

            return chip;
        }

        private void ToggleReverse()
        {
            if (!CrossSideReferencesMode.SupportsReverse(_state.Kind)) return;

            _state.CrossSideReverse = !_state.CrossSideReverse;
        }

        // What the mode does, and — when it cannot answer for this side — why the chip is dim.
        private string ModeTooltip(ICommanderMode mode, bool supported) =>
            supported
                ? mode.Tooltip
                : mode.Tooltip + UnavailableSuffix(
                    $"it does not apply to a {Describe(_state.Kind)} side");

        private static string UnavailableSuffix(string reason) =>
            "\n\nUnavailable here: " + reason + ".";

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

        private Button BuildComponentsChip()
        {
            var chip = new Button(() => _state.ShowComponents = !_state.ShowComponents)
            {
                text = "Components",
            };

            chip.AddToClassList("ac-chip");
            _viewChips.Add(chip);
            return chip;
        }

        // ── Drag and drop ────────────────────────────────────

        // A drop is not a second implementation of anything: it resolves to one of the same
        // ICommanderCommands the buttons run, is planned by the same Plan call, and is confirmed
        // in the same dialog. All the gesture contributes is where the things land.
        private static readonly ICommanderCommand DropMove = CommanderCommands.Get("move");
        private static readonly ICommanderCommand DropCopy = CommanderCommands.Get("copy");
        private static readonly ICommanderCommand DropPrefab = CommanderCommands.Get("prefab");

        private void OnPointerDown(PointerDownEvent evt)
        {
            Activated?.Invoke();

            // Armed, not started: the collection view updates its selection on this same event,
            // so what is being dragged is only known once the pointer has actually moved.
            _dragArmed = evt.button == 0 && RowUnder(evt.target as VisualElement) != null;
            _dragOrigin = evt.position;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragArmed) return;

            if ((evt.pressedButtons & 1) == 0)
            {
                _dragArmed = false;
                return;
            }

            if ((((Vector2)evt.position) - _dragOrigin).sqrMagnitude < DragThreshold * DragThreshold)
                return;

            _dragArmed = false;
            StartDrag();
        }

        private void StartDrag()
        {
            if (_selection.Count == 0) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(DragKey, _state.Id);

            var objects = new List<Object>(_selection.Count);
            var paths = new List<string>(_selection.Count);

            foreach (var item in _selection.Items)
            {
                if (!string.IsNullOrEmpty(item.AssetPath))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                    if (asset != null) objects.Add(asset);
                    paths.Add(item.AssetPath);
                    continue;
                }

                // A preview-scene GameObject is in no hierarchy and nothing outside this window
                // could resolve it, so it travels as generic data only — never as an object
                // reference the rest of the editor might accept a drop of.
                if (item is GameObjectItem gameObjectItem && gameObjectItem.GameObject != null
                    && !_sceneSlot.IsPreview)
                    objects.Add(gameObjectItem.GameObject);
            }

            DragAndDrop.objectReferences = objects.ToArray();
            DragAndDrop.paths = paths.ToArray();
            DragAndDrop.StartDrag(_selection.Describe());
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            var row = RowUnder(evt.target as VisualElement);
            var command = ResolveDrop(row, evt.modifiers, out _);

            if (command == null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                ClearDropFeedback();
                return;
            }

            DragAndDrop.visualMode = command == DropCopy
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Move;

            ShowDropFeedback(row);
            evt.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            var row = RowUnder(evt.target as VisualElement);
            var command = ResolveDrop(row, evt.modifiers, out var context);

            ClearDropFeedback();
            if (command == null) return;

            DragAndDrop.AcceptDrag();
            evt.StopPropagation();

            CommandRunner.Run(command, context);
        }

        // DragUpdated arrives for every mouse move, so which command answers is cached against the
        // row under the cursor and the modifier state, and only re-asked when one of those moves.
        private ICommanderCommand ResolveDrop(CommanderItemRow row, EventModifiers modifiers,
            out CommanderContext context)
        {
            context = default;

            if (_peer == null) return null;
            if (!(DragAndDrop.GetGenericData(DragKey) is SideId source)) return null;

            // Within one panel there is no other side to transfer to, and a command asked to move
            // a selection onto itself is a question with no useful answer.
            if (source == _state.Id) return null;

            var over = row?.Item;
            int overId = over?.Id ?? NoDropTarget;
            bool copy = (modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0;

            context = new CommanderContext(_peer.Side, DropTarget(over));

            if (overId == _dropOverId && copy == _dropCopy) return _dropCommand;

            var transfer = copy ? DropCopy : DropMove;

            _dropCommand = transfer.CanExecute(context)
                ? transfer
                : DropPrefab.CanExecute(context) ? DropPrefab : null;
            _dropOverId = overId;
            _dropCopy = copy;

            return _dropCommand;
        }

        // This side as a destination. A folder row under the cursor becomes the root the transfer
        // plans against; a GameObject row becomes the target side's whole "selection", which is
        // the shape PrefabCommand already reads to decide what an instance is parented to.
        private CommanderSide DropTarget(ICommanderItem over)
        {
            if (_state.Kind == SideKind.Folder)
            {
                var root = over != null && over.Kind == CommanderItemKind.Folder
                    ? over.AssetPath
                    : _state.RootPath;

                return new CommanderSide(_state.Id, SideKind.Folder, root,
                    Array.Empty<ICommanderItem>(), default, false, null, RefreshAfterCommand);
            }

            var selection = over is GameObjectItem
                ? new List<ICommanderItem> { over }
                : (IReadOnlyList<ICommanderItem>)Array.Empty<ICommanderItem>();

            return new CommanderSide(_state.Id, _state.Kind, _state.RootPath, selection,
                _sceneSlot.Scene, _state.Kind == SideKind.Scene && _sceneSlot.IsPreview,
                () => _sceneSlot.PromoteToOpen(true), RefreshAfterCommand);
        }

        private void ShowDropFeedback(CommanderItemRow row)
        {
            if (_dropRow != row)
            {
                _dropRow?.SetDropTarget(false);
                _dropRow = row;
                _dropRow?.SetDropTarget(true);
            }

            // Over the background rather than a row means "this side's root", and the panel
            // border is what says so.
            _panelRoot.EnableInClassList("ac-panel--drop", row == null);
        }

        private void ClearDropFeedback()
        {
            _dropRow?.SetDropTarget(false);
            _dropRow = null;
            _panelRoot.EnableInClassList("ac-panel--drop", false);

            _dropOverId = NoDropTarget;
            _dropCommand = null;
        }

        // Navigation only — no command is bound to a key in this window. Arrows, Home/End,
        // PageUp/PageDown, Enter and the tree's Left/Right are the collection views' own and are
        // left to them; what is added here is the movement a two-panel browser needs and a list
        // cannot know about — leaving a folder, reaching the search box, dropping a selection.
        // Registered on the bubble phase so the view under the cursor always answers first.
        private void OnKeyDown(KeyDownEvent evt)
        {
            const EventModifiers Ctrl = EventModifiers.Control | EventModifiers.Command;

            bool ctrl = (evt.modifiers & Ctrl) != 0;
            bool inText = evt.target is VisualElement element
                          && element.GetFirstAncestorOfType<TextField>() != null;

            // These two work from inside the search box as well: one reaches it, the other is
            // how the user gets back out. Anything else typed in there is text.
            if (ctrl && evt.keyCode == KeyCode.F)
            {
                FocusSearch();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                if (ClearSearchOrSelection()) evt.StopPropagation();
                return;
            }

            if (inText) return;

            if (ctrl && evt.keyCode == KeyCode.A)
            {
                SelectAllVisible();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Backspace && GoUp()) evt.StopPropagation();
        }

        private void FocusSearch()
        {
            Activated?.Invoke();

            var input = _search.Q<TextField>();
            if (input != null) input.Focus();
            else _search.Focus();
        }

        // Escape peels one layer at a time: the filter first, because a hidden filter is the
        // more confusing of the two states to be left in, then the selection.
        private bool ClearSearchOrSelection()
        {
            if (!string.IsNullOrEmpty(_state.Filter))
            {
                _search.value = "";
                Focus();
                return true;
            }

            if (_selection.Count == 0) return false;

            RestoreSelection(new List<int>());
            _selection.Clear();
            UpdateStatus();
            return true;
        }

        // "All" means what is on screen: the flat list in List view, and in Tree view every row
        // the user can actually see — a collapsed folder's children are not part of the
        // selection they think they are making.
        private void SelectAllVisible()
        {
            Activated?.Invoke();

            var items = new List<ICommanderItem>();

            if (_state.ViewMode == SideViewMode.List) items.AddRange(_flat);
            else if (_source != null) CollectVisibleItems(_source.RootItems, items);

            if (items.Count == 0) return;

            var ids = new List<int>(items.Count);
            foreach (var item in items) ids.Add(item.Id);

            RestoreSelection(ids);
            _selection.Set(items);
            UpdateStatus();
        }

        private void CollectVisibleItems(IReadOnlyList<TreeViewItemData<ICommanderItem>> level,
            List<ICommanderItem> into)
        {
            foreach (var node in level)
            {
                if (node.data.Kind == CommanderItemKind.Placeholder) continue;

                into.Add(node.data);

                if (!node.data.HasChildren || !_tree.IsExpanded(node.id)) continue;
                if (_source.TryGetLoadedChildren(node.id, out var children))
                    CollectVisibleItems(children, into);
            }
        }

        // Backspace is "leave this folder", the one movement the collection views have no idea
        // about. A scene side has no parent to go to — its root is the scene file — and neither
        // does "Packages", which is not a folder the AssetDatabase knows.
        private bool GoUp()
        {
            if (_state.Kind != SideKind.Folder) return false;

            int cut = _state.RootPath.LastIndexOf('/');
            if (cut <= 0) return false;

            var parent = _state.RootPath.Substring(0, cut);
            if (!AssetDatabase.IsValidFolder(parent)) return false;

            Activated?.Invoke();
            _state.SetRoot(parent);
            Focus();
            return true;
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
            _tree.AddManipulator(new ContextualMenuManipulator(PopulateContextMenu));
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
            _list.AddManipulator(new ContextualMenuManipulator(PopulateContextMenu));
        }

        // Right-clicking a row the user has not selected acts on that row, not on whatever was
        // selected before — the collection views do not select on right-click themselves.
        private void PopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            Activated?.Invoke();

            var item = ItemUnder(evt.triggerEvent?.target as VisualElement);
            if (item != null && !IsSelected(item)) SelectOnly(item);

            ContextMenuRequested?.Invoke(evt.menu, item);
            AppendItemActions(evt.menu, item);
        }

        private ICommanderItem ItemUnder(VisualElement target) => RowUnder(target)?.Item;

        // Rows are PickingMode.Ignore so the collection view keeps its own click handling, which
        // means the event target is the item container — the row is found by looking inside it.
        private CommanderItemRow RowUnder(VisualElement target)
        {
            for (var element = target; element != null; element = element.parent)
            {
                if (element == _tree || element == _list) return null;

                var row = element as CommanderItemRow ?? element.Q<CommanderItemRow>();
                if (row?.Item != null) return row;
            }

            return null;
        }

        private bool IsSelected(ICommanderItem item)
        {
            foreach (var selected in _selection.Items)
                if (selected.Id == item.Id)
                    return true;

            return false;
        }

        private void SelectOnly(ICommanderItem item)
        {
            RestoreSelection(new List<int> { item.Id });
            _selection.Set(new object[] { item });
            UpdateStatus();
        }

        private void AppendItemActions(DropdownMenu menu, ICommanderItem item)
        {
            var path = item?.AssetPath;
            if (string.IsNullOrEmpty(path)) return;

            var guid = item.Guid;

            menu.AppendSeparator();
            menu.AppendAction("Show in Explorer", _ => EditorUtility.RevealInFinder(path));
            menu.AppendAction("Copy Path", _ => EditorGUIUtility.systemCopyBuffer = path);

            menu.AppendAction("Copy GUID", _ => EditorGUIUtility.systemCopyBuffer = guid,
                string.IsNullOrEmpty(guid) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            menu.AppendAction("Find References",
                _ => AssetReferenceFinder.OpenWindow(AssetDatabase.LoadMainAssetAtPath(path)));
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

            BindBaseSource();
            _selection.Clear();
            RefreshContent(false, true);
            SyncChips();
        }

        // A scene the editor already has open is used live; a closed one is loaded into a preview
        // scene, which keeps browsing from disturbing the user's open-scene setup. Play Mode gets
        // neither: a preview scene cannot survive the reload, and the live one belongs to the player.
        private void BindBaseSource()
        {
            if (_state.Kind == SideKind.Scene)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) _sceneSlot.Release();
                else _sceneSlot.Bind(_state.RootPath);

                _sceneSource.ShowComponents = _state.ShowComponents;
                _sceneSource.SetScene(_sceneSlot.Scene);
                return;
            }

            _sceneSlot.Release();
            _folderSource.SetRoot(_state.RootPath);
        }

        private ISideSource BaseSource =>
            _state.Kind == SideKind.Scene ? (ISideSource)_sceneSource : _folderSource;

        // Every analysis mode is a query against the index, so this runs whole rather than
        // incrementally — and reports how long it took, because a mode that has stopped being a
        // lookup is the failure this window is built to avoid.
        private void EvaluateMode()
        {
            var mode = CommanderModes.Get(_state.ModeId);

            _modeResult = null;
            _modeUnavailable = null;
            _modeMilliseconds = 0;

            if (!mode.Supports(_state.Kind))
            {
                _modeUnavailable = $"{mode.DisplayName} does not apply to a {Describe(_state.Kind)} side.";
                _source = BaseSource;
                return;
            }

            if (mode.Id != CommanderModes.SearchId && !ProjectIndex.IsReady)
            {
                _modeUnavailable = "The project index is still building.";
                _source = BaseSource;
                return;
            }

            var watch = Stopwatch.StartNew();
            var result = mode.Evaluate(new ModeContext(Context, PeerContext, ProjectIndex.Query));
            watch.Stop();

            _modeMilliseconds = watch.ElapsedMilliseconds;

            if (result == null || result.IsPassThrough)
            {
                _source = BaseSource;
                return;
            }

            _modeResult = result;
            _modeSource.SetItems(result.Items);
            _source = _modeSource;
        }

        private SideContext PeerContext => _peer == null ? default : _peer.Context;

        private static string Describe(SideKind kind) => kind == SideKind.Scene ? "scene" : "folder";

        private void SyncChips()
        {
            foreach (var mode in CommanderModes.All)
            {
                var chip = _modeButtons[mode.Id];
                bool supported = mode.Supports(_state.Kind);

                chip.EnableInClassList("ac-chip--selected", mode.Id == _state.ModeId);

                // Dimmed rather than hidden: a chip that vanishes when a side changes kind makes
                // the row jump, and the user loses track of what the modes even are. Dimmed
                // rather than disabled: a disabled element is dropped from the pointer pick, so
                // its tooltip never appears — and an unavailable mode is exactly when the reason
                // is worth reading.
                chip.EnableInClassList("ac-chip--unavailable", !supported);
                chip.tooltip = ModeTooltip(mode, supported);
            }

            // Shown only where it can be acted on — Cross-Side selected, and a side kind that can
            // answer the reversed question. A dimmed toggle would be a control asking to be
            // clicked; the mode chips carry that treatment because they are how a side changes
            // mode, and this one is not.
            bool showReverse = _state.ModeId == CommanderModes.CrossSideId
                               && CrossSideReferencesMode.SupportsReverse(_state.Kind);

            _reverseChip.EnableInClassList(HiddenClass, !showReverse);
            _reverseChip.EnableInClassList("ac-chip--selected", _state.CrossSideReverse);

            foreach (var pair in _viewButtons)
                pair.Value.EnableInClassList("ac-chip--selected", pair.Key == _state.ViewMode);

            _componentsChip.EnableInClassList(HiddenClass, _state.Kind != SideKind.Scene);
            _componentsChip.EnableInClassList("ac-chip--selected", _state.ShowComponents);

            ShowActiveView();

            // Tree and list keep separate selection state, so the one being switched to has to
            // be told what is selected.
            if (_selection.Count > 0) RestoreSelection(CollectSelectedIds());
        }

        private void OnModeSwitched()
        {
            _selection.Clear();
            RefreshContent(false, true);
            SyncChips();
        }

        private void OnFilterChanged() => RefreshContent(true);

        private void ApplyFilter() => _source.Filter = SearchFilter.Parse(_state.Filter);

        private void OnComponentsToggled()
        {
            _sceneSource.ShowComponents = _state.ShowComponents;
            SyncChips();

            if (_state.Kind == SideKind.Scene) RefreshContent(true, true);
        }

        // The disk cache is dropped so the next build sees the new files, and expansion and
        // selection survive by id.
        private void OnProjectChanged()
        {
            if (_state.Kind != SideKind.Folder) return;

            RefreshContent(true, true);
        }

        // A rebuilt index changes what every analysis mode answers, whatever kind this side is.
        private void OnIndexChanged()
        {
            if (_state.Kind != SideKind.Folder && _state.ModeId == CommanderModes.SearchId) return;

            RefreshContent(true, true);
        }

        // hierarchyChanged fires for every rename, reparent and component edit, and the scene
        // open/close/save events pile on top — so the rebuild is deferred to at most one per
        // panel tick.
        private void QueueSceneRefresh()
        {
            if (_refreshQueued) return;
            if (_state.Kind != SideKind.Scene && !WatchesPeerScene) return;

            _refreshQueued = true;
            _panel.schedule.Execute(RunQueuedSceneRefresh);
        }

        // Cross-Side reads the other panel's live scene, so this side has to follow edits made
        // over there as well as its own.
        private bool WatchesPeerScene =>
            _state.ModeId == CommanderModes.CrossSideId && PeerContext.Kind == SideKind.Scene;

        private void RunQueuedSceneRefresh()
        {
            _refreshQueued = false;

            // A scene can be opened or closed behind the window's back, which flips this side
            // between live and preview.
            if (_state.Kind == SideKind.Scene) _sceneSlot.Rebind();
            else if (!WatchesPeerScene) return;

            RefreshContent(true, true);
        }

        private void OnPeerRootChanged()
        {
            if (_state.ModeId == CommanderModes.SearchId) return;

            RefreshContent(true, true);
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (_state.Kind != SideKind.Scene) return;
            if (change != PlayModeStateChange.ExitingEditMode && change != PlayModeStateChange.EnteredEditMode)
                return;

            if (change == PlayModeStateChange.ExitingEditMode) _sceneSlot.Release();
            else _sceneSlot.Bind(_state.RootPath);

            _selection.Clear();
            RefreshContent(false, true);
        }

        private void OpenPreviewedScene()
        {
            if (!_sceneSlot.PromoteToOpen(false)) return;

            _selection.Clear();
            RefreshContent(false, true);
        }

        // The expansion/selection snapshot has to be taken before anything is invalidated —
        // it is read out of the source's own item tree, which the rebuild replaces.
        private void RefreshContent(bool preserveState, bool reread = false)
        {
            var expanded = preserveState ? CollectExpandedIds() : null;
            var selected = preserveState ? CollectSelectedIds() : null;

            if (reread) Reread();

            ApplyFilter();

            if (!HasContent())
            {
                _flat = new List<ICommanderItem>();
                _list.itemsSource = _flat;
                ShowActiveView();
                UpdateStatus();
                return;
            }

            _tree.SetRootItems(_source.BuildRoot());
            _tree.Rebuild();
            if (expanded != null && expanded.Count > 0) RestoreExpansion(_source.RootItems, expanded);

            _flat = _source.BuildFlat();
            _list.itemsSource = _flat;
            ApplySort();
            _list.Rebuild();

            if (selected != null && selected.Count > 0) RestoreSelection(selected);

            ShowActiveView();
            UpdateStatus();
        }

        private void Reread()
        {
            if (_state.Kind == SideKind.Scene) _sceneSource.SetScene(_sceneSlot.Scene);
            else _folderSource.Invalidate();

            EvaluateMode();
        }

        // A mode result stands on its own — it is a list of findings, not a view of the side —
        // so it is shown even when the side itself has nothing to browse.
        private bool HasContent()
        {
            if (_source == _modeSource) return true;

            return _state.Kind == SideKind.Folder || (_state.Kind == SideKind.Scene && _sceneSlot.IsValid);
        }

        // Why this panel is blank, in the panel's own terms. An empty folder, a mode that found
        // nothing and a filter that matched nothing are three different facts, and answering all
        // three with one grey rectangle is what makes a browser feel broken.
        private string PlaceholderText()
        {
            if (_state.Kind == SideKind.Scene && !_sceneSlot.IsValid)
                return EditorApplication.isPlayingOrWillChangePlaymode
                    ? $"{_sceneSlot.SceneName}\nScene sides are unavailable in Play Mode."
                    : $"{_sceneSlot.SceneName}\nCould not load this scene.";

            if (_state.Kind == SideKind.None) return "Drop a folder or a scene here.";

            bool filtered = !string.IsNullOrEmpty(_state.Filter);

            if (_modeResult != null)
            {
                var mode = CommanderModes.Get(_state.ModeId).DisplayName;

                // The filter narrows a result set without re-running the mode, so "the mode found
                // nothing" and "the mode found things, none of them matching" are separate.
                return filtered
                    ? $"{mode} found {_modeSource.TotalCount:N0}, none matching '{_state.Filter}'."
                    : $"{mode} found nothing here.";
            }

            if (filtered) return $"Nothing here matches '{_state.Filter}'.";

            return _state.Kind == SideKind.Scene
                ? $"{_sceneSlot.SceneName} is empty."
                : $"{_state.RootPath} is empty.";
        }

        private void ShowActiveView()
        {
            // A side that has nothing to show and a side that cannot show anything both end up
            // at the placeholder, because to the user they are the same blank rectangle — the
            // difference is the sentence written in it.
            bool content = HasContent() && _flat.Count > 0;
            bool tree = _state.ViewMode == SideViewMode.Tree;

            _tree.EnableInClassList(HiddenClass, !content || !tree);
            _list.EnableInClassList(HiddenClass, !content || tree);
            _placeholder.EnableInClassList(HiddenClass, content);

            // Written here rather than at refresh time because this is the one place that knows
            // the panel is about to show the placeholder at all.
            if (!content) _placeholderText.text = PlaceholderText();

            bool preview = _state.Kind == SideKind.Scene && _sceneSlot.IsPreview;
            _sceneNotice.EnableInClassList(HiddenClass, !preview);
            if (preview)
                _sceneNoticeText.text = $"'{_sceneSlot.SceneName}' is loaded for preview — read-only.";

            var notice = _modeUnavailable ?? _modeResult?.Caveat;
            _modeNotice.EnableInClassList(HiddenClass, string.IsNullOrEmpty(notice));
            if (!string.IsNullOrEmpty(notice)) _modeNotice.text = notice;
        }

        private void UpdateStatus()
        {
            _status.text = _modeResult != null ? ModeStatus() : SideStatus();

            if (_selection.Count > 0) _status.text += $" · {_selection.Count} selected";
        }

        private string ModeStatus()
        {
            var mode = CommanderModes.Get(_state.ModeId);
            var text = $"{mode.DisplayName}: {_modeResult.Summary} · {_modeMilliseconds}ms";

            // The search box narrows a result set without re-running the mode, so the two counts
            // are different facts and both have to be on screen.
            int shown = _flat.Count;
            return shown == _modeSource.TotalCount ? text : $"{text} · {shown:N0} shown";
        }

        private string SideStatus()
        {
            if (_state.Kind == SideKind.Scene)
            {
                var binding = _sceneSlot.Binding == SceneBinding.Live ? "open" : "preview";
                return _sceneSlot.IsValid
                    ? $"{_sceneSlot.SceneName} · {_flat.Count} roots · {binding}"
                    : $"{_sceneSlot.SceneName} · not loaded";
            }

            if (_state.Kind != SideKind.Folder) return $"{_state.Kind} · {_state.RootPath}";

            return $"{_state.RootPath} · {_flat.Count} items";
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
            if (!_source.TryLoadChildren(id, out var placeholderId, out var children)) return false;

            _tree.TryRemoveItem(placeholderId, false);
            for (int i = 0; i < children.Count; i++)
                _tree.AddItem(children[i], id, -1, i == children.Count - 1);

            if (children.Count == 0) _tree.Rebuild();
            return true;
        }

        private void LoadDescendants(int id, ref int loaded)
        {
            if (loaded > ExpandAllBudget) return;
            if (!_source.TryGetLoadedChildren(id, out var children)) return;

            foreach (var child in children)
            {
                if (!child.data.HasChildren) continue;
                if (EnsureLoaded(child.id)) loaded++;

                LoadDescendants(child.id, ref loaded);
                if (loaded > ExpandAllBudget) return;
            }
        }

        private HashSet<int> CollectExpandedIds()
        {
            var expanded = new HashSet<int>();
            if (_source != null) CollectExpandedIds(_source.RootItems, expanded);
            return expanded;
        }

        private void CollectExpandedIds(IReadOnlyList<TreeViewItemData<ICommanderItem>> level, HashSet<int> into)
        {
            foreach (var node in level)
            {
                if (!node.data.HasChildren) continue;
                if (!_tree.IsExpanded(node.id)) continue;

                into.Add(node.id);
                if (_source.TryGetLoadedChildren(node.id, out var children))
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

                if (_source.TryGetLoadedChildren(node.id, out var children))
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

                if (item.Kind == CommanderItemKind.GameObject || item.Kind == CommanderItemKind.Component)
                {
                    Ping(item);
                    return;
                }

                var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                if (asset != null) AssetDatabase.OpenAsset(asset);
                return;
            }
        }

        // Preview-scene objects are deliberately left alone: they are in no hierarchy to ping,
        // and selecting one would put an object nobody can find into the Inspector.
        private void Ping(ICommanderItem item)
        {
            if (item == null) return;

            if (item.Kind == CommanderItemKind.GameObject || item.Kind == CommanderItemKind.Component)
            {
                if (_sceneSlot.IsPreview) return;

                var target = item is GameObjectItem gameObjectItem
                    ? (Object)gameObjectItem.GameObject
                    : (item as ComponentItem)?.Component;

                if (target != null) EditorGUIUtility.PingObject(target);
                return;
            }

            if (item.AssetPath == null) return;

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
