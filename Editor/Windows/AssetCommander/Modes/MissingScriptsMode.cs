using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Prefabs, scenes and assets carrying an m_Script binding that resolves to nothing, and —
    // on a scene side — the live objects Unity has already drawn "Missing (Mono Script)" on.
    public sealed class MissingScriptsMode : ICommanderMode
    {
        private const string DegradedCaveat =
            "Asset Serialization is not set to Force Text, so missing scripts cannot be detected "
            + "in asset files. Scene sides are unaffected.";

        public string Id => CommanderModes.MissingScriptsId;

        public string DisplayName => "Missing Scripts";

        public string Tooltip => "Objects whose script component no longer resolves to a class.";

        public bool Supports(SideKind kind) => kind != SideKind.None;

        public ModeResult Evaluate(ModeContext context) =>
            context.Self.Kind == SideKind.Scene ? EvaluateScene(context) : EvaluateFolder(context);

        private static ModeResult EvaluateFolder(ModeContext context)
        {
            var index = context.Index;
            var items = new List<ICommanderItem>();
            var caveat = ProjectIndex.TextScanningEnabled ? null : DegradedCaveat;

            foreach (var record in ModeScope.RecordsUnder(index, context.Self.RootPath))
            {
                if (!index.HasMissingScript(record.Guid)) continue;

                int count = CountMissing(record, index);
                var badge = ModeScope.Plural(count, "missing script", "missing scripts");
                items.Add(ModeScope.ResultItem(record, context.Self.RootPath, badge, true));
            }

            ModeScope.SortByPath(items);

            return new ModeResult(items, ModeScope.Plural(items.Count, "asset", "assets"), caveat);
        }

        private static int CountMissing(AssetRecord record, IndexQuery index)
        {
            int count = 0;
            foreach (var script in record.ScriptGuids)
                if (index.IsMissing(script))
                    count++;

            // Distinct guids, not instances: two objects in one prefab losing the same script
            // is one thing to go and fix.
            return count;
        }

        private static ModeResult EvaluateScene(ModeContext context)
        {
            if (!context.Self.HasScene) return ModeResult.Empty("Scene not loaded");

            var items = new List<ICommanderItem>();

            foreach (var gameObject in SceneProbe.AllObjects(context.Self.Scene))
            {
                int count = SceneProbe.MissingScriptCount(gameObject);
                if (count == 0) continue;

                var item = new GameObjectItem(gameObject, false);
                item.SetSubLabel(SceneProbe.HierarchyPath(gameObject));
                item.SetBadge(ModeScope.Plural(count, "missing script", "missing scripts"), true);
                items.Add(item);
            }

            return new ModeResult(items, ModeScope.Plural(items.Count, "object", "objects"));
        }
    }
}
