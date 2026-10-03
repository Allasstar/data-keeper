using DataKeeper.Editor.Enhance;
using DataKeeper.Editor.Generic;
using UnityEngine;

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

        public static ReactiveEditorPref<Color> MeshTools_SourceEdgeColor =
            new ReactiveEditorPref<Color>(new Color(0.6f, 0.6f, 0.6f, 0.35f), "Editor_MeshTools_SourceEdgeColor");

        public static ReactiveEditorPref<Color> MeshTools_PreviewEdgeColor =
            new ReactiveEditorPref<Color>(new Color(0.3f, 0.85f, 1f, 0.9f), "Editor_MeshTools_PreviewEdgeColor");
    }
}
