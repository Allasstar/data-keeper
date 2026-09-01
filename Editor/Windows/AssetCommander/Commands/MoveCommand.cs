using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Folder → folder is AssetDatabase.MoveAsset, which keeps the GUID, so every reference to the
    // moved asset survives — that is the whole reason a move is not a copy-then-delete.
    public sealed class MoveCommand : ICommanderCommand
    {
        public string Id => "move";
        public string DisplayName => "Move";

        public string Tooltip =>
            "Move the selection to the other side. Assets keep their GUID, so references survive.";

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
            context.Active.IsScene ? PlanSceneMove(context) : PlanAssetMove(context, DefaultOptions);

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSceneMove(plan);
            else ExecuteAssetMove(plan);
        }

        private static PlanOptions DefaultOptions =>
            new PlanOptions(ConflictResolution.AutoRename, FolderStructure.KeepStructure);

        private static OperationPlan PlanAssetMove(CommanderContext context, PlanOptions options)
        {
            var planner = new TransferPlanner(AssetOperations.Exists);
            var plan = planner.Build(context.Active.SelectedAssetItems(), context.Active.RootPath,
                context.Other.FolderRoot, options, "Move", "Move", true);

            plan.Context = context;
            plan.Rebuild = rebuilt => PlanAssetMove(context, rebuilt);

            return plan;
        }

        private static OperationPlan PlanSceneMove(CommanderContext context)
        {
            var gate = context.Active.EnsureSceneEditable();
            if (!context.Active.ReportSceneGate(gate, "Move")) return null;

            gate = context.Other.EnsureSceneEditable();
            if (!context.Other.ReportSceneGate(gate, "Move")) return null;

            var plan = new OperationPlan("Move", "Move") { Context = context };
            var sceneName = context.Other.Scene.name;

            foreach (var item in context.Active.Selection)
            {
                if (!(item is GameObjectItem gameObjectItem)) continue;

                var gameObject = gameObjectItem.GameObject;
                if (gameObject == null) continue;

                var row = plan.Add(item, gameObject.name, sceneName);

                // A moved object is detached from its parent first, which is a structural change
                // the row list has to admit to before it happens.
                if (gameObject.transform.parent != null) row.Note = "unparented";
            }

            plan.Summary = plan.Operations.Count + " object(s) → scene " + sceneName;
            plan.Caveat = "Moving an object between scenes marks both scenes dirty.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to move.";

            return plan;
        }

        private static void ExecuteAssetMove(OperationPlan plan)
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

                    var error = AssetDatabase.MoveAsset(operation.Source, operation.Destination);
                    if (!string.IsNullOrEmpty(error)) failures.Add(operation.Source + ": " + error);
                }
            });

            AssetOperations.ReportFailures("Move failed", failures);
        }

        private static void ExecuteSceneMove(OperationPlan plan)
        {
            var target = plan.Context.Other.Scene;
            int group = Undo.GetCurrentGroup();

            foreach (var operation in plan.Operations)
            {
                var gameObject = (operation.Item as GameObjectItem)?.GameObject;
                if (gameObject == null) continue;

                // MoveGameObjectToScene only accepts roots, so a child is detached first — both
                // steps go into the same undo group.
                if (gameObject.transform.parent != null)
                    Undo.SetTransformParent(gameObject.transform, null, "Move to Scene");

                Undo.MoveGameObjectToScene(gameObject, target, "Move to Scene");
            }

            Undo.SetCurrentGroupName("Move to Scene");
            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(target);
            EditorSceneManager.MarkSceneDirty(plan.Context.Active.Scene);
        }
    }
}
