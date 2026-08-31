using UnityEditor;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Pushes changed paths into ProjectIndex's queue and returns immediately — the parsing
    // happens on EditorApplication.update, because this callback runs inside the import and
    // anything slow here shows up as import stutter on every asset the user touches.
    internal sealed class IndexPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            ProjectIndex.QueueChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
