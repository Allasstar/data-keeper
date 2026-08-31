using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public enum AssetKind : byte
    {
        Unknown = 0,
        Folder = 1,
        Scene = 2,
        Prefab = 3,
        Script = 4,
        ScriptableObject = 5,
        Material = 6,
        Shader = 7,
        Texture = 8,
        Model = 9,
        Audio = 10,
        Animation = 11,
        Video = 12,
        Font = 13,
        Text = 14,
    }

    public sealed class AssetRecord
    {
        public static readonly string[] NoGuids = Array.Empty<string>();

        public string Guid;
        public string Path;
        public AssetKind Kind;
        public long Size;
        public ulong ContentHash;
        public long LastWriteTicks;

        // Tracked separately from LastWriteTicks because importer settings change the .meta
        // without touching the asset, and the .meta is where model materials, atlas members
        // and avatar references live — reusing a cached record across that edit would keep
        // stale dependencies. Not in the plan's field list.
        public long MetaWriteTicks;

        // Every guid referenced by the asset file AND by its .meta sidecar, minus the
        // asset's own guid. Whether a dependency resolves is a question for ProjectIndex,
        // which is the only place that knows the full guid set.
        public string[] DependencyGuids = NoGuids;

        // Guids appearing in an "m_Script:" binding. The plan called this field
        // MissingScriptGuids, but a worker parsing one file cannot know which of them are
        // missing — ProjectIndex resolves that into its _missingScriptOwners set once every
        // record exists. See ProjectIndex.HasMissingScript.
        public string[] ScriptGuids = NoGuids;
    }

    public static class AssetKinds
    {
        // Extensions parsed for guids unconditionally. Anything else still gets parsed when
        // its first bytes are "%YAML" (see GuidScanner.ScanFile) — the sniff costs nothing
        // because the bytes are already being read for the content hash, and it catches
        // YAML asset types no hardcoded list can keep up with.
        private static readonly HashSet<string> YamlExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".unity", ".prefab", ".asset", ".mat", ".controller", ".overrideController",
            ".anim", ".playable", ".spriteatlas", ".spriteatlasv2", ".physicMaterial",
            ".physicsMaterial", ".physicsMaterial2D", ".terrainlayer", ".lighting",
            ".mixer", ".preset", ".shadervariants", ".guiskin", ".fontsettings",
            ".inputactions", ".renderTexture", ".cubemap", ".mask", ".signal", ".flare",
            ".giparams", ".brush",
        };

        private static readonly Dictionary<string, AssetKind> KindByExtension = new Dictionary<string, AssetKind>(StringComparer.OrdinalIgnoreCase)
        {
            { ".unity", AssetKind.Scene },
            { ".prefab", AssetKind.Prefab },
            { ".cs", AssetKind.Script },
            { ".asmdef", AssetKind.Script },
            { ".asmref", AssetKind.Script },
            { ".asset", AssetKind.ScriptableObject },
            { ".preset", AssetKind.ScriptableObject },
            { ".mat", AssetKind.Material },
            { ".physicMaterial", AssetKind.Material },
            { ".physicsMaterial", AssetKind.Material },
            { ".physicsMaterial2D", AssetKind.Material },
            { ".shader", AssetKind.Shader },
            { ".shadergraph", AssetKind.Shader },
            { ".compute", AssetKind.Shader },
            { ".cginc", AssetKind.Shader },
            { ".hlsl", AssetKind.Shader },
            { ".shadersubgraph", AssetKind.Shader },
            { ".png", AssetKind.Texture },
            { ".jpg", AssetKind.Texture },
            { ".jpeg", AssetKind.Texture },
            { ".tga", AssetKind.Texture },
            { ".psd", AssetKind.Texture },
            { ".tif", AssetKind.Texture },
            { ".tiff", AssetKind.Texture },
            { ".exr", AssetKind.Texture },
            { ".gif", AssetKind.Texture },
            { ".bmp", AssetKind.Texture },
            { ".hdr", AssetKind.Texture },
            { ".webp", AssetKind.Texture },
            { ".spriteatlas", AssetKind.Texture },
            { ".spriteatlasv2", AssetKind.Texture },
            { ".cubemap", AssetKind.Texture },
            { ".renderTexture", AssetKind.Texture },
            { ".fbx", AssetKind.Model },
            { ".obj", AssetKind.Model },
            { ".blend", AssetKind.Model },
            { ".dae", AssetKind.Model },
            { ".3ds", AssetKind.Model },
            { ".max", AssetKind.Model },
            { ".ma", AssetKind.Model },
            { ".mb", AssetKind.Model },
            { ".wav", AssetKind.Audio },
            { ".mp3", AssetKind.Audio },
            { ".ogg", AssetKind.Audio },
            { ".aiff", AssetKind.Audio },
            { ".aif", AssetKind.Audio },
            { ".flac", AssetKind.Audio },
            { ".mixer", AssetKind.Audio },
            { ".anim", AssetKind.Animation },
            { ".controller", AssetKind.Animation },
            { ".overrideController", AssetKind.Animation },
            { ".playable", AssetKind.Animation },
            { ".mask", AssetKind.Animation },
            { ".mp4", AssetKind.Video },
            { ".mov", AssetKind.Video },
            { ".webm", AssetKind.Video },
            { ".avi", AssetKind.Video },
            { ".ttf", AssetKind.Font },
            { ".otf", AssetKind.Font },
            { ".fontsettings", AssetKind.Font },
            { ".txt", AssetKind.Text },
            { ".json", AssetKind.Text },
            { ".xml", AssetKind.Text },
            { ".csv", AssetKind.Text },
            { ".md", AssetKind.Text },
            { ".yaml", AssetKind.Text },
            { ".bytes", AssetKind.Text },
        };

        public static bool IsYamlExtension(string path) =>
            YamlExtensions.Contains(GetExtension(path));

        public static AssetKind FromPath(string path) =>
            KindByExtension.TryGetValue(GetExtension(path), out var kind) ? kind : AssetKind.Unknown;

        private static string GetExtension(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            int dot = path.LastIndexOf('.');
            if (dot < 0) return "";

            // A dot in a directory name is not an extension.
            return path.IndexOf('/', dot) >= 0 ? "" : path.Substring(dot);
        }
    }
}
