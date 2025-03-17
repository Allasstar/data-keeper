using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DataKeeper.Generic;
using DataKeeper.ServiceLocatorPattern;

namespace DataKeeper.Editor
{
    public class ServiceLocatorInspector : EditorWindow
    {
        private enum InspectionMode
        {
            Global = 0,
            Scene = 1,
            GameObject = 2,
            Table = 3,
        }

        private InspectionMode _currentMode = InspectionMode.Global;
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private bool _showNullValues = false;
        private Dictionary<string, bool> _tableFoldouts = new Dictionary<string, bool>();
        private Dictionary<string, bool> _sceneFoldouts = new Dictionary<string, bool>();
        private Dictionary<GameObject, bool> _gameObjectFoldouts = new Dictionary<GameObject, bool>();

        // Table column widths
        private float _keyColumnWidth = 200f;
        private float _valueColumnWidth = 200f;
        private float _typeColumnWidth = 150f;


        [MenuItem("Tools/Windows/Service Locator Inspector", priority = 3)]
        public static void ShowWindow()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_UnityEditor.DebugInspectorWindow");

            var window = GetWindow<ServiceLocatorInspector>();
            window.minSize = new Vector2(400, 300);
            window.titleContent = new GUIContent("Service Locator", icon);
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            
            DrawToolbar();
            DrawSearchBar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_currentMode)
            {
                case InspectionMode.Global:
                    DrawGlobalRegister();
                    break;
                case InspectionMode.Scene:
                    DrawSceneRegisters();
                    break;
                case InspectionMode.GameObject:
                    DrawGameObjectRegisters();
                    break;
                case InspectionMode.Table:
                    DrawTableRegisters();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Toggle(_currentMode == InspectionMode.Global, "Global", EditorStyles.toolbarButton))
                _currentMode = InspectionMode.Global;
            
            if (GUILayout.Toggle(_currentMode == InspectionMode.Scene, "Scene", EditorStyles.toolbarButton))
                _currentMode = InspectionMode.Scene;
            
            if (GUILayout.Toggle(_currentMode == InspectionMode.GameObject, "GameObject", EditorStyles.toolbarButton))
                _currentMode = InspectionMode.GameObject;
            
            if (GUILayout.Toggle(_currentMode == InspectionMode.Table, "Tables", EditorStyles.toolbarButton))
                _currentMode = InspectionMode.Table;
            
            GUILayout.FlexibleSpace();
            
            _showNullValues = GUILayout.Toggle(_showNullValues, "Show Nulls", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.Space();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space();
        }

        private void DrawGlobalRegister()
        {
            EditorGUILayout.LabelField("Global Register", EditorStyles.boldLabel);
            var register = ServiceLocator.ForGlobal();
            DrawRegisterTable(register);
        }

