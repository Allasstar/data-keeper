using System;
using System.Collections.Generic;
using UnityEditor;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Where the dependency walk reads its edges from. The index answers without touching disk;
    // the AssetDatabase fallback exists so a transfer issued while the index is still building
    // does not silently lose its dependencies.
    public interface IDependencySource
    {
        IEnumerable<string> DirectDependencies(string assetPath);
        IEnumerable<string> FilesUnder(string folder);
    }

    public sealed class IndexDependencySource : IDependencySource
    {
        private readonly IndexQuery _index;

        public IndexDependencySource(IndexQuery index)
        {
            _index = index;
        }

        public IEnumerable<string> DirectDependencies(string assetPath)
        {
            if (!_index.TryGetByPath(assetPath, out var record)) yield break;

            foreach (var guid in record.DependencyGuids)
                if (_index.TryGetByGuid(guid, out var dependency) && dependency.Kind != AssetKind.Folder)
                    yield return dependency.Path;
        }

        public IEnumerable<string> FilesUnder(string folder)
        {
            foreach (var record in ModeScope.RecordsUnder(_index, folder))
                yield return record.Path;
        }
    }

    public sealed class AssetDatabaseDependencySource : IDependencySource
    {
        public IEnumerable<string> DirectDependencies(string assetPath)
        {
            foreach (var path in AssetDatabase.GetDependencies(assetPath, false))
                if (path != assetPath && !AssetDatabase.IsValidFolder(path))
                    yield return path;
        }

        public IEnumerable<string> FilesUnder(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(path)) yield return path;
            }
        }
    }

    public sealed class DependencySet
    {
        public static readonly DependencySet Empty = new DependencySet();

        // What the selection needs that the selection does not already carry, sorted by path.
        public readonly List<string> Paths = new List<string>();

        // Everything that travels: the selection, the contents of selected folders, and Paths.
        // A referrer inside this set is not an outside user of a dependency.
        public readonly HashSet<string> Members = new HashSet<string>(StringComparer.Ordinal);

        public int LeftCode;
        public int LeftPackages;
    }

    // The transitive closure of what a selection references — materials, textures, meshes,
    // animator controllers, nested prefabs and a variant's base prefab all arrive as plain
    // dependency edges, so nothing here special-cases prefabs.
    public static class DependencyCollector
    {
        public static IDependencySource CurrentSource() =>
            ProjectIndex.IsReady
                ? new IndexDependencySource(ProjectIndex.Query)
                : (IDependencySource)new AssetDatabaseDependencySource();

        public static DependencySet Collect(IDependencySource source, IReadOnlyList<ICommanderItem> selection)
        {
            var result = new DependencySet();
            var visited = result.Members;
            var pending = new Stack<string>();
            var left = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in selection)
            {
                var path = item?.AssetPath;
                if (string.IsNullOrEmpty(path) || !visited.Add(path)) continue;

                if (item.Kind != CommanderItemKind.Folder)
                {
                    pending.Push(path);
                    continue;
                }

                foreach (var file in source.FilesUnder(path))
                    if (visited.Add(file))
                        pending.Push(file);
            }

            while (pending.Count > 0)
            {
                foreach (var dependency in source.DirectDependencies(pending.Pop()))
                {
                    if (visited.Contains(dependency) || left.Contains(dependency)) continue;

                    if (!IsTransferable(dependency, result))
                    {
                        left.Add(dependency);
                        continue;
                    }

                    visited.Add(dependency);
                    result.Paths.Add(dependency);
                    pending.Push(dependency);
                }
            }

            result.Paths.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        // Packages are not the project's to reorganise, and a copied script or shader is a
        // second class or shader with the same name — a compile error or a silent shadowing,
        // never what "make this prefab self-contained" means. Both stay put and are shared.
        private static bool IsTransferable(string path, DependencySet result)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                result.LeftPackages++;
                return false;
            }

            var kind = AssetKinds.FromPath(path);
            if (kind != AssetKind.Script && kind != AssetKind.Shader) return true;

            result.LeftCode++;
            return false;
        }
    }
}
