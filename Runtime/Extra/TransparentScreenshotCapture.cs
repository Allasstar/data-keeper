using System.IO;
using UnityEngine;

namespace DataKeeper.Extra
{
    [AddComponentMenu("DataKeeper/Extra/Transparent Screenshot Capture")]
    [RequireComponent(typeof(Camera))]
    public class TransparentScreenshotCapture : MonoBehaviour
    {
        public GameObject targetObject;
        public LayerMask isolationLayer;
        public Color backgroundColor = new Color(0, 0, 0, 0);
        
        [Tooltip("Custom output width/height. Set to 0 to use camera's pixel size.")]
        public int customWidth = 0;
        public int customHeight = 0;

        [Tooltip("Folder name (relative to Application.dataPath) where screenshots will be saved. Will be created if it doesn't exist.")]
        public string folderName = "Screenshots";

        [Tooltip("Prefix for the screenshot file name (e.g., 'MyPrefix_'). Applied before the base name.")]
        public string prefix = "";

        [Tooltip("Suffix for the screenshot file name (e.g., '_MySuffix'). Applied after the base name and timestamp (if enabled).")]
        public string suffix = "";

        [Tooltip("Custom base file name. If empty, uses targetObject.name (if set) or 'Screenshot' as fallback.")]
        public string baseFileName = "";

        [Tooltip("Whether to include a timestamp (yyyyMMdd_HHmmss) after the base name.")]
        public bool includeTimestamp = true;

        private Camera cam;

        [ContextMenu("Capture Transparent Screenshot")]
        public void CaptureTransparentScreenshot()
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }

            // Store original camera settings
            CameraClearFlags originalClearFlags = cam.clearFlags;
            Color originalBackgroundColor = cam.backgroundColor;
            LayerMask originalCullingMask = cam.cullingMask;

            int originalLayer = -1;
            RenderTexture rt = null;
            Texture2D screenshot = null;

            try
            {
                // Set for transparency
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundColor;

                if (targetObject != null)
                {
                    // Temporarily move the target object and its children to the isolation layer
                    originalLayer = targetObject.layer;
                    SetLayerRecursively(targetObject, LayerMaskToLayer(isolationLayer));

                    // Set camera to render only the isolation layer
                    cam.cullingMask = isolationLayer;
                }

                // Determine size: Use custom if set, else camera's pixel size
                int width = (customWidth > 0) ? customWidth : Mathf.RoundToInt(cam.pixelWidth);
                int height = (customHeight > 0) ? customHeight : Mathf.RoundToInt(cam.pixelHeight);

                // Create a RenderTexture with alpha support
                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                rt.Create();

                // Temporarily set the camera's target texture
                RenderTexture originalTarget = cam.targetTexture;
                cam.targetTexture = rt;

                // Render the camera's view
                cam.Render();

                // Restore original target immediately after render
                cam.targetTexture = originalTarget;

                // Set the active RenderTexture and read pixels
                RenderTexture.active = rt;
                screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                screenshot.Apply();

                // Encode to PNG (which supports transparency)
                byte[] bytes = screenshot.EncodeToPNG();

                // Create the folder if it doesn't exist
                string folderPath = Path.Combine(Application.dataPath, folderName);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Determine base name: custom if set, else targetObject.name or fallback
                string baseName = string.IsNullOrEmpty(baseFileName) 
                    ? (targetObject != null ? targetObject.name : "Screenshot") 
                    : baseFileName;

                // Optional timestamp
                string timestamp = includeTimestamp ? System.DateTime.Now.ToString("yyyyMMdd_HHmmss") : "";

                // Build file name
                string fileName = prefix + baseName + timestamp + suffix + ".png";

                // Save to file in the specified folder
                string filePath = Path.Combine(folderPath, fileName);
                File.WriteAllBytes(filePath, bytes);

                Debug.Log("Transparent screenshot saved to: " + filePath);
            }
            finally
            {
                // Clean up
                RenderTexture.active = null;
                if (rt != null) rt.Release();
                if (screenshot != null) DestroyImmediate(screenshot);

                // Restore original camera settings
                cam.clearFlags = originalClearFlags;
                cam.backgroundColor = originalBackgroundColor;
                cam.cullingMask = originalCullingMask;

                if (targetObject != null && originalLayer != -1)
                {
                    // Restore the original layer for the target object and its children
                    SetLayerRecursively(targetObject, originalLayer);
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        // Helper to convert LayerMask to single layer index (assuming single layer mask)
        private int LayerMaskToLayer(LayerMask mask)
        {
            if (mask.value == 0)
            {
                Debug.LogWarning("Isolation LayerMask is empty. Defaulting to layer 0.");
                return 0;
            }

            int layer = 0;
            int maskValue = mask.value;
            while (maskValue > 1)
            {
                maskValue >>= 1;
                layer++;
            }
            return layer;
        }
    }
}