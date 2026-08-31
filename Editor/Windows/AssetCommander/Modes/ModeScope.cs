using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Shared plumbing for the analysis modes: which records a side owns, and how one becomes a
    // result row. Every mode answers about the whole subtree under its side's root, not just
    // the level the user happens to be looking at, so all of them start here.
    public static class ModeScope
    {
        public static bool IsUnder(string path, string root)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(root)) return false;
            if (path.Length == root.Length) return string.Equals(path, root, StringComparison.Ordinal);

            return path.Length > root.Length
                   && path[root.Length] == '/'
                   && path.StartsWith(root, StringComparison.Ordinal);
        }

        // Folders carry no content of their own — they exist in the index so a move can be
        // tracked, and every mode would otherwise report them as unused, duplicate or unhashed.
        public static IEnumerable<AssetRecord> RecordsUnder(IndexQuery index, string root)
        {
            foreach (var record in index.Records)
            {
                if (record.Kind == AssetKind.Folder) continue;
                if (!IsUnder(record.Path, root)) continue;

                yield return record;
            }
        }

        public static HashSet<string> GuidsUnder(IndexQuery index, string root)
        {
            var guids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in RecordsUnder(index, root)) guids.Add(record.Guid);

            return guids;
        }

        public static AssetItem ResultItem(AssetRecord record, string root, string badge, bool alert = false)
        {
            var item = new AssetItem(record.Path, false, false, record.Size, record.LastWriteTicks);
            item.SetSubLabel(Location(record, root));
            item.SetBadge(badge, alert);
            return item;
        }

        // "Material · Props/Metal" — the kind alone is already in the list view's Type column,
        // but the tree view has only this one line to say where in the subtree the hit was.
        public static string Location(AssetRecord record, string root)
        {
            var kind = record.Kind == AssetKind.Unknown ? "File" : record.Kind.ToString();
            var directory = DirectoryOf(record.Path);

            if (IsUnder(directory, root))
            {
                if (directory.Length == root.Length) return kind;
                directory = directory.Substring(root.Length + 1);
            }

            return directory.Length == 0 ? kind : kind + " · " + directory;
        }

        public static string DirectoryOf(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash < 0 ? "" : path.Substring(0, slash);
        }

        public static string NameOf(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        // Result sets are ordered by path so that assets from the same folder sit together;
        // the list view's column sorting takes over from there if the user wants otherwise.
        public static void SortByPath(List<ICommanderItem> items) =>
            items.Sort((a, b) => string.Compare(a.AssetPath, b.AssetPath, StringComparison.OrdinalIgnoreCase));

        public static string Plural(int count, string singular, string plural) =>
            count == 1 ? "1 " + singular : count.ToString("N0") + " " + plural;
    }
}
