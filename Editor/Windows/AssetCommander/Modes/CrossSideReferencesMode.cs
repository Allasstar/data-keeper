using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The one mode that needs both sides: what on this side touches, or is touched by, what is
    // on the other. Direction is per-side state, so the two panels can ask opposite questions
    // about the same pair of roots at once.
    public sealed class CrossSideReferencesMode : ICommanderMode
    {
        public string Id => CommanderModes.CrossSideId;

        public string DisplayName => "Cross-Side";

        public string Tooltip =>
            "What this side references on the other side — or, reversed, what the other side "
            + "references here.";

        public bool Supports(SideKind kind) => kind != SideKind.None;

        // A scene object can reference an asset but nothing can reference a scene object, so
        // the reverse question has no answer on a scene side.
        public static bool SupportsReverse(SideKind kind) => kind == SideKind.Folder;

        public ModeResult Evaluate(ModeContext context)
        {
            if (context.Other.Kind == SideKind.None)
                return ModeResult.Empty("The other side is empty");

            var other = OtherSideGuids(context);
            if (other.Count == 0)
                return ModeResult.Empty($"Side {context.Other.Id} holds nothing to match");

            bool reverse = context.Self.Reverse && SupportsReverse(context.Self.Kind);

            return context.Self.Kind == SideKind.Scene
                ? EvaluateScene(context, other)
                : EvaluateFolder(context, other, reverse);
        }

        private static ModeResult EvaluateFolder(ModeContext context, HashSet<string> other, bool reverse)
        {
            var index = context.Index;
            var items = new List<ICommanderItem>();
            var arrow = reverse ? "← " : "→ ";
            var label = context.Other.Id.ToString();

            foreach (var record in ModeScope.RecordsUnder(index, context.Self.RootPath))
            {
                var links = reverse ? index.GetReferencedBy(record.Guid) : record.DependencyGuids;

                int hits = 0;
                foreach (var guid in links)
                    if (other.Contains(guid))
                        hits++;

                if (hits == 0) continue;

                var badge = arrow + hits.ToString("N0") + " " + label;
                items.Add(ModeScope.ResultItem(record, context.Self.RootPath, badge));
            }

            ModeScope.SortByPath(items);

            var direction = reverse ? $"referenced by side {label}" : $"referencing side {label}";
            return new ModeResult(items, $"{ModeScope.Plural(items.Count, "asset", "assets")} {direction}");
        }

        private static ModeResult EvaluateScene(ModeContext context, HashSet<string> other)
        {
            if (!context.Self.HasScene) return ModeResult.Empty("Scene not loaded");

            var items = new List<ICommanderItem>();
            var label = context.Other.Id.ToString();
            var guids = new HashSet<string>();

            foreach (var gameObject in SceneProbe.AllObjects(context.Self.Scene))
            {
                guids.Clear();
                SceneProbe.CollectAssetGuids(gameObject, guids);

                int hits = 0;
                foreach (var guid in guids)
                    if (other.Contains(guid))
                        hits++;

                if (hits == 0) continue;

                var item = new GameObjectItem(gameObject, false);
                item.SetSubLabel(SceneProbe.HierarchyPath(gameObject));
                item.SetBadge("→ " + hits.ToString("N0") + " " + label);
                items.Add(item);
            }

            return new ModeResult(items,
                $"{ModeScope.Plural(items.Count, "object", "objects")} referencing side {label}");
        }

        // A folder side is its records; a scene side is what its objects point at. The live
        // scene is read rather than its saved record so that deleting an instance drops it out
        // of the answer immediately, before the scene is saved.
        private static HashSet<string> OtherSideGuids(ModeContext context)
        {
            var other = context.Other;
            if (other.Kind == SideKind.Folder) return ModeScope.GuidsUnder(context.Index, other.RootPath);

            var guids = new HashSet<string>(System.StringComparer.Ordinal);

            if (other.HasScene)
            {
                foreach (var gameObject in SceneProbe.AllObjects(other.Scene))
                    SceneProbe.CollectAssetGuids(gameObject, guids);

                return guids;
            }

            // Not loaded — the scene's own record still lists what the file referenced when it
            // was last saved, which is the best answer available without opening it.
            if (context.Index.TryGetByPath(other.RootPath, out var record))
                foreach (var guid in record.DependencyGuids)
                    guids.Add(guid);

            return guids;
        }
    }
}
