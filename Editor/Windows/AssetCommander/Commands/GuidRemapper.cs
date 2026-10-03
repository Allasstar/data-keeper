using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // A copied prefab still points at the original material, which still points at the original
    // texture: AssetDatabase.CopyAsset gives each file a new GUID and rewrites nothing. Copying
    // a set therefore has to redirect the references between its members to their copies, or
    // the "copy" is a set of new files wired to the old ones.
    public static class GuidRemapper
    {
        private static readonly Regex GuidReference =
            new Regex(@"guid: ?([0-9a-fA-F]{32})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly byte[] YamlHeader = Encoding.ASCII.GetBytes("%YAML");

        // Binary serialization stores references the text cannot reach.
        public static bool IsSupported => GuidSwapService.IsSupported;

        public static string Rewrite(string text, IReadOnlyDictionary<string, string> map, out bool changed)
        {
            bool any = false;

            var result = GuidReference.Replace(text, match =>
            {
                var guid = match.Groups[1].Value.ToLowerInvariant();
                if (!map.TryGetValue(guid, out var replacement)) return match.Value;

                any = true;
                return match.Value.Substring(0, match.Groups[1].Index - match.Index) + replacement;
            });

            changed = any;
            return result;
        }

        // Source → copy for every file the copy produced. A copied folder contributes each
        // asset inside it, so references between files of one folder are redirected as well.
        public static Dictionary<string, string> BuildMap(IEnumerable<KeyValuePair<string, string>> copies,
            List<string> copiedFiles)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var copy in copies)
            {
                if (!AssetDatabase.IsValidFolder(copy.Key))
                {
                    AddPair(map, copy.Key, copy.Value);
                    copiedFiles.Add(copy.Value);
                    continue;
                }

                AddPair(map, copy.Key, copy.Value);

                foreach (var guid in AssetDatabase.FindAssets("", new[] { copy.Key }))
                {
                    var source = AssetDatabase.GUIDToAssetPath(guid);
                    var destination = copy.Value + source.Substring(copy.Key.Length);

                    AddPair(map, source, destination);
                    if (!AssetDatabase.IsValidFolder(destination)) copiedFiles.Add(destination);
                }
            }

            return map;
        }

        // Rewrites the copies and their .meta files in place and reimports what changed. A model's
        // remapped materials and a sprite's atlas live in the .meta, so it is not optional.
        public static void Apply(List<string> copiedFiles, IReadOnlyDictionary<string, string> map)
        {
            if (map.Count == 0) return;

            var changedPaths = new List<string>();

            foreach (var path in copiedFiles)
            {
                bool changed = RewriteFile(GuidSwapService.ToAbsolute(path), map, IsYaml);
                changed |= RewriteFile(GuidSwapService.ToAbsolute(path + ".meta"), map, _ => true);

                if (changed) changedPaths.Add(path);
            }

            AssetOperations.Run(() =>
            {
                foreach (var path in changedPaths)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            });
        }

        private static void AddPair(Dictionary<string, string> map, string source, string destination)
        {
            var from = AssetDatabase.AssetPathToGUID(source);
            var to = AssetDatabase.AssetPathToGUID(destination);

            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) && from != to) map[from] = to;
        }

        private static bool RewriteFile(string absolutePath, IReadOnlyDictionary<string, string> map,
            Func<string, bool> isText)
        {
            if (!File.Exists(absolutePath) || !isText(absolutePath)) return false;

            var text = File.ReadAllText(absolutePath);
            var rewritten = Rewrite(text, map, out var changed);

            if (changed) File.WriteAllText(absolutePath, rewritten);
            return changed;
        }

        // Sniffed rather than judged by extension, the same way the index decides what to scan:
        // a texture read as text would be rewritten into garbage.
        private static bool IsYaml(string absolutePath)
        {
            using (var stream = File.OpenRead(absolutePath))
            {
                for (int i = 0; i < YamlHeader.Length; i++)
                    if (stream.ReadByte() != YamlHeader[i])
                        return false;
            }

            return true;
        }
    }
}
