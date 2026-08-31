using DataKeeper.Editor.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public static class AssetCommanderPrefs
    {
        private const string Prefix = "DataKeeper.AssetCommander.";

        public static readonly ReactiveEditorPref<float> SplitPosition =
            new ReactiveEditorPref<float>(0f, Prefix + "SplitPosition");

        public static readonly ReactiveEditorPref<string> RootA =
            new ReactiveEditorPref<string>(SidePanelState.RootFolderPath, Prefix + "RootA");

        public static readonly ReactiveEditorPref<string> RootB =
            new ReactiveEditorPref<string>(SidePanelState.RootFolderPath, Prefix + "RootB");

        public static readonly ReactiveEditorPref<int> ViewModeA =
            new ReactiveEditorPref<int>((int)SideViewMode.Tree, Prefix + "ViewModeA");

        public static readonly ReactiveEditorPref<int> ViewModeB =
            new ReactiveEditorPref<int>((int)SideViewMode.Tree, Prefix + "ViewModeB");

        public static readonly ReactiveEditorPref<string> ModeIdA =
            new ReactiveEditorPref<string>("search", Prefix + "ModeIdA");

        public static readonly ReactiveEditorPref<string> ModeIdB =
            new ReactiveEditorPref<string>("search", Prefix + "ModeIdB");

        public static readonly ReactiveEditorPref<bool> ShowComponentsA =
            new ReactiveEditorPref<bool>(false, Prefix + "ShowComponentsA");

        public static readonly ReactiveEditorPref<bool> ShowComponentsB =
            new ReactiveEditorPref<bool>(false, Prefix + "ShowComponentsB");

        public static ReactiveEditorPref<string> Root(SideId id) => id == SideId.A ? RootA : RootB;
        public static ReactiveEditorPref<int> ViewMode(SideId id) => id == SideId.A ? ViewModeA : ViewModeB;
        public static ReactiveEditorPref<string> ModeId(SideId id) => id == SideId.A ? ModeIdA : ModeIdB;

        public static ReactiveEditorPref<bool> ShowComponents(SideId id) =>
            id == SideId.A ? ShowComponentsA : ShowComponentsB;

        public static void Load(SidePanelState state)
        {
            state.SetRoot(Root(state.Id).Value);
            state.ViewMode = (SideViewMode)ViewMode(state.Id).Value;
            state.ModeId = ModeId(state.Id).Value;
            state.ShowComponents = ShowComponents(state.Id).Value;
        }

        public static void Save(SidePanelState state)
        {
            Root(state.Id).UniqueValue = state.RootPath;
            ViewMode(state.Id).UniqueValue = (int)state.ViewMode;
            ModeId(state.Id).UniqueValue = state.ModeId;
            ShowComponents(state.Id).UniqueValue = state.ShowComponents;
        }
    }
}
