using System.Collections.Generic;
using System.IO;
using DataKeeper.Forge.Export;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Forge
{
    public static class WavExporter
    {
        private static readonly HashSet<string> s_Pending = new();

        // The file is imported later (usually in a StopAssetEditing batch); its import settings
        // are applied during that first import, so no second reimport is needed.
        public static void Write(string assetPath, NativeArray<float> interleaved, int channels, int sampleRate,
            WavFormat format)
        {
            using (var stream = File.Create(assetPath))
                WavWriter.Write(stream, interleaved, channels, sampleRate, format, channels);

            s_Pending.Add(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        public static bool IsAssetsFolder(string folder) => folder == "Assets" || folder.StartsWith("Assets/");

        public static string NormalizeFolder(string folder) => folder.Replace('\\', '/').Trim().TrimEnd('/');

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private class ImportSettingsApplier : AssetPostprocessor
        {
            // Generated clips are short one-shots: decompressing on load avoids per-play decode cost.
            private void OnPreprocessAudio()
            {
                if (!s_Pending.Remove(assetPath)) return;

                var importer = (AudioImporter)assetImporter;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
            }
        }
    }
}
