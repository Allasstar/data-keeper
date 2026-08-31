using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Assets pointing at a guid that is in neither the project nor the editor's own built-ins.
    // On a scene side the same question has to be asked of live objects instead, because a
    // reference that no longer resolves leaves nothing in the file for the index to have read.
    public sealed class BrokenReferencesMode : ICommanderMode
    {
        // Enough of the missing guid to recognise it in the .meta or in version control,
        // without a 32-character badge on every row.
        private const int GuidPrefixLength = 8;

        private const int MaxNamedGuids = 2;

        public string Id => CommanderModes.BrokenReferencesId;

        public string DisplayName => "Broken Refs";

        public string Tooltip => "Assets and objects whose references point at something that no longer exists.";

        public bool Supports(SideKind kind) => kind != SideKind.None;

        public ModeResult Evaluate(ModeContext context) =>
            context.Self.Kind == SideKind.Scene ? EvaluateScene(context) : EvaluateFolder(context);

        private static ModeResult EvaluateFolder(ModeContext context)
        {
            var index = context.Index;
            var items = new List<ICommanderItem>();

            foreach (var record in ModeScope.RecordsUnder(index, context.Self.RootPath))
            {
                if (!index.HasBrokenReferences(record.Guid)) continue;

                items.Add(ModeScope.ResultItem(record, context.Self.RootPath, Describe(record, index), true));
            }

            ModeScope.SortByPath(items);

            return new ModeResult(items, ModeScope.Plural(items.Count, "broken asset", "broken assets"));
        }

        private static string Describe(AssetRecord record, IndexQuery index)
        {
            var named = new List<string>(MaxNamedGuids);
            int total = 0;
            bool script = false;

            foreach (var dependency in record.DependencyGuids)
            {
                if (!index.IsMissing(dependency)) continue;

                total++;
                script |= IsScriptGuid(record, dependency);
                if (named.Count < MaxNamedGuids) named.Add(dependency.Substring(0, GuidPrefixLength) + "…");
            }

            if (total == 0) return "broken";

            var text = (script ? "missing script " : "missing ") + string.Join(", ", named);
            return total > named.Count ? text + " +" + (total - named.Count) : text;
        }

        private static bool IsScriptGuid(AssetRecord record, string guid)
        {
            foreach (var script in record.ScriptGuids)
                if (script == guid)
                    return true;

            return false;
        }

        // The index knows nothing about a scene's unresolved references: the file still holds a
        // guid, but whether it resolves in the loaded scene is a question only the loaded scene
        // can answer, and a prefab instance's overrides are not in the file at all.
        private static ModeResult EvaluateScene(ModeContext context)
        {
            var items = new List<ICommanderItem>();
            if (!context.Self.HasScene) return ModeResult.Empty("Scene not loaded");

            foreach (var gameObject in SceneProbe.AllObjects(context.Self.Scene))
            {
                var missing = SceneProbe.DescribeMissingReferences(gameObject);
                if (missing == null) continue;

                var item = new GameObjectItem(gameObject, false);
                item.SetSubLabel(SceneProbe.HierarchyPath(gameObject));
                item.SetBadge(missing, true);
                items.Add(item);
            }

            return new ModeResult(items, ModeScope.Plural(items.Count, "broken object", "broken objects"));
        }
    }
}
