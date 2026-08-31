using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Resolves the module's own folder from the compile-time path of this file instead of a
    // hardcoded "Packages/com.micrarriors.data-keeper/..." string, so the window keeps working
    // when the package is embedded, relocated, or consumed from the PackageCache.
    public static class UxmlLoader
    {
        private const string FallbackDirectory =
            "Packages/com.micrarriors.data-keeper/Editor/Windows/AssetCommander";

        private static string s_Directory;

        public static VisualTreeAsset LoadUxml(string fileName) =>
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Directory}/{fileName}.uxml");

        public static StyleSheet LoadUss(string fileName) =>
            AssetDatabase.LoadAssetAtPath<StyleSheet>($"{Directory}/{fileName}.uss");

        private static string Directory => s_Directory ??= ResolveDirectory();

        private static string ResolveDirectory()
        {
            // UI/ is one level below the module root.
            var uiFolder = Path.GetDirectoryName(ThisFilePath());
            var moduleRoot = Path.GetDirectoryName(uiFolder);
            var relative = ToProjectRelative(moduleRoot);

            return AssetDatabase.IsValidFolder(relative) ? relative : FallbackDirectory;
        }

        private static string ThisFilePath([CallerFilePath] string path = "") => path;

        private static string ToProjectRelative(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return "";

            var projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath)
                ?.Replace('\\', '/');
            var normalized = absolute.Replace('\\', '/');

            return !string.IsNullOrEmpty(projectRoot) && normalized.StartsWith(projectRoot)
                ? normalized.Substring(projectRoot.Length).TrimStart('/')
                : normalized;
        }
    }
}
