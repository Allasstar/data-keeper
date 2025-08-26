#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DataKeeper.DynamicScene
{
    public class AddressableConverterTool : EditorWindow
    {
        private const string LOADER_PREFIX = "[LOADER] ";
        
        private GameObject selectedObject;
        private string addressableKey = "";
        private string groupName = "SubScene Objects";
        private float loadDistance = 1000f;
        private float unloadDistance = 1200f;
        private int maxInstances = 1;
        private bool useObjectPooling = true;
    
        // Batch conversion settings
        private bool convertChildren = false;
        private bool includeInactive = true;
        private string keyPrefix = "";
        private string keySuffix = "";
        private bool createParentFolder = true;
    
        // Duplicate handling settings
        private DuplicateHandlingMode duplicateMode = DuplicateHandlingMode.AutoIncrement;
        private bool usePositionInKey = false;
        private bool useInstanceIdInKey = false;
    
        // Tracking for duplicates
        private Dictionary<string, int> keyCounters = new Dictionary<string, int>();
    
        public enum DuplicateHandlingMode
        {
            AutoIncrement,      // Tree, Tree_1, Tree_2, etc.
            UsePosition,        // Tree_X100_Y50_Z200
            UseInstanceId,      // Tree_12345
            Skip,               // Skip duplicates
            Overwrite           // Overwrite existing prefabs
        }
    
        [MenuItem("Tools/Addressable Converter", priority = 11)]
        public static void ShowWindow()
        {
            GetWindow<AddressableConverterTool>("Addressable Converter");
        }
    
        private void OnGUI()
        {
            GUILayout.Label("Addressable Converter", EditorStyles.boldLabel);
        
            EditorGUILayout.Space();
        
            // Object selection
            selectedObject = (GameObject)EditorGUILayout.ObjectField("GameObject to Convert", selectedObject, typeof(GameObject), true);
        
            if (selectedObject == null && Selection.activeGameObject != null)
            {
                selectedObject = Selection.activeGameObject;
            }
        
            EditorGUILayout.Space();
        
            // Batch conversion settings
            GUILayout.Label("Batch Conversion Settings", EditorStyles.boldLabel);
            convertChildren = EditorGUILayout.Toggle("Convert All Children", convertChildren);
        
            if (convertChildren)
            {
                EditorGUI.indentLevel++;
                includeInactive = EditorGUILayout.Toggle("Include Inactive Children", includeInactive);
            
                EditorGUILayout.Space();
                GUILayout.Label("Key Generation", EditorStyles.boldLabel);
                keyPrefix = EditorGUILayout.TextField("Key Prefix", keyPrefix);
                keySuffix = EditorGUILayout.TextField("Key Suffix", keySuffix);
            
                EditorGUILayout.Space();
                GUILayout.Label("Duplicate Handling", EditorStyles.boldLabel);
                duplicateMode = (DuplicateHandlingMode)EditorGUILayout.EnumPopup("Duplicate Mode", duplicateMode);
            
                // Show additional options based on duplicate mode
                if (duplicateMode == DuplicateHandlingMode.UsePosition)
                {
                    EditorGUILayout.HelpBox("Will append position coordinates to keys (e.g., Tree_X100_Y50_Z200)", MessageType.Info);
                }
                else if (duplicateMode == DuplicateHandlingMode.UseInstanceId)
                {
                    EditorGUILayout.HelpBox("Will append Unity Instance ID to keys (e.g., Tree_12345)", MessageType.Info);
                }
                else if (duplicateMode == DuplicateHandlingMode.AutoIncrement)
                {
                    EditorGUILayout.HelpBox("Will auto-increment duplicate names (e.g., Tree, Tree_1, Tree_2)", MessageType.Info);
                }
                else if (duplicateMode == DuplicateHandlingMode.Skip)
                {
                    EditorGUILayout.HelpBox("Will skip objects with duplicate names", MessageType.Warning);
                }
                else if (duplicateMode == DuplicateHandlingMode.Overwrite)
                {
                    EditorGUILayout.HelpBox("Will overwrite existing prefabs and addressables", MessageType.Warning);
                }
            
                createParentFolder = EditorGUILayout.Toggle("Create Parent Folder", createParentFolder);
                EditorGUI.indentLevel--;
            
                // Show preview of children that will be converted
                if (selectedObject != null)
                {
                    var children = GetChildrenToConvert();
                    var duplicateGroups = GetDuplicateGroups(children);
                
                    if (children.Count > 0)
                    {
                        string infoText = $"Will convert {children.Count} children objects";
                        if (duplicateGroups.Count > 0)
                        {
                            int duplicateCount = duplicateGroups.Sum(g => g.Value);
                            infoText += $" ({duplicateCount} have duplicate names)";
                        }
                        EditorGUILayout.HelpBox(infoText, MessageType.Info);
                    
                        // Show duplicate groups if any
                        if (duplicateGroups.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField("Duplicate Names Found:", EditorStyles.boldLabel);
                            foreach (var group in duplicateGroups.Take(5)) // Limit display
                            {
                                EditorGUILayout.LabelField($"• \"{group.Key}\" ({group.Value} objects)");
                            }
                            if (duplicateGroups.Count > 5)
                            {
                                EditorGUILayout.LabelField($"... and {duplicateGroups.Count - 5} more duplicate groups");
                            }
                            EditorGUI.indentLevel--;
                        }
                    
                        if (children.Count <= 10 && duplicateGroups.Count == 0) // Show list if not too many and no duplicates
                        {
                            EditorGUI.indentLevel++;
                            foreach (var child in children)
                            {
                                EditorGUILayout.LabelField($"• {child.name}");
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No children found to convert", MessageType.Warning);
                    }
                }
            }
        
            EditorGUILayout.Space();
        
            // Single object settings (only shown when not converting children)
            if (!convertChildren)
            {
                GUILayout.Label("Single Object Settings", EditorStyles.boldLabel);
                addressableKey = EditorGUILayout.TextField("Addressable Key", addressableKey);
            }
        
            // Common settings
            GUILayout.Label("Addressable Settings", EditorStyles.boldLabel);
            groupName = EditorGUILayout.TextField("Group Name", groupName);
        
            EditorGUILayout.Space();
        
            // Settings
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            loadDistance = EditorGUILayout.FloatField("Load Distance", loadDistance);
            unloadDistance = EditorGUILayout.FloatField("Unload Distance", unloadDistance);
            maxInstances = EditorGUILayout.IntField("Max Instances", maxInstances);
            useObjectPooling = EditorGUILayout.Toggle("Use Object Pooling", useObjectPooling);
        
            EditorGUILayout.Space();
        
            // Auto-generate key button (only for single objects)
            if (!convertChildren && GUILayout.Button("Auto-Generate Key from Object Name"))
            {
                if (selectedObject != null)
                {
                    addressableKey = GenerateKeyFromName(selectedObject.name);
                }
            }
        
            // Batch key generation preview
            if (convertChildren && selectedObject != null)
            {
                if (GUILayout.Button("Preview Generated Keys"))
                {
                    ShowKeyPreview();
                }
            }
        
            EditorGUILayout.Space();
        
            // Convert button
            string buttonText = convertChildren ? "Convert All Children to Addressable" : "Convert to Addressable";
            GUI.enabled = selectedObject != null && (convertChildren || !string.IsNullOrEmpty(addressableKey));
        
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (convertChildren)
                {
                    ConvertChildrenToAddressableLOD();
                }
                else
                {
                    ConvertToAddressableLOD();
                }
            }
            GUI.enabled = true;
        
            EditorGUILayout.Space();
        
            // Instructions
            string instructions = convertChildren ? 
                "Batch Conversion Instructions:\n" +
                "1. Select a parent GameObject\n" +
                "2. Configure batch settings and key naming\n" +
                "3. Click Convert All Children to process all child objects\n" +
                "4. Each child will become an addressable with its own loader\n" +
                "5. Original children will be deactivated" :
            
                "Single Conversion Instructions:\n" +
                "1. Select a GameObject in the scene\n" +
                "2. Set addressable key and settings\n" +
                "3. Click Convert to create:\n" +
                "   - Prefab in Assets/Addressable Objects/\n" +
                "   - Addressable entry\n" +
                "   - Empty GameObject with Loader script\n" +
                "4. Original object will be deactivated";
        
            EditorGUILayout.HelpBox(instructions, MessageType.Info);
        
            if (GUILayout.Button("Setup Addressable Groups"))
            {
                SetupAddressableGroups();
            }
        }
    
        private void ConvertToAddressableLOD()
        {
            ConvertSingleObject(selectedObject, addressableKey);
        }
    
        private void ConvertChildrenToAddressableLOD()
        {
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a parent object.", "OK");
                return;
            }
        
            var children = GetChildrenToConvert();
        
            if (children.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No children found to convert.", "OK");
                return;
            }
        
            // Reset counters for duplicate handling
            keyCounters.Clear();
        
            // Analyze duplicates
            var duplicateGroups = GetDuplicateGroups(children);
            string duplicateInfo = "";
            if (duplicateGroups.Count > 0)
            {
                int duplicateCount = duplicateGroups.Sum(g => g.Value);
                duplicateInfo = $"\n\nDuplicate names detected: {duplicateCount} objects with duplicate names will be handled using {duplicateMode} mode.";
            }
        
            bool proceed = EditorUtility.DisplayDialog("Batch Conversion", 
                $"Are you sure you want to convert {children.Count} children objects to Addressable?{duplicateInfo}\n\n" +
                "This will:\n" +
                "• Create prefabs for each child\n" +
                "• Add them to Addressables\n" +
                "• Create Loader\n" +
                "• Deactivate original children", 
                "Yes, Convert All", "Cancel");
        
            if (!proceed) return;
        
            // Create parent folder if requested
            string parentFolderPath = "Assets/Addressable Objects";
            if (createParentFolder && !string.IsNullOrEmpty(selectedObject.name))
            {
                parentFolderPath = $"Assets/Addressable Objects/{GenerateKeyFromName(selectedObject.name)}";
                CreateFolderRecursive(parentFolderPath);
            }
        
            int successCount = 0;
            int failCount = 0;
            int skippedCount = 0;
            List<string> createdKeys = new List<string>();
        
            // Show progress bar
            for (int i = 0; i < children.Count; i++)
            {
                GameObject child = children[i];
                float progress = (float)i / children.Count;
            
                if (EditorUtility.DisplayCancelableProgressBar("Converting Children", 
                        $"Processing {child.name} ({i + 1}/{children.Count})", progress))
                {
                    break; // User cancelled
                }
            
                try
                {
                    string childKey = GenerateUniqueKeyForChild(child);
                
                    if (string.IsNullOrEmpty(childKey)) // Skip mode returned empty key
                    {
                        skippedCount++;
                        Debug.LogWarning($"Skipped {child.name} due to duplicate handling mode");
                        continue;
                    }
                
                    // Check if we should overwrite or skip existing assets
                    string prefabPath = $"{parentFolderPath}/{childKey}.prefab";
                    if (File.Exists(prefabPath) && duplicateMode == DuplicateHandlingMode.Skip)
                    {
                        skippedCount++;
                        Debug.LogWarning($"Skipped {child.name} - prefab already exists: {prefabPath}");
                        continue;
                    }
                
                    ConvertSingleObject(child, childKey, parentFolderPath);
                    createdKeys.Add(childKey);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to convert {child.name}: {e.Message}");
                    failCount++;
                }
            }
        
            EditorUtility.ClearProgressBar();
        
            // Show results
            string resultMessage = $"Conversion completed!\n\n" +
                                   $"Successfully converted: {successCount}\n" +
                                   $"Skipped: {skippedCount}\n" +
                                   $"Failed: {failCount}";
        
            if (createdKeys.Count > 0 && createdKeys.Count <= 10)
            {
                resultMessage += $"\n\nCreated keys:\n{string.Join("\n", createdKeys)}";
            }
        
            resultMessage += "\n\nCheck the console for any error details.";
        
            EditorUtility.DisplayDialog("Batch Conversion Complete", resultMessage, "OK");
        
            // Refresh the asset database
            AssetDatabase.Refresh();
        }
    
        private List<GameObject> GetChildrenToConvert()
        {
            List<GameObject> children = new List<GameObject>();
        
            if (selectedObject == null) return children;
        
            for (int i = 0; i < selectedObject.transform.childCount; i++)
            {
                Transform childTransform = selectedObject.transform.GetChild(i);
                GameObject child = childTransform.gameObject;
            
                // Skip if inactive and we don't want inactive objects
                if (!includeInactive && !child.activeInHierarchy)
                    continue;
                
                // Skip objects that already have Loader (avoid double conversion)
                if (child.GetComponent<AddressableLoader>() != null)
                    continue;
            
                children.Add(child);
            }
        
            return children;
        }
    
        private string GenerateKeyForChild(GameObject child)
        {
            string baseName = GenerateKeyFromName(child.name);
            return $"{keyPrefix}{baseName}{keySuffix}";
        }
    
        private string GenerateUniqueKeyForChild(GameObject child)
        {
            string baseKey = GenerateKeyForChild(child);
        
            switch (duplicateMode)
            {
                case DuplicateHandlingMode.AutoIncrement:
                    return GetAutoIncrementedKey(baseKey);
                
                case DuplicateHandlingMode.UsePosition:
                    Vector3 pos = child.transform.position;
                    string positionSuffix = $"_X{Mathf.RoundToInt(pos.x)}_Y{Mathf.RoundToInt(pos.y)}_Z{Mathf.RoundToInt(pos.z)}";
                    return $"{baseKey}{positionSuffix}";
                
                case DuplicateHandlingMode.UseInstanceId:
                    return $"{baseKey}_{child.GetInstanceID()}";
                
                case DuplicateHandlingMode.Skip:
                    // Check if this key already exists
                    if (keyCounters.ContainsKey(baseKey))
                    {
                        return ""; // Empty string means skip
                    }
                    else
                    {
                        keyCounters[baseKey] = 1;
                        return baseKey;
                    }
                
                case DuplicateHandlingMode.Overwrite:
                    return baseKey; // Always use base key, will overwrite
                
                default:
                    return GetAutoIncrementedKey(baseKey);
            }
        }
    
        private string GetAutoIncrementedKey(string baseKey)
        {
            if (!keyCounters.ContainsKey(baseKey))
            {
                keyCounters[baseKey] = 0;
                return baseKey; // First occurrence gets the base name
            }
            else
            {
                keyCounters[baseKey]++;
                return $"{baseKey}_{keyCounters[baseKey]}";
            }
        }
    
        private Dictionary<string, int> GetDuplicateGroups(List<GameObject> objects)
        {
            var nameGroups = objects.GroupBy(obj => GenerateKeyForChild(obj))
                .Where(group => group.Count() > 1)
                .ToDictionary(group => group.Key, group => group.Count());
            return nameGroups;
        }
    
        private string GenerateKeyFromName(string name)
        {
            return name.Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(".", "_")
                .Replace("-", "_")
                .Replace("#", "_");
        }
    
        private void ShowKeyPreview()
        {
            var children = GetChildrenToConvert();
            if (children.Count == 0)
            {
                EditorUtility.DisplayDialog("Key Preview", "No children to convert found.", "OK");
                return;
            }
        
            // Reset counters for preview
            keyCounters.Clear();
        
            string preview = $"Generated Keys Preview ({duplicateMode} mode):\n\n";
            int maxShow = Mathf.Min(children.Count, 25); // Limit to prevent huge dialogs
        
            var duplicateGroups = GetDuplicateGroups(children);
        
            if (duplicateGroups.Count > 0)
            {
                preview += $"⚠️ Found {duplicateGroups.Count} duplicate name groups\n\n";
            }
        
            for (int i = 0; i < maxShow; i++)
            {
                GameObject child = children[i];
                string originalName = child.name;
                string generatedKey = GenerateUniqueKeyForChild(child);
            
                if (string.IsNullOrEmpty(generatedKey))
                {
                    preview += $"• {originalName} → [SKIPPED - Duplicate]\n";
                }
                else if (generatedKey != GenerateKeyForChild(child) && duplicateMode != DuplicateHandlingMode.UsePosition && duplicateMode != DuplicateHandlingMode.UseInstanceId)
                {
                    preview += $"• {originalName} → {generatedKey} [Modified]\n";
                }
                else
                {
                    preview += $"• {originalName} → {generatedKey}\n";
                }
            }
        
            if (children.Count > maxShow)
            {
                preview += $"\n... and {children.Count - maxShow} more objects";
            }
        
            // Add summary
            if (duplicateGroups.Count > 0)
            {
                preview += $"\n\nDuplicate Summary:\n";
                foreach (var group in duplicateGroups.Take(5))
                {
                    preview += $"• \"{group.Key}\" appears {group.Value} times\n";
                }
                if (duplicateGroups.Count > 5)
                {
                    preview += $"... and {duplicateGroups.Count - 5} more duplicate groups\n";
                }
            }
        
            EditorUtility.DisplayDialog("Key Preview", preview, "OK");
        }
    
        private void CreateFolderRecursive(string path)
        {
            string[] folders = path.Replace("Assets/", "").Split('/');
            string currentPath = "Assets";
        
            foreach (string folder in folders)
            {
                string newPath = currentPath + "/" + folder;
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folder);
                }
                currentPath = newPath;
            }
        }
    
        private void ConvertSingleObject(GameObject targetObject, string key, string customDirectory = null)
        {
            if (targetObject == null || string.IsNullOrEmpty(key))
            {
                throw new System.ArgumentException("Invalid target object or key.");
            }
        
            // Create directory for Loader prefabs
            string lodDirectory = customDirectory ?? "Assets/AddressableLOD";
            CreateFolderRecursive(lodDirectory);
        
            // Create prefab from target object
            string prefabPath = $"{lodDirectory}/{key}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
        
            // Setup addressable
            SetupAddressable(prefab, key, groupName);
        
            // Create empty GameObject with Loader
            GameObject lodManager = new GameObject($"{LOADER_PREFIX}{targetObject.name}");
            lodManager.transform.position = targetObject.transform.position;
            lodManager.transform.rotation = targetObject.transform.rotation;
            lodManager.transform.SetParent(targetObject.transform.parent);
        
            // Add Loader component
            AddressableLoader manager = lodManager.AddComponent<AddressableLoader>();
        
            // Setup manager properties
            manager.loadDistance = loadDistance;
            manager.unloadDistance = unloadDistance;
            manager.maxInstances = maxInstances;
            manager.useObjectPooling = useObjectPooling;
        
            // Create AssetReference
            var assetReference = new AssetReferenceGameObject(AssetDatabase.AssetPathToGUID(prefabPath));
        
            // Use reflection to set the private field
            var field = typeof(AddressableLoader).GetField("addressableAsset");
            if (field != null)
            {
                field.SetValue(manager, assetReference);
            }
        
            // Deactivate original object
            targetObject.SetActive(false);
        
            Debug.Log($"Successfully converted {targetObject.name} to Addressable with key: {key}");
        }
    
        private void SetupAddressable(GameObject prefab, string key, string group)
        {
            // Initialize addressables if needed
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    true, true);
            }
        
            var settings = AddressableAssetSettingsDefaultObject.Settings;
        
            // Create or get group
            AddressableAssetGroup assetGroup = settings.FindGroup(group);
            if (assetGroup == null)
            {
                assetGroup = settings.CreateGroup(group, false, false, true, null);
            }
        
            // Add to addressables
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            var entry = settings.CreateOrMoveEntry(guid, assetGroup, false, false);
            entry.SetAddress(key, false);
        
            // Mark dirty
            EditorUtility.SetDirty(settings);
        
            Debug.Log($"Added {key} to addressables in group {group}");
        }
    
        private void SetupAddressableGroups()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                // Create addressable settings
                AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    true, true);
            }
        
            EditorUtility.DisplayDialog("Setup Complete", "Addressable groups have been initialized!", "OK");
        }
    }
}
#endif