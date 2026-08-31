using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Assets whose bytes are identical to another asset's. Grouped by the index's content hash,
    // which is what makes this a lookup rather than a comparison — the hashing already happened
    // during the build. Feeds the swap command in Phase 6.
    public sealed class DuplicatesMode : ICommanderMode
    {
        public string Id => CommanderModes.DuplicatesId;

        public string DisplayName => "Duplicates";

        public string Tooltip => "Assets with byte-identical content, grouped.";

        // Two scene objects being alike is a different question with a different answer; only
        // assets have a content hash.
        public bool Supports(SideKind kind) => kind == SideKind.Folder;

        public ModeResult Evaluate(ModeContext context)
        {
            var index = context.Index;
            var hits = new List<AssetRecord>();

            foreach (var record in ModeScope.RecordsUnder(index, context.Self.RootPath))
            {
                if (record.ContentHash == 0UL) continue;
                if (index.GetDuplicates(record.ContentHash).Count < 2) continue;

                hits.Add(record);
            }

            // Sorted by hash first so a group's members are adjacent rows; path breaks ties so
            // the order inside a group is stable between evaluations.
            hits.Sort(CompareGroupThenPath);

            var items = new List<ICommanderItem>(hits.Count);
            int groups = 0;
            ulong previous = 0UL;

            foreach (var record in hits)
            {
                if (record.ContentHash != previous)
                {
                    previous = record.ContentHash;
                    groups++;
                }

                var badge = Describe(record, index, context.Other);
                items.Add(ModeScope.ResultItem(record, context.Self.RootPath, badge));
            }

            var summary = $"{ModeScope.Plural(items.Count, "asset", "assets")} in "
                          + ModeScope.Plural(groups, "group", "groups");

            return new ModeResult(items, summary);
        }

        private static int CompareGroupThenPath(AssetRecord a, AssetRecord b)
        {
            int result = a.ContentHash.CompareTo(b.ContentHash);
            return result != 0 ? result : string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
        }

        // The count is the whole group including this record; whether a copy sits on the other
        // side is the part that turns a listing into something to act on.
        private static string Describe(AssetRecord record, IndexQuery index, SideContext other)
        {
            var group = index.GetDuplicates(record.ContentHash);
            var badge = group.Count + " copies";

            if (other.Kind != SideKind.Folder) return badge;

            foreach (var guid in group)
            {
                if (guid == record.Guid) continue;
                if (!index.TryGetByGuid(guid, out var twin)) continue;
                if (ModeScope.IsUnder(twin.Path, other.RootPath))
                    return badge + " · also in " + other.Id;
            }

            return badge;
        }
    }
}
