using System.IO;
using UnityEngine;

namespace DataKeeper.Extra
{
    public class TransparentScreenshotCapture : MonoBehaviour
    {
        public GameObject targetObject;
        public LayerMask isolationLayer;
        public Color backgroundColor = new Color(0, 0, 0, 0);

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

                // Create a RenderTexture with alpha support
                rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
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

                // Save to file (e.g., in the project's root folder)
                string filePath = Path.Combine(Application.dataPath, "TransparentScreenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
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