using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Reads folder contents with System.IO rather than AssetDatabase because there is no
    // non-recursive AssetDatabase file listing — FindAssets always walks the whole subtree,
    // which is the cost this lazy tree exists to avoid. Directory enumeration hands back the
    // size and timestamp from the same find data, so the list columns are free.
    public sealed class FolderSideSource : ISideSource
    {
        private static readonly string ProjectRoot = ResolveProjectRoot();

        private readonly Dictionary<string, List<AssetItem>> _read =
            new Dictionary<string, List<AssetItem>>(StringComparer.Ordinal);

        private readonly Dictionary<int, string> _pathById = new Dictionary<int, string>();
        private readonly Dictionary<int, int> _placeholderById = new Dictionary<int, int>();

        private readonly Dictionary<int, List<TreeViewItemData<ICommanderItem>>> _children =
            new Dictionary<int, List<TreeViewItemData<ICommanderItem>>>();

        private List<TreeViewItemData<ICommanderItem>> _rootItems =
            new List<TreeViewItemData<ICommanderItem>>();

        private string _rootPath = SidePanelState.RootFolderPath;
        private SearchFilter _filter = SearchFilter.Empty;

        public string RootPath => _rootPath;

        public SearchFilter Filter
        {
            get => _filter;
            set => _filter = value ?? SearchFilter.Empty;
        }

        public IReadOnlyList<TreeViewItemData<ICommanderItem>> RootItems => _rootItems;

        public void SetRoot(string folderPath)
        {
            _rootPath = folderPath;
            Invalidate();
        }

        // Only the materialised tree is dropped; the disk read stays cached, so re-filtering or
        // toggling tree/list rebuilds from memory.
        public void InvalidateTree()
        {
            _pathById.Clear();
            _placeholderById.Clear();
            _children.Clear();
            _rootItems = new List<TreeViewItemData<ICommanderItem>>();
        }

        public void Invalidate()
        {
            _read.Clear();
            InvalidateTree();
        }

        public List<TreeViewItemData<ICommanderItem>> BuildRoot()
        {
            InvalidateTree();
            _rootItems = BuildLevel(_rootPath);
            return _rootItems;
        }

        public List<ICommanderItem> BuildFlat()
        {
            var source = Read(_rootPath);
            var result = new List<ICommanderItem>(source.Count);

            foreach (var item in source)
                if (Matches(item))
                    result.Add(item);

            return result;
        }

        public bool TryLoadChildren(int parentId, out int placeholderId,
            out List<TreeViewItemData<ICommanderItem>> children)
        {
            children = null;
            placeholderId = 0;

            if (_children.ContainsKey(parentId)) return false;
            if (!_pathById.TryGetValue(parentId, out var path)) return false;
            if (!_placeholderById.TryGetValue(parentId, out placeholderId)) return false;

            children = BuildLevel(path);
            _children[parentId] = children;
            _placeholderById.Remove(parentId);
            return true;
        }

        public bool TryGetLoadedChildren(int parentId, out List<TreeViewItemData<ICommanderItem>> children) =>
            _children.TryGetValue(parentId, out children);

        private List<TreeViewItemData<ICommanderItem>> BuildLevel(string folderPath)
        {
            var source = Read(folderPath);
            var result = new List<TreeViewItemData<ICommanderItem>>(source.Count);

            foreach (var item in source)
            {
                if (!Matches(item)) continue;

                _pathById[item.Id] = item.AssetPath;

                List<TreeViewItemData<ICommanderItem>> stub = null;
                if (item.HasChildren)
                {
                    var placeholder = new PlaceholderItem(item.AssetPath);
                    _placeholderById[item.Id] = placeholder.Id;
                    stub = new List<TreeViewItemData<ICommanderItem>>(1)
                    {
                        new TreeViewItemData<ICommanderItem>(placeholder.Id, placeholder),
                    };
                }

                result.Add(new TreeViewItemData<ICommanderItem>(item.Id, item, stub));
            }

            return result;
        }

        // Folders survive the filter so the tree stays navigable while it is being typed in.
        // The filter only ever narrows this level: a recursive answer is what the analysis
        // modes are for.
        private bool Matches(AssetItem item) =>
            item.Kind == CommanderItemKind.Folder || _filter.Matches(item);

        private List<AssetItem> Read(string folderPath)
        {
            if (_read.TryGetValue(folderPath, out var cached)) return cached;

            var items = ReadFromDisk(folderPath);
            _read[folderPath] = items;
            return items;
        }

        private static List<AssetItem> ReadFromDisk(string folderPath)
        {
            var items = new List<AssetItem>();
            var absolute = ToAbsolute(folderPath);
            if (string.IsNullOrEmpty(absolute)) return items;

            var directory = new DirectoryInfo(absolute);
            if (!directory.Exists) return items;

            foreach (var sub in directory.EnumerateDirectories())
            {
                if (!IsVisible(sub.Name)) continue;

                items.Add(new AssetItem(folderPath + "/" + sub.Name, true, HasVisibleEntry(sub), 0,
                    sub.LastWriteTimeUtc.Ticks));
            }

            foreach (var file in directory.EnumerateFiles())
            {
                if (!IsVisible(file.Name)) continue;

                items.Add(new AssetItem(folderPath + "/" + file.Name, false, false, file.Length,
                    file.LastWriteTimeUtc.Ticks));
            }

            items.Sort(CompareDefault);
            return items;
        }

        private static int CompareDefault(AssetItem a, AssetItem b)
        {
            bool folderA = a.Kind == CommanderItemKind.Folder;
            if (folderA != (b.Kind == CommanderItemKind.Folder)) return folderA ? -1 : 1;

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasVisibleEntry(DirectoryInfo directory)
        {
            foreach (var entry in directory.EnumerateFileSystemInfos())
                if (IsVisible(entry.Name))
                    return true;

            return false;
        }

        // Unity's own hidden-asset rules: dot-prefixed, tilde-suffixed, "cvs", plus the .meta
        // sidecars, which are never items in their own right.
        private static bool IsVisible(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name[0] == '.') return false;
            if (name[name.Length - 1] == '~') return false;
            if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) return false;

            return !string.Equals(name, "cvs", StringComparison.OrdinalIgnoreCase);
        }

        // A package folder's project-relative path says nothing about where it physically
        // lives — embedded, local file:, and the global PackageCache all differ.
        public static string ToAbsolute(string projectRelative)
        {
            if (string.IsNullOrEmpty(projectRelative)) return "";

            if (projectRelative == "Assets" || projectRelative.StartsWith("Assets/", StringComparison.Ordinal))
                return ProjectRoot + "/" + projectRelative;

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(projectRelative);
            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
                return ProjectRoot + "/" + projectRelative;

            var rest = projectRelative.Length > info.assetPath.Length
                ? projectRelative.Substring(info.assetPath.Length)
                : "";

            return (info.resolvedPath + rest).Replace('\\', '/');
        }

        private static string ResolveProjectRoot()
        {
            var assets = Application.dataPath.Replace('\\', '/');
            int slash = assets.LastIndexOf('/');
            return slash < 0 ? assets : assets.Substring(0, slash);
        }
    }
}
