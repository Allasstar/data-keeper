using System;
using System.Collections.Generic;
using UnityEditor;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The AssetDatabase side of executing a plan. Every command funnels its writes through Run so
    // a batch pays for one import pass and the index is patched by the postprocessor afterwards
    // rather than rebuilt.
    public static class AssetOperations
    {
        public static bool Exists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && AssetDatabase.AssetPathExists(assetPath);

        public static void Run(Action body)
        {
            AssetDatabase.StartAssetEditing();

            try
            {
                body();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        // AssetDatabase.CreateFolder only creates one level and only under a parent it already
        // knows, so a destination two folders deep has to be built from the top down.
        public static bool EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return false;
            if (AssetDatabase.IsValidFolder(folder)) return true;

            var missing = new List<string>();
            var current = folder;

            while (!string.IsNullOrEmpty(current) && !AssetDatabase.IsValidFolder(current))
            {
                missing.Add(current);
                current = OperationPaths.Directory(current);
            }

            // Walked all the way past Assets/ or Packages/ without finding a real folder: the
            // path is not one the AssetDatabase can create.
            if (string.IsNullOrEmpty(current)) return false;

            for (int i = missing.Count - 1; i >= 0; i--)
            {
                var parent = OperationPaths.Directory(missing[i]);
                var name = OperationPaths.FileName(missing[i]);

                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name))) return false;
            }

            return true;
        }

        // Reported once at the end rather than per row: a failed batch is one problem the user
        // has to look at, not twenty console lines.
        public static void ReportFailures(string title, List<string> failures)
        {
            if (failures == null || failures.Count == 0) return;

            var text = string.Join("\n", failures.ToArray(), 0, Math.Min(failures.Count, 12));
            if (failures.Count > 12) text += "\n… and " + (failures.Count - 12) + " more";

            EditorUtility.DisplayDialog(title, text, "OK");
        }
    }
}