        private void DrawSceneRegisters()
        {
            // Use reflection to access the SceneRegisters dictionary
            var sceneRegistersField = typeof(ServiceLocator).GetField("SceneRegisters", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (sceneRegistersField == null)
            {
                EditorGUILayout.HelpBox("Could not access SceneRegisters via reflection", MessageType.Error);
                return;
            }
            
            var sceneRegisters = sceneRegistersField.GetValue(null) as Dictionary<string, Register<object>>;
            
            if (sceneRegisters == null || sceneRegisters.Count == 0)
            {
                EditorGUILayout.HelpBox("No scenes have registered objects", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Scene Registers", EditorStyles.boldLabel);
            
            foreach (var kvp in sceneRegisters)
            {
                string sceneName = kvp.Key;
                Register<object> register = kvp.Value;
                
                // Skip if doesn't match search filter when searching
                if (!string.IsNullOrEmpty(_searchFilter) && !sceneName.Contains(_searchFilter))
                {
                    bool hasMatchingItem = false;
                    foreach (var item in register.All)
                    {
                        if (item.Key.Contains(_searchFilter) || 
                            (item.Value != null && item.Value.GetType().Name.Contains(_searchFilter)))
                        {
                            hasMatchingItem = true;
                            break;
                        }
                    }
                    
                    if (!hasMatchingItem)
                        continue;
                }
                
                // Ensure the scene has a foldout state
                if (!_sceneFoldouts.ContainsKey(sceneName))
                    _sceneFoldouts[sceneName] = false;
                
                _sceneFoldouts[sceneName] = EditorGUILayout.Foldout(_sceneFoldouts[sceneName], $"Scene: {sceneName}", true);
                
                if (_sceneFoldouts[sceneName])
                {
                    EditorGUI.indentLevel++;
                    DrawRegisterTable(register);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawTableRegisters()
        {
            // Use reflection to access the TableRegisters dictionary
            var tableRegistersField = typeof(ServiceLocator).GetField("TableRegisters", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (tableRegistersField == null)
            {
                EditorGUILayout.HelpBox("Could not access TableRegisters via reflection", MessageType.Error);
                return;
            }
            
            var tableRegisters = tableRegistersField.GetValue(null) as Dictionary<string, Register<object>>;
            
            if (tableRegisters == null || tableRegisters.Count == 0)
            {
                EditorGUILayout.HelpBox("No tables registered", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Table Registers", EditorStyles.boldLabel);
            
            foreach (var kvp in tableRegisters)
            {
                string tableName = kvp.Key;
                Register<object> register = kvp.Value;
                
                // Skip if doesn't match search filter
                if (!string.IsNullOrEmpty(_searchFilter) && !tableName.Contains(_searchFilter))
                {
                    bool hasMatchingItem = false;
                    foreach (var item in register.All)
                    {
                        if (item.Key.Contains(_searchFilter) || 
                            (item.Value != null && item.Value.GetType().Name.Contains(_searchFilter)))
                        {
                            hasMatchingItem = true;
                            break;
                        }
                    }
                    
                    if (!hasMatchingItem)
                        continue;
                }
                
                // Ensure the table has a foldout state
                if (!_tableFoldouts.ContainsKey(tableName))
                    _tableFoldouts[tableName] = false;
                
                _tableFoldouts[tableName] = EditorGUILayout.Foldout(_tableFoldouts[tableName], $"Table: {tableName}", true);
                
                if (_tableFoldouts[tableName])
                {
                    EditorGUI.indentLevel++;
                    DrawRegisterTable(register);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawGameObjectRegisters()
        {
            // Use reflection to access the GameObjectRegisters dictionary
            var gameObjectRegistersField = typeof(ServiceLocator).GetField("GameObjectRegisters", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            if (gameObjectRegistersField == null)
            {
                EditorGUILayout.HelpBox("Could not access GameObjectRegisters via reflection", MessageType.Error);
                return;
            }
            
            var gameObjectRegisters = gameObjectRegistersField.GetValue(null) as Dictionary<GameObject, Register<object>>;
            
            if (gameObjectRegisters == null || gameObjectRegisters.Count == 0)
            {
                EditorGUILayout.HelpBox("No GameObjects have registered objects", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("GameObject Registers", EditorStyles.boldLabel);
            
            // Make a copy to avoid collection modified issues
            var gameObjects = new List<GameObject>(gameObjectRegisters.Keys);
            
            foreach (var gameObject in gameObjects)
            {
                if (gameObject == null) continue;
                
                Register<object> register = gameObjectRegisters[gameObject];
                string gameObjectName = gameObject.name;
                
                // Skip if doesn't match search filter
                if (!string.IsNullOrEmpty(_searchFilter) && !gameObjectName.Contains(_searchFilter))
                {
                    bool hasMatchingItem = false;
                    foreach (var item in register.All)
                    {
                        if (item.Key.Contains(_searchFilter) || 
                            (item.Value != null && item.Value.GetType().Name.Contains(_searchFilter)))
                        {
                            hasMatchingItem = true;
                            break;
                        }
                    }
                    
                    if (!hasMatchingItem)
                        continue;
                }
                
                // Ensure the GameObject has a foldout state
                if (!_gameObjectFoldouts.ContainsKey(gameObject))
                    _gameObjectFoldouts[gameObject] = false;
                
                EditorGUILayout.BeginHorizontal();
                
                _gameObjectFoldouts[gameObject] = EditorGUILayout.Foldout(_gameObjectFoldouts[gameObject], 
                    $"GameObject: {gameObjectName}", true);
                
                // Add an object field to highlight the GameObject
                EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true, GUILayout.Width(100));
                
                EditorGUILayout.EndHorizontal();
                
                if (_gameObjectFoldouts[gameObject])
                {
                    EditorGUI.indentLevel++;
                    DrawRegisterTable(register);
                    EditorGUI.indentLevel--;
                }
            }
            
            // Clean up references to destroyed GameObjects
            CleanupGameObjectFoldouts();
        }
        
        private void CleanupGameObjectFoldouts()
        {
            var keysToRemove = new List<GameObject>();
            foreach (var kvp in _gameObjectFoldouts)
            {
                if (kvp.Key == null)
                    keysToRemove.Add(kvp.Key);
            }
            
            foreach (var key in keysToRemove)
            {
                _gameObjectFoldouts.Remove(key);
            }
        }

        private void DrawRegisterTable(Register<object> register)
        {
            if (register == null || register.Count == 0)
            {
                EditorGUILayout.LabelField("No objects registered");
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Table header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(_keyColumnWidth));
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.Width(_valueColumnWidth));
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(_typeColumnWidth));
            EditorGUILayout.EndHorizontal();
            
            // Table rows
            bool isAlternate = false;
            foreach (var entry in register.All)
            {
                if (entry.Value == null && !_showNullValues)
                    continue;

                // Apply search filter
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !entry.Key.Contains(_searchFilter) &&
                    !(entry.Value?.GetType().Name.Contains(_searchFilter) ?? false))
                    continue;

                // Alternate row colors
                if (isAlternate)
                    GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
                else
                    GUI.backgroundColor = Color.white;
                
                EditorGUILayout.BeginHorizontal();
                
                // Key
                EditorGUILayout.LabelField(entry.Key, GUILayout.Width(_keyColumnWidth));
                
                // Value
                if (entry.Value == null)
                {
                    EditorGUILayout.LabelField("null", GUILayout.Width(_valueColumnWidth));
                }
                else
                {
                    var unityObject = entry.Value as UnityEngine.Object;
                    if (unityObject != null)
                    {
                        EditorGUILayout.ObjectField(unityObject, unityObject.GetType(), true, GUILayout.Width(_valueColumnWidth));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(entry.Value.ToString(), GUILayout.Width(_valueColumnWidth));
                    }
                }
                
                // Type
                string typeName = entry.Value?.GetType().Name ?? "null";
                EditorGUILayout.LabelField(typeName, GUILayout.Width(_typeColumnWidth));
                
                EditorGUILayout.EndHorizontal();
                
                isAlternate = !isAlternate;
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
    }
}