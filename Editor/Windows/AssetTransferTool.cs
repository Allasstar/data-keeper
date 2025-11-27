using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataKeeper.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows
{
    public class AssetTransferTool : EditorWindow
    {
        // --- Enums ---
        public enum FolderStructureMode
        {
            KeepOriginalStructure,
            FlattenToSingleFolder
        }

        public enum ConflictResolution
        {
            AutoRename,
            Overwrite,
            Skip
        }

        // --- UI Elements ---
        private ObjectField _sourceAssetField;
        private ObjectField _destinationFolderField;
        private EnumField _structureModeField;
        private EnumField _conflictModeField;
        private Button _transferButton;
        private ListView _previewList;
        private Label _statusLabel;

        // --- Data ---
        private List<AssetMoveOperation> _plannedOperations = new List<AssetMoveOperation>();
        private Dictionary<string, string> _destinationCache = new Dictionary<string, string>();

        [MenuItem("Tools/Windows/Asset Transfer", priority = 7)]
        public static void ShowWindow()
        {
            AssetTransferTool window = GetWindow<AssetTransferTool>();
            window.titleContent = new GUIContent("Asset Transfer", EditorGUIUtility.IconContent("FilterByType").image);
            window.minSize = new Vector2(500, 650);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            // Header
            var header = new Label("Deep Dependency Transfer");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(header);

            var helpBox = new HelpBox(
                "Moves assets to a target folder while preserving GUIDs.\n" +
                "Scans ALL sub-folders in destination to detect duplicates.",
                HelpBoxMessageType.Info);
            helpBox.style.marginTop = 5;
            root.Add(helpBox);

            // Source asset
            _sourceAssetField = new ObjectField("Select Prefab/Asset")
            {
                objectType = typeof(Object),
                allowSceneObjects = false
            };
            _sourceAssetField.RegisterValueChangedCallback(evt => RecalculatePreview());
            _sourceAssetField.style.marginTop = 10;
            root.Add(_sourceAssetField);

            // Destination folder selection
            _destinationFolderField = new ObjectField("Target Folder")
            {
                objectType = typeof(DefaultAsset),
                allowSceneObjects = false
            };
            _destinationFolderField.RegisterValueChangedCallback(evt => RecalculatePreview());
            _destinationFolderField.style.marginTop = 5;
            root.Add(_destinationFolderField);

            // Settings box
            var settingsBox = new Box()
                .SetMarginTop(10)
                .SetPadding(5)
                .SetBackgroundColor(new Color(0, 0, 0, 0.1f));

            _structureModeField = new EnumField("Structure", FolderStructureMode.KeepOriginalStructure);
            _structureModeField.RegisterValueChangedCallback(evt => RecalculatePreview());

            _conflictModeField = new EnumField("Collision Action", ConflictResolution.AutoRename);
            _conflictModeField.RegisterValueChangedCallback(evt => RecalculatePreview());

            settingsBox.Add(_structureModeField);
            settingsBox.Add(_conflictModeField);
            root.Add(settingsBox);

            // Preview header
            var listHeader = new VisualElement();
            listHeader.style.flexDirection = FlexDirection.Row;
            listHeader.style.marginTop = 15;
            listHeader.Add(new Label("Source Asset") { style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold } });
            listHeader.Add(new Label("Status") { style = { width = 100, unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(listHeader);

            // Preview list
            _previewList = new ListView()
                .SetFlexGrow(1)
                .SetMinHeight(200)
                .SetBorderWidth(1)
                .SetBorderColor(new Color(0.3f, 0.3f, 0.3f));

            _previewList.fixedItemHeight = 55;
            _previewList.makeItem = MakePreviewItem;
            _previewList.bindItem = BindPreviewItem;

            root.Add(_previewList);

            // Footer
            _statusLabel = new Label("");
            _statusLabel.style.color = Color.white;
            _statusLabel.style.alignSelf = Align.Center;
            _statusLabel.style.marginTop = 5;
            root.Add(_statusLabel);

            _transferButton = new Button(ExecuteTransfer)
            {
                text = "PROCESS MOVE",
                style =
                {
                    height = 40,
                    marginTop = 5,
                    backgroundColor = new Color(0.2f, 0.6f, 0.2f)
                }
            };
            _transferButton.SetEnabled(false);
            root.Add(_transferButton);
        }

        private VisualElement MakePreviewItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.justifyContent = Justify.Center;
            container.style.paddingLeft = 5;

            var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            topRow.Add(new Label() { name = "name", style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold } });
            topRow.Add(new Label() { name = "status", style = { width = 120, unityTextAlign = TextAnchor.MiddleRight } });

            container.Add(topRow);
            container.Add(new Label() { name = "detail", style = { fontSize = 10, opacity = 0.7f } });
            container.Add(new Label() { name = "path", style = { fontSize = 9, opacity = 0.5f } });

            return container;
        }

        private void BindPreviewItem(VisualElement element, int i)
        {
            if (i >= _plannedOperations.Count) return;

            var op = _plannedOperations[i];

            element.Q<Label>("name").text = Path.GetFileName(op.SourcePath);
            element.Q<Label>("path").text = $"-> {op.DestinationPath}";

            var status = element.Q<Label>("status");
            var detail = element.Q<Label>("detail");

            status.text = op.ResolutionType;
            detail.text = "";

            if (op.IsDeepConflict)
            {
                status.text = "Deep Conflict";
                status.style.color = new Color(1f, 0.5f, 0f);
                detail.text = $"Existing: {op.ConflictPath}";
                detail.style.color = new Color(1f, 0.5f, 0f);
            }
            else if (op.ResolutionType == "Overwrite")
            {
                status.style.color = Color.red;
            }
            else if (op.ResolutionType == "Rename")
            {
                status.style.color = Color.cyan;
                detail.text = "Will be renamed";
            }
            else
            {
                status.style.color = Color.green;
                detail.text = "Ready";
            }
        }

        // --- Core Logic ---

        private class AssetMoveOperation
        {
            public string SourcePath;
            public string DestinationPath;
            public string ResolutionType;
            public bool IsDeepConflict;
            public string ConflictPath;
        }

        private void BuildDestinationCache(string rootPath)
        {
            _destinationCache.Clear();

            if (!AssetDatabase.IsValidFolder(rootPath))
                return;

            string[] guids = AssetDatabase.FindAssets("", new[] { rootPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;

                string fileName = Path.GetFileName(path);
                if (!_destinationCache.ContainsKey(fileName))
                    _destinationCache[fileName] = path;
            }
        }

        private void RecalculatePreview()
        {
            _plannedOperations.Clear();

            var sourceObj = _sourceAssetField.value;
            string targetRoot = AssetDatabase.GetAssetPath(_destinationFolderField.value);

            if (sourceObj == null || !AssetDatabase.IsValidFolder(targetRoot))
            {
                _transferButton.SetEnabled(false);
                _previewList.itemsSource = _plannedOperations;
                _previewList.Rebuild();
                _statusLabel.text = "Select asset and destination folder.";
                return;
            }

            _statusLabel.text = "Scanning...";

            // Build deep cache
            BuildDestinationCache(targetRoot);

            // Collect dependencies
            string rootPath = AssetDatabase.GetAssetPath(sourceObj);
            string rootDir = Path.GetDirectoryName(rootPath).Replace("\\", "/");
            string[] deps = AssetDatabase.GetDependencies(rootPath, true);

            var assetsToMove = deps
                .Where(path =>
                    !path.EndsWith(".cs") &&
                    !path.StartsWith("Packages/") &&
                    !path.Contains("/unity_builtin_extra"))
                .Distinct()
                .ToList();

            var structure = (FolderStructureMode)_structureModeField.value;
            var conflict = (ConflictResolution)_conflictModeField.value;

            foreach (var src in assetsToMove)
            {
                if (src.StartsWith(targetRoot + "/"))
                    continue;

                string fileName = Path.GetFileName(src);
                string intendedPath;

                if (structure == FolderStructureMode.FlattenToSingleFolder)
                {
                    intendedPath = $"{targetRoot}/{fileName}";
                }
                else
                {
                    // Keep folder structure relative to the source asset's directory
                    string srcDir = Path.GetDirectoryName(src).Replace("\\", "/");
                    
                    // Calculate relative path from source asset's parent to this dependency
                    string relativePath = "";
                    if (srcDir.StartsWith(rootDir + "/"))
                    {
                        // Dependency is in a subfolder of the source
                        relativePath = srcDir.Substring(rootDir.Length + 1);
                    }
                    else if (srcDir == rootDir)
                    {
                        // Dependency is in the same folder as source
                        relativePath = "";
                    }
                    else
                    {
                        // Dependency is outside source folder - preserve its immediate parent folder name
                        relativePath = Path.GetFileName(srcDir);
                    }
                    
                    if (!string.IsNullOrEmpty(relativePath))
                        intendedPath = $"{targetRoot}/{relativePath}/{fileName}";
                    else
                        intendedPath = $"{targetRoot}/{fileName}";
                }

                var op = new AssetMoveOperation
                {
                    SourcePath = src,
                    DestinationPath = intendedPath,
                    ResolutionType = "OK"
                };

                bool exactExists = File.Exists(intendedPath);

                if (_destinationCache.ContainsKey(fileName))
                {
                    string existingPath = _destinationCache[fileName];

                    if (existingPath == intendedPath)
                    {
                        ApplyConflictResolution(op, conflict, intendedPath);
                    }
                    else
                    {
                        op.IsDeepConflict = true;
                        op.ConflictPath = existingPath;

                        if (conflict == ConflictResolution.Skip)
                            op.ResolutionType = "Skip";
                        else
                        {
                            op.ResolutionType = "Rename";
                            op.DestinationPath = GenerateUniquePath(intendedPath);
                        }
                    }
                }
                else if (exactExists)
                {
                    ApplyConflictResolution(op, conflict, intendedPath);
                }

                if (op.ResolutionType != "Skip")
                    _plannedOperations.Add(op);
            }

            _previewList.itemsSource = _plannedOperations;
            _previewList.Rebuild();
            _transferButton.SetEnabled(_plannedOperations.Count > 0);
            _statusLabel.text = $"Found {_plannedOperations.Count} assets.";
        }

        private void ApplyConflictResolution(AssetMoveOperation op, ConflictResolution conflict, string intendedPath)
        {
            if (conflict == ConflictResolution.Skip)
                op.ResolutionType = "Skip";
            else if (conflict == ConflictResolution.Overwrite)
                op.ResolutionType = "Overwrite";
            else
            {
                op.ResolutionType = "Rename";
                op.DestinationPath = GenerateUniquePath(intendedPath);
            }
        }

        private string GenerateUniquePath(string fullPath)
        {
            string dir = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string ext = Path.GetExtension(fullPath);

            int count = 1;
            string newPath = fullPath;

            while (File.Exists(newPath))
            {
                newPath = $"{dir}/{name} {count}{ext}";
                count++;
            }

            return newPath;
        }

        // --- FIXED: Unity-safe folder creation ---
        private void EnsureUnityFolder(string folder)
        {
            folder = folder.Replace("\\", "/");

            if (AssetDatabase.IsValidFolder(folder))
                return;

            // Build the complete path hierarchy
            List<string> pathParts = new List<string>();
            string current = folder;

            while (!string.IsNullOrEmpty(current) && current != "Assets")
            {
                if (!AssetDatabase.IsValidFolder(current))
                {
                    pathParts.Insert(0, current);
                }
                else
                {
                    break;
                }

                current = Path.GetDirectoryName(current)?.Replace("\\", "/");
            }

            // Create folders from top to bottom
            foreach (string path in pathParts)
            {
                string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string name = Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                {
                    string guid = AssetDatabase.CreateFolder(parent, name);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError($"Failed to create folder: {path}");
                    }
                }
            }
        }

        private void ExecuteTransfer()
        {
            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var op in _plannedOperations)
                {
                    string targetDir = Path.GetDirectoryName(op.DestinationPath).Replace("\\", "/");
                    EnsureUnityFolder(targetDir);

                    if (op.ResolutionType == "Overwrite")
                        AssetDatabase.DeleteAsset(op.DestinationPath);

                    string err = AssetDatabase.MoveAsset(op.SourcePath, op.DestinationPath);
                    if (!string.IsNullOrEmpty(err))
                        Debug.LogError($"Failed to move {op.SourcePath} to {op.DestinationPath}: {err}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            _sourceAssetField.value = null;
            _plannedOperations.Clear();
            _previewList.Rebuild();

            _statusLabel.text = "Move Completed!";
            EditorUtility.DisplayDialog("Success", "Assets moved successfully.", "OK");
        }
    }
}