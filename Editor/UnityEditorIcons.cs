using UnityEngine;
using UnityEditor;

namespace DataKeeper.Editor.Icons
{
    /// <summary>
    /// Static class containing constants for Unity Editor icons and utility methods to retrieve them.
    /// </summary>
    public static class UnityEditorIcons
    {
        #region Transform & Object Icons
        public const string TRANSFORM = "Transform Icon";
        public const string GAMEOBJECT = "GameObject Icon";
        public const string GAMEOBJECT_D = "d_GameObject Icon";
        public const string PREFAB = "Prefab Icon";
        public const string PREFAB_D = "d_Prefab Icon";
        public const string PREFAB_VARIANT = "PrefabVariant Icon";
        public const string PREFAB_MODEL = "PrefabModel Icon";
        #endregion

        #region View & Navigation Icons
        public const string VIEW_TOOL_ORBIT = "ViewToolOrbit";
        public const string VIEW_TOOL_PAN = "ViewToolPan";
        public const string VIEW_TOOL_ZOOM = "ViewToolZoom";
        public const string VIEW_TOOL_MOVE = "ViewToolMove";
        public const string SCENE_VIEW_CAMERA = "SceneViewCamera";
        public const string SCENE_VIEW_LIGHTING = "SceneViewLighting";
        public const string SCENE_VIEW_AUDIO = "SceneViewAudio";
        #endregion

        #region Collider Icons
        public const string BOX_COLLIDER = "BoxCollider Icon";
        public const string SPHERE_COLLIDER = "SphereCollider Icon";
        public const string CAPSULE_COLLIDER = "CapsuleCollider Icon";
        public const string MESH_COLLIDER = "MeshCollider Icon";
        public const string WHEEL_COLLIDER = "WheelCollider Icon";
        public const string TERRAIN_COLLIDER = "TerrainCollider Icon";
        #endregion

        #region Renderer Icons
        public const string MESH_RENDERER = "MeshRenderer Icon";
        public const string SKINNED_MESH_RENDERER = "SkinnedMeshRenderer Icon";
        public const string SPRITE_RENDERER = "SpriteRenderer Icon";
        public const string LINE_RENDERER = "LineRenderer Icon";
        public const string TRAIL_RENDERER = "TrailRenderer Icon";
        public const string BILLBOARD_RENDERER = "BillboardRenderer Icon";
        #endregion

        #region Light Icons
        public const string LIGHT = "Light Icon";
        public const string DIRECTIONAL_LIGHT = "DirectionalLight Icon";
        public const string POINT_LIGHT = "PointLight Icon";
        public const string SPOT_LIGHT = "SpotLight Icon";
        public const string AREA_LIGHT = "AreaLight Icon";
        public const string LIGHT_PROBE_GROUP = "LightProbeGroup Icon";
        public const string LIGHT_PROBE_PROXY_VOLUME = "LightProbeProxyVolume Icon";
        public const string REFLECTION_PROBE = "ReflectionProbe Icon";
        #endregion

        #region Camera Icons
        public const string CAMERA = "Camera Icon";
        public const string CAMERA_D = "d_Camera Icon";
        public const string CAMERA_PREVIEW = "CameraPreview";
        #endregion

        #region Audio Icons
        public const string AUDIO_SOURCE = "AudioSource Icon";
        public const string AUDIO_LISTENER = "AudioListener Icon";
        public const string AUDIO_REVERB_ZONE = "AudioReverbZone Icon";
        public const string AUDIO_LOW_PASS_FILTER = "AudioLowPassFilter Icon";
        public const string AUDIO_HIGH_PASS_FILTER = "AudioHighPassFilter Icon";
        public const string AUDIO_ECHO_FILTER = "AudioEchoFilter Icon";
        #endregion

        #region UI Icons
        public const string CANVAS = "Canvas Icon";
        public const string EVENT_SYSTEM = "EventSystem Icon";
        public const string GRAPHIC_RAYCASTER = "GraphicRaycaster Icon";
        public const string UI_BUTTON = "Button Icon";
        public const string UI_TEXT = "Text Icon";
        public const string UI_IMAGE = "Image Icon";
        public const string UI_SLIDER = "Slider Icon";
        public const string UI_SCROLLBAR = "Scrollbar Icon";
        public const string UI_TOGGLE = "Toggle Icon";
        public const string UI_INPUT_FIELD = "InputField Icon";
        public const string UI_DROPDOWN = "Dropdown Icon";
        public const string UI_SCROLL_VIEW = "ScrollView Icon";
        #endregion

        #region Animation Icons
        public const string ANIMATION = "Animation Icon";
        public const string ANIMATOR = "Animator Icon";
        public const string ANIMATION_CLIP = "AnimationClip Icon";
        public const string AVATAR = "Avatar Icon";
        public const string MOTION = "Motion Icon";
        #endregion

