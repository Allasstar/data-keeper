using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Resolves the module folder from this file's compile-time path, so styles load whether the
    // package is embedded, relocated, or consumed from the PackageCache.
    public static class ForgeAssets
    {
        private const string FallbackDirectory = "Packages/com.micrarriors.data-keeper/Editor/Forge";

        private static string s_Directory;

        public static StyleSheet LoadUss(string fileName) =>
            AssetDatabase.LoadAssetAtPath<StyleSheet>($"{Directory}/{fileName}.uss");

        private static string s_TemplatesFolder;

        public static string TemplatesFolder => s_TemplatesFolder ??= ResolveTemplatesFolder();

        private static string Directory => s_Directory ??= ResolveDirectory();

        // Editor/Forge sits two levels below the package root.
        private static string ResolveTemplatesFolder()
        {
            var packageRoot = Path.GetDirectoryName(Path.GetDirectoryName(Directory))?.Replace('\\', '/');
            return $"{packageRoot}/Runtime/Forge/Templates";
        }

        private static string ResolveDirectory()
        {
            var relative = ToProjectRelative(Path.GetDirectoryName(ThisFilePath()));
            return AssetDatabase.IsValidFolder(relative) ? relative : FallbackDirectory;
        }

        private static string ThisFilePath([CallerFilePath] string path = "") => path;

        private static string ToProjectRelative(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return "";

            var projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath)?.Replace('\\', '/');
            var normalized = absolute.Replace('\\', '/');

            return !string.IsNullOrEmpty(projectRoot) && normalized.StartsWith(projectRoot)
                ? normalized.Substring(projectRoot.Length).TrimStart('/')
                : normalized;
        }
    }
}
