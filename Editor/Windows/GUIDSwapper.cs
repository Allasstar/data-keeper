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
        private bool validateTypesSingle = true;
    
        // Batch mode
        private DefaultAsset folder1;
        private DefaultAsset folder2;
        private List<SwapPair> swapPairs = new List<SwapPair>();
        private List<string> folder2Files = new List<string>();
        private List<string> folder2Names = new List<string>();
        private bool validateTypesBatch = true;
        private bool ignoreExtensions = true;
        private float matchThreshold = 0.75f;
        private string matchKeyword = "";
        private MatchAlgorithm matchAlgorithm = MatchAlgorithm.Fuzzy;
    
        private Vector2 scrollPos;
        private Vector2 tableScrollPos;
        private Vector2 singleScrollPos;
        private Vector2 batchScrollPos;
        private List<string> log = new List<string>();

        private enum MatchAlgorithm { Sequential, Fuzzy, LongestConsecutive, TokenJaccard }

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
            window.minSize = new Vector2(400, 500);
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
                singleScrollPos = EditorGUILayout.BeginScrollView(singleScrollPos);
                DrawSingleModeContent();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                batchScrollPos = EditorGUILayout.BeginScrollView(batchScrollPos);
                DrawBatchModeContent();
                EditorGUILayout.EndScrollView();
            }
        
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);
        
            float logHeight = 100;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(logHeight));
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

        private void DrawSingleModeContent()
        {
            EditorGUILayout.LabelField("Select Assets:", EditorStyles.boldLabel);
            asset1 = EditorGUILayout.ObjectField("Asset 1", asset1, typeof(Object), false);
            asset2 = EditorGUILayout.ObjectField("Asset 2", asset2, typeof(Object), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);
            validateTypesSingle = EditorGUILayout.Toggle("Validate Asset Types", validateTypesSingle);
        
            EditorGUILayout.Space();
        
            GUI.enabled = asset1 != null && asset2 != null;
            if (GUILayout.Button("Swap GUIDs", GUILayout.Height(30)))
            {
                SwapGUIDs(asset1, asset2);
            }
            GUI.enabled = true;
        }

        private void DrawBatchModeContent()
        {
            EditorGUILayout.LabelField("Select Folders:", EditorStyles.boldLabel);
        
            folder1 = EditorGUILayout.ObjectField("First Folder", folder1, typeof(DefaultAsset), false) as DefaultAsset;
            folder2 = EditorGUILayout.ObjectField("Second Folder", folder2, typeof(DefaultAsset), false) as DefaultAsset;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Matching Settings:", EditorStyles.boldLabel);
            matchAlgorithm = (MatchAlgorithm)EditorGUILayout.EnumPopup("Match Algorithm", matchAlgorithm);
            matchThreshold = EditorGUILayout.Slider("Match Threshold", matchThreshold, 0f, 1f);
            matchKeyword = EditorGUILayout.TextField("Filter (comma separated)", matchKeyword);
            ignoreExtensions = EditorGUILayout.Toggle("Ignore File Extensions", ignoreExtensions);
            validateTypesBatch = EditorGUILayout.Toggle("Validate Asset Types", validateTypesBatch);
        
            EditorGUILayout.Space();

            if (GUILayout.Button("Load and Match Files", GUILayout.Height(30)))
            {
                LoadFoldersAndMatch();
            }
        
            if (swapPairs.Count > 0)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Re-Match Files", GUILayout.Height(30)))
                {
                    LoadFoldersAndMatch();
                }
                
                EditorGUILayout.LabelField($"Swap Pairs ({swapPairs.Count}):", EditorStyles.boldLabel);
                
                // Table header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("First File", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("↔", GUILayout.Width(20));
                EditorGUILayout.LabelField("Second File", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginVertical("box");

                float tableHeight = Mathf.Max(150f, position.height * 0.4f);
                tableScrollPos = EditorGUILayout.BeginScrollView(tableScrollPos, GUILayout.Height(tableHeight));
            
                // Create dropdown options with "None" as first option
                var dropdownOptions = new List<string> { "-- None --" };
                dropdownOptions.AddRange(folder2Names);
            
                for (int i = 0; i < swapPairs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                
                    // Source file (read-only)
                    EditorGUILayout.LabelField(swapPairs[i].file1Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField("↔", GUILayout.Width(20));
                
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
            string fullFolder1 = GetFullPath(path1);
            var fullFiles1 = Directory.GetFiles(fullFolder1)
                .Where(f => !f.EndsWith(".meta"))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            var files1 = fullFiles1.Select(GetRelativePath).ToList();

            string fullFolder2 = GetFullPath(path2);
            var fullFiles2 = Directory.GetFiles(fullFolder2)
                .Where(f => !f.EndsWith(".meta"))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            folder2Files = fullFiles2.Select(GetRelativePath).ToList();
        
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
        
            for (int i = 0; i < files1.Count; i++)
            {
                var file1 = files1[i];
                var pair = new SwapPair
                {
                    file1Path = file1,
                    file1Name = Path.GetFileName(file1),
                    selectedIndex = -1 // Default to "None"
                };
            
                // Find best match by name similarity
                int bestMatchIndex = FindBestMatch(pair.file1Name, folder2Names);
                pair.selectedIndex = bestMatchIndex;
            
                swapPairs.Add(pair);
            }
        
            int matchedCount = swapPairs.Count(p => p.selectedIndex >= 0);
            log.Add($"Loaded {files1.Count} files from source folder.");
            log.Add($"Loaded {folder2Files.Count} files from target folder.");
            log.Add($"Auto-matched {matchedCount} files (minimum {(int)(matchThreshold * 100)}% similarity required).");
        }

        private int FindBestMatch(string sourceName, List<string> targetNames)
        {
            int bestIndex = -1;
            float bestSimilarity = 0f;
        
            string sourceCompare = ignoreExtensions ? Path.GetFileNameWithoutExtension(sourceName).ToLower() : sourceName.ToLower();

            string[] keywords = string.IsNullOrEmpty(matchKeyword) 
                ? new string[0] 
                : matchKeyword.Split(',').Select(k => k.Trim().ToLower()).Where(k => !string.IsNullOrEmpty(k)).ToArray();
        
            for (int i = 0; i < targetNames.Count; i++)
            {
                string targetName = targetNames[i];
                string targetLower = targetName.ToLower();

                if (keywords.Length > 0 && !keywords.Any(kw => targetLower.Contains(kw))) continue;

                string targetCompare = ignoreExtensions ? Path.GetFileNameWithoutExtension(targetName).ToLower() : targetName.ToLower();
                float similarity = GetSimilarity(sourceCompare, targetCompare);
            
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestIndex = i;
                }
            }
        
            if (bestSimilarity >= matchThreshold)
            {
                return bestIndex;
            }
        
            return -1;
        }

        private float GetSimilarity(string s1, string s2)
        {
            switch (matchAlgorithm)
            {
                case MatchAlgorithm.Sequential:
                    return SequentialSimilarity(s1, s2);
                case MatchAlgorithm.Fuzzy:
                    return (float)JaroWinklerSimilarity(s1, s2);
                case MatchAlgorithm.LongestConsecutive:
                    return LongestConsecutiveSimilarity(s1, s2);
                case MatchAlgorithm.TokenJaccard:
                    return TokenJaccardSimilarity(s1, s2);
                default:
                    return 0f;
            }
        }

        private float SequentialSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0f;
        
            if (s1 == s2)
                return 1f;
        
            int matches = 0;
            int minLen = Mathf.Min(s1.Length, s2.Length);
            int maxLen = Mathf.Max(s1.Length, s2.Length);
        
            for (int i = 0; i < minLen; i++)
            {
                if (s1[i] == s2[i])
                    matches++;
                else
                    break;
            }
        
            float sequentialSimilarity = (float)matches / maxLen;
        
            bool containsSimilarity = s1.Contains(s2) || s2.Contains(s1);
            if (containsSimilarity)
            {
                float containsScore = (float)minLen / maxLen;
                sequentialSimilarity = Mathf.Max(sequentialSimilarity, containsScore);
            }
        
            return sequentialSimilarity;
        }

        private double JaroSimilarity(string s1, string s2)
        {
            if (s1 == s2) return 1.0;
            int len1 = s1.Length;
            int len2 = s2.Length;
            if (len1 == 0 || len2 == 0) return 0.0;

            int maxDistance = Mathf.Max(len1, len2) / 2 - 1;
            bool[] match1 = new bool[len1];
            bool[] match2 = new bool[len2];
            int matchingCharacters = 0;

            for (int i = 0; i < len1; i++)
            {
                int start = Mathf.Max(0, i - maxDistance);
                int end = Mathf.Min(len2, i + maxDistance + 1);
                for (int j = start; j < end; j++)
                {
                    if (match2[j]) continue;
                    if (s1[i] != s2[j]) continue;
                    match1[i] = true;
                    match2[j] = true;
                    matchingCharacters++;
                    break;
                }
            }

            if (matchingCharacters == 0) return 0.0;

            int transpositions = 0;
            int point = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!match1[i]) continue;
                while (!match2[point]) point++;
                if (s1[i] != s2[point++]) transpositions++;
            }

            transpositions /= 2;

            return ((double)matchingCharacters / len1 +
                    (double)matchingCharacters / len2 +
                    (double)(matchingCharacters - transpositions) / matchingCharacters) / 3.0;
        }

        private double JaroWinklerSimilarity(string s1, string s2)
        {
            double jaro = JaroSimilarity(s1, s2);
            if (jaro < 0.7) return jaro;

            int prefix = 0;
            int minLen = Mathf.Min(s1.Length, s2.Length, 4);
            for (int i = 0; i < minLen; i++)
            {
                if (s1[i] == s2[i]) prefix++;
                else break;
            }

            return jaro + prefix * 0.1 * (1 - jaro);
        }

        private float LongestConsecutiveSimilarity(string s1, string s2)
        {
            int lcsLength = LongestCommonSubstringLength(s1, s2);
            return (float)lcsLength / Mathf.Max(s1.Length, s2.Length);
        }

        private int LongestCommonSubstringLength(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;

            int[,] dp = new int[s1.Length + 1, s2.Length + 1];
            int max = 0;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    if (s1[i - 1] == s2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                        if (dp[i, j] > max) max = dp[i, j];
                    }
                    else
                    {
                        dp[i, j] = 0;
                    }
                }
            }

            return max;
        }

        private float TokenJaccardSimilarity(string s1, string s2)
        {
            var tokens1 = Regex.Split(s1, @"\W+").Where(t => !string.IsNullOrEmpty(t)).Select(t => t.ToLower()).ToHashSet();
            var tokens2 = Regex.Split(s2, @"\W+").Where(t => !string.IsNullOrEmpty(t)).Select(t => t.ToLower()).ToHashSet();

            if (tokens1.Count == 0 || tokens2.Count == 0) return 0f;

            int intersection = tokens1.Intersect(tokens2).Count();
            return (float)intersection / (tokens1.Count + tokens2.Count - intersection);
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
            
                if (SwapGUIDsByPath(pair.file1Path, file2Path, validateTypesBatch))
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
        
            if (SwapGUIDsByPath(path1, path2, validateTypesSingle))
            {
                log.Add($"Successfully swapped GUIDs between:");
                log.Add($"  {path1}");
                log.Add($"  {path2}");
                AssetDatabase.Refresh();
            }
        }

        private bool SwapGUIDsByPath(string path1, string path2, bool validateTypes = false)
        {
            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
            {
                return false;
            }

            if (validateTypes)
            {
                System.Type type1 = AssetDatabase.GetMainAssetTypeAtPath(path1);
                System.Type type2 = AssetDatabase.GetMainAssetTypeAtPath(path2);
                if (type1 != type2)
                {
                    log.Add($"ERROR: Asset types do not match for {Path.GetFileName(path1)} ({type1}) and {Path.GetFileName(path2)} ({type2})");
                    return false;
                }
            }
        
            string metaPath1 = path1 + ".meta";
            string metaPath2 = path2 + ".meta";

            string fullMeta1 = GetFullPath(metaPath1);
            string fullMeta2 = GetFullPath(metaPath2);
        
            if (!File.Exists(fullMeta1) || !File.Exists(fullMeta2))
            {
                return false;
            }
        
            try
            {
                // Read meta files
                string meta1Content = File.ReadAllText(fullMeta1);
                string meta2Content = File.ReadAllText(fullMeta2);
            
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
                File.WriteAllText(fullMeta1, newMeta1);
                File.WriteAllText(fullMeta2, newMeta2);
            
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

        private string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "";

            string normData = Application.dataPath.Replace("\\", "/");
            string projectRoot = normData.Substring(0, normData.Length - 6); // Remove "Assets"
            string normRel = relativePath.Replace("\\", "/");

            return projectRoot + normRel;
        }

        private string GetRelativePath(string fullPath)
        {
            string normFull = fullPath.Replace("\\", "/");
            string normData = Application.dataPath.Replace("\\", "/");

            if (normFull.StartsWith(normData))
            {
                return "Assets" + normFull.Substring(normData.Length);
            }

            return "";
        }
    }
}