using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The one command whose meaning is decided by which way the two sides point: a scene object
    // sent to a folder becomes a prefab asset, a prefab sent to a scene becomes an instance.
    // Same button, because to the user it is one gesture in two directions.
    public sealed class PrefabCommand : ICommanderCommand
    {
        private enum Direction
        {
            None = 0,
            Save = 1,
            Instantiate = 2,
        }

        public string Id => "prefab";

        public string DisplayName => "Prefab";

        public string Tooltip =>
            "Scene objects → folder side: save them as prefabs and connect the originals. "
            + "Prefabs → scene side: instantiate them.";

        public bool CanExecute(CommanderContext context) => Resolve(context) != Direction.None;

        public OperationPlan Plan(CommanderContext context)
        {
            switch (Resolve(context))
            {
                case Direction.Save: return PlanSave(context);
                case Direction.Instantiate: return PlanInstantiate(context);
                default:
                    return OperationPlan.Rejected("Prefab",
                        "Point one side at a folder and the other at a scene, then select either "
                        + "scene objects or prefabs.");
            }
        }

        public void Execute(OperationPlan plan)
        {
            if (plan.Context.Active.IsScene) ExecuteSave(plan);
            else ExecuteInstantiate(plan);
        }

        private static Direction Resolve(CommanderContext context)
        {
            var active = context.Active;
            var other = context.Other;

            if (active.Count == 0) return Direction.None;

            if (active.IsScene && other.IsFolder && active.SelectionIsSceneObjects())
                return Direction.Save;

            if (active.IsFolder && other.IsScene && other.HasScene && HasOnlyPrefabs(active))
                return Direction.Instantiate;

            return Direction.None;
        }

        private static bool HasOnlyPrefabs(CommanderSide side)
        {
            foreach (var item in side.Selection)
                if (item.AssetPath == null ||
                    !item.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    return false;

            return true;
        }

        private static OperationPlan PlanSave(CommanderContext context)
        {
            var gate = context.Active.EnsureSceneEditable();
            if (!context.Active.ReportSceneGate(gate, "Prefab")) return null;

            var plan = new OperationPlan("Save as Prefab", "Save") { Context = context };
            var folder = context.Other.FolderRoot;
            var claimed = new HashSet<string>(StringComparer.Ordinal);

            bool Taken(string path) => claimed.Contains(path) || AssetOperations.Exists(path);

            foreach (var item in context.Active.Selection)
            {
                if (!(item is GameObjectItem gameObjectItem) || gameObjectItem.GameObject == null) continue;

                var destination = OperationPaths.MakeUnique(
                    OperationPaths.Combine(folder, gameObjectItem.GameObject.name + ".prefab"), Taken);

                claimed.Add(destination);
                plan.Add(item, gameObjectItem.GameObject.name, destination);
            }

            plan.Summary = plan.Operations.Count + " prefab(s) → " + folder;
            plan.Caveat = "The scene objects become instances of the new prefabs.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to save.";

            return plan;
        }

        private static OperationPlan PlanInstantiate(CommanderContext context)
        {
            var gate = context.Other.EnsureSceneEditable();
            if (!context.Other.ReportSceneGate(gate, "Prefab")) return null;

            var parent = ParentFor(context.Other);
            var target = parent == null ? "scene " + context.Other.Scene.name : parent.name;

            var plan = new OperationPlan("Instantiate Prefab", "Instantiate") { Context = context };

            foreach (var item in context.Active.SelectedAssetItems())
                plan.Add(item, item.Name, target);

            plan.Summary = plan.Operations.Count + " instance(s) → " + target;
            plan.Caveat = parent == null
                ? "Instances land at the scene root. Undoable with Ctrl+Z."
                : "Undoable with Ctrl+Z.";

            if (plan.Operations.Count == 0) plan.Blocked = "Nothing to instantiate.";

            return plan;
        }

        // A single GameObject selected on the scene side is the command-bar stand-in for the drop
        // target a drag would have had.
        private static GameObject ParentFor(CommanderSide side) =>
            side.Count == 1 && side.Selection[0] is GameObjectItem item ? item.GameObject : null;

        private static void ExecuteSave(OperationPlan plan)
        {
            var failures = new List<string>();
            int group = Undo.GetCurrentGroup();

            AssetOperations.EnsureFolder(plan.Context.Other.FolderRoot);

            foreach (var operation in plan.Operations)
            {
                var gameObject = (operation.Item as GameObjectItem)?.GameObject;
                if (gameObject == null) continue;

                var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, operation.Destination,
                    InteractionMode.UserAction);

                if (saved == null) failures.Add(operation.Source + ": could not be saved as a prefab.");
            }

            Undo.SetCurrentGroupName("Save as Prefab");
            Undo.CollapseUndoOperations(group);

            AssetDatabase.Refresh();
            AssetOperations.ReportFailures("Save as Prefab failed", failures);
        }

        private static void ExecuteInstantiate(OperationPlan plan)
        {
            var scene = plan.Context.Other.Scene;
            var parent = ParentFor(plan.Context.Other);
            var failures = new List<string>();
            int group = Undo.GetCurrentGroup();

            foreach (var operation in plan.Operations)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(operation.Item.AssetPath);
                if (asset == null)
                {
                    failures.Add(operation.Item.AssetPath + ": could not be loaded.");
                    continue;
                }

                var instance = parent != null
                    ? PrefabUtility.InstantiatePrefab(asset, parent.transform) as GameObject
                    : PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;

                if (instance == null)
                {
                    failures.Add(operation.Item.AssetPath + ": could not be instantiated.");
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            }

            Undo.SetCurrentGroupName("Instantiate Prefab");
            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetOperations.ReportFailures("Instantiate failed", failures);
        }
    }
}
