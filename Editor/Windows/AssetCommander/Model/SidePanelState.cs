using DataKeeper.Signals;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public enum SideId
    {
        A = 0,
        B = 1,
    }

    public enum SideKind
    {
        None = 0,
        Folder = 1,
        Scene = 2,
    }

    public enum SideViewMode
    {
        Tree = 0,
        List = 1,
    }

    public class SidePanelState
    {
        public const string RootFolderPath = "Assets";

        public readonly SideId Id;

        public readonly Signal OnRootChanged = new Signal();
        public readonly Signal OnViewChanged = new Signal();
        public readonly Signal OnFilterChanged = new Signal();

        private string _rootPath = RootFolderPath;
        private SideKind _kind = SideKind.Folder;
        private SideViewMode _viewMode = SideViewMode.Tree;
        private string _modeId = "search";
        private string _filter = "";

        public SidePanelState(SideId id)
        {
            Id = id;
        }

        public string RootPath => _rootPath;
        public SideKind Kind => _kind;

        public SideViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode == value) return;
                _viewMode = value;
                OnViewChanged.Invoke();
            }
        }

        public string ModeId
        {
            get => _modeId;
            set
            {
                if (_modeId == value) return;
                _modeId = value;
                OnViewChanged.Invoke();
            }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                value ??= "";
                if (_filter == value) return;
                _filter = value;
                OnFilterChanged.Invoke();
            }
        }

        public Object RootAsset => string.IsNullOrEmpty(_rootPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Object>(_rootPath);

        public void SetRoot(string path)
        {
            var kind = ResolveKind(path);
            if (kind == SideKind.None)
            {
                path = RootFolderPath;
                kind = SideKind.Folder;
            }

            if (_rootPath == path && _kind == kind) return;

            _rootPath = path;
            _kind = kind;
            OnRootChanged.Invoke();
        }

        // A DefaultAsset is both a folder and any file Unity has no importer for, so the
        // folder test has to be AssetDatabase.IsValidFolder, not a type check.
        public static SideKind ResolveKind(string path)
        {
            if (string.IsNullOrEmpty(path)) return SideKind.None;
            if (AssetDatabase.IsValidFolder(path)) return SideKind.Folder;

            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SceneAsset)
                ? SideKind.Scene
                : SideKind.None;
        }

        public static bool IsAcceptableRoot(Object asset)
        {
            if (asset == null) return false;
            return ResolveKind(AssetDatabase.GetAssetPath(asset)) != SideKind.None;
        }
    }
}
