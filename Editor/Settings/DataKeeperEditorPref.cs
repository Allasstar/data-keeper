using DataKeeper.Editor.Generic;

namespace DataKeeper.Editor.Settings
{
    public static class DataKeeperEditorPref
    {
        public static ReactiveEditorPref<bool> EnhanceHierarchyIconPref =
            new ReactiveEditorPref<bool>(true, "EnhanceHierarchyIcon_Enabled");
    }
}
