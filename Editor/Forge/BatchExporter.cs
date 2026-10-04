using System;
using System.Collections.Generic;
using System.IO;
using DataKeeper.Forge;
using DataKeeper.Forge.Export;
using DataKeeper.Forge.Render;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Forge
{
    // File 1 is the recipe as designed; files 2..N are filtered mutations of it. Their seeds
    // derive from the recipe seed, so exporting the same recipe again writes the same files.
    public sealed class BatchExporter : IDisposable
    {
        private const uint VariationSalt = 0xE4907u;

        private readonly SfxRenderer _renderer = new();
        private readonly ExportProcessor _processor = new();
        private readonly VariationGenerator _generator;
        private readonly SfxRecipe _scratch;
        private readonly List<string> _variants = new();
        private readonly List<string> _paths = new();

        public BatchExporter(VariationGenerator generator)
        {
            _generator = generator;
            _scratch = ScriptableObject.CreateInstance<SfxRecipe>();
            _scratch.hideFlags = HideFlags.HideAndDontSave;
        }

        public IReadOnlyList<string> Paths => _paths;

        // Returns a status line; an empty Paths list means nothing was written.
        public string Export(SfxRecipe recipe, ExportSettings settings, float trimStart, float trimEnd)
        {
            _paths.Clear();
            var folder = WavExporter.NormalizeFolder(settings.Folder);
            if (!WavExporter.IsAssetsFolder(folder)) return $"Export folder must be inside Assets: '{folder}'.";

            var count = math.clamp(settings.Count, 1, ExportSettings.MaxCount);
            CollectVariants(recipe, count);

            for (var n = 1; n <= count; n++)
            {
                JsonUtility.FromJsonOverwrite(_variants[n - 1], _scratch);
                var fileName = ExportNaming.Format(settings.NameTemplate, recipe.Category, recipe.name, n, count, _scratch.Seed);
                var path = $"{folder}/{fileName}.wav";
                if (_paths.Contains(path))
                {
                    _paths.Clear();
                    return "The naming template gives several files the same name; add {n} to it.";
                }

                _paths.Add(path);
            }

            if (!ConfirmOverwrite())
            {
                _paths.Clear();
                return "Export cancelled.";
            }

            WavExporter.EnsureFolder(folder);
            var limited = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (var i = 0; i < count; i++)
                {
                    JsonUtility.FromJsonOverwrite(_variants[i], _scratch);
                    _renderer.Render(_scratch);

                    var result = _processor.Process(Trim(trimStart, trimEnd), SfxRenderer.Channels, _renderer.SampleRate, settings);
                    if (result.GainLimitedByPeak) limited++;
                    WavExporter.Write(_paths[i], _processor.Output, result.Channels, _renderer.SampleRate, settings.Format);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AudioClip>(_paths[0]));
            var status = count == 1 ? $"Exported {_paths[0]}" : $"Exported {count} files to {folder}";
            if (limited > 0) status += $" ({limited} held below the peak target instead of reaching the loudness target)";
            return status;
        }

        public void ClearSampleCache() => _renderer.ClearSampleCache();

        public void Dispose()
        {
            _renderer.Dispose();
            _processor.Dispose();
            if (_scratch != null) Object.DestroyImmediate(_scratch);
        }

        private void CollectVariants(SfxRecipe recipe, int count)
        {
            _variants.Clear();
            if (count > 1)
            {
                var candidates = math.max(recipe.Randomizer.CandidateCount, 2 * (count - 1));
                _generator.Generate(recipe, null, math.hash(new uint2(recipe.Seed, VariationSalt)), candidates, count - 1, _variants);
            }

            _variants.Insert(0, JsonUtility.ToJson(recipe));
        }

        // The trim handles are fractions of the length, so they carry over to mutations that
        // changed the length.
        private NativeArray<float> Trim(float trimStart, float trimEnd)
        {
            var frames = _renderer.FrameCount;
            var first = Mathf.Clamp(Mathf.RoundToInt(trimStart * frames), 0, frames - 1);
            var last = Mathf.Clamp(Mathf.RoundToInt(trimEnd * frames), first + 1, frames);
            return _renderer.Output.GetSubArray(first * SfxRenderer.Channels, (last - first) * SfxRenderer.Channels);
        }

        private bool ConfirmOverwrite()
        {
            var existing = 0;
            foreach (var path in _paths)
                if (File.Exists(path)) existing++;

            return existing == 0 || EditorUtility.DisplayDialog("Overwrite files?",
                $"{existing} of {_paths.Count} files already exist in the export folder. Overwrite them?",
                "Overwrite", "Cancel");
        }
    }
}
