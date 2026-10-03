using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public enum ConflictResolution
    {
        AutoRename = 0,
        Overwrite = 1,
        Skip = 2,
    }

    public enum FolderStructure
    {
        Flatten = 0,
        KeepStructure = 1,
    }

    // One row of a plan: what will happen to one selected item, already resolved. Everything the
    // confirm dialog shows comes from here, so nothing is decided while the operation runs.
    public sealed class PlannedOperation
    {
        public PlannedOperation(ICommanderItem item, string source, string destination)
        {
            Item = item;
            Source = source;
            Destination = destination;
        }

        public ICommanderItem Item { get; }
        public string Source { get; }
        public string Destination { get; set; }

        // Why this row is not the plain case: a rename to dodge a collision, an overwrite, an
        // inbound reference count. Drawn next to the row and, when Alert, highlighted.
        public string Note { get; set; }
        public bool Alert { get; set; }

        // Overwrite has to delete the target before the operation runs, and only the planner
        // knows that the destination was occupied.
        public bool Overwrites { get; set; }

        // Pulled in by the dependency walk rather than selected.
        public bool IsDependency { get; set; }

        public void AppendNote(string text) => Note = string.IsNullOrEmpty(Note) ? text : Note + " · " + text;
    }

    // Everything the confirm dialog lets the user change, in one value — so rebuilding a plan is
    // one call whatever mix of controls a command offers.
    public readonly struct PlanOptions
    {
        public readonly ConflictResolution Conflict;
        public readonly FolderStructure Structure;
        public readonly string Pattern;
        public readonly bool IncludeDependencies;

        public PlanOptions(ConflictResolution conflict, FolderStructure structure = FolderStructure.KeepStructure,
            string pattern = null, bool includeDependencies = false)
        {
            Conflict = conflict;
            Structure = structure;
            Pattern = pattern;
            IncludeDependencies = includeDependencies;
        }

        public PlanOptions With(ConflictResolution conflict) =>
            new PlanOptions(conflict, Structure, Pattern, IncludeDependencies);

        public PlanOptions With(FolderStructure structure) =>
            new PlanOptions(Conflict, structure, Pattern, IncludeDependencies);

        public PlanOptions WithPattern(string pattern) =>
            new PlanOptions(Conflict, Structure, pattern, IncludeDependencies);

        public PlanOptions WithDependencies(bool include) => new PlanOptions(Conflict, Structure, Pattern, include);
    }

    // A command's whole answer, produced before anything is written. The dialog renders it; the
    // command's Execute reads it and nothing else.
    public sealed class OperationPlan
    {
        public OperationPlan(string title, string verb)
        {
            Title = title;
            Verb = verb;
        }

        public string Title { get; }
        public string Verb { get; }

        public List<PlannedOperation> Operations { get; } = new List<PlannedOperation>();

        public string Summary { get; set; }

        // Shown above the rows when the operation has a consequence the row list cannot express
        // — a new GUID, a meta rewrite, an undo that does not cover it.
        public string Caveat { get; set; }

        // Set instead of rows when the command cannot run at all; the dialog turns into a
        // message and the confirm button disappears.
        public string Blocked { get; set; }

        public CommanderContext Context { get; set; }

        // Live options the dialog offers. Changing one calls Rebuild, so the rows the user
        // confirms are always the rows the current options produce.
        public bool ShowConflictOption { get; set; }
        public bool ShowStructureOption { get; set; }
        public bool ShowDependenciesOption { get; set; }
        public string PatternLabel { get; set; }
        public PlanOptions Options { get; set; }
        public Func<PlanOptions, OperationPlan> Rebuild { get; set; }

        public bool IsBlocked => !string.IsNullOrEmpty(Blocked);
        public bool CanRun => !IsBlocked && Operations.Count > 0;

        public PlannedOperation Add(ICommanderItem item, string source, string destination)
        {
            var operation = new PlannedOperation(item, source, destination);
            Operations.Add(operation);
            return operation;
        }

        public static OperationPlan Rejected(string title, string reason) =>
            new OperationPlan(title, "") { Blocked = reason };
    }

    // Pure path arithmetic — no AssetDatabase, no disk. Split out because destination resolution
    // is the part of a destructive command worth pinning with tests, and it is the part that
    // needs neither.
    public static class OperationPaths
    {
        public static string Directory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "";

            int slash = assetPath.LastIndexOf('/');
            return slash < 0 ? "" : assetPath.Substring(0, slash);
        }

        public static string FileName(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "";

            int slash = assetPath.LastIndexOf('/');
            return slash < 0 ? assetPath : assetPath.Substring(slash + 1);
        }

        public static string NameWithoutExtension(string assetPath)
        {
            var file = FileName(assetPath);
            int dot = file.LastIndexOf('.');
            return dot <= 0 ? file : file.Substring(0, dot);
        }

        public static string Extension(string assetPath)
        {
            var file = FileName(assetPath);
            int dot = file.LastIndexOf('.');
            return dot <= 0 ? "" : file.Substring(dot);
        }

        public static string Combine(string folder, string name) =>
            string.IsNullOrEmpty(folder) ? name : folder + "/" + name;

        // KeepStructure reproduces the source's path below the side's root inside the target;
        // Flatten drops everything into the target folder. A source that is not under the stated
        // root — a mode result spans folders — has no relative part and flattens either way.
        public static string Destination(string sourcePath, string sourceRoot, string targetRoot,
            FolderStructure structure)
        {
            if (structure == FolderStructure.KeepStructure && !string.IsNullOrEmpty(sourceRoot))
            {
                var prefix = sourceRoot.EndsWith("/", StringComparison.Ordinal) ? sourceRoot : sourceRoot + "/";
                if (sourcePath.StartsWith(prefix, StringComparison.Ordinal))
                    return Combine(targetRoot, sourcePath.Substring(prefix.Length));
            }

            return Combine(targetRoot, FileName(sourcePath));
        }

        // " 1", " 2", … appended to the name, matching what Unity itself produces, so a project
        // ends up with one naming convention rather than two.
        public static string MakeUnique(string desiredPath, Func<string, bool> exists)
        {
            if (exists == null || !exists(desiredPath)) return desiredPath;

            var folder = Directory(desiredPath);
            var name = NameWithoutExtension(desiredPath);
            var extension = Extension(desiredPath);

            for (int i = 1; i < 10000; i++)
            {
                var candidate = Combine(folder, name + " " + i + extension);
                if (!exists(candidate)) return candidate;
            }

            return desiredPath;
        }

        public static bool IsSelfOrDescendant(string folder, string path)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(path)) return false;

            return path == folder || path.StartsWith(folder + "/", StringComparison.Ordinal);
        }

        // The deepest folder holding every path. Dependencies live all over the project, so this
        // is the root KeepStructure reproduces when they come along — the side's root alone would
        // flatten every one of them.
        public static string CommonFolder(string folder, IEnumerable<string> paths)
        {
            var common = folder;

            foreach (var path in paths)
            {
                if (common == null)
                {
                    common = Directory(path);
                    continue;
                }

                while (common.Length > 0 && !IsSelfOrDescendant(common, path))
                    common = Directory(common);
            }

            return common ?? "";
        }
    }

    // Builds the plan for Move and Copy: same destination arithmetic, same collisions, different
    // verb. The existence test is injected so the resolution rules can be asserted without any
    // asset on disk.
    public sealed class TransferPlanner
    {
        private readonly Func<string, bool> _exists;

        public TransferPlanner(Func<string, bool> exists)
        {
            _exists = exists;
        }

        // dependencies: what the selection needs, already closed over by DependencyCollector.
        // sameNameInTarget: file name → an asset anywhere under the target carrying that name —
        // the likely duplicate a check of the one destination path cannot see.
        public OperationPlan Build(IReadOnlyList<ICommanderItem> items, string sourceRoot,
            string targetRoot, PlanOptions options, string title, string verb, bool rejectSameFolder,
            IReadOnlyList<string> dependencies = null, Func<string, string> sameNameInTarget = null)
        {
            if (string.IsNullOrEmpty(targetRoot))
                return OperationPlan.Rejected(title, "The other side is not a folder.");

            var conflict = options.Conflict;
            var structure = options.Structure;
            dependencies = dependencies ?? Array.Empty<string>();

            var plan = new OperationPlan(title, verb)
            {
                ShowConflictOption = true,
                ShowStructureOption = true,
                Options = options,
            };

            if (structure == FolderStructure.KeepStructure && dependencies.Count > 0)
                sourceRoot = OperationPaths.CommonFolder(sourceRoot, AllSources(items, dependencies));

            // Earlier rows of the same plan are not on disk yet, so their destinations have to
            // count as taken — two sources with one name would otherwise resolve to one path.
            var claimed = new HashSet<string>(StringComparer.Ordinal);

            bool Taken(string path) => claimed.Contains(path) || (_exists != null && _exists(path));

            var counts = new Counts();

            void Resolve(ICommanderItem item, bool dependency)
            {
                var source = item?.AssetPath;
                if (string.IsNullOrEmpty(source)) return;

                // A dependency already somewhere under the target is reachable from there as is.
                if (dependency && OperationPaths.IsSelfOrDescendant(targetRoot, source))
                {
                    counts.AlreadyInTarget++;
                    return;
                }

                // Moving something into the folder it already lives in is a no-op the user did
                // not ask for; copying into it is Duplicate's job, which names the result.
                if (rejectSameFolder && OperationPaths.Directory(source) == targetRoot)
                {
                    counts.SameFolder++;
                    return;
                }

                // A folder cannot be moved inside itself — the AssetDatabase would leave the
                // project in a state neither path describes.
                if (item.Kind == CommanderItemKind.Folder &&
                    OperationPaths.IsSelfOrDescendant(source, targetRoot))
                {
                    counts.IntoSelf++;
                    return;
                }

                var destination = OperationPaths.Destination(source, sourceRoot, targetRoot, structure);
                PlannedOperation row;

                if (!Taken(destination))
                {
                    row = plan.Add(item, source, destination);
                    claimed.Add(destination);

                    var sameName = item.Kind == CommanderItemKind.Folder
                        ? null
                        : sameNameInTarget?.Invoke(OperationPaths.FileName(source));

                    if (!string.IsNullOrEmpty(sameName) && sameName != destination && sameName != source)
                    {
                        row.AppendNote("same name at " + sameName);
                        row.Alert = true;
                    }
                }
                else if (conflict == ConflictResolution.Skip)
                {
                    counts.Skipped++;
                    return;
                }
                else if (conflict == ConflictResolution.Overwrite)
                {
                    row = plan.Add(item, source, destination);
                    row.AppendNote("overwrites");
                    row.Alert = true;
                    row.Overwrites = true;
                    claimed.Add(destination);
                }
                else
                {
                    var unique = OperationPaths.MakeUnique(destination, Taken);
                    row = plan.Add(item, source, unique);
                    row.AppendNote("renamed to " + OperationPaths.FileName(unique));
                    claimed.Add(unique);
                }

                if (!dependency) return;

                counts.Dependencies++;
                row.IsDependency = true;
                row.Note = string.IsNullOrEmpty(row.Note) ? "dependency" : "dependency · " + row.Note;
            }

            foreach (var item in items) Resolve(item, false);
            foreach (var path in dependencies) Resolve(new AssetItem(path, false, false, 0, 0), true);

            plan.Summary = Describe(plan.Operations.Count, counts, targetRoot);

            if (plan.Operations.Count == 0) plan.Blocked = plan.Summary;

            return plan;
        }

        private static IEnumerable<string> AllSources(IReadOnlyList<ICommanderItem> items,
            IReadOnlyList<string> dependencies)
        {
            foreach (var item in items)
                if (!string.IsNullOrEmpty(item?.AssetPath))
                    yield return item.AssetPath;

            foreach (var path in dependencies) yield return path;
        }

        private static string Describe(int planned, Counts counts, string targetRoot)
        {
            if (planned == 0 && counts.IntoSelf > 0) return "A folder cannot be moved inside itself.";

            if (planned == 0 && counts.SameFolder > 0 && counts.Skipped == 0)
                return counts.SameFolder == 1
                    ? "That asset is already in " + targetRoot + "."
                    : "Those " + counts.SameFolder + " assets are already in " + targetRoot + ".";

            if (planned == 0) return "Nothing left to do — every row was skipped.";

            var text = planned + (planned == 1 ? " item → " : " items → ") + targetRoot;
            if (counts.Dependencies > 0) text += " · " + counts.Dependencies + " of them dependencies";
            if (counts.AlreadyInTarget > 0) text += " · " + counts.AlreadyInTarget + " dependencies already there";
            if (counts.Skipped > 0) text += " · " + counts.Skipped + " skipped";
            if (counts.SameFolder > 0) text += " · " + counts.SameFolder + " already there";
            if (counts.IntoSelf > 0) text += " · " + counts.IntoSelf + " would nest in itself";

            return text;
        }

        private sealed class Counts
        {
            public int Skipped;
            public int SameFolder;
            public int IntoSelf;
            public int Dependencies;
            public int AlreadyInTarget;
        }
    }
}
