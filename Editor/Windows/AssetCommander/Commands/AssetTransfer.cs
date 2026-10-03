using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The folder → folder half of Move and Copy. They differ in verb, in whether the source's own
    // folder is a legal target, and in what a dependency costs — a moved one leaves everything
    // else that uses it pointing at the new path, a copied one leaves it untouched.
    public static class AssetTransfer
    {
        public static PlanOptions DefaultOptions =>
            new PlanOptions(ConflictResolution.AutoRename, FolderStructure.KeepStructure,
                includeDependencies: AssetCommanderPrefs.IncludeDependencies.Value);

        public static OperationPlan Plan(CommanderContext context, PlanOptions options, bool move)
        {
            AssetCommanderPrefs.IncludeDependencies.UniqueValue = options.IncludeDependencies;

            var verb = move ? "Move" : "Copy";
            var selection = context.Active.SelectedAssetItems();
            var targetRoot = context.Other.FolderRoot;

            var dependencies = options.IncludeDependencies
                ? DependencyCollector.Collect(DependencyCollector.CurrentSource(), selection)
                : DependencySet.Empty;

            // Copying into the source's own folder is legal — it is what Duplicate does — so the
            // same-folder rejection is Move's alone.
            var plan = new TransferPlanner(AssetOperations.Exists).Build(selection, context.Active.RootPath,
                targetRoot, options, verb, verb, move, dependencies.Paths, SameNameLookup(targetRoot));

            plan.Context = context;
            plan.ShowDependenciesOption = true;
            plan.Rebuild = rebuilt => Plan(context, rebuilt, move);

            int shared = move ? MarkShared(plan, dependencies) : 0;
            plan.Caveat = Caveat(move, options.IncludeDependencies, dependencies, shared);

            return plan;
        }

        // A moved dependency keeps its GUID, so its other users keep working — but it leaves the
        // folder they expect it in, and the row should say who else is affected.
        private static int MarkShared(OperationPlan plan, DependencySet dependencies)
        {
            if (!ProjectIndex.IsReady) return 0;

            int shared = 0;

            foreach (var row in plan.Operations)
            {
                if (!row.IsDependency || !ProjectIndex.TryGetByPath(row.Source, out var record)) continue;

                int outside = 0;
                foreach (var referrer in ProjectIndex.GetReferencedBy(record.Guid))
                    if (ProjectIndex.TryGetByGuid(referrer, out var user) && !dependencies.Members.Contains(user.Path))
                        outside++;

                if (outside == 0) continue;

                shared++;
                row.AppendNote("also used by " + outside);
            }

            return shared;
        }

        private static string Caveat(bool move, bool withDependencies, DependencySet dependencies, int shared)
        {
            var parts = new List<string>(3);

            if (!move)
                parts.Add(GuidRemapper.IsSupported
                    ? "Copies get new GUIDs. References between the copied assets are redirected to "
                      + "the copies; everything else keeps pointing at the originals."
                    : "Copies get new GUIDs, and without Force Text serialization they keep "
                      + "referencing the originals.");

            if (withDependencies)
            {
                int left = dependencies.LeftCode + dependencies.LeftPackages;
                if (left > 0)
                    parts.Add(left + " script, shader or package dependencies stay where they are and are shared.");

                if (shared > 0)
                    parts.Add(shared + " moved dependencies are also used by assets that stay behind; "
                              + "those keep working, the files just change folder.");

                if (!ProjectIndex.IsReady)
                    parts.Add("The index is still building, so dependencies were read from the AssetDatabase.");
            }

            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        // Any asset already under the target with the same file name: the duplicate a transfer is
        // most likely creating, which no check of the one destination path would notice.
        private static Func<string, string> SameNameLookup(string targetRoot)
        {
            if (string.IsNullOrEmpty(targetRoot) || !ProjectIndex.IsReady) return null;

            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in ModeScope.RecordsUnder(ProjectIndex.Query, targetRoot))
            {
                var name = ModeScope.NameOf(record.Path);
                if (!byName.ContainsKey(name)) byName.Add(name, record.Path);
            }

            return name => byName.TryGetValue(name, out var path) ? path : null;
        }
    }
}
