using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // What a mode is told about one side — flattened out of SidePanelState rather than holding
    // a reference to it, so a test can describe a side without an AssetDatabase behind it. The
    // scene travels here because a preview scene exists nowhere the other side could look up.
    public readonly struct SideContext
    {
        public readonly SideId Id;
        public readonly SideKind Kind;
        public readonly string RootPath;
        public readonly bool Reverse;
        public readonly Scene Scene;

        public SideContext(SidePanelState state, Scene scene)
        {
            Id = state?.Id ?? SideId.A;
            Kind = state?.Kind ?? SideKind.None;
            RootPath = state?.RootPath ?? "";
            Reverse = state != null && state.CrossSideReverse;
            Scene = scene;
        }

        public SideContext(SideId id, SideKind kind, string rootPath, bool reverse = false,
            Scene scene = default)
        {
            Id = id;
            Kind = kind;
            RootPath = rootPath ?? "";
            Reverse = reverse;
            Scene = scene;
        }

        public bool HasScene => Kind == SideKind.Scene && Scene.IsValid() && Scene.isLoaded;
    }

    public readonly struct ModeContext
    {
        public readonly SideContext Self;
        public readonly SideContext Other;
        public readonly IndexQuery Index;

        public ModeContext(SideContext self, SideContext other, IndexQuery index)
        {
            Self = self;
            Other = other;
            Index = index;
        }
    }

    // A mode's answer. Items is null for a mode that only narrows what the side already shows
    // (Search), so the panel keeps its lazy tree instead of materialising every row; the
    // analysis modes hand back a flat, annotated result set that replaces it.
    public sealed class ModeResult
    {
        public static readonly ModeResult PassThrough = new ModeResult(null, null, null);

        public ModeResult(List<ICommanderItem> items, string summary, string caveat = null)
        {
            Items = items;
            Summary = summary;
            Caveat = caveat;
        }

        public List<ICommanderItem> Items { get; }

        // Status-line text: what the mode found, in the mode's own words.
        public string Summary { get; }

        // Shown above the results when the answer has a limit the user has to know about —
        // Unused cannot see Resources.Load(string), and nothing can see reflection.
        public string Caveat { get; }

        public bool IsPassThrough => Items == null;

        public static ModeResult Empty(string summary, string caveat = null) =>
            new ModeResult(new List<ICommanderItem>(), summary, caveat);
    }

    public interface ICommanderMode
    {
        string Id { get; }
        string DisplayName { get; }
        string Tooltip { get; }

        bool Supports(SideKind kind);

        ModeResult Evaluate(ModeContext context);
    }

    public static class CommanderModes
    {
        public const string SearchId = "search";
        public const string BrokenReferencesId = "broken-refs";
        public const string MissingScriptsId = "missing-scripts";
        public const string CrossSideId = "cross-side";
        public const string UnusedId = "unused";
        public const string DuplicatesId = "duplicates";

        // Modes hold no state between evaluations, so one instance each is enough.
        private static readonly ICommanderMode[] Registry =
        {
            new SearchMode(),
            new BrokenReferencesMode(),
            new MissingScriptsMode(),
            new CrossSideReferencesMode(),
            new UnusedAssetsMode(),
            new DuplicatesMode(),
        };

        public static IReadOnlyList<ICommanderMode> All => Registry;

        // Falls back to Search rather than returning null: a stale pref from an older layout
        // must not leave a side with no mode at all.
        public static ICommanderMode Get(string id)
        {
            foreach (var mode in Registry)
                if (mode.Id == id)
                    return mode;

            return Registry[0];
        }
    }
}
