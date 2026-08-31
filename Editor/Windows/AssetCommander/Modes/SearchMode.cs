namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The only mode that does not produce a result set. The search box already narrows the
    // side's own items through SearchFilter, so the panel keeps its lazy tree — materialising
    // every asset under Assets/ just to filter it is the cost that tree exists to avoid.
    public sealed class SearchMode : ICommanderMode
    {
        public string Id => CommanderModes.SearchId;

        public string DisplayName => "Search";

        public string Tooltip =>
            "Filter this side by name. \"t:mat\" matches by type or extension, * and ? glob the "
            + "name, and several terms all have to match.";

        public bool Supports(SideKind kind) => kind != SideKind.None;

        public ModeResult Evaluate(ModeContext context) => ModeResult.PassThrough;
    }
}
