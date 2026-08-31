using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public enum CommanderItemKind : byte
    {
        Folder = 0,
        Asset = 1,
        Scene = 2,
        GameObject = 3,
        Component = 4,
        Placeholder = 5,
    }

    public interface ICommanderItem
    {
        int Id { get; }
        string Name { get; }
        string SubLabel { get; }
        Texture Icon { get; }
        CommanderItemKind Kind { get; }
        string Guid { get; }
        string AssetPath { get; }
        GlobalObjectId? SceneId { get; }
        bool HasChildren { get; }
        long Size { get; }
        long ModifiedTicks { get; }
    }

    // Ids have to outlive a rebuild: expansion and selection are restored by id, and a counter
    // that restarted per rebuild would drop both. Keys carry a kind prefix so a scene object
    // (Phase 4) can never land on the same id as an asset path.
    public static class CommanderItemIds
    {
        private static readonly Dictionary<string, int> Ids = new Dictionary<string, int>(StringComparer.Ordinal);

        private static int _next = 1;

        public static int For(string key)
        {
            if (Ids.TryGetValue(key, out var id)) return id;

            id = _next++;
            Ids[key] = id;
            return id;
        }

        public static int ForAsset(string path) => For("a:" + path);

        public static int ForPlaceholder(string parentPath) => For("p:" + parentPath);
    }
}
