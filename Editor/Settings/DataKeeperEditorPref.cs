using DataKeeper.Editor.Enhance;
using DataKeeper.Editor.Generic;

namespace DataKeeper.Editor.Settings
{
    public static class DataKeeperEditorPref
    {
        public static ReactiveEditorPref<bool> EnhanceHierarchyIconPref =
            new ReactiveEditorPref<bool>(true, "EnhanceHierarchyIcon_Enabled");
        
        public static ReactiveEditorPref<PrefabHierarchyIcon> EnhanceHierarchyPrefabIconPref =
            new ReactiveEditorPref<PrefabHierarchyIcon>(PrefabHierarchyIcon.Small, "EnhanceHierarchyPrefabIcon_Enabled");
    }
}
