using DataKeeper.Editor.Enhance;
using DataKeeper.Editor.Generic;

namespace DataKeeper.Editor.Settings
{
    public static class DataKeeperEditorPref
    {
        public static ReactiveEditorPref<bool> EnhanceHierarchy_Enabled =
            new ReactiveEditorPref<bool>(false, "Editor_EnhanceHierarchy_Enabled");
        
        public static ReactiveEditorPref<PrefabHierarchyIcon> EnhanceHierarchy_PrefabIconType =
            new ReactiveEditorPref<PrefabHierarchyIcon>(PrefabHierarchyIcon.Small, "Editor_EnhanceHierarchy_PrefabIconType");
        
        public static ReactiveEditorPref<HierarchyIconType> EnhanceHierarchy_IconType =
            new ReactiveEditorPref<HierarchyIconType>(HierarchyIconType.All, "Editor_EnhanceHierarchy_IconType");
    }
}
