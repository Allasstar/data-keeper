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
        public string Id => "copy";
        public string DisplayName => "Copy";

        public string Tooltip =>
            "Copy the selection to the other side. The copies get new GUIDs; references between "
            + "copied assets follow the copies, everything else keeps pointing at the originals. "
            + "Tick 'With dependencies' in the dialog to copy everything the selection uses.";

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
            context.Active.IsScene
                ? PlanSceneCopy(context)
                : AssetTransfer.Plan(context, AssetTransfer.DefaultOptions, false);

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSceneCopy(plan);
            else ExecuteAssetCopy(plan);
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
            var copies = new List<KeyValuePair<string, string>>(plan.Operations.Count);

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

                    if (AssetDatabase.CopyAsset(operation.Source, operation.Destination))
                        copies.Add(new KeyValuePair<string, string>(operation.Source, operation.Destination));
                    else
                        failures.Add(operation.Source + ": copy failed.");
                }
            });

            // Only after the batch is imported: a copy has no GUID to map to until then.
            if (GuidRemapper.IsSupported && copies.Count > 0)
            {
                var copiedFiles = new List<string>();
                var map = GuidRemapper.BuildMap(copies, copiedFiles);
                GuidRemapper.Apply(copiedFiles, map);
            }

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
