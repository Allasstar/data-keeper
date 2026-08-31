using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Assets nothing points at and nothing can reach. "Nothing points at it" alone is not
    // enough — an asset used only by another dead asset still has a referrer — so the answer
    // is the intersection of an empty reverse map and unreachability from the project's roots.
    public sealed class UnusedAssetsMode : ICommanderMode
    {
        public const string Caveat =
            "Only references stored as guids can be seen. Anything loaded by name or path at "
            + "runtime — Resources.Load, Addressables by key, reflection, code that news a type "
            + "up — is invisible here. Scripts are never listed for that reason. Confirm before "
            + "deleting.";

        private static readonly string[] RootFolderMarkers =
        {
            "/Resources/",
            "/StreamingAssets/",
            "/Editor Default Resources/",
        };

        private readonly Func<IndexQuery, IEnumerable<string>> _roots;

        public UnusedAssetsMode() : this(null)
        {
        }

        // The root set is the one part of this mode that is not a pure index lookup — it reads
        // build settings and, if the package is there, Addressables. Injected so a test can pin
        // it to a known set of guids.
        public UnusedAssetsMode(Func<IndexQuery, IEnumerable<string>> roots)
        {
            _roots = roots ?? ProjectRoots;
        }

        public string Id => CommanderModes.UnusedId;

        public string DisplayName => "Unused";

        public string Tooltip => "Assets no build scene, Resources folder or other asset can reach.";

        // A scene object is not an asset; whether the scene itself is used is a question for the
        // folder side that holds it.
        public bool Supports(SideKind kind) => kind == SideKind.Folder;

        public ModeResult Evaluate(ModeContext context)
        {
            var index = context.Index;
            var reachable = Reach(index, _roots(index));
            var items = new List<ICommanderItem>();

            foreach (var record in ModeScope.RecordsUnder(index, context.Self.RootPath))
            {
                // A script is reachable through code this index cannot read, so listing one as
                // unused would be a guess dressed up as a finding.
                if (record.Kind == AssetKind.Script) continue;
                if (reachable.Contains(record.Guid)) continue;
                if (index.GetReferencedBy(record.Guid).Count > 0) continue;

                items.Add(ModeScope.ResultItem(record, context.Self.RootPath, "unused"));
            }

            ModeScope.SortByPath(items);

            return new ModeResult(items, ModeScope.Plural(items.Count, "unused asset", "unused assets"),
                Caveat);
        }

        // Breadth-first over the forward dependency map. Every asset a root can reach, however
        // indirectly, is in use — a material nobody references directly is still alive if the
        // prefab in a build scene uses it.
        public static HashSet<string> Reach(IndexQuery index, IEnumerable<string> roots)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>();

            foreach (var guid in roots)
            {
                if (guid != null && reachable.Add(guid)) pending.Push(guid);
            }

            while (pending.Count > 0)
            {
                if (!index.TryGetByGuid(pending.Pop(), out var record)) continue;

                foreach (var dependency in record.DependencyGuids)
                {
                    if (reachable.Add(dependency)) pending.Push(dependency);
                }
            }

            return reachable;
        }

        private static IEnumerable<string> ProjectRoots(IndexQuery index)
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                if (index.TryGetByPath(scene.path, out var record)) yield return record.Guid;
            }

            foreach (var record in index.Records)
            {
                if (record.Kind == AssetKind.Folder) continue;
                if (IsInRootFolder(record.Path)) yield return record.Guid;
            }

            foreach (var guid in AddressableGuids()) yield return guid;
        }

        private static bool IsInRootFolder(string path)
        {
            foreach (var marker in RootFolderMarkers)
                if (path.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            return false;
        }

        // Reached by reflection rather than by an assembly reference: com.unity.addressables is
        // optional, and an asmdef reference to a package the consumer may not have installed
        // would stop the whole editor assembly from compiling. An addressable entry stores its
        // target in a plain string field, so the guid scanner cannot see it either — this is the
        // only way these roots are found at all.
        private static IEnumerable<string> AddressableGuids()
        {
            var settings = AddressableSettings();
            if (settings == null) yield break;

            var groups = settings.GetType().GetProperty("groups")?.GetValue(settings) as IEnumerable;
            if (groups == null) yield break;

            foreach (var group in groups)
            {
                if (group == null) continue;

                var entries = group.GetType().GetProperty("entries")?.GetValue(group) as IEnumerable;
                if (entries == null) continue;

                foreach (var entry in entries)
                {
                    var guid = entry?.GetType().GetProperty("guid")?.GetValue(entry) as string;
                    if (!string.IsNullOrEmpty(guid)) yield return guid;
                }
            }
        }

        private static object AddressableSettings()
        {
            var type = Type.GetType(
                "UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");

            // The property allocates a settings object when asked to; the read-only overload is
            // the one that answers "is Addressables actually set up here".
            var getter = type?.GetMethod("GetSettings", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(bool) }, null);

            return getter?.Invoke(null, new object[] { false });
        }
    }
}
