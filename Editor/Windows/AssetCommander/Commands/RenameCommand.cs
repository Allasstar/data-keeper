using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Single and batch are the same command: the pattern field defaults to the one selected name,
    // so renaming one asset is typing over it, and renaming forty is typing a pattern. Either way
    // the dialog previews every resulting name before a single file is touched.
    public sealed class RenameCommand : ICommanderCommand
    {
        private static readonly CommandShortcut[] Keys = { new CommandShortcut(KeyCode.F2) };

        public string Id => "rename";
        public string DisplayName => "Rename";

        public string Tooltip =>
            "F2 — rename the selection. Use {name}, {n} and {n:000} to build a batch pattern.";

        public IReadOnlyList<CommandShortcut> Shortcuts => Keys;

        public bool CanExecute(CommanderContext context)
        {
            var active = context.Active;
            if (active.Count == 0) return false;

            return active.IsFolder ? active.SelectionIsAssets() : active.SelectionIsSceneObjects();
        }

        public OperationPlan Plan(CommanderContext context)
        {
            if (context.Active.IsScene)
            {
                var gate = context.Active.EnsureSceneEditable();
                if (!context.Active.ReportSceneGate(gate, "Rename")) return null;
            }

            return Build(context, new PlanOptions(ConflictResolution.AutoRename,
                FolderStructure.KeepStructure, DefaultPattern(context.Active)));
        }

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSceneRename(plan);
            else ExecuteAssetRename(plan);
        }

        // One selected item starts from its own name, which makes F2 behave like the Project
        // window's rename; anything more starts from the identity pattern.
        private static string DefaultPattern(CommanderSide side)
        {
            if (side.Count != 1) return "{name}";

            var item = side.Selection[0];
            return item.AssetPath != null ? OperationPaths.NameWithoutExtension(item.AssetPath) : item.Name;
        }

        private static OperationPlan Build(CommanderContext context, PlanOptions options)
        {
            var plan = new OperationPlan("Rename", "Rename")
            {
                Context = context,
                Options = options,
                PatternLabel = "Pattern",
                ShowConflictOption = context.Active.IsFolder,
            };

            plan.Rebuild = rebuilt => Build(context, rebuilt);

            if (context.Active.IsScene) BuildSceneRows(plan, context, options);
            else BuildAssetRows(plan, context, options);

            if (plan.Operations.Count == 0 && string.IsNullOrEmpty(plan.Blocked))
                plan.Blocked = "That pattern renames nothing.";

            return plan;
        }

        private static void BuildAssetRows(OperationPlan plan, CommanderContext context, PlanOptions options)
        {
            var claimed = new HashSet<string>(StringComparer.Ordinal);

            bool Taken(string path) => claimed.Contains(path) || AssetOperations.Exists(path);

            int index = 1;
            int skipped = 0;
            int unchanged = 0;

            foreach (var item in context.Active.SelectedAssetItems())
            {
                var source = item.AssetPath;
                var folder = OperationPaths.Directory(source);
                var extension = OperationPaths.Extension(source);

                var name = NamePattern.Apply(options.Pattern, OperationPaths.NameWithoutExtension(source), index++);
                if (string.IsNullOrEmpty(name)) continue;

                var destination = OperationPaths.Combine(folder, name + extension);

                if (destination == source)
                {
                    unchanged++;
                    claimed.Add(destination);
                    continue;
                }

                if (Taken(destination))
                {
                    if (options.Conflict == ConflictResolution.Skip)
                    {
                        skipped++;
                        continue;
                    }

                    if (options.Conflict == ConflictResolution.Overwrite)
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
                    renamed.Note = "name taken";
                    claimed.Add(unique);
                    continue;
                }

                plan.Add(item, source, destination);
                claimed.Add(destination);
            }

            plan.Summary = plan.Operations.Count + " renamed"
                           + (unchanged > 0 ? " · " + unchanged + " unchanged" : "")
                           + (skipped > 0 ? " · " + skipped + " skipped" : "");

            plan.Caveat = "Renaming an asset keeps its GUID, so every reference to it survives.";
        }

        private static void BuildSceneRows(OperationPlan plan, CommanderContext context, PlanOptions options)
        {
            int index = 1;
            int unchanged = 0;

            foreach (var item in context.Active.Selection)
            {
                if (!(item is GameObjectItem gameObjectItem) || gameObjectItem.GameObject == null) continue;

                var current = gameObjectItem.GameObject.name;
                var name = NamePattern.Apply(options.Pattern, current, index++);

                if (string.IsNullOrEmpty(name)) continue;

                if (name == current)
                {
                    unchanged++;
                    continue;
                }

                plan.Add(item, current, name);
            }

            plan.Summary = plan.Operations.Count + " renamed"
                           + (unchanged > 0 ? " · " + unchanged + " unchanged" : "");

            plan.Caveat = "Undoable with Ctrl+Z.";
        }

        private static void ExecuteAssetRename(OperationPlan plan)
        {
            var failures = new List<string>();

            AssetOperations.Run(() =>
            {
                foreach (var operation in plan.Operations)
                {
                    if (operation.Overwrites) AssetDatabase.DeleteAsset(operation.Destination);

                    // RenameAsset takes a bare name, not a path: the folder never changes here.
                    var name = OperationPaths.NameWithoutExtension(operation.Destination);
                    var error = AssetDatabase.RenameAsset(operation.Source, name);

                    if (!string.IsNullOrEmpty(error)) failures.Add(operation.Source + ": " + error);
                }
            });

            AssetOperations.ReportFailures("Rename failed", failures);
        }

        private static void ExecuteSceneRename(OperationPlan plan)
        {
            int group = Undo.GetCurrentGroup();

            foreach (var operation in plan.Operations)
            {
                var gameObject = (operation.Item as GameObjectItem)?.GameObject;
                if (gameObject == null) continue;

                Undo.RecordObject(gameObject, "Rename");
                gameObject.name = operation.Destination;
            }

            Undo.SetCurrentGroupName("Rename");
            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(plan.Context.Active.Scene);
        }
    }

    // {name} the original, {n} a 1-based counter, {n:000} the same counter zero-padded. Anything
    // else in the pattern is literal, which is how prefixes and suffixes are written.
    public static class NamePattern
    {
        public static string Apply(string pattern, string originalName, int index)
        {
            if (string.IsNullOrEmpty(pattern)) return originalName;

            var builder = new System.Text.StringBuilder(pattern.Length + 8);

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] != '{')
                {
                    builder.Append(pattern[i]);
                    continue;
                }

                int close = pattern.IndexOf('}', i + 1);
                if (close < 0)
                {
                    builder.Append(pattern[i]);
                    continue;
                }

                var token = pattern.Substring(i + 1, close - i - 1);
                builder.Append(Expand(token, originalName, index, pattern, i, close, out bool handled));

                if (handled) i = close;
            }

            return builder.ToString();
        }

        private static string Expand(string token, string originalName, int index, string pattern,
            int open, int close, out bool handled)
        {
            handled = true;

            if (token == "name") return originalName;
            if (token == "n") return index.ToString(CultureInfo.InvariantCulture);

            if (token.StartsWith("n:", StringComparison.Ordinal))
            {
                var format = token.Substring(2);
                return format.Length > 0 && IsAllZeroes(format)
                    ? index.ToString(format, CultureInfo.InvariantCulture)
                    : index.ToString(CultureInfo.InvariantCulture);
            }

            // An unknown token is left exactly as typed rather than silently dropped.
            handled = false;
            return pattern.Substring(open, 1);
        }

        private static bool IsAllZeroes(string format)
        {
            foreach (var character in format)
                if (character != '0')
                    return false;

            return true;
        }
    }
}
