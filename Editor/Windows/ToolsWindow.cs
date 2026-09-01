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
using System.IO;
using System.Reflection;
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

        // Screenshot fields
        private enum ScreenshotSource
        {
            Game,
            Scene
        }

        private enum ScreenshotMode
        {
            WithUI,
            NoUI,
            Transparent
        }

        private const float ScreenshotMinScale = 0.5f;
        private const float ScreenshotMaxScale = 3f;

        private static ScreenshotSource screenshotSource = ScreenshotSource.Game;
        private static ScreenshotMode screenshotMode = ScreenshotMode.WithUI;
        private static float screenshotScale = 1f;
        private static bool screenshotCopyToClipboard = false;

        private Button screenshotWithUIBtn;
        private Button screenshotNoUIBtn;
        private Button screenshotTransparentBtn;
        private Label screenshotInfoLabel;
        private Slider screenshotScaleSlider;
        private FloatField screenshotScaleField;

        private static PropertyInfo urpPostProcessingProperty;

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
            CreateShortcutsSection(mainContainer);
            CreateBuffersSection(mainContainer);
            CreateTimeScaleSection(mainContainer);
            CreateScreenshotSection(mainContainer);
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


            // ── Clipboard slots ──
            bufferSlotsContainer = new VisualElement()
                .SetMarginBottom(6);

            RefreshBufferSlotsUI();
            section.Add(bufferSlotsContainer);

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

        private void CreateScreenshotSection(VisualElement parent)
        {
            var section = CreateSection("Screenshot Tool", parent);

            // ── Source switch ──
            var sourceRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(4)
                .SetChildOf(section);

            Button gameBtn = null;
            Button sceneBtn = null;

            gameBtn = new Button(() => SetScreenshotSource(ScreenshotSource.Game, gameBtn, sceneBtn))
                .SetText("Game View")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetMarginRight(4)
                .SetChildOf(sourceRow);

            sceneBtn = new Button(() => SetScreenshotSource(ScreenshotSource.Scene, gameBtn, sceneBtn))
                .SetText("Scene View")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetChildOf(sourceRow);

            RefreshScreenshotSourceButtons(gameBtn, sceneBtn);

            // ── Mode switch ──
            var modeRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(4)
                .SetChildOf(section);

            screenshotWithUIBtn = new Button(() => SetScreenshotMode(ScreenshotMode.WithUI))
                .SetText("With UI")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetMarginRight(4)
                .SetChildOf(modeRow);
            screenshotWithUIBtn.tooltip =
                "Grab the Game View backbuffer so Screen-Space Overlay canvases are included. Forces integer scale and an opaque background.";

            screenshotNoUIBtn = new Button(() => SetScreenshotMode(ScreenshotMode.NoUI))
                .SetText("No UI")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetMarginRight(4)
                .SetChildOf(modeRow);
            screenshotNoUIBtn.tooltip = "Render the camera straight to a texture. Overlay UI cannot be in the shot.";

            screenshotTransparentBtn = new Button(() => SetScreenshotMode(ScreenshotMode.Transparent))
                .SetText("Transparent")
                .SetHeight(20)
                .SetFlexGrow(1)
                .SetChildOf(modeRow);
            screenshotTransparentBtn.tooltip = "Camera render with the background cleared to alpha 0.";

            // ── Scale ──
            var scaleRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(4)
                .SetChildOf(section);

            screenshotScaleSlider = new Slider(ScreenshotMinScale, ScreenshotMaxScale)
                .SetFlexGrow(1f)
                .SetChildOf(scaleRow);

            screenshotScaleField = new FloatField()
                .SetWidth(50)
                .SetChildOf(scaleRow);

            screenshotScaleSlider.value = screenshotScale;
            screenshotScaleField.value = screenshotScale;

            screenshotScaleSlider.RegisterValueChangedCallback(evt =>
            {
                screenshotScale = evt.newValue;
                screenshotScaleField.SetValueWithoutNotify(screenshotScale);
                RefreshScreenshotInfo();
            });

            screenshotScaleField.RegisterValueChangedCallback(evt =>
            {
                screenshotScale = Mathf.Clamp(evt.newValue, ScreenshotMinScale, ScreenshotMaxScale);
                screenshotScaleSlider.SetValueWithoutNotify(screenshotScale);
                RefreshScreenshotInfo();
            });

            var clipboardRow = new VisualElement()
                .SetFlexRow()
                .SetMarginBottom(6)
                .SetChildOf(section);

            new ToggleButton("Copy Image to Clipboard", screenshotCopyToClipboard, v => screenshotCopyToClipboard = v)
                .SetFlexGrow(1)
                .SetHeight(20)
                .SetChildOf(clipboardRow);

            screenshotInfoLabel = new Label()
                .SetFontSize(10)
                .SetMarginBottom(6)
                .SetChildOf(section);
            screenshotInfoLabel.style.whiteSpace = WhiteSpace.Normal;

            var captureBtn = CreateIconButton(
                "Take Screenshot",
                "Camera Icon",
                TakeScreenshot);
            captureBtn.tooltip = "Save a PNG to Pictures/" + Application.productName;
            section.Add(captureBtn);

            var openFolderBtn = CreateIconButton(
                "Open Screenshot Folder",
                "d_Folder Icon",
                OpenScreenshotFolder);
            openFolderBtn.tooltip = ScreenshotFolder;
            section.Add(openFolderBtn);

            RefreshScreenshotModeButtons();
            RefreshScreenshotInfo();
        }

        private void SetScreenshotSource(ScreenshotSource source, Button gameBtn, Button sceneBtn)
        {
            screenshotSource = source;
            RefreshScreenshotSourceButtons(gameBtn, sceneBtn);
            RefreshScreenshotModeButtons();
            RefreshScreenshotInfo();
        }

        private void SetScreenshotMode(ScreenshotMode mode)
        {
            screenshotMode = mode;
            RefreshScreenshotModeButtons();
            RefreshScreenshotInfo();
        }

        private static void RefreshScreenshotSourceButtons(Button gameBtn, Button sceneBtn)
        {
            var activeColor = new Color(0.3f, 0.5f, 0.8f);
            var inactiveColor = new Color(0.3f, 0.3f, 0.3f);
            bool isGame = screenshotSource == ScreenshotSource.Game;
            gameBtn.style.backgroundColor = isGame ? activeColor : inactiveColor;
            sceneBtn.style.backgroundColor = isGame ? inactiveColor : activeColor;
        }

        private void RefreshScreenshotModeButtons()
        {
            // Overlay UI only lands in the Game View backbuffer grab, so for the Scene View
            // the option is meaningless rather than merely unavailable.
            bool canIncludeUI = screenshotSource == ScreenshotSource.Game;
            if (!canIncludeUI && screenshotMode == ScreenshotMode.WithUI)
                screenshotMode = ScreenshotMode.NoUI;

            screenshotWithUIBtn.SetDisplay(canIncludeUI ? DisplayStyle.Flex : DisplayStyle.None);

            var activeColor = new Color(0.3f, 0.5f, 0.8f);
            var inactiveColor = new Color(0.3f, 0.3f, 0.3f);
            screenshotWithUIBtn.style.backgroundColor =
                screenshotMode == ScreenshotMode.WithUI ? activeColor : inactiveColor;
            screenshotNoUIBtn.style.backgroundColor =
                screenshotMode == ScreenshotMode.NoUI ? activeColor : inactiveColor;
            screenshotTransparentBtn.style.backgroundColor =
                screenshotMode == ScreenshotMode.Transparent ? activeColor : inactiveColor;
        }

        private void RefreshScreenshotInfo()
        {
            if (screenshotInfoLabel == null) return;

            bool backbuffer = UsesBackbufferCapture();
            float scale = backbuffer ? Mathf.Max(1, Mathf.RoundToInt(screenshotScale)) : screenshotScale;
            Vector2Int native = GetScreenshotSourceSize();
            int width = Mathf.Max(1, Mathf.RoundToInt(native.x * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(native.y * scale));

            string mode;
            if (backbuffer)
                mode = "Game View backbuffer — integer scale, opaque, Overlay UI included";
            else if (screenshotSource == ScreenshotSource.Scene)
                mode = "Scene camera render — no gizmos or grid";
            else
                mode = "Game camera render — no Overlay UI";

            if (screenshotMode == ScreenshotMode.Transparent)
                mode += ", alpha 0 background";

            screenshotInfoLabel.text = $"{mode}\n{native.x}x{native.y} → {width}x{height} @ {scale:0.##}x";
        }

        private static bool UsesBackbufferCapture()
        {
            return screenshotSource == ScreenshotSource.Game && screenshotMode == ScreenshotMode.WithUI;
        }

        private static Vector2Int GetScreenshotSourceSize()
        {
            if (screenshotSource == ScreenshotSource.Scene)
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null && sceneView.camera != null)
                    return new Vector2Int(sceneView.camera.pixelWidth, sceneView.camera.pixelHeight);
            }

            Vector2 gameViewSize = Handles.GetMainGameViewSize();
            return new Vector2Int(Mathf.RoundToInt(gameViewSize.x), Mathf.RoundToInt(gameViewSize.y));
        }

        private static string ScreenshotFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.productName);

        private static string NextScreenshotPath(string directory)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string path = Path.Combine(directory, $"Screenshot_{stamp}.png");

            int suffix = 1;
            while (File.Exists(path))
                path = Path.Combine(directory, $"Screenshot_{stamp}_{suffix++}.png");

            return path;
        }

        private static void OpenScreenshotFolder()
        {
            string directory = ScreenshotFolder;
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory + Path.DirectorySeparatorChar);
        }

        private static void TakeScreenshot()
        {
            string directory = ScreenshotFolder;
            Directory.CreateDirectory(directory);
            string path = NextScreenshotPath(directory);

            if (UsesBackbufferCapture())
            {
                CaptureBackbuffer(path);
                return;
            }

            Camera camera;
            if (screenshotSource == ScreenshotSource.Scene)
            {
                var sceneView = SceneView.lastActiveSceneView;
                camera = sceneView == null ? null : sceneView.camera;
            }
            else
            {
                camera = Camera.main;
                if (camera == null && Camera.allCamerasCount > 0)
                    camera = Camera.allCameras[0];
            }

            if (camera == null)
            {
                Debug.LogWarning(screenshotSource == ScreenshotSource.Scene
                    ? "No active Scene View to capture from"
                    : "No enabled camera in the scene to capture from");
                return;
            }

            Vector2Int native = GetScreenshotSourceSize();
            int outWidth = Mathf.Max(1, Mathf.RoundToInt(native.x * screenshotScale));
            int outHeight = Mathf.Max(1, Mathf.RoundToInt(native.y * screenshotScale));

            // Below 1x we still render at native size and filter down, otherwise thin
            // geometry and text alias badly instead of being averaged.
            float renderScale = Mathf.Max(1f, screenshotScale);
            int renderWidth = Mathf.Max(1, Mathf.RoundToInt(native.x * renderScale));
            int renderHeight = Mathf.Max(1, Mathf.RoundToInt(native.y * renderScale));

            var texture = RenderCameraToTexture(camera, renderWidth, renderHeight, outWidth, outHeight,
                screenshotMode == ScreenshotMode.Transparent);

            File.WriteAllBytes(path, texture.EncodeToPNG());
            DestroyImmediate(texture);

            FinishScreenshot(path, outWidth, outHeight);
        }

        private static Texture2D RenderCameraToTexture(Camera camera, int renderWidth, int renderHeight, int outWidth,
            int outHeight, bool transparent)
        {
            var previousTarget = camera.targetTexture;
            var previousClearFlags = camera.clearFlags;
            var previousBackground = camera.backgroundColor;
            var previousActive = RenderTexture.active;
            bool previousPostProcessing = false;

            var renderTexture = RenderTexture.GetTemporary(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.filterMode = FilterMode.Bilinear;
            camera.targetTexture = renderTexture;

            if (transparent)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                // URP's UberPost writes alpha = 1, which would flatten the cutout back to opaque.
                previousPostProcessing = SetUrpPostProcessing(camera, false);
            }

            camera.Render();

            var readTexture = renderTexture;
            RenderTexture downsampled = null;

            if (outWidth != renderWidth || outHeight != renderHeight)
            {
                downsampled = RenderTexture.GetTemporary(outWidth, outHeight, 0, RenderTextureFormat.ARGB32);
                downsampled.filterMode = FilterMode.Bilinear;
                Graphics.Blit(renderTexture, downsampled);
                readTexture = downsampled;
            }

            RenderTexture.active = readTexture;
            var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, outWidth, outHeight), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;
            camera.targetTexture = previousTarget;
            camera.clearFlags = previousClearFlags;
            camera.backgroundColor = previousBackground;
            if (transparent) SetUrpPostProcessing(camera, previousPostProcessing);

            if (downsampled != null) RenderTexture.ReleaseTemporary(downsampled);
            RenderTexture.ReleaseTemporary(renderTexture);

            return texture;
        }

        // URP is not an asmdef reference so the package still compiles on Built-in RP;
        // the camera data is reached by reflection and simply no-ops elsewhere.
        private static bool SetUrpPostProcessing(Camera camera, bool enabled)
        {
            var dataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (dataType == null) return false;

            var cameraData = camera.GetComponent(dataType);
            if (cameraData == null) return false;

            if (urpPostProcessingProperty == null)
                urpPostProcessingProperty = dataType.GetProperty("renderPostProcessing");
            if (urpPostProcessingProperty == null) return false;

            bool previous = (bool)urpPostProcessingProperty.GetValue(cameraData);
            urpPostProcessingProperty.SetValue(cameraData, enabled);
            return previous;
        }

        private static void CaptureBackbuffer(string path)
        {
            int superSize = Mathf.Max(1, Mathf.RoundToInt(screenshotScale));
            ScreenCapture.CaptureScreenshot(path, superSize);

            Vector2 native = Handles.GetMainGameViewSize();
            int width = Mathf.RoundToInt(native.x) * superSize;
            int height = Mathf.RoundToInt(native.y) * superSize;

            // CaptureScreenshot only lands at the end of a rendered frame, and outside
            // Play mode the Game View does not tick on its own — pump it until the file shows up.
            int ticks = 0;
            int settle = 0;
            EditorApplication.CallbackFunction poll = null;

            poll = () =>
            {
                ticks++;

                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    if (++settle < 2) return;
                    EditorApplication.update -= poll;
                    FinishScreenshot(path, width, height);
                    return;
                }

                if (ticks > 300)
                {
                    EditorApplication.update -= poll;
                    Debug.LogWarning($"Screenshot timed out — is a Game View open and rendering? {path}");
                    return;
                }

                RepaintMainGameView();
                EditorApplication.QueuePlayerLoopUpdate();
            };

            RepaintMainGameView();
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.update += poll;
        }

        private static void RepaintMainGameView()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            for (int i = 0; i < windows.Length; i++)
            {
                var window = windows[i] as EditorWindow;
                if (window != null) window.Repaint();
            }
        }

        private static void FinishScreenshot(string path, int width, int height)
        {
            Debug.Log($"Screenshot saved ({width}x{height}): <a href=\"{path}\">{path}</a>");

            if (screenshotCopyToClipboard)
                CopyImageToClipboard(path);
        }

        private static void CopyImageToClipboard(string path)
        {
#if UNITY_EDITOR_WIN
            // systemCopyBuffer is text-only. SetData("PNG") is what keeps alpha alive for
            // apps that read it (Discord, Photoshop, GIMP); SetImage covers everything else,
            // where transparent pixels will come through black.
            string escaped = path.Replace("'", "''");
            string script =
                "Add-Type -AssemblyName System.Windows.Forms,System.Drawing;" +
                $"$img=[System.Drawing.Image]::FromFile('{escaped}');" +
                "$ms=New-Object System.IO.MemoryStream;" +
                "$img.Save($ms,[System.Drawing.Imaging.ImageFormat]::Png);" +
                "$d=New-Object System.Windows.Forms.DataObject;" +
                "$d.SetData('PNG',$false,$ms);" +
                "$d.SetImage($img);" +
                "[System.Windows.Forms.Clipboard]::SetDataObject($d,$true);";

            var startInfo = new System.Diagnostics.ProcessStartInfo("powershell.exe")
            {
                Arguments = $"-NoProfile -STA -NonInteractive -WindowStyle Hidden -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            System.Diagnostics.Process.Start(startInfo);
#else
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log("Image clipboard copy is Windows-only — copied the file path instead.");
#endif
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