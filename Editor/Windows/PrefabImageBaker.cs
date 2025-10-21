using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.Windows
{
    public class PrefabImageBaker : EditorWindow
    {
        private PreviewRenderUtility previewRenderUtility;
        private GameObject previewInstance;
        private Light previewLight;
        
        // UI References
        private ObjectField prefabField;
        private ColorField backgroundColorField;
        private FloatField cameraDistanceField;
        private Vector2Field cameraOrbitField;
        private Vector3Field cameraPivotField;
        private Vector3Field lightRotationField;
        private ColorField lightColorField;
        private FloatField lightIntensityField;
        private ColorField ambientColorField;
        private IntegerField imageWidthField;
        private IntegerField imageHeightField;
        private IMGUIContainer previewContainer;
        private Button bakeButton;
        
        // Settings
        private GameObject selectedPrefab;
        private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f);
        private float cameraDistance = 6f;
        private Vector2 cameraOrbit = new Vector2(0, 0); // x = horizontal angle, y = vertical angle
        private Vector3 cameraPivot = Vector3.zero;
        private Vector3 lightRotation = new Vector3(50, -30, 0);
        private Color lightColor = Color.white;
        private float lightIntensity = 1f;
        private Color ambientColor = new Color(0f, 0f, 0f, 0f);
        private int imageWidth = 512;
        private int imageHeight = 512;
        
        private Rect previewRect;
        
        // [MenuItem("Tools/Windows/Prefab Image Baker (Beta)")]
        public static void ShowWindow()
        {
            PrefabImageBaker window = GetWindow<PrefabImageBaker>();
            window.titleContent = new GUIContent("Prefab Image Baker");
            window.minSize = new Vector2(400, 800);
        }
        
        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            
            // Prefab Selection
            Label prefabLabel = new Label("Prefab Selection");
            prefabLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            prefabLabel.style.marginBottom = 5;
            root.Add(prefabLabel);
            
            prefabField = new ObjectField("Prefab")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false
            };
            prefabField.RegisterValueChangedCallback(evt => 
            {
                selectedPrefab = evt.newValue as GameObject;
                UpdatePreview();
            });
            root.Add(prefabField);
            
            root.Add(CreateSpacer());
            
            // Camera Settings
            Label cameraLabel = new Label("Camera Settings");
            cameraLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            cameraLabel.style.marginBottom = 5;
            root.Add(cameraLabel);
            
            backgroundColorField = new ColorField("Background Color")
            {
                value = backgroundColor,
                showAlpha = true,
                hdr = false
            };
            backgroundColorField.RegisterValueChangedCallback(evt => 
            {
                backgroundColor = evt.newValue;
                UpdatePreview();
            });
            root.Add(backgroundColorField);
            
            cameraDistanceField = new FloatField("Distance")
            {
                value = cameraDistance
            };
            cameraDistanceField.RegisterValueChangedCallback(evt => 
            {
                cameraDistance = Mathf.Max(0.1f, evt.newValue);
                UpdatePreview();
            });
            root.Add(cameraDistanceField);
            
            cameraOrbitField = new Vector2Field("Orbit (H/V Angle)")
            {
                value = cameraOrbit
            };
            cameraOrbitField.RegisterValueChangedCallback(evt => 
            {
                cameraOrbit = evt.newValue;
                UpdatePreview();
            });
            root.Add(cameraOrbitField);
            
            cameraPivotField = new Vector3Field("Pivot Point")
            {
                value = cameraPivot
            };
            cameraPivotField.RegisterValueChangedCallback(evt => 
            {
                cameraPivot = evt.newValue;
                UpdatePreview();
            });
            root.Add(cameraPivotField);
            
            root.Add(CreateSpacer());
            
            // Light Settings
            Label lightLabel = new Label("Light Settings");
            lightLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            lightLabel.style.marginBottom = 5;
            root.Add(lightLabel);
            
            lightRotationField = new Vector3Field("Light Rotation")
            {
                value = lightRotation
            };
            lightRotationField.RegisterValueChangedCallback(evt => 
            {
                lightRotation = evt.newValue;
                UpdatePreview();
            });
            root.Add(lightRotationField);
            
            lightColorField = new ColorField("Light Color")
            {
                value = lightColor,
                showAlpha = false,
                hdr = false
            };
            lightColorField.RegisterValueChangedCallback(evt => 
            {
                lightColor = evt.newValue;
                UpdatePreview();
            });
            root.Add(lightColorField);
            
            lightIntensityField = new FloatField("Intensity")
            {
                value = lightIntensity
            };
            lightIntensityField.RegisterValueChangedCallback(evt => 
            {
                lightIntensity = Mathf.Max(0f, evt.newValue);
                UpdatePreview();
            });
            root.Add(lightIntensityField);
            
            ambientColorField = new ColorField("Ambient Color")
            {
                value = ambientColor,
                showAlpha = false,
                hdr = false
            };
            ambientColorField.RegisterValueChangedCallback(evt => 
            {
                ambientColor = evt.newValue;
                UpdatePreview();
            });
            root.Add(ambientColorField);
            
            root.Add(CreateSpacer());
            
            // Output Settings
            Label outputLabel = new Label("Output Settings");
            outputLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            outputLabel.style.marginBottom = 5;
            root.Add(outputLabel);
            
            imageWidthField = new IntegerField("Image Width")
            {
                value = imageWidth
            };
            imageWidthField.RegisterValueChangedCallback(evt => 
            {
                imageWidth = Mathf.Max(1, evt.newValue);
            });
            root.Add(imageWidthField);
            
            imageHeightField = new IntegerField("Image Height")
            {
                value = imageHeight
            };
            imageHeightField.RegisterValueChangedCallback(evt => 
            {
                imageHeight = Mathf.Max(1, evt.newValue);
            });
            root.Add(imageHeightField);
            
            root.Add(CreateSpacer());
            
            // Preview
            Label previewLabel = new Label("Preview (Output Frame in Red)");
            previewLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewLabel.style.marginBottom = 5;
            root.Add(previewLabel);
            
            previewContainer = new IMGUIContainer(DrawPreview);
            previewContainer.style.height = 300;
            previewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            root.Add(previewContainer);
            
            root.Add(CreateSpacer());
            
            // Bake Button
            bakeButton = new Button(BakeImage)
            {
                text = "Bake Image"
            };
            bakeButton.style.height = 30;
            bakeButton.style.fontSize = 14;
            root.Add(bakeButton);
            
            InitializePreviewUtility();
        }
        
        private VisualElement CreateSpacer()
        {
            VisualElement spacer = new VisualElement();
            spacer.style.height = 15;
            return spacer;
        }
        
        private void InitializePreviewUtility()
        {
            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility();
                previewRenderUtility.camera.fieldOfView = 30;
                previewRenderUtility.camera.nearClipPlane = 0.1f;
                previewRenderUtility.camera.farClipPlane = 1000f;
                
                // Add directional light
                previewLight = previewRenderUtility.lights[0];
            }
            UpdatePreview();
        }
        
        private void UpdateCameraTransform(Camera camera, Vector3 pivot, Vector2 orbit, float distance)
        {
            // Calculate camera position based on orbit angles and distance
            float horizontalRad = orbit.x * Mathf.Deg2Rad;
            float verticalRad = orbit.y * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad) * distance,
                Mathf.Sin(verticalRad) * distance,
                -Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad) * distance
            );
            
            camera.transform.position = pivot + offset;
            camera.transform.LookAt(pivot);
        }
        
        private void UpdatePreview()
        {
            if (previewRenderUtility == null)
            {
                InitializePreviewUtility();
                return;
            }
            
            // Clean up previous instance
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
            }
            
            // Create new instance if prefab is selected
            if (selectedPrefab != null)
            {
                previewInstance = previewRenderUtility.InstantiatePrefabInScene(selectedPrefab);
                previewInstance.transform.position = Vector3.zero;
                previewInstance.transform.rotation = Quaternion.identity;
                previewInstance.transform.localScale = Vector3.one;
            }
            
            // Update camera and light
            UpdateCameraTransform(previewRenderUtility.camera, cameraPivot, cameraOrbit, cameraDistance);
            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = backgroundColor;
            previewRenderUtility.ambientColor = ambientColor;
            previewLight.transform.rotation = Quaternion.Euler(lightRotation);
            previewLight.color = lightColor;
            previewLight.intensity = lightIntensity;
            
            previewContainer.MarkDirtyRepaint();
        }
        
        private void DrawPreview()
        {
            if (previewRenderUtility == null) return;
            
            Rect rect = GUILayoutUtility.GetRect(300, 300);
            previewRect = rect;
            
            if (Event.current.type == EventType.Repaint)
            {
                previewRenderUtility.BeginPreview(rect, GUIStyle.none);
                previewRenderUtility.Render();
                Texture resultTexture = previewRenderUtility.EndPreview();
                GUI.DrawTexture(rect, resultTexture, ScaleMode.ScaleToFit, false);
                
                // Draw output frame overlay
                DrawOutputFrame(rect);
            }
        }
        
        private void DrawOutputFrame(Rect previewRect)
        {
            // Calculate the aspect ratio of output image
            float outputAspect = (float)imageWidth / imageHeight;
            float previewAspect = previewRect.width / previewRect.height;
            
            Rect frameRect;
            
            if (outputAspect > previewAspect)
            {
                // Output is wider - fit to width
                float frameHeight = previewRect.width / outputAspect;
                float offsetY = (previewRect.height - frameHeight) / 2f;
                frameRect = new Rect(previewRect.x, previewRect.y + offsetY, previewRect.width, frameHeight);
            }
            else
            {
                // Output is taller - fit to height
                float frameWidth = previewRect.height * outputAspect;
                float offsetX = (previewRect.width - frameWidth) / 2f;
                frameRect = new Rect(previewRect.x + offsetX, previewRect.y, frameWidth, previewRect.height);
            }
            
            // Draw red frame
            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(frameRect.xMin, frameRect.yMin), new Vector3(frameRect.xMax, frameRect.yMin));
            Handles.DrawLine(new Vector3(frameRect.xMax, frameRect.yMin), new Vector3(frameRect.xMax, frameRect.yMax));
            Handles.DrawLine(new Vector3(frameRect.xMax, frameRect.yMax), new Vector3(frameRect.xMin, frameRect.yMax));
            Handles.DrawLine(new Vector3(frameRect.xMin, frameRect.yMax), new Vector3(frameRect.xMin, frameRect.yMin));
            Handles.EndGUI();
        }
        
        private void BakeImage()
        {
            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a prefab first!", "OK");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel(
                "Save Baked Image",
                "Assets",
                selectedPrefab.name + ".png",
                "png"
            );
            
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            RenderTexture renderTexture = null;
            GameObject cameraObj = null;
            GameObject lightObj = null;
            GameObject bakeInstance = null;
            cameraObj = new GameObject("BakeCamera");
            Camera bakeCamera = cameraObj.AddComponent<Camera>();

            try
            {
                // Create camera
                EditorSceneManager.MoveGameObjectToScene(cameraObj, previewScene);
                bakeCamera.fieldOfView = 30;
                bakeCamera.nearClipPlane = 0.1f;
                bakeCamera.farClipPlane = 1000f;
                bakeCamera.clearFlags = CameraClearFlags.SolidColor;
                bakeCamera.backgroundColor = backgroundColor;
                
                // Update camera transform
                UpdateCameraTransform(bakeCamera, cameraPivot, cameraOrbit, cameraDistance);
                
                // Create light
                lightObj = new GameObject("BakeLight");
                EditorSceneManager.MoveGameObjectToScene(lightObj, previewScene);
                Light bakeLight = lightObj.AddComponent<Light>();
                bakeLight.type = LightType.Directional;
                bakeLight.color = lightColor;
                bakeLight.intensity = lightIntensity;
                bakeLight.transform.rotation = Quaternion.Euler(lightRotation);
                
                // Create prefab instance
                bakeInstance = Instantiate(selectedPrefab);
                EditorSceneManager.MoveGameObjectToScene(bakeInstance, previewScene);
                bakeInstance.transform.position = Vector3.zero;
                bakeInstance.transform.rotation = Quaternion.identity;
                bakeInstance.transform.localScale = Vector3.one;
                
                // Temporarily set ambient
                var prevAmbientMode = RenderSettings.ambientMode;
                var prevAmbientLight = RenderSettings.ambientLight;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor;
                
                // Create render texture
                renderTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 4;
                
                bakeCamera.targetTexture = renderTexture;
                bakeCamera.aspect = (float)imageWidth / imageHeight;
                
                // Render
                bakeCamera.Render();
                
                // Restore ambient
                RenderSettings.ambientMode = prevAmbientMode;
                RenderSettings.ambientLight = prevAmbientLight;
                
                // Read pixels
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
                texture.Apply();
                
                // Save to file
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                
                DestroyImmediate(texture);
                
                if (path.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }
                
                EditorUtility.DisplayDialog("Success", $"Image saved to:\n{path}", "OK");
            }
            finally
            {
                if (renderTexture != null)
                {
                    bakeCamera.targetTexture = null;

                    DestroyImmediate(renderTexture);
                }
                if (bakeInstance != null)
                {
                    DestroyImmediate(bakeInstance);
                }
                if (lightObj != null)
                {
                    DestroyImmediate(lightObj);
                }
                if (cameraObj != null)
                {
                    DestroyImmediate(cameraObj);
                }
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }
        
        private void OnDisable()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
            }
            
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }
        }
    }
}