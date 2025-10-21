using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;

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
        private Vector3Field lightPositionField;
        private Vector3Field lightRotationField;
        private ColorField lightColorField;
        private FloatField lightIntensityField;
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
        private Vector3 lightPosition = new Vector3(0, 3, -3);
        private Vector3 lightRotation = new Vector3(50, -30, 0);
        private Color lightColor = Color.white;
        private float lightIntensity = 1f;
        private int imageWidth = 512;
        private int imageHeight = 512;
        
        private Rect previewRect;
        
        [MenuItem("Tools/Windows/Prefab Image Baker")]
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
            
            lightPositionField = new Vector3Field("Light Position")
            {
                value = lightPosition
            };
            lightPositionField.RegisterValueChangedCallback(evt => 
            {
                lightPosition = evt.newValue;
                UpdatePreview();
            });
            root.Add(lightPositionField);
            
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
                UpdateCameraTransform();
                previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewRenderUtility.camera.backgroundColor = backgroundColor;
                previewRenderUtility.camera.fieldOfView = 30;
                
                // Add directional light
                previewLight = previewRenderUtility.lights[0];
                previewLight.transform.position = lightPosition;
                previewLight.transform.rotation = Quaternion.Euler(lightRotation);
                previewLight.color = lightColor;
                previewLight.intensity = lightIntensity;
            }
        }
        
        private void UpdateCameraTransform()
        {
            if (previewRenderUtility == null) return;
            
            // Calculate camera position based on orbit angles and distance
            float horizontalRad = cameraOrbit.x * Mathf.Deg2Rad;
            float verticalRad = cameraOrbit.y * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad) * cameraDistance,
                Mathf.Sin(verticalRad) * cameraDistance,
                -Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad) * cameraDistance
            );
            
            previewRenderUtility.camera.transform.position = cameraPivot + offset;
            previewRenderUtility.camera.transform.LookAt(cameraPivot);
        }
        
        private void UpdatePreview()
        {
            if (previewRenderUtility == null)
            {
                InitializePreviewUtility();
            }
            
            // Clean up previous instance
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
            }
            
            // Create new instance if prefab is selected
            if (selectedPrefab != null)
            {
                previewInstance = Instantiate(selectedPrefab);
                previewInstance.hideFlags = HideFlags.HideAndDontSave;
                previewInstance.transform.position = Vector3.zero;
                previewInstance.transform.rotation = Quaternion.identity;
                previewInstance.transform.localScale = Vector3.one;
            }
            
            // Update camera and light
            UpdateCameraTransform();
            previewRenderUtility.camera.backgroundColor = backgroundColor;
            previewLight.transform.position = lightPosition;
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
                
                if (previewInstance != null)
                {
                    RenderObject(previewInstance);
                }
                
                previewRenderUtility.camera.Render();
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
        
        private void RenderObject(GameObject obj)
        {
            // Render all mesh renderers
            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    Renderer renderer = meshFilter.GetComponent<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        previewRenderUtility.DrawMesh(
                            meshFilter.sharedMesh,
                            meshFilter.transform.localToWorldMatrix,
                            renderer.sharedMaterial,
                            0
                        );
                    }
                }
            }
            
            // Render skinned mesh renderers
            SkinnedMeshRenderer[] skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedRenderer in skinnedRenderers)
            {
                if (skinnedRenderer.sharedMesh != null && skinnedRenderer.sharedMaterial != null)
                {
                    previewRenderUtility.DrawMesh(
                        skinnedRenderer.sharedMesh,
                        skinnedRenderer.transform.localToWorldMatrix,
                        skinnedRenderer.sharedMaterial,
                        0
                    );
                }
            }
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
            
            // Create temporary camera and setup
            GameObject tempCameraObj = new GameObject("TempBakeCamera");
            tempCameraObj.hideFlags = HideFlags.HideAndDontSave;
            Camera bakeCamera = tempCameraObj.AddComponent<Camera>();
            
            // Setup camera
            UpdateCameraTransform();
            bakeCamera.transform.position = previewRenderUtility.camera.transform.position;
            bakeCamera.transform.rotation = previewRenderUtility.camera.transform.rotation;
            bakeCamera.fieldOfView = previewRenderUtility.camera.fieldOfView;
            bakeCamera.clearFlags = CameraClearFlags.SolidColor;
            bakeCamera.backgroundColor = backgroundColor;
            bakeCamera.cullingMask = ~0;
            
            // Create temporary light
            GameObject tempLightObj = new GameObject("TempBakeLight");
            tempLightObj.hideFlags = HideFlags.HideAndDontSave;
            Light bakeLight = tempLightObj.AddComponent<Light>();
            bakeLight.type = LightType.Directional;
            bakeLight.color = lightColor;
            bakeLight.intensity = lightIntensity;
            
            // Position light relative to camera view
            Vector3 lightDir = Quaternion.Euler(lightRotation) * Vector3.forward;
            bakeLight.transform.rotation = Quaternion.LookRotation(lightDir);
            
            // Create temporary instance of prefab
            GameObject tempInstance = Instantiate(selectedPrefab);
            tempInstance.hideFlags = HideFlags.HideAndDontSave;
            tempInstance.transform.position = Vector3.zero;
            tempInstance.transform.rotation = Quaternion.identity;
            tempInstance.transform.localScale = Vector3.one;
            
            // Create render texture
            RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 4;
            
            bakeCamera.targetTexture = renderTexture;
            
            // Render
            RenderTexture.active = renderTexture;
            bakeCamera.Render();
            
            // Read pixels
            Texture2D texture = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
            texture.Apply();
            
            // Save to file
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            
            // Cleanup - clear camera target texture BEFORE destroying it
            bakeCamera.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(renderTexture);
            DestroyImmediate(texture);
            DestroyImmediate(tempInstance);
            DestroyImmediate(tempLightObj);
            DestroyImmediate(tempCameraObj);
            
            // Refresh asset database if saved in project
            if (path.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }
            
            EditorUtility.DisplayDialog("Success", $"Image saved to:\n{path}", "OK");
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