        #region Physics Icons
        public const string RIGIDBODY = "Rigidbody Icon";
        public const string RIGIDBODY_2D = "Rigidbody2D Icon";
        public const string FIXED_JOINT = "FixedJoint Icon";
        public const string HINGE_JOINT = "HingeJoint Icon";
        public const string SPRING_JOINT = "SpringJoint Icon";
        public const string CHARACTER_JOINT = "CharacterJoint Icon";
        public const string CONFIGURABLE_JOINT = "ConfigurableJoint Icon";
        #endregion

        #region Terrain Icons
        public const string TERRAIN = "Terrain Icon";
        public const string TERRAIN_DATA = "TerrainData Icon";
        public const string TREE = "Tree Icon";
        public const string WIND_ZONE = "WindZone Icon";
        #endregion

        #region Particle System Icons
        public const string PARTICLE_SYSTEM = "ParticleSystem Icon";
        public const string PARTICLE_SYSTEM_FORCE_FIELD = "ParticleSystemForceField Icon";
        #endregion

        #region Tool Icons
        public const string TOOL_MOVE = "MoveTool";
        public const string TOOL_ROTATE = "RotateTool";
        public const string TOOL_SCALE = "ScaleTool";
        public const string TOOL_RECT = "RectTool";
        public const string TOOL_SELECT = "SelectTool";
        public const string TOOL_VIEW = "ViewTool";
        #endregion

        #region Grid & Snap Icons
        public const string GRID = "Grid Icon";
        public const string GRID_D = "d_Grid Icon";
        public const string SNAP_INCREMENT = "SnapIncrement";
        public const string SNAP_VERTEX = "SnapVertex";
        public const string SNAP_GRID = "SnapGrid";
        #endregion

        #region File & Folder Icons
        public const string FOLDER = "Folder Icon";
        public const string FOLDER_EMPTY = "FolderEmpty Icon";
        public const string FOLDER_OPENED = "FolderOpened Icon";
        public const string ASSET = "DefaultAsset Icon";
        public const string SCRIPT = "cs Script Icon";
        public const string SHADER = "Shader Icon";
        public const string MATERIAL = "Material Icon";
        public const string TEXTURE = "Texture Icon";
        public const string TEXTURE_2D = "Texture2D Icon";
        public const string MESH = "Mesh Icon";
        public const string FONT = "Font Icon";
        #endregion

        #region Action Icons
        public const string DUPLICATE = "TreeEditor.Duplicate";
        public const string DUPLICATE_D = "d_TreeEditor.Duplicate";
        public const string DELETE = "TreeEditor.Trash";
        public const string REFRESH = "TreeEditor.Refresh";
        public const string CLIPBOARD = "Clipboard";
        public const string CLIPBOARD_D = "d_Clipboard";
        public const string SETTINGS = "Settings";
        public const string SETTINGS_D = "d_Settings";
        #endregion

        #region Console Icons
        public const string CONSOLE_INFO = "console.infoicon";
        public const string CONSOLE_INFO_SMALL = "console.infoicon.sml";
        public const string CONSOLE_WARNING = "console.warnicon";
        public const string CONSOLE_WARNING_SMALL = "console.warnicon.sml";
        public const string CONSOLE_ERROR = "console.erroricon";
        public const string CONSOLE_ERROR_SMALL = "console.erroricon.sml";
        #endregion

        #region Hierarchy Icons
        public const string HIERARCHY_ICON = "UnityEditor.HierarchyWindow";
        public const string PROJECT_ICON = "Project";
        public const string INSPECTOR_ICON = "UnityEditor.InspectorWindow";
        public const string SCENE_ICON = "UnityEditor.SceneView";
        public const string GAME_ICON = "UnityEditor.GameView";
        #endregion

        #region Playmode Icons
        public const string PLAY = "PlayButton";
        public const string PAUSE = "PauseButton";
        public const string STEP = "StepButton";
        public const string PLAY_ON = "PlayButton On";
        public const string PAUSE_ON = "PauseButton On";
        #endregion

        #region Visibility Icons
        public const string VISIBILITY_ON = "VisibilityOn";
        public const string VISIBILITY_OFF = "VisibilityOff";
        public const string EYE_OPEN = "ViewToolOrbit On";
        public const string EYE_CLOSED = "ViewToolOrbit";
        #endregion

        #region Package Manager Icons
        public const string PACKAGE_MANAGER = "Package Manager";
        public const string ASSET_STORE = "Asset Store";
        public const string UNITY_LOGO = "UnityLogo";
        #endregion

        #region Version Control Icons
        public const string COLLAB = "Collab";
        public const string COLLAB_BUILD = "Collab.Build";
        public const string COLLAB_CHANGES = "Collab.Changes";
        public const string COLLAB_CONFLICT = "Collab.Conflict";
        public const string VERSION_CONTROL = "VersionControl";
        #endregion

        #region Build Icons
        public const string BUILD_SETTINGS = "BuildSettings.Editor";
        public const string PLAYER_SETTINGS = "PlayerSettings";
        public const string PLATFORM_SETTINGS = "PlatformSettings";
        #endregion

