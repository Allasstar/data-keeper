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
    }

    // Everything the confirm dialog lets the user change, in one value — so rebuilding a plan is
    // one call whatever mix of controls a command offers.
    public readonly struct PlanOptions
    {
        public readonly ConflictResolution Conflict;
        public readonly FolderStructure Structure;
        public readonly string Pattern;

        public PlanOptions(ConflictResolution conflict, FolderStructure structure = FolderStructure.KeepStructure,
            string pattern = null)
        {
            Conflict = conflict;
            Structure = structure;
            Pattern = pattern;
        }

        public PlanOptions With(ConflictResolution conflict) => new PlanOptions(conflict, Structure, Pattern);

        public PlanOptions With(FolderStructure structure) => new PlanOptions(Conflict, structure, Pattern);

        public PlanOptions WithPattern(string pattern) => new PlanOptions(Conflict, Structure, pattern);
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

        // " 1", " 2", … appended to the name, matching what AssetTransferTool and Unity itself
        // produce, so a project ends up with one naming convention rather than two.
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

        public OperationPlan Build(IReadOnlyList<ICommanderItem> items, string sourceRoot,
            string targetRoot, PlanOptions options, string title, string verb, bool rejectSameFolder)
        {
            if (string.IsNullOrEmpty(targetRoot))
                return OperationPlan.Rejected(title, "The other side is not a folder.");

            var conflict = options.Conflict;
            var structure = options.Structure;

            var plan = new OperationPlan(title, verb)
            {
                ShowConflictOption = true,
                ShowStructureOption = true,
                Options = options,
            };

            // Earlier rows of the same plan are not on disk yet, so their destinations have to
            // count as taken — two sources with one name would otherwise resolve to one path.
            var claimed = new HashSet<string>(StringComparer.Ordinal);

            bool Taken(string path) => claimed.Contains(path) || (_exists != null && _exists(path));

            int skipped = 0;
            int sameFolder = 0;
            int intoSelf = 0;

            foreach (var item in items)
            {
                var source = item?.AssetPath;
                if (string.IsNullOrEmpty(source)) continue;

                // Moving something into the folder it already lives in is a no-op the user did
                // not ask for; copying into it is Duplicate's job, which names the result.
                if (rejectSameFolder && OperationPaths.Directory(source) == targetRoot)
                {
                    sameFolder++;
                    continue;
                }

                // A folder cannot be moved inside itself — the AssetDatabase would leave the
                // project in a state neither path describes.
                if (item.Kind == CommanderItemKind.Folder &&
                    OperationPaths.IsSelfOrDescendant(source, targetRoot))
                {
                    intoSelf++;
                    continue;
                }

                var destination = OperationPaths.Destination(source, sourceRoot, targetRoot, structure);

                if (Taken(destination))
                {
                    if (conflict == ConflictResolution.Skip)
                    {
                        skipped++;
                        continue;
                    }

                    if (conflict == ConflictResolution.Overwrite)
                    {
                        var overwrite = plan.Add(item, source, destination);
                        overwrite.Note = "overwrites";
                        overwrite.Alert = true;
                        overwrite.Overwrites = true;
                        claimed.Add(destination);
                        continue;
                    }

                    var unique = OperationPaths.MakeUnique(destination, Taken);
                    var renamed = plan.Add(item, source, unique);
                    renamed.Note = "renamed to " + OperationPaths.FileName(unique);
                    claimed.Add(unique);
                    continue;
                }

                plan.Add(item, source, destination);
                claimed.Add(destination);
            }

            plan.Summary = Describe(plan.Operations.Count, skipped, sameFolder, intoSelf, targetRoot);

            if (plan.Operations.Count == 0) plan.Blocked = plan.Summary;

            return plan;
        }

        private static string Describe(int planned, int skipped, int sameFolder, int intoSelf,
            string targetRoot)
        {
            if (planned == 0 && intoSelf > 0) return "A folder cannot be moved inside itself.";

            if (planned == 0 && sameFolder > 0 && skipped == 0)
                return sameFolder == 1
                    ? "That asset is already in " + targetRoot + "."
                    : "Those " + sameFolder + " assets are already in " + targetRoot + ".";

            if (planned == 0) return "Nothing left to do — every row was skipped.";

            var text = planned + (planned == 1 ? " item → " : " items → ") + targetRoot;
            if (skipped > 0) text += " · " + skipped + " skipped";
            if (sameFolder > 0) text += " · " + sameFolder + " already there";
            if (intoSelf > 0) text += " · " + intoSelf + " would nest in itself";

            return text;
        }
    }
}
