using System;
using System.Collections.Generic;
using System.IO;
using DataKeeper.Forge;
using UnityEditor;

namespace DataKeeper.Editor.Forge
{
    // Presets are whole recipes as JSON. EditorJsonUtility stores clip references as asset
    // GUIDs, so sample and granular layers survive a reload, unlike plain JsonUtility.
    public static class ForgePresets
    {
        public const string DefaultFolder = "Assets/Forge Presets";

        private const string RecipeMarker = "\"Layers\"";

        public static void Find(string folder, List<string> paths)
        {
            paths.Clear();
            if (!AssetDatabase.IsValidFolder(folder)) return;

            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.ReadAllText(path).Contains(RecipeMarker))
                    paths.Add(path);
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
        }

        public static void Save(SfxRecipe recipe, string assetPath)
        {
            File.WriteAllText(assetPath, EditorJsonUtility.ToJson(recipe, true));
            AssetDatabase.ImportAsset(assetPath);
        }

        public static void Load(SfxRecipe recipe, string assetPath)
        {
            Undo.RecordObject(recipe, "Load Preset");

            // The JSON carries the preset's object name; keeping the asset's own name avoids a
            // mismatch with its file name.
            var name = recipe.name;
            EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(assetPath), recipe);
            recipe.name = name;
            EditorUtility.SetDirty(recipe);
        }

        public static string DisplayName(string assetPath) =>
            string.IsNullOrEmpty(assetPath) ? "No preset" : Path.GetFileNameWithoutExtension(assetPath);
    }
}
