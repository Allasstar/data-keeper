using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public class AssetCommanderWindow : EditorWindow
    {
        private const float DefaultSplitPosition = 450f;

        private readonly SidePanelState[] _states =
        {
            new SidePanelState(SideId.A),
            new SidePanelState(SideId.B),
        };

        private readonly SidePanelView[] _views = new SidePanelView[2];

        private TwoPaneSplitView _split;
        private IndexStatusBar _statusBar;
        private Label _selectionStatus;
        private SideId _activeSide = SideId.A;

        private string _pendingModeId;
        private string _pendingRoot;

        [MenuItem("Tools/Windows/Asset Commander", priority = 6)]
        public static void ShowWindow() => Open();

        public static AssetCommanderWindow Open()
        {
            var window = GetWindow<AssetCommanderWindow>();
            window.titleContent = new GUIContent("Asset Commander",
                EditorGUIUtility.FindTexture("d_Project"));
            window.minSize = new Vector2(900, 500);
            return window;
        }

        // How other menu items hand this window a question to answer. The request is held until
        // CreateGUI has run, because GetWindow does not guarantee the UI exists yet.
        public static void Show(string modeId, string sideARoot)
        {
            var window = Open();
            window._pendingModeId = modeId;
            window._pendingRoot = sideARoot;

            if (window._views[0] != null) window.ApplyRequest();
        }

        private void ApplyRequest()
        {
            var state = _states[(int)SideId.A];

            if (!string.IsNullOrEmpty(_pendingRoot)) state.SetRoot(_pendingRoot);
            if (!string.IsNullOrEmpty(_pendingModeId)) state.ModeId = _pendingModeId;

            if (_pendingModeId != null || _pendingRoot != null) SetActiveSide(SideId.A);

            _pendingModeId = null;
            _pendingRoot = null;
        }

        public void CreateGUI()
        {
            var tree = UxmlLoader.LoadUxml("AssetCommander");
            var sideTemplate = UxmlLoader.LoadUxml("SidePanel");
            var style = UxmlLoader.LoadUss("AssetCommander");

            rootVisualElement.styleSheets.Add(style);
            tree.CloneTree(rootVisualElement);
            rootVisualElement.Q<VisualElement>("root").style.flexGrow = 1;

            foreach (var state in _states) AssetCommanderPrefs.Load(state);

            _split = rootVisualElement.Q<TwoPaneSplitView>("split");
            _split.fixedPaneInitialDimension = LoadSplitPosition();

            _selectionStatus = rootVisualElement.Q<Label>("selection-status");

            _views[0] = CreateSide(SideId.A, "side-a", sideTemplate);
            _views[1] = CreateSide(SideId.B, "side-b", sideTemplate);

            // Cross-Side mode asks about the other panel, so each side is told who that is once
            // both exist.
            _views[0].SetPeer(_views[1]);
            _views[1].SetPeer(_views[0]);

            SetActiveSide(_activeSide);

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            _statusBar = new IndexStatusBar(rootVisualElement);

            // The index outlives the window, so opening it only has to ask for one — a
            // rebuild is a toolbar button away.
            ProjectIndex.EnsureBuilt();

            ApplyRequest();
        }

        private void OnDisable()
        {
            SaveSplitPosition();

            foreach (var state in _states) AssetCommanderPrefs.Save(state);
            foreach (var view in _views) view?.Dispose();

            _statusBar?.Dispose();
            _statusBar = null;
        }

        private SidePanelView CreateSide(SideId id, string hostName, VisualTreeAsset template)
        {
            var state = _states[(int)id];
            var host = rootVisualElement.Q<VisualElement>(hostName);
            var view = new SidePanelView(state, host, template);

            // Prefs are flushed in OnDisable, which the editor also calls before a domain
            // reload — so per-change saving would be redundant.
            view.Activated += () => SetActiveSide(id);
            view.Selection.OnChanged.AddListener(UpdateSelectionStatus);

            return view;
        }

        private void SetActiveSide(SideId id)
        {
            _activeSide = id;
            for (int i = 0; i < _views.Length; i++)
                _views[i]?.SetActive(i == (int)id);

            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            if (_selectionStatus == null) return;

            var selection = _views[(int)_activeSide]?.Selection;
            _selectionStatus.text = selection == null ? "Nothing selected" : selection.Describe();
        }

        // Tab is the commander's side switch, so it is claimed before the focus controller can
        // treat it as "move to the next focusable element" — except inside the search field,
        // where it still has to behave like a text field.
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Tab && evt.character != '\t') return;
            if (evt.target is VisualElement element && element.GetFirstAncestorOfType<TextField>() != null) return;

            SetActiveSide(_activeSide == SideId.A ? SideId.B : SideId.A);
            _views[(int)_activeSide]?.Focus();

            evt.StopPropagation();
        }

        private static float LoadSplitPosition()
        {
            var saved = AssetCommanderPrefs.SplitPosition.Value;
            return saved > 1f ? saved : DefaultSplitPosition;
        }

        private void SaveSplitPosition()
        {
            var width = _split?.fixedPane?.resolvedStyle.width ?? 0f;
            if (width > 1f) AssetCommanderPrefs.SplitPosition.UniqueValue = width;
        }
    }
}
