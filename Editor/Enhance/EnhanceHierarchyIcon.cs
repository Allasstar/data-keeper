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
        private static readonly Color DefaultColorPro = new Color(0.219f, 0.219f, 0.219f);
        private static readonly Color DefaultColorLight = new Color(0.219f, 0.219f, 0.219f);
        
        private static readonly Color HoveredColorPro = new Color(0.270f, 0.270f, 0.270f);
        private static readonly Color HoveredColorLight = new Color(0.698f, 0.698f, 0.698f);
        
        private static readonly Color SelectedColorPro = new Color(0.172f, 0.364f, 0.529f);
        private static readonly Color SelectedColorLight = new Color(0.227f, 0.447f, 0.690f);
        
        private static readonly Color SelectedUnfocusColorPro = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color SelectedUnfocusColorLight = new Color(0.68f, 0.68f, 0.68f);
        
        private static Color DefaultColor => EditorGUIUtility.isProSkin 
            ? DefaultColorPro
            : DefaultColorLight;

        private static Color HoveredColor => EditorGUIUtility.isProSkin 
            ? HoveredColorPro
            : HoveredColorLight;
        
        private static Color SelectedColor => EditorGUIUtility.isProSkin 
            ? SelectedColorPro
            : SelectedColorLight;

        private static Color SelectedUnfocusColor => EditorGUIUtility.isProSkin 
            ? SelectedUnfocusColorPro
            : SelectedUnfocusColorLight;
        
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
            if (!isEnabled) return;

            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;

            Component targetComponent = GetIconComponent(gameObject);
            if (targetComponent == null) return;

            Texture icon = GetComponentIcon(targetComponent);
            if (icon == null) return;

            Rect iconRect = new Rect(selectionRect);
            iconRect.width = 16f;
            iconRect.height = 16f;

            Event e = Event.current;

            bool isSelected = Selection.Contains(instanceID);
            bool isHovered = selectionRect.Contains(e.mousePosition);
            bool isMouseHeld = e.type == EventType.MouseDown;
            bool isClicked = isHovered && isMouseHeld;

            Color backgroundColor = DefaultColor;

            if (isSelected || isClicked)
            {
                backgroundColor = SelectedColor;
            }
            else if (isHovered && e.type == EventType.Repaint)
            {
                backgroundColor = HoveredColor;
            }

            bool isPrefab = PrefabUtility.GetPrefabInstanceStatus(gameObject) != PrefabInstanceStatus.NotAPrefab;

            if (isPrefab && isEnabledPrefab)
            {
                iconRect.height *= 0.7f;
                iconRect.width *= 0.7f;
                iconRect.x += iconRect.width * 0.5f;
                iconRect.y += iconRect.height * 0.5f;
            }

            EditorGUI.DrawRect(iconRect, backgroundColor);
            GUI.DrawTexture(iconRect, icon);
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