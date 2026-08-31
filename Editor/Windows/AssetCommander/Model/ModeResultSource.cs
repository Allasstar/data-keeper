using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Renders whatever an analysis mode found. A result set is an answer, not a hierarchy — the
    // hits span folders and scene depths — so it is flat in both views, and the search box
    // narrows it in place rather than re-running the mode.
    public sealed class ModeResultSource : ISideSource
    {
        private List<ICommanderItem> _items = new List<ICommanderItem>();

        private List<TreeViewItemData<ICommanderItem>> _rootItems =
            new List<TreeViewItemData<ICommanderItem>>();

        private SearchFilter _filter = SearchFilter.Empty;

        public SearchFilter Filter
        {
            get => _filter;
            set => _filter = value ?? SearchFilter.Empty;
        }

        public IReadOnlyList<TreeViewItemData<ICommanderItem>> RootItems => _rootItems;

        public int TotalCount => _items.Count;

        public void SetItems(List<ICommanderItem> items) => _items = items ?? new List<ICommanderItem>();

        public List<TreeViewItemData<ICommanderItem>> BuildRoot()
        {
            _rootItems = new List<TreeViewItemData<ICommanderItem>>(_items.Count);

            foreach (var item in _items)
            {
                if (_filter.Matches(item))
                    _rootItems.Add(new TreeViewItemData<ICommanderItem>(item.Id, item));
            }

            return _rootItems;
        }

        public List<ICommanderItem> BuildFlat()
        {
            var result = new List<ICommanderItem>(_items.Count);

            foreach (var item in _items)
                if (_filter.Matches(item))
                    result.Add(item);

            return result;
        }

        public bool TryLoadChildren(int parentId, out int placeholderId,
            out List<TreeViewItemData<ICommanderItem>> children)
        {
            placeholderId = 0;
            children = null;
            return false;
        }

        public bool TryGetLoadedChildren(int parentId, out List<TreeViewItemData<ICommanderItem>> children)
        {
            children = null;
            return false;
        }

        public void Invalidate() => _rootItems = new List<TreeViewItemData<ICommanderItem>>();
    }
}
