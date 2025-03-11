using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor
{
    [InitializeOnLoad]
    public class HierarchyIconSetter
    {
        // Dictionary to cache component icons
        private static Dictionary<System.Type, Texture> componentIconCache = new Dictionary<System.Type, Texture>();
    
        // Settings
        private static bool isEnabled = true;
        private static List<System.Type> priorityComponents = new List<System.Type>();
        private static bool useFirstComponentFound = true;

        // Constructor runs when Unity starts or scripts recompile
        static HierarchyIconSetter()
        {
            // Load settings from EditorPrefs
            isEnabled = EditorPrefs.GetBool("HierarchyIconReplacer_Enabled", true);
        
            // Initialize with some default priority components if none are set
            if (priorityComponents.Count == 0)
            {
                priorityComponents.Add(typeof(Camera));
                priorityComponents.Add(typeof(Light));
                priorityComponents.Add(typeof(AudioSource));
                priorityComponents.Add(typeof(Rigidbody));
                priorityComponents.Add(typeof(Collider));
                priorityComponents.Add(typeof(ParticleSystem));
                priorityComponents.Add(typeof(DataKeeper.UI.ButtonUI));
                priorityComponents.Add(typeof(DataKeeper.UI.ToggleUI));
                priorityComponents.Add(typeof(UnityEngine.UI.Button));
                priorityComponents.Add(typeof(UnityEngine.UI.Slider));
                priorityComponents.Add(typeof(UnityEngine.UI.Toggle));
                priorityComponents.Add(typeof(UnityEngine.UI.Dropdown));
                priorityComponents.Add(typeof(UnityEngine.UI.InputField));
                priorityComponents.Add(typeof(UnityEngine.UI.Scrollbar));
                priorityComponents.Add(typeof(UnityEngine.UI.ScrollRect));
                priorityComponents.Add(typeof(TMP_InputField));
                priorityComponents.Add(typeof(TextMeshProUGUI));
                priorityComponents.Add(typeof(UnityEngine.UI.ToggleGroup));
                priorityComponents.Add(typeof(DataKeeper.UI.ToggleUIGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.Image));
                priorityComponents.Add(typeof(DataKeeper.UI.SafeAreaUI));
            }
        
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (!isEnabled)
                return;
            
            // Get the GameObject with the given instanceID
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        
            if (gameObject == null)
                return;

            // Get the component to use for the icon
            Component targetComponent = GetIconComponent(gameObject);
        
            if (targetComponent != null)
            {
                // Get icon for this component
                Texture icon = GetComponentIcon(targetComponent);
            
                if (icon != null)
                {
                    // Draw the icon
                    Rect iconRect = new Rect(selectionRect);
                    iconRect.x = selectionRect.x;
                    iconRect.width = 16f;
                    iconRect.height = 16f;
                
                    // Cover default icon with background
                    EditorGUI.DrawRect(iconRect, EditorGUIUtility.isProSkin ? 
                        new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f));
                
                    GUI.DrawTexture(iconRect, icon);
                }
            }
        }

        // Gets the component to use for icon based on priority settings
        public static Component GetIconComponent(GameObject gameObject)
        {
            // First check priority list
            foreach (var componentType in priorityComponents)
            {
                Component component = gameObject.GetComponent(componentType);
                if (component != null)
                    return component;
            }
        
            // If no priority component found and we're using first component
            if (useFirstComponentFound)
            {
                Component[] components = gameObject.GetComponents<Component>();
                // Skip Transform as it's on every GameObject
                return components.Length > 1 ? components[1] : components[0];
            }
        
            return null;
        }

        // Gets icon for a component (with caching)
        public static Texture GetComponentIcon(Component component)
        {
            System.Type componentType = component.GetType();
        
            // Check cache first
            if (componentIconCache.TryGetValue(componentType, out Texture cachedIcon))
                return cachedIcon;
            
            // Get icon through Editor utility
            Texture icon = EditorGUIUtility.ObjectContent(component, componentType).image;
        
            // Cache the result
            if (icon != null)
                componentIconCache[componentType] = icon;
            
            return icon;
        }
    }

    // UIToolkit-based Editor Window
    public class HierarchyIconSettingsWindow : EditorWindow
    {
        [MenuItem("Tools/Hierarchy Icon Settings")]
        public static void ShowWindow()
        {
            HierarchyIconSettingsWindow wnd = GetWindow<HierarchyIconSettingsWindow>();
            wnd.titleContent = new GUIContent("Hierarchy Icons");
            wnd.minSize = new Vector2(300, 350);
        }

        public void CreateGUI()
        {
            // Load UI from UXML if you have a template file
            // var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/HierarchyIconSettings.uxml");
            // visualTree.CloneTree(rootVisualElement);
        
            // Or create UI manually
            var root = rootVisualElement;
        
            // Title
            var titleLabel = new Label("Hierarchy Icon Settings");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            root.Add(titleLabel);
        
            // Enable toggle
            var enableToggle = new Toggle("Enable Custom Icons")
            {
                value = EditorPrefs.GetBool("HierarchyIconReplacer_Enabled", true)
            };
            enableToggle.RegisterValueChangedCallback(evt => {
                EditorPrefs.SetBool("HierarchyIconReplacer_Enabled", evt.newValue);
                EditorApplication.RepaintHierarchyWindow();
            });
            root.Add(enableToggle);
        
            // First component toggle
            var firstComponentToggle = new Toggle("Use First Component Found")
            {
                value = EditorPrefs.GetBool("HierarchyIconReplacer_UseFirstComponent", true)
            };
            firstComponentToggle.RegisterValueChangedCallback(evt => {
                EditorPrefs.SetBool("HierarchyIconReplacer_UseFirstComponent", evt.newValue);
                EditorApplication.RepaintHierarchyWindow();
            });
            root.Add(firstComponentToggle);
        
            // Component type selection
            root.Add(new Label("Priority Components:") { style = { marginTop = 10 } });
        
            // Component list could be implemented with ListView
            var listContainer = new ScrollView();
            listContainer.style.height = 200;
            // listContainer.style.border = new BorderColor(Color.gray);
            listContainer.style.borderLeftWidth = 1;
            listContainer.style.borderRightWidth = 1;
            listContainer.style.borderTopWidth = 1;
            listContainer.style.borderBottomWidth = 1;
            root.Add(listContainer);
        
            // Apply button
            var applyButton = new Button(() => EditorApplication.RepaintHierarchyWindow())
            {
                text = "Apply Changes"
            };
            applyButton.style.marginTop = 10;
            root.Add(applyButton);
        
            // Apply to selection button
            var applyToSelectionButton = new Button(ApplyIconsToSelection)
            {
                text = "Apply Permanently to Selection"
            };
            root.Add(applyToSelectionButton);
        }
    
        private void ApplyIconsToSelection()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                Component iconComponent = HierarchyIconSetter.GetIconComponent(obj);
                if (iconComponent != null)
                {
                    Texture icon = HierarchyIconSetter.GetComponentIcon(iconComponent);
                    if (icon != null)
                    {
                        // Set icon permanently using reflection
                        SetIconForObject(obj, icon);
                    }
                }
            }
        }
    
        // Method to set icon via reflection
        private static void SetIconForObject(GameObject obj, Texture icon)
        {
            var editorGUIUtilityType = typeof(EditorGUIUtility);
            var bindingFlags = System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var args = new object[] { obj, icon };
            editorGUIUtilityType.InvokeMember("SetIconForObject", bindingFlags, null, null, args);
        }
    }
}