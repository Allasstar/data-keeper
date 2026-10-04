using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    internal static class ForgeTestRecipes
    {
        public static SfxRecipe Create(float lengthMs, params Layer[] layers)
        {
            var recipe = ScriptableObject.CreateInstance<SfxRecipe>();
            recipe.LengthMs = lengthMs;
            recipe.Layers = new List<Layer>(layers);
            return recipe;
        }

        public static Layer Oscillator(Waveform waveform, float pitch = 0f, float levelDb = 0f) => new()
        {
            Source = new SourceSettings
            {
                Type = SourceType.Oscillator,
                Oscillator = new OscillatorSettings { Waveform = waveform },
            },
            Pitch = pitch,
            LevelDb = levelDb,
        };

        public static Layer Noise(NoiseColor color, float levelDb = 0f) => new()
        {
            Source = new SourceSettings
            {
                Type = SourceType.Noise,
                Noise = new NoiseSettings { Color = color },
            },
            LevelDb = levelDb,
        };

        public static Layer Flat(Layer layer)
        {
            layer.AmpCurve = new Curve { Points = new List<Breakpoint> { new(0f, 1f), new(1f, 1f) } };
            return layer;
        }

        public static Dictionary<SfxCategory, CategoryTemplate> LoadTemplates()
        {
            var templates = new Dictionary<SfxCategory, CategoryTemplate>();
            foreach (var file in Directory.GetFiles(TemplatesFolder(), "*.json"))
            {
                var template = CategoryTemplate.FromJson(File.ReadAllText(file));
                templates[template.Category] = template;
            }

            return templates;
        }

        // Resolved from this file's location so the tests work wherever the package lives.
        private static string TemplatesFolder([CallerFilePath] string thisFile = "")
        {
            var packageRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile), "..", "..", ".."));
            return Path.Combine(packageRoot, "Runtime", "Forge", "Templates");
        }
    }
}
