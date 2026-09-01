using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The scene-side twin of FolderSideSource: same lazy level-at-a-time contract, reading a
    // live or preview Scene through the real GameObject APIs instead of the disk. Whether the
    // scene is open in the editor or loaded for preview is SceneSlot's business — from here a
    // Scene is a Scene.
    public sealed class SceneSideSource : ISideSource
    {
        private readonly Dictionary<int, GameObjectItem> _itemById = new Dictionary<int, GameObjectItem>();
        private readonly Dictionary<int, int> _placeholderById = new Dictionary<int, int>();

        private readonly Dictionary<int, List<TreeViewItemData<ICommanderItem>>> _children =
            new Dictionary<int, List<TreeViewItemData<ICommanderItem>>>();

        private List<TreeViewItemData<ICommanderItem>> _rootItems =
            new List<TreeViewItemData<ICommanderItem>>();

        private Scene _scene;
        private bool _showComponents;
        private SearchFilter _filter = SearchFilter.Empty;

        public Scene Scene => _scene;

        public bool ShowComponents
        {
            get => _showComponents;
            set
            {
                if (_showComponents == value) return;
                _showComponents = value;
                Invalidate();
            }
        }

        public SearchFilter Filter
        {
            get => _filter;
            set => _filter = value ?? SearchFilter.Empty;
        }

        public IReadOnlyList<TreeViewItemData<ICommanderItem>> RootItems => _rootItems;

        public void SetScene(Scene scene)
        {
            _scene = scene;
            Invalidate();
        }

        // Scene objects are read fresh every build — there is no disk read to cache, and the
        // hierarchy is exactly the thing that changes under us.
        public void Invalidate()
        {
            _itemById.Clear();
            _placeholderById.Clear();
            _children.Clear();
            _rootItems = new List<TreeViewItemData<ICommanderItem>>();
        }

        public List<TreeViewItemData<ICommanderItem>> BuildRoot()
        {
            Invalidate();
            _rootItems = BuildLevel(Roots());
            return _rootItems;
        }

        public List<ICommanderItem> BuildFlat()
        {
            var roots = Roots();
            var result = new List<ICommanderItem>(roots.Count);

            foreach (var root in roots)
            {
                var item = new GameObjectItem(root, _showComponents);
                if (Matches(item)) result.Add(item);
            }

            return result;
        }

        public bool TryLoadChildren(int parentId, out int placeholderId,
            out List<TreeViewItemData<ICommanderItem>> children)
        {
            children = null;
            placeholderId = 0;

            if (_children.ContainsKey(parentId)) return false;
            if (!_itemById.TryGetValue(parentId, out var parent)) return false;
            if (!_placeholderById.TryGetValue(parentId, out placeholderId)) return false;

            children = BuildChildren(parent);
            _children[parentId] = children;
            _placeholderById.Remove(parentId);
            return true;
        }

        public bool TryGetLoadedChildren(int parentId, out List<TreeViewItemData<ICommanderItem>> children) =>
            _children.TryGetValue(parentId, out children);

        private List<GameObject> Roots()
        {
            var roots = new List<GameObject>();
            if (!_scene.IsValid() || !_scene.isLoaded) return roots;

            _scene.GetRootGameObjects(roots);
            return roots;
        }

        private List<TreeViewItemData<ICommanderItem>> BuildChildren(GameObjectItem parent)
        {
            var result = new List<TreeViewItemData<ICommanderItem>>();
            var gameObject = parent.GameObject;
            if (gameObject == null) return result;

            var transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
                AddNode(result, new GameObjectItem(transform.GetChild(i).gameObject, _showComponents));

            if (!_showComponents) return result;

            int components = gameObject.GetComponentCount();
            for (int i = 0; i < components; i++)
                AddNode(result, new ComponentItem(gameObject.GetComponentAtIndex(i), gameObject, i));

            return result;
        }

        // Hierarchy order is authored order, so unlike the folder side nothing here is sorted —
        // a scene tree that reorders itself alphabetically is a different scene to read.
        private List<TreeViewItemData<ICommanderItem>> BuildLevel(List<GameObject> level)
        {
            var result = new List<TreeViewItemData<ICommanderItem>>(level.Count);

            foreach (var gameObject in level)
                AddNode(result, new GameObjectItem(gameObject, _showComponents));

            return result;
        }

        private void AddNode(List<TreeViewItemData<ICommanderItem>> into, ICommanderItem item)
        {
            if (!Matches(item)) return;

            List<TreeViewItemData<ICommanderItem>> stub = null;

            if (item is GameObjectItem gameObjectItem)
            {
                _itemById[item.Id] = gameObjectItem;

                if (item.HasChildren)
                {
                    var placeholder = new ScenePlaceholderItem(gameObjectItem.GameObject);
                    _placeholderById[item.Id] = placeholder.Id;
                    stub = new List<TreeViewItemData<ICommanderItem>>(1)
                    {
                        new TreeViewItemData<ICommanderItem>(placeholder.Id, placeholder),
                    };
                }
            }

            into.Add(new TreeViewItemData<ICommanderItem>(item.Id, item, stub));
        }

        // Parents survive the filter for the same reason folders do: the tree has to stay
        // navigable while the filter is being typed.
        private bool Matches(ICommanderItem item) => item.HasChildren || _filter.Matches(item);
    }

    public sealed class ScenePlaceholderItem : ICommanderItem
    {
        public ScenePlaceholderItem(GameObject owner)
        {
            Id = CommanderItemIds.ForScenePlaceholder(owner.GetEntityId());
        }

        public int Id { get; }
        public string Name => "…";
        public string SubLabel => "";
        public Texture Icon => null;
        public CommanderItemKind Kind => CommanderItemKind.Placeholder;
        public string Guid => null;
        public string AssetPath => null;
        public GlobalObjectId? SceneId => null;
        public bool HasChildren => false;
        public long Size => 0;
        public long ModifiedTicks => 0;
        public string Badge => null;
        public bool BadgeIsAlert => false;
    }
}
