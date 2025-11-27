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
            AutoRename,     // Create Asset 1.png
            Overwrite,      // Dangerous: Deletes destination
            Skip            // Don't move this specific file
        }

        // --- UI Elements ---
        private ObjectField _prefabField;
        private TextField _destinationPathField;
        private EnumField _structureModeField;
        private EnumField _conflictModeField;
        private Button _transferButton;
        private ListView _previewList;
        private Label _statusLabel;

        // --- Data ---
        private List<AssetMoveOperation> _plannedOperations = new List<AssetMoveOperation>();
        // Cache: FileName -> RelativeAssetPath (e.g., "Texture.png" -> "Assets/Game/Sub/Texture.png")
        private Dictionary<string, string> _destinationCache = new Dictionary<string, string>();

        [MenuItem("Tools/Windows/Asset Transfer", priority = 7)]
        public static void ShowWindow()
        {
            AssetTransferTool window = GetWindow<AssetTransferTool>();
            window.titleContent = new GUIContent("Asset Transfer", EditorGUIUtility.IconContent("Prefab Icon").image);
            window.minSize = new Vector2(500, 650);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            // 1. Header
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

            // 2. Selection Inputs
            _prefabField = new ObjectField("Select Prefab/Asset") { objectType = typeof(Object) };
            _prefabField.RegisterValueChangedCallback(evt => RecalculatePreview());
            _prefabField.style.marginTop = 10;
            root.Add(_prefabField);

            // Destination Group
            var destGroup = new VisualElement();
            destGroup.style.flexDirection = FlexDirection.Row;
            destGroup.style.marginTop = 5;
            
            _destinationPathField = new TextField("Target Root") { value = "Assets/MigratedAssets" };
            _destinationPathField.style.flexGrow = 1;
            _destinationPathField.RegisterValueChangedCallback(evt => RecalculatePreview());
            
            var pickFolderBtn = new Button(() => {
                string path = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                        _destinationPathField.value = path;
                    }
                    else EditorUtility.DisplayDialog("Error", "Select a folder inside Assets.", "OK");
                }
            }) { text = "..." };
            
            destGroup.Add(_destinationPathField);
            destGroup.Add(pickFolderBtn);
            root.Add(destGroup);

            // 3. Settings
            var settingsBox = new Box()
                .SetMarginTop(10)
                .SetPadding(5)
                .SetBackgroundColor(new Color(0,0,0, 0.1f));

            _structureModeField = new EnumField("Structure", FolderStructureMode.KeepOriginalStructure);
            _structureModeField.RegisterValueChangedCallback(evt => RecalculatePreview());
            
            _conflictModeField = new EnumField("Collision Action", ConflictResolution.AutoRename);
            _conflictModeField.RegisterValueChangedCallback(evt => RecalculatePreview());

            settingsBox.Add(_structureModeField);
            settingsBox.Add(_conflictModeField);
            root.Add(settingsBox);

            // 4. Preview List
            var listHeader = new VisualElement();
            listHeader.style.flexDirection = FlexDirection.Row;
            listHeader.style.marginTop = 15;
            listHeader.Add(new Label("Source Asset") { style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold } });
            listHeader.Add(new Label("Status") { style = { width = 100, unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(listHeader);

            _previewList = new ListView()
                .SetFlexGrow(1)
                .SetMinHeight(200)
                .SetBorderWidth(1)
                .SetBorderColor(new Color(0.3f, 0.3f, 0.3f));
            
            _previewList.fixedItemHeight = 55;
            
            _previewList.makeItem = () => 
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Column;
                container.style.justifyContent = Justify.Center;
                container.style.paddingLeft = 5;
                
                var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                var lblName = new Label() { name = "name", style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold } };
                var lblStatus = new Label() { name = "status", style = { width = 120, unityTextAlign = TextAnchor.MiddleRight } };
                
                topRow.Add(lblName);
                topRow.Add(lblStatus);

                var lblDetail = new Label() { name = "detail", style = { fontSize = 10, opacity = 0.7f } };
                var lblPath = new Label() { name = "path", style = { fontSize = 9, opacity = 0.5f } };

                container.Add(topRow);
                container.Add(lblDetail);
                container.Add(lblPath);
                return container;
            };

            _previewList.bindItem = (element, i) => 
            {
                if(i >= _plannedOperations.Count) return;
                var op = _plannedOperations[i];
                
                element.Q<Label>("name").text = Path.GetFileName(op.SourcePath);
                element.Q<Label>("path").text = $"-> {op.DestinationPath}";
                
                var statusLbl = element.Q<Label>("status");
                var detailLbl = element.Q<Label>("detail");

                statusLbl.text = op.ResolutionType.ToString();
                detailLbl.text = "";

                // Color Logic
                if (op.IsDeepConflict)
                {
                    statusLbl.text = "Deep Conflict";
                    statusLbl.style.color = new Color(1f, 0.5f, 0f); // Orange
                    detailLbl.text = $"Found existing: {op.ConflictPath}";
                    detailLbl.style.color = new Color(1f, 0.5f, 0f);
                }
                else if (op.ResolutionType == "Overwrite") 
                {
                    statusLbl.style.color = Color.red;
                }
                else if (op.ResolutionType == "Rename") 
                {
                    statusLbl.style.color = Color.cyan;
                    detailLbl.text = "Will be renamed to avoid collision";
                }
                else 
                {
                    statusLbl.style.color = Color.green;
                    detailLbl.text = "Ready to move";
                }
            };
            root.Add(_previewList);

            // 5. Footer
            _statusLabel = new Label("");
            _statusLabel.style.color = Color.white;
            _statusLabel.style.alignSelf = Align.Center;
            _statusLabel.style.marginTop = 5;
            root.Add(_statusLabel);

            _transferButton = new Button(ExecuteTransfer) { text = "PROCESS MOVE", style = { height = 40, marginTop = 5, backgroundColor = new Color(0.2f, 0.6f, 0.2f) } };
            _transferButton.SetEnabled(false);
            root.Add(_transferButton);
        }

        // --- Core Logic ---

        private class AssetMoveOperation
        {
            public string SourcePath;
            public string DestinationPath;
            public string ResolutionType; // OK, Rename, Overwrite, Skip
            public bool IsDeepConflict;
            public string ConflictPath; // Where the existing file was found
        }

        private void BuildDestinationCache(string rootPath)
        {
            _destinationCache.Clear();
            if (!AssetDatabase.IsValidFolder(rootPath)) return;

            string[] guids = AssetDatabase.FindAssets("", new[] { rootPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;

                string fileName = Path.GetFileName(path);
                // We keep the first one found, or overwrite if multiple. 
                // Just need to know it exists somewhere.
                if (!_destinationCache.ContainsKey(fileName))
                {
                    _destinationCache[fileName] = path;
                }
            }
        }

        private void RecalculatePreview()
        {
            _plannedOperations.Clear();
            _statusLabel.text = "Scanning...";
            
            var targetObj = _prefabField.value;
            string targetRoot = _destinationPathField.value;

            if (targetObj == null || string.IsNullOrEmpty(targetRoot))
            {
                _transferButton.SetEnabled(false);
                _previewList.itemsSource = _plannedOperations;
                _previewList.Rebuild();
                _statusLabel.text = "Please select asset and destination.";
                return;
            }

            // Clean path
            if(targetRoot.EndsWith("/")) targetRoot = targetRoot.TrimEnd('/');

            // 1. Build Deep Cache of Target
            BuildDestinationCache(targetRoot);

            // 2. Gather Dependencies
            string rootPath = AssetDatabase.GetAssetPath(targetObj);
            string[] deps = AssetDatabase.GetDependencies(rootPath, true);

            var assetsToMove = deps.Where(path => 
                !path.EndsWith(".cs") && 
                !path.StartsWith("Packages/") && 
                !path.Contains("/unity_builtin_extra")
            ).Distinct().ToList();

            FolderStructureMode structureMode = (FolderStructureMode)_structureModeField.value;
            ConflictResolution conflictMode = (ConflictResolution)_conflictModeField.value;

            foreach (var src in assetsToMove)
            {
                // Skip if source is already inside target root (already moved)
                if (src.StartsWith(targetRoot + "/")) continue;

                string fileName = Path.GetFileName(src);
                string intendedPath = "";

                // Calculate intended path
                if (structureMode == FolderStructureMode.FlattenToSingleFolder)
                {
                    intendedPath = $"{targetRoot}/{fileName}";
                }
                else
                {
                    // Relative structure logic
                    string relativePath = src.StartsWith("Assets/") ? src.Substring(7) : src;
                    intendedPath = $"{targetRoot}/{relativePath}";
                }

                var op = new AssetMoveOperation { SourcePath = src, DestinationPath = intendedPath, ResolutionType = "OK" };

                // --- COLLISION CHECKS ---

                // Check 1: Exact Path Collision
                bool exactPathExists = File.Exists(intendedPath);

                // Check 2: Deep Cache Collision (Is this filename ANYWHERE in the dest folder?)
                if (_destinationCache.ContainsKey(fileName))
                {
                    string existingPath = _destinationCache[fileName];
                    
                    // If it's the exact same path, it's a standard overwrite/skip
                    if (existingPath == intendedPath)
                    {
                        if (conflictMode == ConflictResolution.Skip) op.ResolutionType = "Skip";
                        else if (conflictMode == ConflictResolution.Overwrite) op.ResolutionType = "Overwrite";
                        else if (conflictMode == ConflictResolution.AutoRename) 
                        {
                            op.ResolutionType = "Rename";
                            op.DestinationPath = GenerateUniquePath(intendedPath);
                        }
                    }
                    else
                    {
                        // It exists, but in a DIFFERENT subfolder!
                        op.IsDeepConflict = true;
                        op.ConflictPath = existingPath;
                        
                        // Default behavior for deep conflict:
                        // Usually you want to rename to avoid two files named "Texture.png" causing confusion,
                        // OR you might want to skip. We apply the ConflictMode logic here too.
                        
                        if (conflictMode == ConflictResolution.Skip) op.ResolutionType = "Skip";
                        else 
                        {
                            // Even if Overwrite is selected, we cannot overwrite a file in a DIFFERENT folder
                            // by moving this one. So we force Rename to keep both safe.
                            op.ResolutionType = "Rename";
                            op.DestinationPath = GenerateUniquePath(intendedPath);
                        }
                    }
                }
                else if (exactPathExists) // Fallback if cache missed but file exists
                {
                     if (conflictMode == ConflictResolution.Skip) op.ResolutionType = "Skip";
                     else if (conflictMode == ConflictResolution.Overwrite) op.ResolutionType = "Overwrite";
                     else { op.ResolutionType = "Rename"; op.DestinationPath = GenerateUniquePath(intendedPath); }
                }

                if(op.ResolutionType != "Skip")
                {
                    _plannedOperations.Add(op);
                }
            }

            _previewList.itemsSource = _plannedOperations;
            _previewList.Rebuild();
            _transferButton.SetEnabled(_plannedOperations.Count > 0);
            _statusLabel.text = $"Found {_plannedOperations.Count} assets to move.";
        }

        private string GenerateUniquePath(string fullPath)
        {
            string dir = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string ext = Path.GetExtension(fullPath);
            int count = 1;
            string newPath = fullPath;
            
            // Checks file system to find next available name
            while (File.Exists(newPath))
            {
                newPath = $"{dir}/{name} {count}{ext}";
                count++;
            }
            return newPath;
        }

        private void ExecuteTransfer()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var op in _plannedOperations)
                {
                    string targetDir = Path.GetDirectoryName(op.DestinationPath);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    if (op.ResolutionType == "Overwrite")
                    {
                        AssetDatabase.DeleteAsset(op.DestinationPath);
                    }

                    string err = AssetDatabase.MoveAsset(op.SourcePath, op.DestinationPath);
                    if(!string.IsNullOrEmpty(err)) Debug.LogError(err);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            
            // Clean UI
            _prefabField.value = null;
            _plannedOperations.Clear();
            _previewList.Rebuild();
            _statusLabel.text = "Move Completed!";
            EditorUtility.DisplayDialog("Success", "Assets moved successfully.", "OK");
        }
    }
}