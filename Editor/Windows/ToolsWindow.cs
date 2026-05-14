using System;
using DataKeeper.Editor.MenuItems;
using DataKeeper.Extensions;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using DataKeeper.UIToolkit;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using DataKeeper.UIToolkit.Elements;

namespace DataKeeper.Editor.Windows
{
    public class ToolsWindow : EditorWindow
    {
        private VisualElement root;

        private FloatField cellSizeField;

        // Buffer fields
        private struct TransformSnapshot
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
            public string sourceName;
            public bool hasData;
        }

        private const int BufferSlotCount = 5;
        private static TransformSnapshot[] bufferSlots = new TransformSnapshot[BufferSlotCount];
        private static int activeSlotIndex = 0;
        private static bool bufferUseWorldSpace = true;
        private static bool bufferCopyPos = true;
        private static bool bufferCopyRot = true;
        private static bool bufferCopyScale = true;

        // UI references for buffer
        private static Label bufferLabel;
        private VisualElement bufferSlotsContainer;

        // Add these fields to your class
        private Slider timeScaleSlider;
        private float timeScale = 1f;

        // Scene management fields
        private ScrollView mainContainer;
        private VisualElement sceneView;
        private static readonly Color SceneBorderColorLoaded = new Color(0.3f, 0.3f, 0.7f);
        private static readonly Color SceneBorderColorNormal = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color SceneLabelColorNormal = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color SceneLabelColorLoaded = Color.white;

        [MenuItem("Tools/Windows/Tools", priority = 10)]
        public static void ShowWindow()
        {
            ToolsWindow window = GetWindow<ToolsWindow>();
            window.titleContent = new GUIContent("Tools", EditorGUIUtility.IconContent("Transform Icon").image);
            window.minSize = new Vector2(300, 400);
            window.maxSize = new Vector2(300, 800);
        }

        public void CreateGUI()
        {
            root = rootVisualElement;

            mainContainer = new ScrollView(ScrollViewMode.Vertical)
                .SetPadding(10);

            CreateGroundSnapSection(mainContainer);
            CreateGroupingToolsSection(mainContainer);
            CreateTimeScaleSection(mainContainer);
            CreateBuffersSection(mainContainer);
            CreateShortcutsSection(mainContainer);
            CreateSceneManagementSection(mainContainer);

            root.Add(mainContainer);
        }

        private void SubRefreshSceneListEvent(bool isVisible)
        {
            if (isVisible)
            {
                EditorSceneEvent.SubscribeToEvents(RefreshSceneList);
                RefreshSceneList();
            }
            else
            {
                EditorSceneEvent.UnsubscribeFromEvents(RefreshSceneList);
            }
        }

        private void CreateSceneManagementSection(VisualElement parent)
        {
            var section = CreateSection("Scene Management", parent, SubRefreshSceneListEvent);

            sceneView = new VisualElement();
            section.Add(sceneView);

            RefreshSceneList();
        }

