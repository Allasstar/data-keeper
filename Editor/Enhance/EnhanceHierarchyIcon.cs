using System;
using System.Collections.Generic;
using DataKeeper.Editor.Settings;
using DataKeeper.Extra;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Enhance
{
    public enum PrefabHierarchyIcon
    {
        Small = 0,
        Big = 1,
        Default = 2,
    }
    
    public enum HierarchyIconType
    {
        All = 0,
        Primary = 1,
        Secondary = 2,
    }

    [InitializeOnLoad]
    public class EnhanceHierarchyIcon
    {
        private const string SCENE_HIERARCHY_WINDOW = "SceneHierarchyWindow";
        
        private static Color _guiColor;
        private static EditorWindow _hierarchyWindow;
        private static bool _hierarchyHasFocus;
        private static bool _isMouseDown = false;

        
        public static bool GetIsMouseDown()
        {
            if (Event.current == null)
                return _isMouseDown;

            var mouseEvent = Event.current.type;
            
            if (mouseEvent == EventType.DragExited)
                _isMouseDown = false;

            if (mouseEvent == EventType.MouseDown)
                _isMouseDown = true;
            
            if (mouseEvent == EventType.MouseUp)
                _isMouseDown = false;
            
            return _isMouseDown;
        }

        // Dictionary to cache component icons
        private static Dictionary<System.Type, Texture> componentIconCache = new Dictionary<System.Type, Texture>();

        // Settings
        private static List<System.Type> priorityComponents = new List<System.Type>()
        {
            typeof(Camera),
            typeof(Light),
            typeof(CharacterController),
            typeof(LODGroup),
            typeof(Rigidbody),
            typeof(Animator),
            typeof(Animation),
            typeof(AudioSource),
            typeof(Collider),
            typeof(ParticleSystem),
            typeof(MeshRenderer),
            typeof(AudioListener),
            typeof(Canvas),
            typeof(DataKeeper.UI.ButtonUI),
            typeof(DataKeeper.UI.ToggleUI),
            typeof(UnityEngine.UI.Button),
            typeof(UnityEngine.UI.Slider),
            typeof(UnityEngine.UI.Toggle),
            typeof(UnityEngine.UI.Dropdown),
            typeof(UnityEngine.UI.InputField),
            typeof(UnityEngine.UI.Scrollbar),
            typeof(UnityEngine.UI.ScrollRect),
            typeof(UnityEngine.UI.ToggleGroup),
            typeof(DataKeeper.UI.ToggleUIGroup),
            typeof(UnityEngine.UI.LayoutGroup),
            typeof(UnityEngine.UI.LayoutElement),
            typeof(UnityEngine.UI.ContentSizeFitter),
            typeof(UnityEngine.UI.AspectRatioFitter),
            typeof(UnityEngine.UI.GridLayoutGroup),
            typeof(UnityEngine.UI.HorizontalLayoutGroup),
            typeof(UnityEngine.UI.VerticalLayoutGroup),
            typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup),
            typeof(DataKeeper.UI.SafeAreaUI),
            typeof(DataKeeper.DynamicScene.AddressableLoader),
            typeof(DataKeeper.DynamicScene.SubScene),
            typeof(TMP_Dropdown),
            typeof(TMP_InputField),
            typeof(TMP_Text),
            typeof(UnityEngine.UI.Image),
            typeof(HideFlagsManager),
            typeof(LifecycleEvents),
        };

        private static bool isEnabled => DataKeeperEditorPref.EnhanceHierarchy_Enabled.Value;

        private static PrefabHierarchyIcon PrefabHierarchyIconType =>
            DataKeeperEditorPref.EnhanceHierarchy_PrefabIconType.Value;
        
        private static HierarchyIconType HierarchyIconType =>
            DataKeeperEditorPref.EnhanceHierarchy_IconType.Value;

        // Colors
        private static readonly Color DefaultColorPro = new Color(0.219f, 0.219f, 0.219f);
        private static readonly Color DefaultColorLight = new Color(0.219f, 0.219f, 0.219f);

        private static readonly Color HoveredColorPro = new Color(0.270f, 0.270f, 0.270f);
        private static readonly Color HoveredColorLight = new Color(0.698f, 0.698f, 0.698f);
        
        private static readonly Color HoveredUnfocusColorPro = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color HoveredUnfocusColorLight = new Color(0.68f, 0.68f, 0.68f);
        
        private static readonly Color SelectedColorPro = new Color(0.172f, 0.364f, 0.529f);
        private static readonly Color SelectedColorLight = new Color(0.227f, 0.447f, 0.690f);

        private static readonly Color SelectedUnfocusColorPro = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color SelectedUnfocusColorLight = new Color(0.68f, 0.68f, 0.68f);
        
        private static readonly Color DisabledIconColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);


        private static Color DefaultColor => EditorGUIUtility.isProSkin
            ? DefaultColorPro
            : DefaultColorLight;

        private static Color HoveredColor => EditorGUIUtility.isProSkin
            ? HoveredColorPro
            : HoveredUnfocusColorLight;
        
        private static Color HoveredUnfocusColor => EditorGUIUtility.isProSkin
            ? HoveredUnfocusColorPro
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
            EditorApplication.update -= HandleEditorAppUpdate;
            EditorApplication.update += HandleEditorAppUpdate;
            EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }
        
        private static void HandleEditorAppUpdate()
        {
            if (!_hierarchyWindow && IsHierarchyWindowFocused())
            {
                _hierarchyWindow = EditorWindow.GetWindow(Type.GetType($"{nameof(UnityEditor)}.{SCENE_HIERARCHY_WINDOW}, {nameof(UnityEditor)}"));
            }

            _hierarchyHasFocus = EditorWindow.focusedWindow && EditorWindow.focusedWindow == _hierarchyWindow;
        }
        
        private static bool IsHierarchyWindowFocused()
        {
            EditorWindow window = EditorWindow.focusedWindow;
            return window != null && window.GetType().Name == SCENE_HIERARCHY_WINDOW;
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (!isEnabled) return;

            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;
            
            switch (HierarchyIconType)
            {
                case HierarchyIconType.All:
                    var primaryIcon = DrawPrimaryIcon(gameObject, instanceID, selectionRect);
                    DrawSecondaryIcons(gameObject, selectionRect, primaryIcon);
                    break;
                case HierarchyIconType.Primary:
                    DrawPrimaryIcon(gameObject, instanceID, selectionRect);
                    break;
                case HierarchyIconType.Secondary:
                    DrawSecondaryIcons(gameObject, selectionRect, null);
                    break;
            }
        }

        private static Component DrawPrimaryIcon(GameObject gameObject, int instanceID, Rect selectionRect)
        {
            Component targetComponent = GetPrimaryIconComponent(gameObject);
            if (targetComponent == null) return null;

            Texture icon = GetComponentIcon(targetComponent);
            if (icon == null) return null;

            Rect iconRect = new Rect(selectionRect);
            iconRect.width = 16f;
            iconRect.height = 16f;

            bool isSelected = Selection.Contains(instanceID);
            bool isHovered = selectionRect.Contains(Event.current.mousePosition);
            bool isClicked = isHovered && GetIsMouseDown();
            
            Color backgroundColor = DefaultColor;
            
            if (isSelected || isClicked)
            {
                backgroundColor = _hierarchyHasFocus ? SelectedColor : HoveredUnfocusColor;
            } 
            else if (isHovered)
            {
                backgroundColor = HoveredColor;
            }

            bool isPrefab = PrefabUtility.GetPrefabInstanceStatus(gameObject) != PrefabInstanceStatus.NotAPrefab;

            if (isPrefab)
            {
                switch (PrefabHierarchyIconType)
                {
                    case PrefabHierarchyIcon.Small:
                        iconRect.height *= 0.7f;
                        iconRect.width *= 0.7f;
                        iconRect.x += iconRect.width * 0.5f;
                        iconRect.y += iconRect.height * 0.5f;
                        break;
                    case PrefabHierarchyIcon.Big:
                        break;
                    case PrefabHierarchyIcon.Default:
                        return null;
                }
            }

            EditorGUI.DrawRect(iconRect, backgroundColor);

            _guiColor = GUI.color;
            if (!gameObject.activeInHierarchy)
            {
                GUI.color = DisabledIconColor;
            }

            GUI.DrawTexture(iconRect, icon);

            GUI.color = _guiColor;
            
            return targetComponent;
        }

        private static void DrawSecondaryIcons(GameObject gameObject, Rect selectionRect, Component exclude)
        {
            List<Component> secondaryComponentsFound = new List<Component>();
            
            // Collect all secondary components on this GameObject
            foreach (var componentType in priorityComponents)
            {
                Component component = gameObject.GetComponent(componentType);
                if (component != null && component != exclude)
                {
                    secondaryComponentsFound.Add(component);
                }
            }

            if (secondaryComponentsFound.Count == 0) return;

            // Calculate starting position (right side of the hierarchy item)
            float iconSize = 16f;
            float iconSpacing = 2f;
            float rightOffset = 0f;

            _guiColor = GUI.color;
            if (!gameObject.activeInHierarchy)
            {
                GUI.color = DisabledIconColor;
            }

            // Draw icons from right to left
            for (int i = secondaryComponentsFound.Count - 1; i >= 0; i--)
            {
                Component component = secondaryComponentsFound[i];
                Texture icon = GetComponentIcon(component);
                if (icon == null) continue;

                Rect iconRect = new Rect(
                    selectionRect.xMax - iconSize - rightOffset,
                    selectionRect.y,
                    iconSize,
                    iconSize
                );

                // Draw icon without background
                GUI.DrawTexture(iconRect, icon);

                rightOffset += iconSize + iconSpacing;
            }

            GUI.color = _guiColor;
        }

        // Gets the primary component to use for icon based on priority settings
        private static Component GetPrimaryIconComponent(GameObject gameObject)
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