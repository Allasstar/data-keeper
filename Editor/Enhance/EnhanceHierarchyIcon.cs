using System.Collections.Generic;
using DataKeeper.Editor.Settings;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Enhance
{
    [InitializeOnLoad]
    public class EnhanceHierarchyIcon
    {
        // Dictionary to cache component icons
        private static Dictionary<System.Type, Texture> componentIconCache = new Dictionary<System.Type, Texture>();

        // Settings
        private static List<System.Type> priorityComponents = new List<System.Type>();
        private static bool isEnabled => DataKeeperEditorPref.EnhanceHierarchyIconPref.Value;
        private static bool isEnabledPrefab => DataKeeperEditorPref.EnhanceHierarchyPrefabIconPref.Value;

        // Colors
        private static readonly Color SelectionColor = new Color(0.17f, 0.36f, 0.53f);
        
        private static readonly Color HoverProColor = new Color(0.27f, 0.27f, 0.27f);
        private static readonly Color HoverLightColor = new Color(0.85f, 0.85f, 0.85f);
        
        private static readonly Color NormalProColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color NormalLightColor = new Color(0.76f, 0.76f, 0.76f);
        
        private static readonly Color PrefabColor = new Color(0.3f, 0.7f, 0.4f);
        
        // Constructor runs when Unity starts or scripts recompile
        static EnhanceHierarchyIcon()
        {
            // Initialize with some default priority components if none are set
            if (priorityComponents.Count == 0)
            {
                priorityComponents.Add(typeof(Camera));
                priorityComponents.Add(typeof(Light));
                priorityComponents.Add(typeof(AudioSource));
                priorityComponents.Add(typeof(Animator));
                priorityComponents.Add(typeof(Rigidbody));
                priorityComponents.Add(typeof(Animation));
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
                priorityComponents.Add(typeof(UnityEngine.UI.ToggleGroup));
                priorityComponents.Add(typeof(DataKeeper.UI.ToggleUIGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.Image));
                priorityComponents.Add(typeof(UnityEngine.UI.LayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.LayoutElement));
                priorityComponents.Add(typeof(UnityEngine.UI.ContentSizeFitter));
                priorityComponents.Add(typeof(UnityEngine.UI.AspectRatioFitter));
                priorityComponents.Add(typeof(UnityEngine.UI.GridLayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.HorizontalLayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.VerticalLayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup));
                priorityComponents.Add(typeof(DataKeeper.UI.SafeAreaUI));
                priorityComponents.Add(typeof(TextMeshProUGUI));
                priorityComponents.Add(typeof(TMP_InputField));
            }

            EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
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

                    // Determine if this item is selected
                    bool isSelected = Selection.Contains(instanceID);
                    bool isMouseDown = Event.current.type == EventType.MouseDown || 
                                       Event.current.type == EventType.MouseDrag ||
                                       (Event.current.type == EventType.Repaint && 
                                        Event.current.button > 0 
                                        && Event.current.mousePosition.y > 0);


                    // Get appropriate background color based on selection state
                    Color backgroundColor;

                    if (isSelected || isMouseDown)
                    {
                        // Use Unity's selection color
                        backgroundColor = SelectionColor;
                    }
                    else
                    {
                        // Check if item is being hovered (mouse over)
                        bool isHovered = selectionRect.Contains(Event.current.mousePosition);
                      
                        // Fix for hover color when mouse is held down
                        if (isHovered && Event.current.type == EventType.Repaint)
                        {
                            // Use Unity's hover color
                            backgroundColor = EditorGUIUtility.isProSkin
                                ? HoverProColor
                                : HoverLightColor;
                        }
                        else
                        {
                            // Use normal background color
                            backgroundColor = EditorGUIUtility.isProSkin
                                ? NormalProColor
                                : NormalLightColor;
                        }
                    }

                    EditorGUI.DrawRect(iconRect, backgroundColor);

                    if (isEnabledPrefab)
                    {
                        bool isPrefab = PrefabUtility.GetPrefabInstanceStatus(gameObject) != PrefabInstanceStatus.NotAPrefab;
                        
                        if (isPrefab)
                        {
                            var prefabRect = new Rect(iconRect);
                            prefabRect.x = 40f;
                            prefabRect.width = 3f;
                            EditorGUI.DrawRect(prefabRect, PrefabColor);
                        }
                    }

                    GUI.DrawTexture(iconRect, icon);
                }
            }
        }

        // Gets the component to use for icon based on priority settings
        private static Component GetIconComponent(GameObject gameObject)
        {
            // First check priority list
            foreach (var componentType in priorityComponents)
            {
                Component component = gameObject.GetComponent(componentType);
                if (component != null)
                    return component;
            }

            Component[] components = gameObject.GetComponents<Component>();
            // Skip Transform as it's on every GameObject
            return components.Length > 1 ? components[1] : components[0];
        }

        // Gets icon for a component (with caching)
        private static Texture GetComponentIcon(Component component)
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
}