        private void RefreshSceneList()
        {
            sceneView.Clear();

            // Get scenes from build settings
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int loadedSceneCount = SceneManager.sceneCount;

            if (scenes.Length == 0)
            {
                var helpBox = new HelpBox("No scenes found in Build Settings", HelpBoxMessageType.Warning);
                sceneView.Add(helpBox);
                return;
            }

            foreach (var buildScene in scenes)
            {
                string scenePath = buildScene.path;
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                // Get scene status
                Scene sceneObject = SceneManager.GetSceneByPath(scenePath);
                bool isLoaded = sceneObject.isLoaded;

                // Create scene container
                var sceneContainer = new VisualElement()
                    .SetMarginBottom(5)
                    .SetPadding(5)
                    .SetBorderRadius(3)
                    .SetBorderWidth(1)
                    .SetBorderColor(isLoaded ? SceneBorderColorLoaded : SceneBorderColorNormal);

                // Scene name label
                var nameLabel = new Label(sceneName + (sceneObject.isDirty ? "*" : ""))
                    .SetFontStyle(isLoaded ? FontStyle.Bold : FontStyle.Normal)
                    .SetColor(isLoaded ? SceneLabelColorLoaded : SceneLabelColorNormal)
                    .SetMarginBottom(3);

                sceneContainer.Add(nameLabel);

                // Button container
                var buttonContainer = new VisualElement()
                    .SetFlexRow();

                // Load button
                var loadBtn = new Button(() => LoadScene(scenePath))
                    .SetText("Load")
                    .SetFlexGrow(1)
                    .SetMarginRight(2)
                    .SetHeight(20)
                    .SetEnabledSelf(!isLoaded);

                // Add button
                var addBtn = new Button(() => LoadSceneAdditive(scenePath))
                    .SetText("Add")
                    .SetFlexGrow(1)
                    .SetMarginRight(2)
                    .SetHeight(20)
                    .SetEnabledSelf(!isLoaded);

                // Unload button
                var unloadBtn = new Button(() => UnloadScene(scenePath))
                    .SetText("Unload")
                    .SetFlexGrow(1)
                    .SetMarginRight(2)
                    .SetHeight(20)
                    .SetEnabledSelf(isLoaded && loadedSceneCount > 1);

                // Save button
                var saveBtn = new Button(() => SaveScene(sceneObject))
                    .SetText("Save")
                    .SetFlexGrow(1)
                    .SetHeight(20)
                    .SetEnabledSelf(isLoaded && sceneObject.isDirty);

                buttonContainer.Add(loadBtn);
                buttonContainer.Add(addBtn);
                buttonContainer.Add(unloadBtn);
                buttonContainer.Add(saveBtn);

                sceneContainer.Add(buttonContainer);
                sceneView.Add(sceneContainer);
            }
        }