        #region Profiler Icons
        public const string PROFILER = "Profiler.FirstFrame";
        public const string PROFILER_CPU = "Profiler.CPU";
        public const string PROFILER_GPU = "Profiler.GPU";
        public const string PROFILER_MEMORY = "Profiler.Memory";
        public const string PROFILER_AUDIO = "Profiler.Audio";
        public const string PROFILER_RENDERING = "Profiler.Rendering";
        #endregion

        #region Help Icons
        public const string HELP = "Help";
        public const string QUESTION_MARK = "_Help";
        public const string UNITY_EDITOR_HELP = "UnityEditor.ConsoleWindow";
        #endregion

        #region Custom Editor Icons
        public const string FAVORITE = "Favorite";
        public const string LABEL = "FilterByLabel";
        public const string SEARCH = "Search Icon";
        public const string FILTER = "FilterByType";
        public const string SORT = "AlphabeticalSorting";
        public const string LOCK = "IN LockButton on";
        public const string UNLOCK = "IN LockButton";
        #endregion

        #region 2D Icons
        public const string SPRITE_ATLAS = "SpriteAtlas Icon";
        public const string SPRITE_MASK = "SpriteMask Icon";
        public const string SORTING_GROUP = "SortingGroup Icon";
        public const string CANVAS_GROUP = "CanvasGroup Icon";
        public const string TILEMAP = "Tilemap Icon";
        public const string TILEMAP_RENDERER = "TilemapRenderer Icon";
        public const string TILE_PALETTE = "TilePalette";
        public const string GRID_2D = "Grid Icon";
        #endregion

        /// <summary>
        /// Gets a Unity Editor icon by its constant name.
        /// </summary>
        /// <param name="iconConstant">The icon constant from this class</param>
        /// <returns>GUIContent with the icon, or null if not found</returns>
        public static GUIContent GetIcon(string iconConstant)
        {
            if (string.IsNullOrEmpty(iconConstant))
                return null;

            var content = EditorGUIUtility.IconContent(iconConstant);
            return content?.image != null ? content : null;
        }

        /// <summary>
        /// Gets a Unity Editor icon texture by its constant name.
        /// </summary>
        /// <param name="iconConstant">The icon constant from this class</param>
        /// <returns>Texture2D of the icon, or null if not found</returns>
        public static Texture2D GetIconTexture(string iconConstant)
        {
            var content = GetIcon(iconConstant);
            return content?.image as Texture2D;
        }

        /// <summary>
        /// Checks if an icon exists in the Unity Editor.
        /// </summary>
        /// <param name="iconConstant">The icon constant to check</param>
        /// <returns>True if the icon exists and has a valid texture</returns>
        public static bool IconExists(string iconConstant)
        {
            if (string.IsNullOrEmpty(iconConstant))
                return false;

            var content = EditorGUIUtility.IconContent(iconConstant);
            return content?.image != null;
        }

        /// <summary>
        /// Gets an icon with fallback options if the primary icon is not found.
        /// </summary>
        /// <param name="primaryIcon">The preferred icon constant</param>
        /// <param name="fallbackIcons">Array of fallback icon constants</param>
        /// <returns>GUIContent with the first available icon, or null if none found</returns>
        public static GUIContent GetIconWithFallback(string primaryIcon, params string[] fallbackIcons)
        {
            var icon = GetIcon(primaryIcon);
            if (icon != null)
                return icon;

            if (fallbackIcons != null)
            {
                foreach (var fallbackIcon in fallbackIcons)
                {
                    icon = GetIcon(fallbackIcon);
                    if (icon != null)
                        return icon;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a GUIContent with both icon and text.
        /// </summary>
        /// <param name="text">The text content</param>
        /// <param name="iconConstant">The icon constant</param>
        /// <param name="tooltip">Optional tooltip text</param>
        /// <returns>GUIContent with text and icon</returns>
        public static GUIContent CreateContent(string text, string iconConstant, string tooltip = "")
        {
            var icon = GetIconTexture(iconConstant);
            return new GUIContent(text, icon, tooltip);
        }

        /// <summary>
        /// Gets all available icon constants as an array of strings.
        /// Useful for debugging or creating icon browsers.
        /// </summary>
        /// <returns>Array of all icon constant values</returns>
        public static string[] GetAllIconConstants()
        {
            var fields = typeof(UnityEditorIcons).GetFields(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Static | 
                System.Reflection.BindingFlags.FlattenHierarchy);

            var constants = new System.Collections.Generic.List<string>();
            
            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    constants.Add((string)field.GetRawConstantValue());
                }
            }

            return constants.ToArray();
        }

        /// <summary>
        /// Debug method to test if icons exist and log missing ones.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ValidateAllIcons()
        {
            var allIcons = GetAllIconConstants();
            int validCount = 0;
            int invalidCount = 0;

            Debug.Log($"Validating {allIcons.Length} Unity Editor icons...");

            foreach (var iconName in allIcons)
            {
                if (IconExists(iconName))
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogWarning($"Icon not found: {iconName}");
                }
            }

            Debug.Log($"Validation complete: {validCount} valid icons, {invalidCount} missing icons");
        }
    }
}