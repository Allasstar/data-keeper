using System;
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
        
        public static ReactiveEditorPref<Type> SelectedStaticClassPref =
            new ReactiveEditorPref<Type>(typeof(object), "selected_static_class");
    }
}
