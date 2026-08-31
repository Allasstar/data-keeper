using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // MoveAssetToTrash, never DeleteAsset: the OS trash is the only undo an asset deletion has.
    // Before anything goes, the index's reverse map puts the inbound reference count on every
    // row — the question "is anything still using this" is the whole reason the index exists.
    public sealed class DeleteCommand : ICommanderCommand
    {
        private static readonly CommandShortcut[] Keys =
        {
            new CommandShortcut(KeyCode.Delete),
            new CommandShortcut(KeyCode.F8),
        };

        public string Id => "delete";
        public string DisplayName => "Delete";

        public string Tooltip =>
            "Del / F8 — send the selected assets to the OS trash, or destroy the selected scene "
            + "objects. Rows are annotated with how many assets still reference them.";

        public IReadOnlyList<CommandShortcut> Shortcuts => Keys;

        public bool CanExecute(CommanderContext context)
        {
            var active = context.Active;
            if (active.Count == 0) return false;

            return active.IsFolder ? active.SelectionIsAssets() : IsSceneSelection(active);
        }

        public OperationPlan Plan(CommanderContext context) =>
            context.Active.IsScene ? PlanSceneDelete(context) : PlanAssetDelete(context);

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSceneDelete(plan);
            else ExecuteAssetDelete(plan);
        }

        private static bool IsSceneSelection(CommanderSide side)
        {
            foreach (var item in side.Selection)
                if (!(item is GameObjectItem) && !(item is ComponentItem))
                    return false;

            return true;
        }

        private static OperationPlan PlanAssetDelete(CommanderContext context)
        {
            var plan = new OperationPlan("Delete", "Delete") { Context = context };

            int referenced = 0;

            foreach (var item in context.Active.SelectedAssetItems())
            {
                var row = plan.Add(item, item.AssetPath, null);

                int count = ProjectIndex.IsReady ? ProjectIndex.GetReferencedBy(item.Guid).Count : 0;
                if (count <= 0) continue;

                referenced++;
                row.Note = count == 1 ? "1 reference" : count + " references";
                row.Alert = true;
            }

            plan.Summary = plan.Operations.Count + " item(s) to the trash"
                           + (referenced > 0 ? " · " + referenced + " still referenced" : "");

            plan.Caveat = ProjectIndex.IsReady
                ? "Deleted assets go to the OS trash and can be restored from there."
                : "The project index is not ready, so reference counts are not shown.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to delete.";

            return plan;
        }

        private static OperationPlan PlanSceneDelete(CommanderContext context)
        {
            var gate = context.Active.EnsureSceneEditable();
            if (!context.Active.ReportSceneGate(gate, "Delete")) return null;

            var plan = new OperationPlan("Delete", "Delete") { Context = context };

            foreach (var item in context.Active.Selection)
            {
                var target = Target(item);
                if (target == null) continue;

                var row = plan.Add(item, item.Name, null);
                if (item is ComponentItem) row.Note = "component on " + target.name;
            }

            plan.Summary = plan.Operations.Count + " object(s) destroyed in " + context.Active.Scene.name;
            plan.Caveat = "Undoable with Ctrl+Z.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to delete.";

            return plan;
        }

        private static Object Target(ICommanderItem item)
        {
            if (item is GameObjectItem gameObjectItem) return gameObjectItem.GameObject;

            return (item as ComponentItem)?.Component;
        }

        private static void ExecuteAssetDelete(OperationPlan plan)
        {
            var paths = new List<string>(plan.Operations.Count);
            foreach (var operation in plan.Operations) paths.Add(operation.Source);

            var failures = new List<string>();
            AssetDatabase.MoveAssetsToTrash(paths.ToArray(), failures);

            AssetDatabase.Refresh();
            AssetOperations.ReportFailures("Could not delete", failures);
        }

        private static void ExecuteSceneDelete(OperationPlan plan)
        {
            int group = Undo.GetCurrentGroup();

            foreach (var operation in plan.Operations)
            {
                var target = Target(operation.Item);
                if (target != null) Undo.DestroyObjectImmediate(target);
            }

            Undo.SetCurrentGroupName("Delete");
            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(plan.Context.Active.Scene);
        }
    }
}