        // Scene management methods
        private void LoadScene(string scenePath)
        {
            try
            {
                if (SaveDirtyScenesPrompt())
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Debug.Log($"Scene loaded: {scenePath}");
                    RefreshSceneList();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene {scenePath}: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Scene Loading Error",
                    $"Could not load scene:\n{scenePath}\n\nError: {e.Message}",
                    "OK"
                );
            }
        }

        private void LoadSceneAdditive(string scenePath)
        {
            try
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                Debug.Log($"Scene loaded additively: {scenePath}");
                RefreshSceneList();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene additively {scenePath}: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Scene Loading Error",
                    $"Could not load scene additively:\n{scenePath}\n\nError: {e.Message}",
                    "OK"
                );
            }
        }

        private void UnloadScene(string scenePath)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.isLoaded)
                {
                    if (scene.isDirty)
                    {
                        bool saveScene = EditorUtility.DisplayDialog(
                            "Unsaved Changes",
                            $"Scene '{scene.name}' has unsaved changes. Do you want to save before unloading?",
                            "Save",
                            "Discard Changes"
                        );

                        if (saveScene)
                        {
                            EditorSceneManager.SaveScene(scene);
                        }
                    }

                    EditorSceneManager.CloseScene(scene, true);
                    Debug.Log($"Scene unloaded: {scenePath}");
                    RefreshSceneList();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to unload scene {scenePath}: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Scene Unloading Error",
                    $"Could not unload scene:\n{scenePath}\n\nError: {e.Message}",
                    "OK"
                );
            }
        }

        private void SaveScene(Scene scene)
        {
            try
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Scene saved: {scene.name}");
                RefreshSceneList();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save scene {scene.name}: {e.Message}");
            }
        }

        private bool SaveDirtyScenesPrompt()
        {
            var dirtyScenesNames = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    dirtyScenesNames.Add(scene.name);
                }
            }

            if (dirtyScenesNames.Count == 0)
                return true;

            int option = EditorUtility.DisplayDialogComplex(
                "Unsaved Changes",
                $"The following scenes have unsaved changes:\n\n{string.Join("\n", dirtyScenesNames)}\n\nYour changes will be lost if you don't save them.\n\nWhat would you like to do?",
                "Save All",
                "Discard All",
                "Cancel"
            );

            switch (option)
            {
                case 0: // Save All
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.isDirty)
                        {
                            EditorSceneManager.SaveScene(scene);
                        }
                    }

                    return true;

                case 1: // Discard All
                    return true;

                case 2: // Cancel
                    return false;

                default:
                    return false;
            }
        }

        private void CreateTimeScaleSection(VisualElement parent)
        {
            var section = CreateSection("Time Scale", parent);

            // Create horizontal container (similar to EditorGUILayout.BeginHorizontal)
            var timeScaleContainer = new VisualElement()
                .SetFlexRow()
                .SetFlexGrow(1)
                .SetMarginBottom(5);

            // TimeScale slider
            timeScaleSlider = new Slider(0f, 10f)
                .SetFlexGrow(1f);

            var floatField = new FloatField()
                .SetWidth(50);

            floatField.value = timeScale;
            timeScaleSlider.value = timeScale;

            floatField.RegisterValueChangedCallback(evt =>
            {
                timeScale = evt.newValue;
                timeScaleSlider.SetValueWithoutNotify(evt.newValue);
            });

            timeScaleSlider.RegisterValueChangedCallback(evt =>
            {
                timeScale = evt.newValue;
                floatField.SetValueWithoutNotify(evt.newValue);
            });

            var buttonContainer = new VisualElement()
                .SetFlexRow();

            // Apply button
            var applyBtn = new Button(() => { Time.timeScale = timeScale; })
                .SetText("Apply")
                .SetMarginRight(2)
                .SetChildOf(buttonContainer);

            // Reset button
            var resetBtn = new Button(() =>
                {
                    timeScale = 1f;
                    Time.timeScale = 1f;
                    timeScaleSlider.value = timeScale;
                })
                .SetText("Reset")
                .SetMarginRight(2)
                .SetChildOf(buttonContainer);

            // Refresh button
            var refreshBtn = new Button(() =>
                {
                    timeScale = Time.timeScale;
                    timeScaleSlider.value = timeScale;
                })
                .SetText("Refresh")
                .SetChildOf(buttonContainer);

            // Add all elements to container
            timeScaleContainer.Add(timeScaleSlider);
            timeScaleContainer.Add(floatField);

            section.Add(timeScaleContainer);
            section.Add(buttonContainer);
        }

        private void CreateBuffersSection(VisualElement parent)
        {
            var section = CreateSection("Buffer", parent);

            // ── World / Local space toggle ──
            var spaceRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(4)
                .SetChildOf(section);

            var worldBtn = new Button(() => SetBufferSpace(true))
                .SetText("World")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetChildOf(spaceRow);

            var localBtn = new Button(() => SetBufferSpace(false))
                .SetText("Local")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetChildOf(spaceRow);

            RefreshSpaceButtons(worldBtn, localBtn);

            // ── Component filter toggles ──
            var filterRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(6)
                .SetChildOf(section);

            new ToggleButton("Pos", bufferCopyPos, v => bufferCopyPos = v)
                .SetFlexGrow(1).SetMarginRight(4).SetChildOf(filterRow);
            new ToggleButton("Rot", bufferCopyRot, v => bufferCopyRot = v)
                .SetFlexGrow(1).SetMarginRight(4).SetChildOf(filterRow);
            new ToggleButton("Scale", bufferCopyScale, v => bufferCopyScale = v)
                .SetFlexGrow(1).SetChildOf(filterRow);


            // ── Copy / Paste / Paste Offset buttons ──
            var actionRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(6);

            var copyBtn = new Button(CopyTransform)
                .SetText("Copy")
                .SetFlexGrow(1)
                .SetHeight(22)
                .SetMarginRight(2);
            copyBtn.tooltip = "Copy transform of selected object into active slot";

            var pasteBtn = new Button(PasteTransform)
                .SetText("Paste")
                .SetFlexGrow(1)
                .SetHeight(22)
                .SetMarginRight(2);
            pasteBtn.tooltip = "Paste active slot values onto selected objects";

            var pasteOffsetBtn = new Button(PasteTransformOffset)
                .SetText("Paste Offset")
                .SetFlexGrow(1)
                .SetHeight(22);
            pasteOffsetBtn.tooltip = "Add active slot values as offset to selected objects";

            actionRow.Add(copyBtn);
            actionRow.Add(pasteBtn);
            actionRow.Add(pasteOffsetBtn);
            section.Add(actionRow);

            // ── Clipboard slots ──
            bufferSlotsContainer = new VisualElement()
                .SetMarginBottom(6);

            RefreshBufferSlotsUI();
            section.Add(bufferSlotsContainer);

            // ── Readout label ──
            bufferLabel = new Label()
                .SetFontSize(10);
            bufferLabel.style.whiteSpace = WhiteSpace.Normal;
            section.Add(bufferLabel);

            RefreshBufferLabel();

            // store button refs so we can re-style on toggle
            worldBtn.userData = (Action)(() => RefreshSpaceButtons(worldBtn, localBtn));
            localBtn.userData = (Action)(() => RefreshSpaceButtons(worldBtn, localBtn));

            // re-wire now that userData is set
            worldBtn.clicked += () => (worldBtn.userData as Action)?.Invoke();
            localBtn.clicked += () => (localBtn.userData as Action)?.Invoke();
        }

