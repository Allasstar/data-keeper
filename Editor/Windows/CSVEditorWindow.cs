using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows
{
    public class CSVEditorWindow : EditorWindow
    {
        private string csvData;
        private List<List<string>> tableData = new List<List<string>>();
        private Vector2 scrollPosition;
        private Action<string> onSaveCallback;

        private string[] boolOptions = new string[] { "False", "True" };

        private int columnWeight = 150;

        public static void OpenWindow(string csv, Action<string> saveCallback)
        {
            CSVEditorWindow window = GetWindow<CSVEditorWindow>();
            window.titleContent = new GUIContent("CSV Editor");
            window.csvData = csv;
            window.onSaveCallback = saveCallback;
            window.ParseCSV();
            window.Show();
        }

        private void ParseCSV()
        {
            tableData.Clear();

            if (string.IsNullOrEmpty(csvData))
                return;

            string[] lines = csvData.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                List<string> row = new List<string>();
                bool inQuotes = false;
                string current = "";

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        row.Add(current);
                        current = "";
                    }
                    else
                    {
                        current += c;
                    }
                }

                row.Add(current); // Add the last cell
                tableData.Add(row);
            }
        }

        private string GenerateCSV()
        {
            List<string> lines = new List<string>();

            foreach (List<string> row in tableData)
            {
                List<string> cells = new List<string>();

                foreach (string cell in row)
                {
                    // Escape quotes and wrap in quotes if needed
                    string escaped = cell.Replace("\"", "\"\"");
                    if (cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n"))
                    {
                        escaped = "\"" + escaped + "\"";
                    }

                    cells.Add(escaped);
                }

                lines.Add(string.Join(",", cells));
            }

            return string.Join("\n", lines);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add Row", style: EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (tableData.Count > 0)
                {
                    int columns = tableData[0].Count;
                    List<string> newRow = Enumerable.Repeat("", columns).ToList();
                    tableData.Add(newRow);
                }
                else
                {
                    tableData.Add(new List<string> {""});
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Column Width", EditorStyles.toolbarButton);
            columnWeight = EditorGUILayout.IntSlider(columnWeight, 50, 300, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save", style: EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                csvData = GenerateCSV();
                onSaveCallback?.Invoke(csvData);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Draw table
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (tableData.Count > 0)
            {
                for (int row = 0; row < tableData.Count; row++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (row == 0)
                    {
                        GUILayout.Label("", GUILayout.Width(20));
                    }
                    else
                    {
                        if (GUILayout.Button("x", GUILayout.Width(20)))
                        {
                            tableData.RemoveAt(row);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }

                    for (int column = 0; column < tableData[row].Count; column++)
                    {
                        if (row == 0)
                        {
                            tableData[row][column] =
                                EditorGUILayout.TextField(tableData[row][column], GUILayout.Width(columnWeight));
                        }
                        else
                        {
                            var headerString = tableData[0][column];

                            var type = CSVUtility.GetTypeFromHeader(headerString);

                            if (typeof(Object).IsAssignableFrom(type))
                            {
                                // Convert GUID to Object and display object field
                                Object unityObj = null;

                                // If we have a value, try to convert from GUID to Object
                                if (!string.IsNullOrEmpty(tableData[row][column]))
                                {
                                    // Check if it's a sprite with texture reference
                                    if (type == typeof(Sprite) && tableData[row][column].Contains("|"))
                                    {
                                        string[] parts = tableData[row][column].Split('|');
                                        if (parts.Length == 2)
                                        {
                                            unityObj = CSVUtility.ResolveSprite(parts[0], parts[1]);
                                        }
                                    }
                                    else
                                    {
                                        unityObj = CSVUtility.GUIDToUnityObject(tableData[row][column], type);
                                    }
                                }

                                // Display object field and handle changes
                                EditorGUI.BeginChangeCheck();
                                Object newObj = EditorGUILayout.ObjectField(unityObj, type, false, GUILayout.Width(columnWeight));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (newObj == null)
                                    {
                                        tableData[row][column] = string.Empty;
                                    }
                                    else if (newObj is Sprite sprite)
                                    {
                                        // Special handling for sprites to store texture GUID and sprite name
                                        string textureGuid = CSVUtility.UnityObjectToGUID(sprite.texture);
                                        tableData[row][column] = $"{textureGuid}|{sprite.name}";
                                    }
                                    else
                                    {
                                        tableData[row][column] = CSVUtility.UnityObjectToGUID(newObj);
                                    }
                                }
                            }
                            else
                            {
                                // Handle basic types with appropriate editor fields
                                if (type == typeof(int) || type == typeof(int?))
                                {
                                    int value = 0;
                                    int.TryParse(tableData[row][column], out value);
                                    int newValue = EditorGUILayout.IntField(value, GUILayout.Width(columnWeight));
                                    if (newValue != value)
                                    {
                                        tableData[row][column] = newValue.ToString();
                                    }
                                }
                                else if (type == typeof(float) || type == typeof(float?))
                                {
                                    float value = 0f;
                                    float.TryParse(tableData[row][column], out value);
                                    float newValue = EditorGUILayout.FloatField(value, GUILayout.Width(columnWeight));
                                    if (newValue != value)
                                    {
                                        tableData[row][column] = newValue.ToString();
                                    }
                                }
                                else if (type == typeof(bool) || type == typeof(bool?))
                                {
                                    bool value = false;
                                    bool.TryParse(tableData[row][column], out value);
                                    
                                    int index = value ? 1 : 0;
    
                                    // Show dropdown and get new selected index
                                    int newIndex = EditorGUILayout.Popup(index, boolOptions, GUILayout.Width(columnWeight));
    
                                    // If selection changed, update the value
                                    if (newIndex != index)
                                    {
                                        value = newIndex == 1;
                                        tableData[row][column] = value.ToString();
                                    }
                                }
                                else if (type == typeof(Vector2))
                                {
                                    Vector2 value = Vector2.zero;
                                    string[] parts = tableData[row][column].Split(';');
                                    if (parts.Length == 2)
                                    {
                                        float.TryParse(parts[0], out value.x);
                                        float.TryParse(parts[1], out value.y);
                                    }

                                    Vector2 newValue = EditorGUILayout.Vector2Field("", value, GUILayout.Width(columnWeight));
                                    if (newValue != value)
                                    {
                                        tableData[row][column] = $"{newValue.x};{newValue.y}";
                                    }
                                }
                                else if (type == typeof(Vector3))
                                {
                                    Vector3 value = Vector3.zero;
                                    string[] parts = tableData[row][column].Split(';');
                                    if (parts.Length == 3)
                                    {
                                        float.TryParse(parts[0], out value.x);
                                        float.TryParse(parts[1], out value.y);
                                        float.TryParse(parts[2], out value.z);
                                    }

                                    Vector3 newValue = EditorGUILayout.Vector3Field("", value, GUILayout.Width(columnWeight));
                                    if (newValue != value)
                                    {
                                        tableData[row][column] = $"{newValue.x};{newValue.y};{newValue.z}";
                                    }
                                }
                                else if (type == typeof(Color))
                                {
                                    Color value = Color.white;
                                    string[] parts = tableData[row][column].Split(';');
                                    if (parts.Length == 4)
                                    {
                                        float.TryParse(parts[0], out value.r);
                                        float.TryParse(parts[1], out value.g);
                                        float.TryParse(parts[2], out value.b);
                                        float.TryParse(parts[3], out value.a);
                                    }

                                    Color newValue = EditorGUILayout.ColorField(value, GUILayout.Width(columnWeight));
                                    if (newValue != value)
                                    {
                                        tableData[row][column] = $"{newValue.r};{newValue.g};{newValue.b};{newValue.a}";
                                    }
                                }
                                else
                                {
                                    // Default to text field for other types
                                    tableData[row][column] = EditorGUILayout.TextField(tableData[row][column], GUILayout.MinWidth(100));
                                }
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}