using System.Collections.Generic;
using DataKeeper.Forge;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Forge
{
    public static class ForgeTemplates
    {
        private static Dictionary<SfxCategory, CategoryTemplate> s_Templates;

        public static CategoryTemplate Get(SfxCategory category)
        {
            s_Templates ??= Load();
            return s_Templates.TryGetValue(category, out var template) ? template : null;
        }

        public static void Invalidate() => s_Templates = null;

        private static Dictionary<SfxCategory, CategoryTemplate> Load()
        {
            var templates = new Dictionary<SfxCategory, CategoryTemplate>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { ForgeAssets.TemplatesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json")) continue;

                var template = CategoryTemplate.FromJson(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text);
                templates[template.Category] = template;
            }

            return templates;
        }

        // Templates are hand-edited data; picking up a saved change without a domain reload
        // is what makes iterating on them practical.
        private class TemplateChangeWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
                string[] movedFrom)
            {
                if (s_Templates == null) return;
                if (TouchesTemplates(imported) || TouchesTemplates(deleted) || TouchesTemplates(moved))
                    Invalidate();
            }

            private static bool TouchesTemplates(string[] paths)
            {
                foreach (var path in paths)
                    if (path.StartsWith(ForgeAssets.TemplatesFolder) && path.EndsWith(".json")) return true;
                return false;
            }
        }
    }
}
