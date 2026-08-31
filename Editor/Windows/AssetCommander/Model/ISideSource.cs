using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The seam SidePanelView binds to, so Phase 4's SceneSideSource can replace the folder
    // source without the view knowing which kind of side it is showing.
    public interface ISideSource
    {
        SearchFilter Filter { get; set; }

        IReadOnlyList<TreeViewItemData<ICommanderItem>> RootItems { get; }

        List<TreeViewItemData<ICommanderItem>> BuildRoot();

        List<ICommanderItem> BuildFlat();

        bool TryLoadChildren(int parentId, out int placeholderId,
            out List<TreeViewItemData<ICommanderItem>> children);

        bool TryGetLoadedChildren(int parentId, out List<TreeViewItemData<ICommanderItem>> children);

        void Invalidate();
    }
}
