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

namespace DataKeeper.Editor.Windows
{
    public class ToolsWindow : EditorWindow
    {
        private VisualElement root;

        private FloatField cellSizeField;

        private static Label bufferLabel;
        private static Vector3 copiedPosition;
        private static Vector3 copiedRotation;
        private static Vector3 copiedScale = Vector3.one;

        // Add these fields to your class
        private Slider timeScaleSlider;
        private float timeScale = 1f;

        // Scene management fields
        private ScrollView mainContainer;
        private VisualElement sceneView;

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
                    .SetBorderColor(new Color(0.3f, 0.3f, 0.3f));

                // Scene name label
                var nameLabel = new Label(sceneName + (sceneObject.isDirty ? "*" : ""))
                    .SetFontStyle(isLoaded ? FontStyle.Bold : FontStyle.Normal)
                    .SetColor(isLoaded ? Color.white : new Color(0.8f, 0.8f, 0.8f))
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

            // Transform Copy/Paste Buttons
            var transformContainer = new VisualElement()
                .SetFlexDirection(FlexDirection.Row)
                .SetMarginTop(5);

            var copyTransformBtn = CreateIconButton(
                "Copy Transform",
                "",
                CopyTransform);

            copyTransformBtn.tooltip = "Copy position, rotation, and scale of selected object";
            copyTransformBtn.style.flexGrow = 1;
            copyTransformBtn.style.marginRight = 2;

            var pasteTransformBtn = CreateIconButton(
                "Paste Transform",
                "",
                PasteTransform);

            pasteTransformBtn.tooltip = "Paste copied transform values to selected objects";
            pasteTransformBtn.style.flexGrow = 1;
            pasteTransformBtn.style.marginLeft = 2;

            transformContainer.Add(copyTransformBtn);
            transformContainer.Add(pasteTransformBtn);
            section.Add(transformContainer);

            bufferLabel = new Label();
            section.Add(bufferLabel);
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
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("No object selected");
                return;
            }

            var newObj = new GameObject("Empty_" + selected.name);
            newObj.transform.position = selected.transform.position;
            newObj.transform.rotation = selected.transform.rotation;

            Undo.RegisterCreatedObjectUndo(newObj, "Create Empty at Position");
            Selection.activeGameObject = newObj;
        }

        private static void CopyTransform()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("No object selected to copy transform from");
                return;
            }

            copiedPosition = selected.transform.position;
            copiedRotation = selected.transform.eulerAngles;
            copiedScale = selected.transform.localScale;

            bufferLabel.SetText($"p: {copiedPosition}\nr: {copiedRotation}\ns: {copiedScale}");
            Debug.Log($"Copied transform from {selected.name}");
        }

        private static void PasteTransform()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("No objects selected to paste transform to");
                return;
            }

            if (copiedScale == Vector3.zero) // Check if we have copied data
            {
                Debug.LogWarning("No transform data copied. Use Copy Transform first.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paste Transform");

            foreach (var obj in selected)
            {
                Undo.RecordObject(obj.transform, "Paste Transform");
                obj.transform.position = copiedPosition;
                obj.transform.eulerAngles = copiedRotation;
                obj.transform.localScale = copiedScale;
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
            Debug.Log($"Pasted transform to {selected.Length} objects");
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