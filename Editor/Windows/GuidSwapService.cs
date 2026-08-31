using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    // A swap rewrites two .meta files behind the AssetDatabase's back, so there must be exactly
    // one copy of it: this is the implementation GUIDSwapper and Asset Commander's Swap command
    // both call.
    public static class GuidSwapService
    {
        private static readonly Regex GuidPattern = new Regex(@"guid:\s*([a-f0-9]{32})");

        // Binary and mixed serialization store references as compiled ids, so rewriting the two
        // meta files would leave every referrer pointing at the old asset.
        public static bool IsSupported => EditorSettings.serializationMode == SerializationMode.ForceText;

        public static string ExtractGuid(string metaContent)
        {
            var match = GuidPattern.Match(metaContent ?? "");
            return match.Success ? match.Groups[1].Value : null;
        }

        public static bool Swap(string path1, string path2, bool validateTypes, out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
            {
                error = "Both assets must exist in the project.";
                return false;
            }

            if (path1 == path2)
            {
                error = "An asset cannot be swapped with itself.";
                return false;
            }

            if (!IsSupported)
            {
                error = "Asset Serialization must be set to Force Text to swap GUIDs.";
                return false;
            }

            if (validateTypes)
            {
                var type1 = AssetDatabase.GetMainAssetTypeAtPath(path1);
                var type2 = AssetDatabase.GetMainAssetTypeAtPath(path2);
                if (type1 != type2)
                {
                    error = $"Asset types differ: {Path.GetFileName(path1)} ({type1?.Name}) and " +
                            $"{Path.GetFileName(path2)} ({type2?.Name}).";
                    return false;
                }
            }

            var meta1 = ToAbsolute(path1 + ".meta");
            var meta2 = ToAbsolute(path2 + ".meta");

            if (!File.Exists(meta1) || !File.Exists(meta2))
            {
                error = "One of the assets has no .meta file.";
                return false;
            }

            var content1 = File.ReadAllText(meta1);
            var content2 = File.ReadAllText(meta2);

            var guid1 = ExtractGuid(content1);
            var guid2 = ExtractGuid(content2);

            if (string.IsNullOrEmpty(guid1) || string.IsNullOrEmpty(guid2))
            {
                error = "Could not read a GUID out of one of the .meta files.";
                return false;
            }

            // Both reads and both rewrites happen before either write, so a malformed meta file
            // cannot leave the project with one half of a swap applied.
            var rewritten1 = content1.Replace("guid: " + guid1, "guid: " + guid2);
            var rewritten2 = content2.Replace("guid: " + guid2, "guid: " + guid1);

            File.WriteAllText(meta1, rewritten1);
            File.WriteAllText(meta2, rewritten2);

            return true;
        }

        public static string ToAbsolute(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath)) return "";

            var dataPath = Application.dataPath.Replace('\\', '/');
            var projectRoot = dataPath.Substring(0, dataPath.Length - "Assets".Length);

            return projectRoot + projectRelativePath.Replace('\\', '/');
        }
    }
}
