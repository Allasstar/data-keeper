using System.Collections.Generic;
using System.IO;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.Extra
{
    [AddComponentMenu("DataKeeper/Extra/Transparent Screenshot Capture")]
    [RequireComponent(typeof(Camera))]
    public class TransparentScreenshotCapture : MonoBehaviour
    {
        [Tooltip("Objects to capture. Each object is captured one by one into its own screenshot. If empty, captures the full camera view once.")]
        public List<GameObject> targetObjects = new List<GameObject>();
        public LayerMask isolationLayer = 1 << 1;
        public Color backgroundColor = new Color(0, 0, 0, 0);

        [Tooltip("Custom output width/height. Set to 0 to use camera's pixel size.")]
        public int customWidth;
        public int customHeight;

        [Tooltip("Folder where screenshots are saved. Relative to Application.dataPath in the Editor, Application.persistentDataPath in builds. Created if it doesn't exist.")]
        public string folderName = "Screenshots";

        [Tooltip("Prefix for the screenshot file name (e.g., 'MyPrefix_'). Applied before the base name.")]
        public string prefix = "";

        [Tooltip("Suffix for the screenshot file name (e.g., '_MySuffix'). Applied after the base name and timestamp (if enabled).")]
        public string suffix = "";

        [Tooltip("Custom base file name. If empty, uses each target object's name (or 'Screenshot' as fallback).")]
        public Optional<string> baseFileName = new Optional<string>();

        [Tooltip("Whether to include a timestamp (yyyyMMdd_HHmmss) after the base name.")]
        public bool includeTimestamp = true;

        [Tooltip("If enabled, an existing file with the same name is overwritten. If disabled, a numeric suffix is added to keep both files.")]
        public bool replaceExistingFile = false;

        private Camera _cam;
        private readonly List<KeyValuePair<GameObject, int>> _savedLayers = new List<KeyValuePair<GameObject, int>>();

        [ContextMenu("Capture Transparent Screenshots")]
        public void CaptureTransparentScreenshots()
        {
            bool capturedAny = false;
            for (int i = 0; i < targetObjects.Count; i++)
            {
                if (targetObjects[i] == null) continue;
                CaptureTransparentScreenshot(targetObjects[i]);
                capturedAny = true;
            }

            // No targets assigned: capture the full camera view once
            if (!capturedAny)
            {
                CaptureTransparentScreenshot(null);
            }
        }

        public void CaptureTransparentScreenshot(GameObject target)
        {
            if (_cam == null)
            {
                _cam = GetComponent<Camera>();
            }
            Camera cam = _cam;

            // Store original camera/render state
            CameraClearFlags originalClearFlags = cam.clearFlags;
            Color originalBackgroundColor = cam.backgroundColor;
            int originalCullingMask = cam.cullingMask;
            RenderTexture originalTarget = cam.targetTexture;
            RenderTexture originalActive = RenderTexture.active;

            RenderTexture rt = null;
            Texture2D screenshot = null;
            _savedLayers.Clear();

            try
            {
                // Set for transparency
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundColor;

                if (target != null)
                {
                    // Temporarily move the target object and its children to the isolation layer
                    int layer = LayerMaskToLayer(isolationLayer);
                    StoreLayersRecursively(target);
                    SetLayerRecursively(target, layer);

                    // Render only the isolation layer
                    cam.cullingMask = 1 << layer;
                }

                // Determine size: use custom if set, else camera's pixel size
                int width = customWidth > 0 ? customWidth : cam.pixelWidth;
                int height = customHeight > 0 ? customHeight : cam.pixelHeight;
                if (width <= 0 || height <= 0)
                {
                    Debug.LogError($"Invalid capture size {width}x{height}.", this);
                    return;
                }

                // RenderTexture with alpha support
                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                rt.Create();

                cam.targetTexture = rt;
                cam.Render();

                // Read pixels from the RenderTexture
                RenderTexture.active = rt;
                screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                byte[] bytes = screenshot.EncodeToPNG();

#if UNITY_EDITOR
                string rootPath = Application.dataPath;
#else
                string rootPath = Application.persistentDataPath;
#endif
                string folderPath = Path.Combine(rootPath, folderName);
                Directory.CreateDirectory(folderPath);

                // Base name: custom if set, else target's name or fallback
                string baseName = baseFileName.Enabled
                ? baseFileName.Value
                : (target != null ? target.name : "Screenshot");

                string timestamp = includeTimestamp ? "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") : "";
                string fileName = SanitizeFileName(prefix + baseName + timestamp + suffix);

                string filePath = replaceExistingFile
                    ? Path.Combine(folderPath, fileName + ".png")
                    : GetUniqueFilePath(folderPath, fileName);
                File.WriteAllBytes(filePath, bytes);

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                SetAlphaIsTransparency(filePath);
#endif
                Debug.Log("Transparent screenshot saved to: " + filePath, this);
            }
            finally
            {
                // Restore render state (also on exceptions mid-capture)
                cam.targetTexture = originalTarget;
                RenderTexture.active = originalActive;

                if (rt != null)
                {
                    rt.Release();
                    DestroyImmediate(rt);
                }

                if (screenshot != null)
                {
                    DestroyImmediate(screenshot);
                }

                cam.clearFlags = originalClearFlags;
                cam.backgroundColor = originalBackgroundColor;
                cam.cullingMask = originalCullingMask;

                RestoreLayers();
            }
        }

        // Returns a non-clashing path so captures of same-named objects don't overwrite each other
        private static string GetUniqueFilePath(string folderPath, string fileName)
        {
            string path = Path.Combine(folderPath, fileName + ".png");
            int counter = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(folderPath, fileName + "_" + counter + ".png");
                counter++;
            }

            return path;
        }

#if UNITY_EDITOR
        // Only works for files saved under Assets (Editor); persistentDataPath files aren't assets
        private static void SetAlphaIsTransparency(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath).Replace('\\', '/');
            string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (!fullPath.StartsWith(dataPath)) return;

            string assetPath = "Assets" + fullPath.Substring(dataPath.Length);
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer != null && !importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
#endif

        private void StoreLayersRecursively(GameObject obj)
        {
            _savedLayers.Add(new KeyValuePair<GameObject, int>(obj, obj.layer));
            foreach (Transform child in obj.transform)
            {
                StoreLayersRecursively(child.gameObject);
            }
        }

        private void RestoreLayers()
        {
            for (int i = 0; i < _savedLayers.Count; i++)
            {
                if (_savedLayers[i].Key != null)
                {
                    _savedLayers[i].Key.layer = _savedLayers[i].Value;
                }
            }

            _savedLayers.Clear();
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                sb.Append(System.Array.IndexOf(invalid, name[i]) >= 0 ? '_' : name[i]);
            }

            string result = sb.ToString().Trim();
            return result.Length > 0 ? result : "Screenshot";
        }

        // Converts a LayerMask to a single layer index (lowest set bit).
        private int LayerMaskToLayer(LayerMask mask)
        {
            int value = mask.value;
            if (value == 0)
            {
                Debug.LogWarning("Isolation LayerMask is empty. Defaulting to layer 0.", this);
                return 0;
            }

            int layer = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                layer++;
            }

            if (value != 1 && mask.value != 1 << layer)
            {
                Debug.LogWarning($"Isolation LayerMask has multiple layers set. Using lowest: {layer} ({LayerMask.LayerToName(layer)}).", this);
            }

            return layer;
        }
    }
}