// ── Space toggle helpers ──

        private static void SetBufferSpace(bool world)
        {
            bufferUseWorldSpace = world;
        }

        private static void RefreshSpaceButtons(Button worldBtn, Button localBtn)
        {
            var activeColor = new Color(0.3f, 0.5f, 0.8f);
            var inactiveColor = new Color(0.3f, 0.3f, 0.3f);
            worldBtn.style.backgroundColor = bufferUseWorldSpace ? activeColor : inactiveColor;
            localBtn.style.backgroundColor = bufferUseWorldSpace ? inactiveColor : activeColor;
        }

// ── Slot UI ──

        private void RefreshBufferSlotsUI()
        {
            bufferSlotsContainer.Clear();

            for (int i = 0; i < BufferSlotCount; i++)
            {
                int idx = i; // capture
                var slot = bufferSlots[i];
                bool isActive = idx == activeSlotIndex;

                var row = new VisualElement()
                    .SetFlexRow()
                    .SetMarginBottom(2)
                    .SetPadding(3)
                    .SetBorderRadius(3)
                    .SetBorderWidth(1)
                    .SetBorderColor(isActive ? new Color(0.3f, 0.5f, 0.8f) : new Color(0.25f, 0.25f, 0.25f));

                // Slot label
                string slotText = slot.hasData
                    ? $"[{idx + 1}] {slot.sourceName}"
                    : $"[{idx + 1}] empty";

                var label = new Label(slotText)
                    .SetFlexGrow(1)
                    .SetAlignSelf(Align.Center)
                    .SetFontSize(11);

                if (isActive)
                    label.SetFontStyle(FontStyle.Bold);

                // Activate button
                var activateBtn = new Button(() =>
                    {
                        activeSlotIndex = idx;
                        RefreshBufferSlotsUI();
                        RefreshBufferLabel();
                    })
                    .SetText("◀")
                    .SetWidth(24)
                    .SetHeight(18);
                activateBtn.tooltip = "Set as active slot";

                // Clear button
                var clearBtn = new Button(() =>
                    {
                        bufferSlots[idx] = default;
                        if (activeSlotIndex == idx) RefreshBufferLabel();
                        RefreshBufferSlotsUI();
                    })
                    .SetText("✕")
                    .SetWidth(24)
                    .SetHeight(18);
                clearBtn.tooltip = "Clear this slot";
                clearBtn.SetEnabled(slot.hasData);

                row.Add(label);
                row.Add(activateBtn);
                row.Add(clearBtn);
                bufferSlotsContainer.Add(row);
            }
        }

        private static void RefreshBufferLabel()
        {
            if (bufferLabel == null) return;
            var s = bufferSlots[activeSlotIndex];
            if (!s.hasData)
            {
                bufferLabel.text = "Active slot is empty";
                return;
            }

            bufferLabel.text = $"p: {s.position}\nr: {s.rotation}\nsc: {s.scale}";
        }

