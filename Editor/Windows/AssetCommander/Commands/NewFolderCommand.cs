using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The one command that creates rather than moves. It runs through the same plan dialog as
    // everything else so the name is typed and previewed in one place, and so the command bar
    // has no special case in it.
    public sealed class NewFolderCommand : ICommanderCommand
    {
        private const string DefaultName = "New Folder";

        public string Id => "new-folder";
        public string DisplayName => "New Folder";
        public string Tooltip => "Create a folder inside the active side's root.";

        public bool CanExecute(CommanderContext context) => context.Active.IsFolder;

        public OperationPlan Plan(CommanderContext context) =>
            Build(context, new PlanOptions(ConflictResolution.AutoRename,
                FolderStructure.KeepStructure, DefaultName));

        public void Execute(OperationPlan plan)
        {
            var failures = new List<string>();

            foreach (var operation in plan.Operations)
                if (!AssetOperations.EnsureFolder(operation.Destination))
                    failures.Add(operation.Destination + ": could not be created.");

            AssetDatabase.Refresh();
            AssetOperations.ReportFailures("New Folder failed", failures);
        }

        private static OperationPlan Build(CommanderContext context, PlanOptions options)
        {
            var plan = new OperationPlan("New Folder", "Create")
            {
                Context = context,
                Options = options,
                PatternLabel = "Name",
            };

            plan.Rebuild = rebuilt => Build(context, rebuilt);

            var name = (options.Pattern ?? "").Trim();
            if (name.Length == 0)
            {
                plan.Blocked = "Type a folder name.";
                return plan;
            }

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                plan.Blocked = "That name contains characters a folder cannot have.";
                return plan;
            }

            var destination = OperationPaths.Combine(context.Active.RootPath, name);
            var unique = OperationPaths.MakeUnique(destination, AssetOperations.Exists);

            var row = plan.Add(null, context.Active.RootPath, unique);
            if (unique != destination) row.Note = "name taken";

            plan.Summary = "Creates " + unique;

            return plan;
        }
    }
}
