using System.Collections.Generic;
using DataKeeper.Editor.Generic;
using DataKeeper.Editor.MeshTools;
using DataKeeper.Editor.Settings;
using DataKeeper.UIToolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows
{
    public class MeshToolsWindow : EditorWindow
    {
        private enum PivotMode { Transform, Bounds }

        private const int PivotTab = 0;
        private const int UVTab = 1;
        private const float ViewMinRange = -0.25f;
        private const float ViewMaxRange = 1.25f;
        private const float ViewPadding = 0.05f;
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 200f;
        private const float ZoomStep = 1.05f;

        private static readonly Color ViewBackground = new Color(0.16f, 0.16f, 0.16f);
        private static readonly Color FrameColor = new Color(1f, 1f, 1f, 0.4f);
        private static readonly Color BoundsColor = new Color(1f, 0.8f, 0.2f, 0.6f);
        private static readonly Color PivotTargetColor = new Color(1f, 0.8f, 0.2f, 1f);
        private static readonly List<string> AnchorXChoices = new List<string> { "Left", "Center", "Right" };
        private static readonly List<string> AnchorYChoices = new List<string> { "Bottom", "Center", "Top" };
        private static readonly List<string> AnchorZChoices = new List<string> { "Back", "Center", "Front" };
        private static readonly int UVViewControlHint = "MeshToolsUVView".GetHashCode();

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private int _tabIndex;
        [SerializeField] private PivotMode _pivotMode = PivotMode.Bounds;
        [SerializeField] private Transform _pivot;
        [SerializeField] private Vector3Int _boundsAnchor = new Vector3Int(1, 0, 1);
        [SerializeField] private int _uvChannel;
        [SerializeField] private Vector2 _uvScale = Vector2.one;
        [SerializeField] private Vector2 _uvOffset;
        [SerializeField] private bool _scenePreview = true;

        private readonly MeshUVPreview _preview = new MeshUVPreview();
        private IMGUIContainer _uvView;
        private Vector2Field _uvScaleField;
        private Vector2Field _uvOffsetField;
        private VisualElement _transformModeGroup;
        private VisualElement _boundsModeGroup;
        private Vector3[] _sourceSegments;
        private Vector3[] _previewSegments;
        private Rect _viewRect;
        private Vector2 _viewMin;
        private float _viewRange;
        private Vector2 _fitCenter;
        private float _fitRange;
        private float _zoom = 1f;
        private Vector2 _pan;
        private bool _viewPinned;

        [MenuItem("Tools/Windows/Mesh Tools", priority = 11)]
        public static void ShowWindow()
        {
            var window = GetWindow<MeshToolsWindow>();
            window.titleContent = new GUIContent("Mesh Tools", EditorGUIUtility.IconContent("Mesh Icon").image);
            window.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Rebind;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Rebind;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            _preview.EndScenePreview();
        }

        public void CreateGUI()
        {
            var root = new ScrollView(ScrollViewMode.Vertical)
                .SetPadding(10)
                .SetChildOf(rootVisualElement);

            var meshField = new ObjectField("Mesh")
                {
                    objectType = typeof(MeshFilter),
                    allowSceneObjects = true,
                    value = _meshFilter
                }
                .SetTooltip("Object with a MeshFilter. Imported and built-in meshes are copied to a new .asset; .asset meshes are edited in place")
                .SetMarginBottom(10)
                .SetChildOf(root);
            meshField.RegisterValueChangedCallback(evt =>
            {
                _meshFilter = evt.newValue as MeshFilter;
                ResetView();
                Rebind();
            });

            var tabView = new TabView().SetChildOf(root);
            var pivotTab = new Tab("Pivot").SetChildOf(tabView);
            var uvTab = new Tab("UV").SetChildOf(tabView);

            CreatePivotTab(pivotTab);
            CreateUVTab(uvTab);

            tabView.selectedTabIndex = _tabIndex;
            tabView.activeTabChanged += (previous, current) =>
            {
                _tabIndex = tabView.selectedTabIndex;
                UpdateScenePreview();
                SceneView.RepaintAll();
            };

            Rebind();
        }

        private void CreatePivotTab(VisualElement tab)
        {
            var modeGroup = new ToggleButtonGroup("Mode")
                {
                    isMultipleSelection = false,
                    allowEmptySelection = false
                }
                .SetMarginTop(5)
                .SetMarginBottom(5)
                .SetChildOf(tab);
            modeGroup.Add(new Button { text = "Transform", tooltip = "Pivot moves to a transform's position" });
            modeGroup.Add(new Button { text = "Bounds", tooltip = "Pivot moves to a point on the mesh bounds" });
            modeGroup.SetValueWithoutNotify(new ToggleButtonGroupState(1UL << (int)_pivotMode, 2));
            modeGroup.RegisterValueChangedCallback(evt =>
            {
                _pivotMode = evt.newValue[(int)PivotMode.Bounds] ? PivotMode.Bounds : PivotMode.Transform;
                RefreshPivotMode();
                SceneView.RepaintAll();
            });

            _transformModeGroup = new VisualElement().SetChildOf(tab);
            new ObjectField("New Pivot")
                {
                    objectType = typeof(Transform),
                    allowSceneObjects = true,
                    value = _pivot
                }
                .SetTooltip("The mesh pivot moves to this transform's position")
                .SetMarginBottom(5)
                .SetChildOf(_transformModeGroup)
                .RegisterValueChangedCallback(evt =>
                {
                    _pivot = evt.newValue as Transform;
                    SceneView.RepaintAll();
                });

            _boundsModeGroup = new VisualElement().SetChildOf(tab);
            CreateAnchorField("X", AnchorXChoices, 0, _boundsModeGroup);
            CreateAnchorField("Y", AnchorYChoices, 1, _boundsModeGroup);
            CreateAnchorField("Z", AnchorZChoices, 2, _boundsModeGroup);

            RefreshPivotMode();

            new Button(ApplyPivot)
                .SetTextValue("Apply Pivot")
                .SetTooltip("Move the mesh pivot to the target (shown in the Scene view) without moving the mesh in the scene")
                .SetHeight(25)
                .SetMarginTop(5)
                .SetChildOf(tab);
        }

        private void CreateUVTab(VisualElement tab)
        {
            var channelChoices = new List<string>(MeshEditUtility.UVChannelCount);
            for (int i = 0; i < MeshEditUtility.UVChannelCount; i++)
                channelChoices.Add($"UV{i}");

            new DropdownField("Channel", channelChoices, _uvChannel)
                .SetMarginTop(5)
                .SetMarginBottom(5)
                .SetChildOf(tab)
                .RegisterValueChangedCallback(evt =>
                {
                    _uvChannel = channelChoices.IndexOf(evt.newValue);
                    Rebind();
                });

            _uvScaleField = new Vector2Field("Scale") { value = _uvScale }
                .SetMarginBottom(5)
                .SetChildOf(tab);
            _uvScaleField.RegisterValueChangedCallback(evt =>
            {
                _uvScale = evt.newValue;
                RefreshPreview();
            });

            _uvOffsetField = new Vector2Field("Offset") { value = _uvOffset }
                .SetMarginBottom(5)
                .SetChildOf(tab);
            _uvOffsetField.RegisterValueChangedCallback(evt =>
            {
                _uvOffset = evt.newValue;
                RefreshPreview();
            });

            new Toggle("Preview in Scene") { value = _scenePreview }
                .SetTooltip("Show the edited UVs on the object until Apply. The scene keeps the original mesh when saved")
                .SetMarginBottom(5)
                .SetChildOf(tab)
                .RegisterValueChangedCallback(evt =>
                {
                    _scenePreview = evt.newValue;
                    UpdateScenePreview();
                    SceneView.RepaintAll();
                });

            CreateLineColorField("Original Lines", DataKeeperEditorPref.MeshTools_SourceEdgeColor, tab);
            CreateLineColorField("Edited Lines", DataKeeperEditorPref.MeshTools_PreviewEdgeColor, tab);

            _uvView = new IMGUIContainer(DrawUVView)
                .SetTooltip("Wheel: zoom   Drag: pan   Double-click: fit")
                .SetHeight(280)
                .SetMarginBottom(5)
                .SetChildOf(tab);

            new Button(ApplyUV)
                .SetTextValue("Apply UV")
                .SetTooltip("uv * scale + offset, same math as material Tiling/Offset")
                .SetHeight(25)
                .SetChildOf(tab);
        }

        private void RefreshPivotMode()
        {
            _transformModeGroup.style.display = _pivotMode == PivotMode.Transform ? DisplayStyle.Flex : DisplayStyle.None;
            _boundsModeGroup.style.display = _pivotMode == PivotMode.Bounds ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CreateAnchorField(string label, List<string> choices, int axis, VisualElement parent)
        {
            new DropdownField(label, choices, _boundsAnchor[axis])
                .SetMarginBottom(5)
                .SetChildOf(parent)
                .RegisterValueChangedCallback(evt =>
                {
                    _boundsAnchor[axis] = choices.IndexOf(evt.newValue);
                    SceneView.RepaintAll();
                });
        }

        private void CreateLineColorField(string label, ReactiveEditorPref<Color> pref, VisualElement parent)
        {
            new ColorField(label) { value = pref.Value }
                .SetMarginBottom(5)
                .SetChildOf(parent)
                .RegisterValueChangedCallback(evt =>
                {
                    pref.Value = evt.newValue;
                    _uvView.MarkDirtyRepaint();
                });
        }

        private void Rebind()
        {
            _preview.Bind(_meshFilter, _uvChannel);
            _preview.SetTransform(_uvScale, _uvOffset);
            UpdateScenePreview();

            _uvView?.MarkDirtyRepaint();
            SceneView.RepaintAll();
        }

        // Swapped-in UVs would be confusing while placing a pivot, so the scene preview only runs on the UV tab.
        private void UpdateScenePreview()
        {
            if (_scenePreview && _tabIndex == UVTab)
                _preview.BeginScenePreview();
            else
                _preview.EndScenePreview();
        }

        private void RefreshPreview()
        {
            _preview.SetTransform(_uvScale, _uvOffset);
            _uvView.MarkDirtyRepaint();
            SceneView.RepaintAll();
        }

        private void OnSceneSaving(Scene scene, string path) => _preview.EndScenePreview();

        private void OnSceneSaved(Scene scene) => UpdateScenePreview();

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                _preview.EndScenePreview();
            else if (change == PlayModeStateChange.EnteredEditMode)
                Rebind();
        }

        private bool TryGetPivotTarget(out Vector3 worldPivot)
        {
            worldPivot = default;
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
                return false;

            if (_pivotMode == PivotMode.Transform)
            {
                if (_pivot == null)
                    return false;
                worldPivot = _pivot.position;
                return true;
            }

            var bounds = _meshFilter.sharedMesh.bounds;
            var local = bounds.min + Vector3.Scale(bounds.size, (Vector3)_boundsAnchor * 0.5f);
            worldPivot = _meshFilter.transform.TransformPoint(local);
            return true;
        }

        private void ApplyPivot()
        {
            if (!TryGetPivotTarget(out var worldPivot))
            {
                Debug.LogWarning(_pivotMode == PivotMode.Transform
                    ? "Assign a Mesh object with a mesh and a New Pivot transform."
                    : "Assign a Mesh object with a mesh.");
                return;
            }

            _preview.EndScenePreview();
            MeshEditUtility.SetPivot(_meshFilter, worldPivot);
            Rebind();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint || _tabIndex != PivotTab || _meshFilter == null || _meshFilter.sharedMesh == null)
                return;

            var bounds = _meshFilter.sharedMesh.bounds;
            using (new Handles.DrawingScope(BoundsColor, _meshFilter.transform.localToWorldMatrix))
                Handles.DrawWireCube(bounds.center, bounds.size);

            if (!TryGetPivotTarget(out var worldPivot))
                return;

            using (new Handles.DrawingScope(PivotTargetColor))
                Handles.SphereHandleCap(0, worldPivot, Quaternion.identity, HandleUtility.GetHandleSize(worldPivot) * 0.15f, EventType.Repaint);
        }

        private void ApplyUV()
        {
            if (_meshFilter == null)
            {
                Debug.LogWarning("Assign a Mesh object with a mesh.");
                return;
            }

            _preview.EndScenePreview();
            if (_meshFilter.sharedMesh != null)
                MeshEditUtility.TransformUV(_meshFilter, _uvChannel, _uvScale, _uvOffset);

            // The edit is now baked into the mesh, so the fields return to identity to keep the preview truthful.
            _uvScale = Vector2.one;
            _uvOffset = Vector2.zero;
            _uvScaleField.SetValueWithoutNotify(_uvScale);
            _uvOffsetField.SetValueWithoutNotify(_uvOffset);
            Rebind();
        }

        private void DrawUVView()
        {
            var bounds = _uvView.contentRect;
            var size = Mathf.Min(bounds.width, bounds.height);
            var viewRect = new Rect(bounds.x + (bounds.width - size) * 0.5f, bounds.y + (bounds.height - size) * 0.5f, size, size);
            FitView(size);

            var current = Event.current;
            if (current.type != EventType.Repaint)
            {
                HandleViewInput(current, viewRect);
                return;
            }

            EditorGUI.DrawRect(viewRect, ViewBackground);
            EditorGUIUtility.AddCursorRect(viewRect, MouseCursor.Pan);

            // Handles ignore the IMGUIContainer bounds, so lines are clipped explicitly; inside the clip, coordinates are local.
            GUI.BeginClip(viewRect);

            var topLeft = ToView(new Vector2(0f, 1f));
            var bottomRight = ToView(new Vector2(1f, 0f));
            var unitRect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);

            if (_preview.MainTexture != null)
                GUI.DrawTexture(unitRect, _preview.MainTexture, ScaleMode.StretchToFill, true);

            Handles.DrawSolidRectangleWithOutline(unitRect, Color.clear, FrameColor);

            if (_preview.Edges.Count == 0)
            {
                GUI.Label(_viewRect, _meshFilter == null ? "No mesh" : $"No UV{_uvChannel}", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawEdges(_preview.SourceUVs, DataKeeperEditorPref.MeshTools_SourceEdgeColor.Value, ref _sourceSegments);
                DrawEdges(_preview.PreviewUVs, DataKeeperEditorPref.MeshTools_PreviewEdgeColor.Value, ref _previewSegments);
            }

            GUI.EndClip();
        }

        private void HandleViewInput(Event current, Rect viewRect)
        {
            var controlId = GUIUtility.GetControlID(UVViewControlHint, FocusType.Passive, viewRect);
            var local = current.mousePosition - viewRect.position;

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.ScrollWheel when viewRect.Contains(current.mousePosition):
                    ZoomAt(local, Mathf.Pow(ZoomStep, -current.delta.y), viewRect.width);
                    current.Use();
                    break;

                case EventType.MouseDown when viewRect.Contains(current.mousePosition):
                    if (current.clickCount == 2)
                        ResetView();
                    else
                        GUIUtility.hotControl = controlId;
                    current.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    _viewPinned = true;
                    var unitsPerPixel = _viewRange / viewRect.width;
                    _pan.x -= current.delta.x * unitsPerPixel;
                    _pan.y += current.delta.y * unitsPerPixel;
                    current.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;

                default:
                    return;
            }

            _uvView.MarkDirtyRepaint();
        }

        private void ZoomAt(Vector2 local, float factor, float size)
        {
            _viewPinned = true;
            var anchor = FromView(local);
            _zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
            FitView(size);
            _pan += anchor - FromView(local);
        }

        private void ResetView()
        {
            _zoom = 1f;
            _pan = Vector2.zero;
            _viewPinned = false;
            _uvView?.MarkDirtyRepaint();
        }

        private void FitView(float size)
        {
            _viewRect = new Rect(0f, 0f, size, size);

            // Once the user zooms or pans, the fit stops following the UV bounds so editing values doesn't move the view.
            if (!_viewPinned)
            {
                var uvBounds = _preview.Edges.Count > 0 ? _preview.Bounds : Rect.MinMaxRect(0f, 0f, 1f, 1f);
                var min = new Vector2(Mathf.Min(uvBounds.xMin, ViewMinRange), Mathf.Min(uvBounds.yMin, ViewMinRange));
                var max = new Vector2(Mathf.Max(uvBounds.xMax, ViewMaxRange), Mathf.Max(uvBounds.yMax, ViewMaxRange));

                _fitCenter = (min + max) * 0.5f;
                _fitRange = Mathf.Max(max.x - min.x, max.y - min.y) * (1f + ViewPadding * 2f);
            }

            _viewRange = _fitRange / _zoom;
            _viewMin = _fitCenter + _pan - new Vector2(_viewRange, _viewRange) * 0.5f;
        }

        private void DrawEdges(List<Vector2> uvs, Color color, ref Vector3[] segments)
        {
            var edges = _preview.Edges;
            if (segments == null || segments.Length != edges.Count)
                segments = new Vector3[edges.Count];

            for (int i = 0; i < edges.Count; i++)
                segments[i] = ToView(uvs[edges[i]]);

            using (new Handles.DrawingScope(color))
                Handles.DrawLines(segments);
        }

        private Vector3 ToView(Vector2 uv)
        {
            return new Vector3(
                _viewRect.x + (uv.x - _viewMin.x) / _viewRange * _viewRect.width,
                _viewRect.y + (1f - (uv.y - _viewMin.y) / _viewRange) * _viewRect.height,
                0f);
        }

        private Vector2 FromView(Vector2 local)
        {
            return new Vector2(
                _viewMin.x + (local.x - _viewRect.x) / _viewRect.width * _viewRange,
                _viewMin.y + (1f - (local.y - _viewRect.y) / _viewRect.height) * _viewRange);
        }
    }
}
