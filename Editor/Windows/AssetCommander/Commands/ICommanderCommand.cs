using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Why a scene side could not be edited. A promoted side is not simply "ready": promotion
    // closes the preview scene and reopens the file, so every GameObject the selection was
    // holding is destroyed and the command has to be re-issued against the new objects.
    public enum SceneGate
    {
        Ready = 0,
        Cancelled = 1,
        Reopened = 2,
    }

    // One side as a command sees it: flattened out of SidePanelView the same way SideContext is
    // flattened out of SidePanelState, so a command can be handed a made-up side with no window
    // behind it. The two delegates are the only things a command is allowed to do to the panel.
    public sealed class CommanderSide
    {
        public static readonly CommanderSide None =
            new CommanderSide(SideId.A, SideKind.None, "", Array.Empty<ICommanderItem>());

        private readonly Func<bool> _promoteScene;
        private readonly Action _refresh;

        public CommanderSide(SideId id, SideKind kind, string rootPath,
            IReadOnlyList<ICommanderItem> selection, Scene scene = default,
            bool isPreviewScene = false, Func<bool> promoteScene = null, Action refresh = null)
        {
            Id = id;
            Kind = kind;
            RootPath = rootPath ?? "";
            Selection = selection ?? Array.Empty<ICommanderItem>();
            Scene = scene;
            IsPreviewScene = isPreviewScene;
            _promoteScene = promoteScene;
            _refresh = refresh;
        }

        public SideId Id { get; }
        public SideKind Kind { get; }
        public string RootPath { get; }
        public IReadOnlyList<ICommanderItem> Selection { get; }
        public Scene Scene { get; }
        public bool IsPreviewScene { get; }

        public bool IsFolder => Kind == SideKind.Folder;
        public bool IsScene => Kind == SideKind.Scene;
        public bool HasScene => IsScene && Scene.IsValid() && Scene.isLoaded;
        public int Count => Selection.Count;

        public string FolderRoot => IsFolder ? RootPath : null;

        public void Refresh() => _refresh?.Invoke();

        // The gate every mutating command runs before touching a scene item. A preview-backed
        // side is read-only by design, and an Undo entry against a scene nobody can see is not
        // an edit anyone could review.
        public SceneGate EnsureSceneEditable()
        {
            if (!IsScene) return SceneGate.Ready;
            if (!IsPreviewScene) return HasScene ? SceneGate.Ready : SceneGate.Cancelled;

            return _promoteScene != null && _promoteScene() ? SceneGate.Reopened : SceneGate.Cancelled;
        }

        public bool ReportSceneGate(SceneGate gate, string title)
        {
            if (gate == SceneGate.Ready) return true;

            if (gate == SceneGate.Reopened)
                EditorUtility.DisplayDialog(title,
                    "The scene is now open in the Hierarchy. Its objects were reloaded, so the "
                    + "selection no longer points at them.\n\nRe-select and run the command again.",
                    "OK");

            return false;
        }

        // ── Selection views the commands ask for ────────────────────────────────────────────

        public bool SelectionIsAssets()
        {
            if (Selection.Count == 0) return false;

            foreach (var item in Selection)
                if (item.AssetPath == null)
                    return false;

            return true;
        }

        public bool SelectionIsSceneObjects()
        {
            if (Selection.Count == 0) return false;

            foreach (var item in Selection)
                if (!(item is GameObjectItem))
                    return false;

            return true;
        }

        public List<string> SelectedAssetPaths()
        {
            var paths = new List<string>(Selection.Count);

            foreach (var item in Selection)
                if (!string.IsNullOrEmpty(item.AssetPath))
                    paths.Add(item.AssetPath);

            return paths;
        }

        public List<ICommanderItem> SelectedAssetItems()
        {
            var items = new List<ICommanderItem>(Selection.Count);

            foreach (var item in Selection)
                if (!string.IsNullOrEmpty(item.AssetPath))
                    items.Add(item);

            return items;
        }

        public List<GameObject> SelectedGameObjects()
        {
            var objects = new List<GameObject>(Selection.Count);

            foreach (var item in Selection)
                if (item is GameObjectItem gameObjectItem && gameObjectItem.GameObject != null)
                    objects.Add(gameObjectItem.GameObject);

            return objects;
        }
    }

    public readonly struct CommanderContext
    {
        public readonly CommanderSide Active;
        public readonly CommanderSide Other;

        public CommanderContext(CommanderSide active, CommanderSide other)
        {
            Active = active ?? CommanderSide.None;
            Other = other ?? CommanderSide.None;
        }

        public IReadOnlyList<ICommanderItem> Selection => Active?.Selection ?? Array.Empty<ICommanderItem>();

        public bool HasSelection => Active != null && Active.Count > 0;

        public void RefreshBoth()
        {
            Active?.Refresh();
            Other?.Refresh();
        }
    }

    public interface ICommanderCommand
    {
        string Id { get; }
        string DisplayName { get; }
        string Tooltip { get; }

        bool CanExecute(CommanderContext context);

        OperationPlan Plan(CommanderContext context);

        void Execute(OperationPlan plan);
    }

    public static class CommanderCommands
    {
        // Ordered the way the command bar reads left to right, which is the Total Commander
        // function-key order rather than alphabetical.
        private static readonly ICommanderCommand[] Registry =
        {
            new RenameCommand(),
            new CopyCommand(),
            new MoveCommand(),
            new NewFolderCommand(),
            new DeleteCommand(),
            new DuplicateCommand(),
            new SwapCommand(),
            new PrefabCommand(),
        };

        public static IReadOnlyList<ICommanderCommand> All => Registry;

        public static ICommanderCommand Get(string id)
        {
            foreach (var command in Registry)
                if (command.Id == id)
                    return command;

            return null;
        }
    }
}
