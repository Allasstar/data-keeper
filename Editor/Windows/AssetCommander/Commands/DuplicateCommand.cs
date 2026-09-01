using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Copy in place. Unity's own GenerateUniqueAssetPath names the result, so a duplicate made
    // here is indistinguishable from one made in the Project window.
    public sealed class DuplicateCommand : ICommanderCommand
    {
        public string Id => "duplicate";
        public string DisplayName => "Duplicate";

        public string Tooltip =>
            "Duplicate the selection in place. Asset duplicates get new GUIDs; scene "
            + "duplicates land under the same parent.";

        public bool CanExecute(CommanderContext context)
        {
            var active = context.Active;
            if (active.Count == 0) return false;

            return active.IsFolder ? active.SelectionIsAssets() : active.SelectionIsSceneObjects();
        }

        public OperationPlan Plan(CommanderContext context) =>
            context.Active.IsScene ? PlanSceneDuplicate(context) : PlanAssetDuplicate(context);

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSceneDuplicate(plan);
            else ExecuteAssetDuplicate(plan);
        }

        private static OperationPlan PlanAssetDuplicate(CommanderContext context)
        {
            var plan = new OperationPlan("Duplicate", "Duplicate") { Context = context };

            // GenerateUniqueAssetPath only knows what is on disk, so earlier rows of this plan
            // are held back by hand — two duplicates of the same asset would otherwise collide.
            var claimed = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (var item in context.Active.SelectedAssetItems())
            {
                var destination = AssetDatabase.GenerateUniqueAssetPath(item.AssetPath);
                if (claimed.Contains(destination))
                    destination = OperationPaths.MakeUnique(destination,
                        path => claimed.Contains(path) || AssetOperations.Exists(path));

                claimed.Add(destination);
                plan.Add(item, item.AssetPath, destination);
            }

            plan.Summary = plan.Operations.Count + " duplicate(s)";
            plan.Caveat = "Duplicates get new GUIDs. Nothing that referenced the original follows.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to duplicate.";

            return plan;
        }

        private static OperationPlan PlanSceneDuplicate(CommanderContext context)
        {
            var gate = context.Active.EnsureSceneEditable();
            if (!context.Active.ReportSceneGate(gate, "Duplicate")) return null;

            var plan = new OperationPlan("Duplicate", "Duplicate") { Context = context };

            foreach (var item in context.Active.Selection)
            {
                if (!(item is GameObjectItem gameObjectItem) || gameObjectItem.GameObject == null) continue;

                var gameObject = gameObjectItem.GameObject;
                var parent = gameObject.transform.parent;

                plan.Add(item, gameObject.name, parent == null ? "scene root" : parent.name);
            }

            plan.Summary = plan.Operations.Count + " duplicate(s) in " + context.Active.Scene.name;
            plan.Caveat = "Undoable with Ctrl+Z.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to duplicate.";

            return plan;
        }

        private static void ExecuteAssetDuplicate(OperationPlan plan)
        {
            var failures = new List<string>();

            AssetOperations.Run(() =>
            {
                foreach (var operation in plan.Operations)
                    if (!AssetDatabase.CopyAsset(operation.Source, operation.Destination))
                        failures.Add(operation.Source + ": duplicate failed.");
            });

            AssetOperations.ReportFailures("Duplicate failed", failures);
        }

        private static void ExecuteSceneDuplicate(OperationPlan plan)
        {
            int group = Undo.GetCurrentGroup();

            foreach (var operation in plan.Operations)
            {
                var source = (operation.Item as GameObjectItem)?.GameObject;
                if (source == null) continue;

                var parent = source.transform.parent;
                var clone = Object.Instantiate(source, parent);

                clone.name = GameObjectUtility.GetUniqueNameForSibling(parent, source.name);

                // Instantiating a root-level object drops it into the *active* scene, which is
                // not necessarily the one this side is showing.
                if (parent == null && clone.scene != source.scene)
                    SceneManager.MoveGameObjectToScene(clone, source.scene);

                clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

                Undo.RegisterCreatedObjectUndo(clone, "Duplicate");
            }

            Undo.SetCurrentGroupName("Duplicate");
            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(plan.Context.Active.Scene);
        }
    }
}
