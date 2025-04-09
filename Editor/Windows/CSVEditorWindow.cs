using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
            GUILayout.Label("CSV Table Editor", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Row"))
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
            
            if (GUILayout.Button("Save"))
            {
                csvData = GenerateCSV();
                onSaveCallback?.Invoke(csvData);
            }
            
            EditorGUILayout.Space();
            
            // Draw table
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (tableData.Count > 0)
            {
                for (int i = 0; i < tableData.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        tableData.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    
                    for (int j = 0; j < tableData[i].Count; j++)
                    {
                        tableData[i][j] = EditorGUILayout.TextField(tableData[i][j], GUILayout.MinWidth(100));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}
