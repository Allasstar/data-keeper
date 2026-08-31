using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public sealed class AssetItem : ICommanderItem
    {
        private string _guid;

        public AssetItem(string assetPath, bool isFolder, bool hasChildren, long size, long modifiedTicks)
        {
            AssetPath = assetPath;
            Id = CommanderItemIds.ForAsset(assetPath);
            Name = NameOf(assetPath);
            HasChildren = hasChildren;
            Size = size;
            ModifiedTicks = modifiedTicks;
            SubLabel = TypeLabel(assetPath, isFolder);

            Kind = isFolder
                ? CommanderItemKind.Folder
                : assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                    ? CommanderItemKind.Scene
                    : CommanderItemKind.Asset;
        }

        public int Id { get; }
        public string Name { get; }
        public string SubLabel { get; }
        public CommanderItemKind Kind { get; }
        public string AssetPath { get; }
        public bool HasChildren { get; }
        public long Size { get; }
        public long ModifiedTicks { get; }

        public GlobalObjectId? SceneId => null;

        // Asset badges are the analysis modes' business (Phase 5); the row already knows how to
        // draw one.
        public string Badge => null;
        public bool BadgeIsAlert => false;

        // Pulled per bind rather than stored on the item: AssetDatabase already caches icons,
        // and materialising 10k of them at build time is the exact cost the lazy tree avoids.
        public Texture Icon => AssetDatabase.GetCachedIcon(AssetPath);

        public string Guid => _guid ??= AssetDatabase.AssetPathToGUID(AssetPath);

        public string SizeLabel => Kind == CommanderItemKind.Folder ? "" : FormatSize(Size);

        public string ModifiedLabel => ModifiedTicks <= 0
            ? ""
            : new DateTime(ModifiedTicks, DateTimeKind.Utc).ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024L * 1024L) return (bytes / 1024f).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
            if (bytes < 1024L * 1024L * 1024L)
                return (bytes / (1024f * 1024f)).ToString("0.#", CultureInfo.InvariantCulture) + " MB";

            return (bytes / (1024f * 1024f * 1024f)).ToString("0.##", CultureInfo.InvariantCulture) + " GB";
        }

        private static string NameOf(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        private static string TypeLabel(string path, bool isFolder)
        {
            if (isFolder) return "Folder";

            var kind = AssetKinds.FromPath(path);
            if (kind != AssetKind.Unknown) return kind.ToString();

            int dot = path.LastIndexOf('.');
            return dot < 0 || dot == path.Length - 1
                ? "File"
                : path.Substring(dot + 1).ToUpperInvariant();
        }
    }

    // Stands in for a folder's unread contents so the expand arrow appears before anything is
    // read from disk; SidePanelView swaps it for the real children on the expand event.
    public sealed class PlaceholderItem : ICommanderItem
    {
        public PlaceholderItem(string parentPath)
        {
            Id = CommanderItemIds.ForPlaceholder(parentPath);
        }

        public int Id { get; }
        public string Name => "…";
        public string SubLabel => "";
        public Texture Icon => null;
        public CommanderItemKind Kind => CommanderItemKind.Placeholder;
        public string Guid => null;
        public string AssetPath => null;
        public GlobalObjectId? SceneId => null;
        public bool HasChildren => false;
        public long Size => 0;
        public long ModifiedTicks => 0;
        public string Badge => null;
        public bool BadgeIsAlert => false;
    }
}
