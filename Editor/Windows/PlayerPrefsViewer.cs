using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class PlayerPrefsViewer : EditorWindow
{
    private Vector2 scrollPosition;
    private string searchString = "";
    private Dictionary<string, object> prefsDict = new Dictionary<string, object>();
    private Dictionary<string, System.Type> prefsTypes = new Dictionary<string, System.Type>();
    private Dictionary<string, object> editedValues = new Dictionary<string, object>();
    private List<string> keysToDelete = new List<string>();
    private string newKeyName = "";
    private int newValueType = 0;
    private string newValueString = "";

    [MenuItem("Tools/Windows/PlayerPrefs Viewer", priority = 1)]
    public static void ShowWindow()
    {
        GetWindow<PlayerPrefsViewer>("PlayerPrefs Viewer");
    }

    private void OnEnable()
    {
        RefreshPlayerPrefs();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        DrawToolbar();
        
        GUILayout.Space(10);
        DrawPlayerPrefsList();
       
        GUILayout.Space(10);
        DrawBottomButtons();
        
        GUILayout.Space(10);
        DrawAddNewKeySection();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete All", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Delete All PlayerPrefs", 
                    "Are you sure you want to delete all PlayerPrefs for this application?", 
                    "Yes, delete all", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                RefreshPlayerPrefs();
            }
        }
        
        GUILayout.Space(10);

        GUILayout.Label("Search:", GUILayout.Width(50));
        string newSearch = GUILayout.TextField(searchString);
        if (newSearch != searchString)
        {
            searchString = newSearch;
        }

        if (GUILayout.Button("Clear", GUILayout.Width(55)))
        {
            searchString = "";
            GUI.FocusControl(null);
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            RefreshPlayerPrefs();
        }
       
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawPlayerPrefsList()
    {
        var columnKey = GUILayout.Width(200);
        var columnType = GUILayout.Width(50);
        var columnValue = GUILayout.Width(200);
        var columnDelete = GUILayout.Width(60);
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        if (prefsDict.Count == 0)
        {
            EditorGUILayout.HelpBox("No PlayerPrefs found for this application.", MessageType.Info);
            return;
        }

        // Column headers
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, columnKey);
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, columnType);
        EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, columnValue);
        EditorGUILayout.LabelField("", EditorStyles.boldLabel, columnDelete);
        EditorGUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var filteredKeys = prefsDict.Keys.Where(w => w.Contains(searchString)).OrderBy(k => k).ToList();

        if (filteredKeys.Count == 0)
        {
            EditorGUILayout.HelpBox($"No results found for '{searchString}'", MessageType.Info);
        }

        foreach (var key in filteredKeys)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Key
            EditorGUILayout.LabelField(key, columnKey);
            
            // Type
            string typeName = prefsTypes[key].Name;
            EditorGUILayout.LabelField(typeName, columnType);
            
            // Value with editing capability
            object newValue = null;
            var type = prefsTypes[key];
            
            if (type == typeof(int))
            {
                int currentValue = (int)prefsDict[key];
                if (editedValues.ContainsKey(key))
                {
                    currentValue = (int)editedValues[key];
                }
                newValue = EditorGUILayout.IntField(currentValue, columnValue);
            }
            else if (type == typeof(float))
            {
                float currentValue = (float)prefsDict[key];
                if (editedValues.ContainsKey(key))
                {
                    currentValue = (float)editedValues[key];
                }
                newValue = EditorGUILayout.FloatField(currentValue, columnValue);
            }
            else if (type == typeof(string))
            {
                string currentValue = (string)prefsDict[key];
                if (editedValues.ContainsKey(key))
                {
                    currentValue = (string)editedValues[key];
                }
                newValue = EditorGUILayout.TextField(currentValue, columnValue);
            }
            
            // Check if value changed
            bool valueChanged = false;
            if (newValue != null)
            {
                if (!editedValues.ContainsKey(key))
                {
                    if (!prefsDict[key].Equals(newValue))
                    {
                        editedValues[key] = newValue;
                        valueChanged = true;
                    }
                }
                else if (!editedValues[key].Equals(newValue))
                {
                    editedValues[key] = newValue;
                    valueChanged = true;
                }
            }
            
            // Mark edited values
            if (editedValues.ContainsKey(key))
            {
                EditorGUILayout.LabelField("*", GUILayout.Width(10));
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(10));
            }
            
            // Delete button
            if (GUILayout.Button("Delete", columnDelete))
            {
                if (!keysToDelete.Contains(key))
                {
                    keysToDelete.Add(key);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Show pending deletion notification
            if (keysToDelete.Contains(key))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("Will be deleted when you save changes", MessageType.Warning);
                if (GUILayout.Button("Cancel", columnDelete))
                {
                    keysToDelete.Remove(key);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawAddNewKeySection()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Add New PlayerPref", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Key:", GUILayout.Width(50));
        newKeyName = EditorGUILayout.TextField(newKeyName);
        GUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Type:", GUILayout.Width(50));
        string[] types = new string[] { "Int", "Float", "String" };
        newValueType = EditorGUILayout.Popup(newValueType, types);
        GUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Value:", GUILayout.Width(50));
        newValueString = EditorGUILayout.TextField(newValueString);
        GUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        bool canAdd = !string.IsNullOrEmpty(newKeyName);
        EditorGUI.BeginDisabledGroup(!canAdd);
        
        if (GUILayout.Button("Add PlayerPref", GUILayout.Width(120)))
        {
            AddNewPlayerPref();
        }
        
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
    }

    private void AddNewPlayerPref()
    {
        try
        {
            switch (newValueType)
            {
                case 0: // Int
                    int intValue = int.Parse(newValueString);
                    PlayerPrefs.SetInt(newKeyName, intValue);
                    break;
                case 1: // Float
                    float floatValue = float.Parse(newValueString);
                    PlayerPrefs.SetFloat(newKeyName, floatValue);
                    break;
                case 2: // String
                    PlayerPrefs.SetString(newKeyName, newValueString);
                    break;
            }
            
            PlayerPrefs.Save();
            RefreshPlayerPrefs();
            
            // Clear fields
            newKeyName = "";
            newValueString = "";
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error Adding PlayerPref", 
                $"Failed to add PlayerPref: {e.Message}", "OK");
        }
    }

    private void DrawBottomButtons()
    {
        bool hasChanges = editedValues.Count > 0 || keysToDelete.Count > 0;
        
        EditorGUI.BeginDisabledGroup(!hasChanges);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Discard Changes", GUILayout.Width(120)))
        {
            editedValues.Clear();
            keysToDelete.Clear();
        }
        
        if (GUILayout.Button("Save Changes", GUILayout.Width(120)))
        {
            SaveChanges();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.EndDisabledGroup();
        
        if (hasChanges)
        {
            int totalChanges = editedValues.Count + keysToDelete.Count;
            EditorGUILayout.HelpBox($"You have {totalChanges} unsaved changes.", MessageType.Info);
        }
    }

    private void SaveChanges()
    {
        // Apply edits
        foreach (var kvp in editedValues)
        {
            string key = kvp.Key;
            object value = kvp.Value;
            
            if (value is int intValue)
            {
                PlayerPrefs.SetInt(key, intValue);
            }
            else if (value is float floatValue)
            {
                PlayerPrefs.SetFloat(key, floatValue);
            }
            else if (value is string stringValue)
            {
                PlayerPrefs.SetString(key, stringValue);
            }
        }
        
        // Apply deletions
        foreach (var key in keysToDelete)
        {
            PlayerPrefs.DeleteKey(key);
        }
        
        PlayerPrefs.Save();
        editedValues.Clear();
        keysToDelete.Clear();
        RefreshPlayerPrefs();
    }

    private void RefreshPlayerPrefs()
    {
        prefsDict.Clear();
        prefsTypes.Clear();
        
        // Since Unity doesn't provide an API to enumerate all PlayerPrefs keys,
        // we'll use the CreatePlayerPrefsEditor technique to try to find PlayerPrefs
        
        string[] knownKeys = FindPlayerPrefsKeys();
        
        foreach (string key in knownKeys)
        {
            // Check if it's int first
            int intValue = PlayerPrefs.GetInt(key, int.MinValue);
            if (intValue != int.MinValue)
            {
                prefsDict[key] = intValue;
                prefsTypes[key] = typeof(int);
                continue;
            }
            
            // Check if it's float
            float floatValue = PlayerPrefs.GetFloat(key, float.MinValue);
            if (!Mathf.Approximately(floatValue, float.MinValue))
            {
                prefsDict[key] = floatValue;
                prefsTypes[key] = typeof(float);
                continue;
            }
            
            // Assume it's string
            string stringValue = PlayerPrefs.GetString(key, "");
            prefsDict[key] = stringValue;
            prefsTypes[key] = typeof(string);
        }
        
        Repaint();
    }

    private string[] FindPlayerPrefsKeys()
    {
        List<string> keys = new List<string>();
        
        // This is a workaround since Unity doesn't provide a method to enumerate PlayerPrefs keys.
        // On Windows, we can read from the registry directly
        #if UNITY_EDITOR_WIN
        try
        {
            string companyName = PlayerSettings.companyName;
            string productName = PlayerSettings.productName;
            string registryPath = $"Software\\Unity\\UnityEditor\\{companyName}\\{productName}";
            
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryPath);
            if (key != null)
            {
                string[] valueNames = key.GetValueNames();
                foreach (string valueName in valueNames)
                {
                    string actualKey = valueName;
                    // Unity's registry values are prefixed with types
                    if (valueName.StartsWith("unity."))
                    {
                        if (valueName.StartsWith("unity.int"))
                            actualKey = valueName.Substring(9);
                        else if (valueName.StartsWith("unity.float"))
                            actualKey = valueName.Substring(11);
                        else if (valueName.StartsWith("unity.string"))
                            actualKey = valueName.Substring(13);
                    }
                    
                    int lastUnderscoreIndex = actualKey.LastIndexOf('_');
                    if (lastUnderscoreIndex != -1)
                    {
                        actualKey = actualKey.Substring(0, lastUnderscoreIndex);
                    }
                    
                    if (!string.IsNullOrEmpty(actualKey) && !keys.Contains(actualKey))
                    {
                        keys.Add(actualKey);
                    }
                }
                key.Close();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading PlayerPrefs from registry: {e.Message}");
        }
        #elif UNITY_EDITOR_OSX
            string plistPath = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}/Library/Preferences/unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist";
            keys = ParsePlistKeys(plistPath);

        #elif UNITY_EDITOR_LINUX
        try
        {
            // On Linux, PlayerPrefs are stored in the home directory
            string prefsPath = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}/.config/unity3d/{PlayerSettings.companyName}/{PlayerSettings.productName}/prefs";
            
            if (File.Exists(prefsPath))
            {
                // Try to parse the prefs file to extract keys
                string[] lines = File.ReadAllLines(prefsPath);
                foreach (string line in lines)
                {
                    if (line.Contains(":"))
                    {
                        string key = line.Split(':')[0].Trim();
                        if (!string.IsNullOrEmpty(key) && !keys.Contains(key))
                        {
                            keys.Add(key);
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading PlayerPrefs from file: {e.Message}");
        }
        #endif
        
        
        return keys.ToArray();
    }
    
    private List<string> ParsePlistKeys(string path)
    {
        var keys = new List<string>();

        if (!System.IO.File.Exists(path)) return keys;

        try
        {
            // Convert binary plist to XML
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unity_prefs.plist");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/usr/bin/plutil";
            process.StartInfo.Arguments = $"-convert xml1 \"{path}\" -o \"{tempPath}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            // Read the converted XML plist
            if (System.IO.File.Exists(tempPath))
            {
                string plistContent = System.IO.File.ReadAllText(tempPath);
                var matches = System.Text.RegularExpressions.Regex.Matches(plistContent, "<key>(.*?)</key>");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    keys.Add(match.Groups[1].Value);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error reading PlayerPrefs: " + ex.Message);
        }

        return keys;
    }
}