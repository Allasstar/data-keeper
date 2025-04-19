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

            string[] lines = csvData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
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
                    tableData.Add(new List<string> { "" });
                }
            }

            EditorGUILayout.Space(10, true);

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
                        if(row == 0)
                        {
                            tableData[row][column] = EditorGUILayout.TextField(tableData[row][column], GUILayout.MinWidth(100));
                        }
                        else
                        {
                            var headerString = tableData[0][column];
                            
                            var type = CSVUtility.GetTypeFromHeader(headerString);
                            if (typeof(Object).IsAssignableFrom(type))
                            {
                                // TODO: Convert GUID to Object and back
                                GUILayout.Label(tableData[row][column], GUILayout.MinWidth(100));
                            }
                            else
                            {
                                tableData[row][column] = EditorGUILayout.TextField(tableData[row][column], GUILayout.MinWidth(100));
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