// ── Core operations ──

        private void CopyTransform()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("No object selected to copy transform from");
                return;
            }

            var snap = new TransformSnapshot
            {
                hasData = true,
                sourceName = selected.name
            };

            if (bufferUseWorldSpace)
            {
                snap.position = selected.transform.position;
                snap.rotation = selected.transform.eulerAngles;
                snap.scale = selected.transform.lossyScale;
            }
            else
            {
                snap.position = selected.transform.localPosition;
                snap.rotation = selected.transform.localEulerAngles;
                snap.scale = selected.transform.localScale;
            }

            bufferSlots[activeSlotIndex] = snap;
            RefreshBufferSlotsUI();
            RefreshBufferLabel();
            Debug.Log($"Copied transform from '{selected.name}' into slot {activeSlotIndex + 1}");
        }

        private static void PasteTransform()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("No objects selected to paste transform to");
                return;
            }

            var snap = bufferSlots[activeSlotIndex];
            if (!snap.hasData)
            {
                Debug.LogWarning($"Slot {activeSlotIndex + 1} is empty. Copy a transform first.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Paste Transform");

            foreach (var obj in selected)
            {
                Undo.RecordObject(obj.transform, "Paste Transform");

                if (bufferUseWorldSpace)
                {
                    if (bufferCopyPos) obj.transform.position = snap.position;
                    if (bufferCopyRot) obj.transform.eulerAngles = snap.rotation;
                    if (bufferCopyScale) obj.transform.localScale = snap.scale;
                }
                else
                {
                    if (bufferCopyPos) obj.transform.localPosition = snap.position;
                    if (bufferCopyRot) obj.transform.localEulerAngles = snap.rotation;
                    if (bufferCopyScale) obj.transform.localScale = snap.scale;
                }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            Debug.Log($"Pasted transform (slot {activeSlotIndex + 1}) to {selected.Length} object(s)");
        }

        private static void PasteTransformOffset()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("No objects selected");
                return;
            }

            var snap = bufferSlots[activeSlotIndex];
            if (!snap.hasData)
            {
                Debug.LogWarning($"Slot {activeSlotIndex + 1} is empty. Copy a transform first.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Paste Transform Offset");

            foreach (var obj in selected)
            {
                Undo.RecordObject(obj.transform, "Paste Transform Offset");

                if (bufferUseWorldSpace)
                {
                    if (bufferCopyPos) obj.transform.position += snap.position;
                    if (bufferCopyRot) obj.transform.eulerAngles += snap.rotation;
                    if (bufferCopyScale) obj.transform.localScale += snap.scale - Vector3.one; // delta from identity
                }
                else
                {
                    if (bufferCopyPos) obj.transform.localPosition += snap.position;
                    if (bufferCopyRot) obj.transform.localEulerAngles += snap.rotation;
                    if (bufferCopyScale) obj.transform.localScale += snap.scale - Vector3.one;
                }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            Debug.Log($"Applied transform offset (slot {activeSlotIndex + 1}) to {selected.Length} object(s)");
        }

        private void CreateShortcutsSection(VisualElement parent)
        {
            var section = CreateSection("Shortcuts", parent);

            var saveProjectBtn = CreateIconButton(
                "Save Project",
                "d_SaveAs",
                SaveProject);
            saveProjectBtn.tooltip = "Save all modified assets and dirty scenes";
            section.Add(saveProjectBtn);

            var reloadDomainBtn = CreateIconButton(
                "Reload Domain",
                "d_RotateTool",
                ReloadDomain);
            reloadDomainBtn.tooltip = "Force a script domain reload";
            section.Add(reloadDomainBtn);

            var recompileBtn = CreateIconButton(
                "Recompile Scripts",
                "d_cs Script Icon",
                RecompileScripts);
            recompileBtn.tooltip = "Request a full script recompilation";
            section.Add(recompileBtn);
        }

        private static void SaveProject()
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Project saved.");
        }

        private static void ReloadDomain()
        {
            EditorUtility.RequestScriptReload();
            Debug.Log("Domain reload requested.");
        }

        private static void RecompileScripts()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log("Script recompilation requested.");
        }

        private void CreateGroundSnapSection(VisualElement parent)
        {
            var section = CreateSection("Ground Snap Tools", parent);

            var bringToViewBtn = CreateIconButton(
                "Bring Selected to View",
                "ViewToolOrbit",
                SnapToGroundEditor.PerformSnapToScreenCenter);

            bringToViewBtn.tooltip = "Move Selected Objects to Scene view";
            section.Add(bringToViewBtn);

            var snapOriginBtn = CreateIconButton(
                "Snap to Ground (Origin)",
                "Transform Icon",
                SnapToGroundEditor.SnapToGroundTransform);

            snapOriginBtn.tooltip = "Snap object to ground using transform origin point";
            section.Add(snapOriginBtn);

            var snapColliderBtn = CreateIconButton(
                "Snap to Ground (Collider)",
                "BoxCollider Icon",
                SnapToGroundEditor.SnapToGroundCollider);

            snapColliderBtn.tooltip = "Snap object to ground using collider bounds";
            section.Add(snapColliderBtn);

            var snapMeshBtn = CreateIconButton(
                "Snap to Ground (Mesh)",
                "MeshRenderer Icon",
                SnapToGroundEditor.SnapToGroundMesh);

            snapMeshBtn.tooltip = "Snap object to ground using mesh bounds";
            section.Add(snapMeshBtn);
        }

        private void CreateGroupingToolsSection(VisualElement parent)
        {
            var section = CreateSection("Grouping Tools", parent);

            // Group Selected Objects Button
            var groupBtn = CreateIconButton(
                "Group Selected Objects",
                "d_Prefab Icon",
                GroupSelectedObjects);

            groupBtn.tooltip = "Create new GameObject at center of selected objects and parent them to it";
            section.Add(groupBtn);

            var btnContainer = new VisualElement()
                .SetFlexRow()
                .SetFlexGrow(1);

            // Split by Cell Button
            var splitBtn = CreateIconButton(
                    "Split Children by Cell",
                    "d_Grid Icon",
                    SplitChildrenByCell)
                .SetFlexGrow(1)
                .SetChildOf(btnContainer);

            cellSizeField = new FloatField()
                .SetWidth(50)
                .SetHeight(18)
                .SetMarginTop(5)
                .SetChildOf(btnContainer);

            cellSizeField.value = 20;

            splitBtn.tooltip = "Split child objects into groups based on cell size";
            section.Add(btnContainer);

            // Create Empty at Position Button
            var createEmptyBtn = CreateIconButton(
                "Create Empty at Selected",
                "d_GameObject Icon",
                CreateEmptyAtPosition);

            createEmptyBtn.tooltip = "Create new empty GameObject with same position and rotation as selected";
            section.Add(createEmptyBtn);
        }

        // Add these methods to handle the functionality
        private static void GroupSelectedObjects()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("No objects selected to group");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Group Selected Objects");

            // Calculate center position
            Vector3 center = Vector3.zero;
            foreach (var obj in selected)
            {
                center += obj.transform.position;
            }

            center /= selected.Length;

            // Create parent object
            var parent = new GameObject("Group_" + System.DateTime.Now.ToString("HHmmss"));
            parent.transform.position = center;
            parent.transform.parent = selected[0].transform.parent;

            Undo.RegisterCreatedObjectUndo(parent, "Create Group Parent");

            // Parent all selected objects
            foreach (var obj in selected)
            {
                Undo.SetTransformParent(obj.transform, parent.transform, "Parent to Group");
            }

            Selection.activeGameObject = parent;
            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        private void SplitChildrenByCell()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("No object selected");
                return;
            }

            var children = new Transform[selected.transform.childCount];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = selected.transform.GetChild(i);
            }

            if (children.Length == 0)
            {
                Debug.LogWarning("Selected object has no children to split");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Split Children by Cell");

            float cellSize = cellSizeField.value;
            var cellGroups =
                new System.Collections.Generic.Dictionary<Vector2Int, System.Collections.Generic.List<Transform>>();

            // Group children by cell position
            foreach (var child in children)
            {
                Vector3 pos = child.position;
                Vector2Int cellPos = new Vector2Int(
                    Mathf.RoundToInt(pos.x / cellSize),
                    Mathf.RoundToInt(pos.z / cellSize)
                );

                if (!cellGroups.ContainsKey(cellPos))
                {
                    cellGroups[cellPos] = new System.Collections.Generic.List<Transform>();
                }

                cellGroups[cellPos].Add(child);
            }

            // Create group objects for each cell
            foreach (var kvp in cellGroups)
            {
                if (kvp.Value.Count > 1) // Only create groups for cells with multiple objects
                {
                    var cellGroup = new GameObject($"Cell_{kvp.Key.x}_{kvp.Key.y}");
                    cellGroup.transform.parent = selected.transform;

                    Vector3 cellCenter = Vector3.zero;
                    foreach (var child in kvp.Value)
                    {
                        cellCenter += child.position;
                    }

                    cellCenter /= kvp.Value.Count;
                    cellGroup.transform.position = cellCenter;

                    Undo.RegisterCreatedObjectUndo(cellGroup, "Create Cell Group");

                    foreach (var child in kvp.Value)
                    {
                        Undo.SetTransformParent(child, cellGroup.transform, "Parent to Cell Group");
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        private static void CreateEmptyAtPosition()
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                Debug.LogWarning("No objects selected to group");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroupIndex = Undo.GetCurrentGroup();

            var created = new List<GameObject>();

            foreach (var selected in selection)
            {
                if (selected == null)
                {
                    continue;
                }

                var newObj = new GameObject("Empty_" + selected.name);
                newObj.transform.position = selected.transform.position;
                newObj.transform.rotation = selected.transform.rotation;
                newObj.transform.parent = selected.transform.parent;
                newObj.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
                created.Add(newObj);

                Undo.RegisterCreatedObjectUndo(newObj, "Create Empty at Position");
            }

            Selection.objects = created.ToArray();

            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        private VisualElement CreateSection(string title, VisualElement parent, Action<bool> onToggle = null)
        {
            var sectionContainer = new VisualElement();

            // Create foldout instead of label
            var foldout = new Foldout()
                .SetFontSize(12)
                .SetFontStyle(FontStyle.Bold)
                .SetMarginBottom(5);

            foldout.text = title;

            // Style the foldout toggle
            foldout.Q<Toggle>().SetColor(new Color(0.85f, 0.85f, 0.85f));
            foldout.value = false;
            if (onToggle != null)
            {
                foldout.RegisterValueChangedCallback(evt => onToggle(evt.newValue));
            }

            // Create content container for section items
            var contentContainer = new VisualElement();

            foldout.Add(contentContainer);
            sectionContainer.Add(foldout);
            parent.Add(sectionContainer);

            return contentContainer; // Return the content container instead of section container
        }

        private Button CreateIconButton(string text, string iconName, System.Action onClick)
        {
            var button = new Button(onClick)
                .SetFontSize(12)
                .SetText(text)
                .SetHeight(25)
                .SetMarginBottom(5)
                .SetFlexDirection(FlexDirection.Row)
                .SetJustifyContent(Justify.Center)
                .SetAlignItems(Align.Center);

            // Try to set icon

            if (!iconName.IsNullOrEmpty())
            {
                var icon = EditorGUIUtility.IconContent(iconName);
                if (icon != null && icon.image != null)
                {
                    var iconElement = new VisualElement()
                        .SetWidth(16)
                        .SetHeight(16)
                        .SetMarginRight(5)
                        .SetPositionAbsolute(8)
                        .SetBackgroundImage(new StyleBackground((Texture2D)icon.image));

                    button.Insert(0, iconElement);
                }
            }

            return button;
        }
    }
}