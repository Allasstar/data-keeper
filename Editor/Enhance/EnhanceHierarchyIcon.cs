using System;
using System.Collections.Generic;
using DataKeeper.Editor.Settings;
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
        private static List<System.Type> priorityComponents = new List<System.Type>();
        private static bool isEnabled => DataKeeperEditorPref.EnhanceHierarchyIconPref.Value;

        private static PrefabHierarchyIcon PrefabHierarchyIconType =>
            DataKeeperEditorPref.EnhanceHierarchyPrefabIconPref.Value;

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
            // Initialize with some default priority components if none are set
            if (priorityComponents.Count == 0)
            {
                priorityComponents.Add(typeof(Camera));
                priorityComponents.Add(typeof(Light));
                priorityComponents.Add(typeof(CharacterController));
                priorityComponents.Add(typeof(LODGroup));
                priorityComponents.Add(typeof(Rigidbody));
                priorityComponents.Add(typeof(Animator));
                priorityComponents.Add(typeof(Animation));
                priorityComponents.Add(typeof(AudioSource));
                priorityComponents.Add(typeof(Collider));
                priorityComponents.Add(typeof(ParticleSystem));
                priorityComponents.Add(typeof(MeshFilter));
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
                priorityComponents.Add(typeof(UnityEngine.UI.LayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.LayoutElement));
                priorityComponents.Add(typeof(UnityEngine.UI.ContentSizeFitter));
                priorityComponents.Add(typeof(UnityEngine.UI.AspectRatioFitter));
                priorityComponents.Add(typeof(UnityEngine.UI.GridLayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.HorizontalLayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.VerticalLayoutGroup));
                priorityComponents.Add(typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup));
                priorityComponents.Add(typeof(DataKeeper.UI.SafeAreaUI));
                priorityComponents.Add(typeof(UnityEngine.UI.Image));
                priorityComponents.Add(typeof(TextMeshProUGUI));
                priorityComponents.Add(typeof(TMP_InputField));
            }

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

            Component targetComponent = GetIconComponent(gameObject);
            if (targetComponent == null) return;

            Texture icon = GetComponentIcon(targetComponent);
            if (icon == null) return;

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
                        return;
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