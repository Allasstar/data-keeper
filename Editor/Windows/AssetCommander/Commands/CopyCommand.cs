using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // A copy is a new asset with a new GUID by design: nothing that referenced the original will
    // follow it. That is the difference from Move and it is said out loud in the plan dialog.
    public sealed class CopyCommand : ICommanderCommand
    {
        private static readonly CommandShortcut[] Keys = { new CommandShortcut(KeyCode.F5) };

        public string Id => "copy";
        public string DisplayName => "Copy";

        public string Tooltip =>
            "F5 — copy the selection to the other side. The copies get new GUIDs; existing "
            + "references keep pointing at the originals.";

        public IReadOnlyList<CommandShortcut> Shortcuts => Keys;

        public bool CanExecute(CommanderContext context)
        {
            var active = context.Active;
            var other = context.Other;

            if (active.Count == 0) return false;
            if (active.IsFolder && other.IsFolder) return active.SelectionIsAssets();

            return active.IsScene && other.IsScene && other.HasScene
                   && active.Scene != other.Scene && active.SelectionIsSceneObjects();
        }

        public OperationPlan Plan(CommanderContext context) =>
            context.Active.IsScene ? PlanSceneCopy(context) : PlanAssetCopy(context, DefaultOptions);

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSceneCopy(plan);
            else ExecuteAssetCopy(plan);
        }

        private static PlanOptions DefaultOptions =>
            new PlanOptions(ConflictResolution.AutoRename, FolderStructure.KeepStructure);

        private static OperationPlan PlanAssetCopy(CommanderContext context, PlanOptions options)
        {
            var planner = new TransferPlanner(AssetOperations.Exists);

            // Copying into the source's own folder is legal — it is what Duplicate does — so the
            // same-folder rejection Move needs is off here.
            var plan = planner.Build(context.Active.SelectedAssetItems(), context.Active.RootPath,
                context.Other.FolderRoot, options, "Copy", "Copy", false);

            plan.Context = context;
            plan.Caveat = "Copies get new GUIDs. References to the originals are not redirected.";
            plan.Rebuild = rebuilt => PlanAssetCopy(context, rebuilt);

            return plan;
        }

        private static OperationPlan PlanSceneCopy(CommanderContext context)
        {
            var gate = context.Other.EnsureSceneEditable();
            if (!context.Other.ReportSceneGate(gate, "Copy")) return null;

            var plan = new OperationPlan("Copy", "Copy") { Context = context };
            var sceneName = context.Other.Scene.name;

            foreach (var item in context.Active.Selection)
            {
                if (!(item is GameObjectItem gameObjectItem) || gameObjectItem.GameObject == null) continue;

                plan.Add(item, gameObjectItem.GameObject.name, sceneName);
            }

            plan.Summary = plan.Operations.Count + " object(s) copied into scene " + sceneName;
            plan.Caveat = "Copies land at the scene root, not under the original's parent.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to copy.";

            return plan;
        }

        private static void ExecuteAssetCopy(OperationPlan plan)
        {
            var failures = new List<string>();

            AssetOperations.Run(() =>
            {
                foreach (var operation in plan.Operations)
                {
                    if (!AssetOperations.EnsureFolder(OperationPaths.Directory(operation.Destination)))
                    {
                        failures.Add(operation.Source + ": could not create the destination folder.");
                        continue;
                    }

                    if (operation.Overwrites) AssetDatabase.DeleteAsset(operation.Destination);

                    if (!AssetDatabase.CopyAsset(operation.Source, operation.Destination))
                        failures.Add(operation.Source + ": copy failed.");
                }
            });

            AssetOperations.ReportFailures("Copy failed", failures);
        }

        private static void ExecuteSceneCopy(OperationPlan plan)
        {
            var target = plan.Context.Other.Scene;
            int group = Undo.GetCurrentGroup();

            foreach (var operation in plan.Operations)
            {
                var source = (operation.Item as GameObjectItem)?.GameObject;
                if (source == null) continue;

                var clone = Object.Instantiate(source);
                clone.name = source.name;

                Undo.RegisterCreatedObjectUndo(clone, "Copy to Scene");
                Undo.MoveGameObjectToScene(clone, target, "Copy to Scene");
            }

            Undo.SetCurrentGroupName("Copy to Scene");
            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(target);
        }
    }
}
