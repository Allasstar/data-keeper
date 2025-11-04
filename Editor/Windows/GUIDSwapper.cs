using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class GUIDSwapper : EditorWindow
    {
        private enum Mode { Single, Batch }
        private Mode currentMode = Mode.Single;
    
        // Single mode
        private Object asset1;
        private Object asset2;
    
        // Batch mode
        private DefaultAsset folder1;
        private DefaultAsset folder2;
        private List<SwapPair> swapPairs = new List<SwapPair>();
        private List<string> folder2Files = new List<string>();
        private List<string> folder2Names = new List<string>();
    
        private Vector2 scrollPos;
        private Vector2 tableScrollPos;
        private List<string> log = new List<string>();
        private float _matchThreshold = 0.75f;

        [System.Serializable]
        private class SwapPair
        {
            public string file1Path;
            public string file1Name;
            public int selectedIndex = -1; // -1 means "None"
        }

        [MenuItem("Tools/Windows/GUID Swapper", priority = 6)]
        public static void ShowWindow()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_preAudioLoopOff@2x");
            var window = GetWindow<GUIDSwapper>();
            // window.minSize = new Vector2(400, 300);
            window.titleContent = new GUIContent("GUID Swapper", icon);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("WARNING: This tool swaps GUIDs in .meta files. Always backup your project first!", MessageType.Warning);
        
            EditorGUILayout.Space();
        
            // Mode selection
            currentMode = (Mode)GUILayout.Toolbar((int)currentMode, new string[] { "Single Swap", "Batch Swap" });
        
            EditorGUILayout.Space();
        
            if (currentMode == Mode.Single)
            {
                DrawSingleMode();
            }
            else
            {
                DrawBatchMode();
            }
        
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);
        
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            foreach (var line in log)
            {
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        
            if (GUILayout.Button("Clear Log"))
            {
                log.Clear();
            }
        }

        private void DrawSingleMode()
        {
            asset1 = EditorGUILayout.ObjectField("Asset 1", asset1, typeof(Object), false);
            asset2 = EditorGUILayout.ObjectField("Asset 2", asset2, typeof(Object), false);
        
            EditorGUILayout.Space();
        
            GUI.enabled = asset1 != null && asset2 != null;
            if (GUILayout.Button("Swap GUIDs", GUILayout.Height(30)))
            {
                SwapGUIDs(asset1, asset2);
            }
            GUI.enabled = true;
        }

        private void DrawBatchMode()
        {
            EditorGUILayout.LabelField("Select Folders:", EditorStyles.boldLabel);
        
            folder1 = EditorGUILayout.ObjectField("Source Folder", folder1, typeof(DefaultAsset), false) as DefaultAsset;
            folder2 = EditorGUILayout.ObjectField("Target Folder", folder2, typeof(DefaultAsset), false) as DefaultAsset;
        
            if (GUILayout.Button("Load Files from Folders"))
            {
                LoadFoldersAndMatch();
            }
        
            if (swapPairs.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Swap Pairs ({swapPairs.Count}):", EditorStyles.boldLabel);
            
                // Table header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Source File", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                EditorGUILayout.LabelField("Target File", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginVertical("box");
                tableScrollPos = EditorGUILayout.BeginScrollView(tableScrollPos, GUILayout.Height(300));
            
                // Create dropdown options with "None" as first option
                var dropdownOptions = new List<string> { "-- None --" };
                dropdownOptions.AddRange(folder2Names);
            
                for (int i = 0; i < swapPairs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                
                    // Source file (read-only)
                    EditorGUILayout.LabelField(swapPairs[i].file1Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                
                    // Target file (dropdown) - add 1 to index because of "None" option
                    int displayIndex = swapPairs[i].selectedIndex + 1;
                    displayIndex = EditorGUILayout.Popup(displayIndex, dropdownOptions.ToArray());
                    swapPairs[i].selectedIndex = displayIndex - 1; // Convert back to actual index
                
                    EditorGUILayout.EndHorizontal();
                }
            
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            
                EditorGUILayout.Space();
            
                if (GUILayout.Button("Swap All GUIDs", GUILayout.Height(30)))
                {
                    BatchSwapGUIDs();
                }
            }
        }

        private void LoadFoldersAndMatch()
        {
            if (folder1 == null || folder2 == null)
            {
                log.Add("ERROR: Please select both folders.");
                return;
            }
        
            string path1 = AssetDatabase.GetAssetPath(folder1);
            string path2 = AssetDatabase.GetAssetPath(folder2);
        
            if (!AssetDatabase.IsValidFolder(path1) || !AssetDatabase.IsValidFolder(path2))
            {
                log.Add("ERROR: Selected assets must be folders.");
                return;
            }
        
            // Get all files from both folders (non-recursive)
            var files1 = Directory.GetFiles(path1)
                .Where(f => !f.EndsWith(".meta"))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();
            
            folder2Files = Directory.GetFiles(path2)
                .Where(f => !f.EndsWith(".meta"))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();
        
            folder2Names = folder2Files.Select(f => Path.GetFileName(f)).ToList();
        
            if (files1.Count == 0)
            {
                log.Add("ERROR: No files found in source folder.");
                return;
            }
        
            if (folder2Files.Count == 0)
            {
                log.Add("ERROR: No files found in target folder.");
                return;
            }
        
            // Create swap pairs with best match preselected
            swapPairs.Clear();
        
            foreach (var file1 in files1)
            {
                var pair = new SwapPair
                {
                    file1Path = file1,
                    file1Name = Path.GetFileName(file1),
                    selectedIndex = -1 // Default to "None"
                };
            
                // Find best match by name similarity (minimum 50% match required)
                int bestMatchIndex = FindBestMatch(pair.file1Name, folder2Names);
                pair.selectedIndex = bestMatchIndex;
            
                swapPairs.Add(pair);
            }
        
            int matchedCount = swapPairs.Count(p => p.selectedIndex >= 0);
            log.Add($"Loaded {files1.Count} files from source folder.");
            log.Add($"Loaded {folder2Files.Count} files from target folder.");
            log.Add($"Auto-matched {matchedCount} files (minimum 50% similarity required).");
        }

        private int FindBestMatch(string sourceName, List<string> targetNames)
        {
            int bestIndex = -1; // -1 means no match found
            float bestSimilarity = 0f;
        
            // Remove extension for comparison
            string sourceBase = Path.GetFileNameWithoutExtension(sourceName).ToLower();
        
            for (int i = 0; i < targetNames.Count; i++)
            {
                string targetBase = Path.GetFileNameWithoutExtension(targetNames[i]).ToLower();
                float similarity = CalculateSimilarity(sourceBase, targetBase);
            
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestIndex = i;
                }
            }
        
            // Only return match if similarity is at least 50%
            if (bestSimilarity >= _matchThreshold)
            {
                return bestIndex;
            }
        
            return -1; // No good match found
        }

        private float CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0f;
        
            // Exact match
            if (s1 == s2)
                return 1f;
        
            // Calculate percentage of matching characters in correct order
            int matches = 0;
            int minLen = Mathf.Min(s1.Length, s2.Length);
            int maxLen = Mathf.Max(s1.Length, s2.Length);
        
            // Count matching characters in sequence from start
            for (int i = 0; i < minLen; i++)
            {
                if (s1[i] == s2[i])
                    matches++;
                else
                    break; // Stop at first mismatch
            }
        
            // Calculate similarity as percentage of matching sequential characters
            float sequentialSimilarity = (float)matches / maxLen;
        
            // Also check if one string contains the other (for cases like "img_01" and "img_01_converted")
            bool containsSimilarity = s1.Contains(s2) || s2.Contains(s1);
            if (containsSimilarity)
            {
                float containsScore = (float)minLen / maxLen;
                sequentialSimilarity = Mathf.Max(sequentialSimilarity, containsScore);
            }
        
            return sequentialSimilarity;
        }

        private void BatchSwapGUIDs()
        {
            int validPairs = swapPairs.Count(p => p.selectedIndex >= 0);
        
            if (validPairs == 0)
            {
                EditorUtility.DisplayDialog("No Valid Pairs", 
                    "No files are matched. Please select target files from the dropdowns.", 
                    "OK");
                return;
            }
        
            if (!EditorUtility.DisplayDialog("Batch Swap GUIDs", 
                    $"This will swap GUIDs for {validPairs} file pairs (skipping unmatched files).\n\n" +
                    "This means all references will be swapped.\n\n" +
                    "BACKUP YOUR PROJECT FIRST!\n\n" +
                    "Continue?", 
                    "I Have a Backup, Continue", "Cancel"))
            {
                return;
            }

            log.Clear();
            int successCount = 0;
            int failCount = 0;
            int skippedCount = 0;
        
            for (int i = 0; i < swapPairs.Count; i++)
            {
                var pair = swapPairs[i];
            
                // Skip if no file selected
                if (pair.selectedIndex < 0)
                {
                    skippedCount++;
                    log.Add($"⊘ Skipped: {pair.file1Name} (no match selected)");
                    continue;
                }
            
                string file2Path = folder2Files[pair.selectedIndex];
            
                EditorUtility.DisplayProgressBar("Swapping GUIDs", 
                    $"Processing {i + 1}/{swapPairs.Count}", 
                    (float)i / swapPairs.Count);
            
                if (SwapGUIDsByPath(pair.file1Path, file2Path))
                {
                    successCount++;
                    log.Add($"✓ Swapped: {pair.file1Name} ↔ {Path.GetFileName(file2Path)}");
                }
                else
                {
                    failCount++;
                    log.Add($"✗ Failed: {pair.file1Name} ↔ {Path.GetFileName(file2Path)}");
                }
            }
        
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        
            log.Add("");
            log.Add($"Batch swap complete! Success: {successCount}, Failed: {failCount}, Skipped: {skippedCount}");
        
            EditorUtility.DisplayDialog("Batch Swap Complete", 
                $"Successfully swapped: {successCount}\nFailed: {failCount}\nSkipped: {skippedCount}\n\nCheck the log for details.", 
                "OK");
        }

        private void SwapGUIDs(Object obj1, Object obj2)
        {
            string path1 = AssetDatabase.GetAssetPath(obj1);
            string path2 = AssetDatabase.GetAssetPath(obj2);
        
            if (SwapGUIDsByPath(path1, path2))
            {
                log.Add($"Successfully swapped GUIDs between:");
                log.Add($"  {path1}");
                log.Add($"  {path2}");
                AssetDatabase.Refresh();
            }
        }

        private bool SwapGUIDsByPath(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
            {
                return false;
            }
        
            string metaPath1 = path1 + ".meta";
            string metaPath2 = path2 + ".meta";
        
            if (!File.Exists(metaPath1) || !File.Exists(metaPath2))
            {
                return false;
            }
        
            try
            {
                // Read meta files
                string meta1Content = File.ReadAllText(metaPath1);
                string meta2Content = File.ReadAllText(metaPath2);
            
                // Extract GUIDs
                string guid1 = ExtractGUID(meta1Content);
                string guid2 = ExtractGUID(meta2Content);
            
                if (string.IsNullOrEmpty(guid1) || string.IsNullOrEmpty(guid2))
                {
                    return false;
                }
            
                // Swap GUIDs in meta files
                string newMeta1 = meta1Content.Replace($"guid: {guid1}", $"guid: {guid2}");
                string newMeta2 = meta2Content.Replace($"guid: {guid2}", $"guid: {guid1}");
            
                // Write back
                File.WriteAllText(metaPath1, newMeta1);
                File.WriteAllText(metaPath2, newMeta2);
            
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error swapping GUIDs: {e.Message}");
                return false;
            }
        }
    
        private string ExtractGUID(string metaContent)
        {
            var match = Regex.Match(metaContent, @"guid:\s*([a-f0-9]+)");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}