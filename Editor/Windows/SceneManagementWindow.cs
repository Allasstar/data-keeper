using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class SceneManagementWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private float timeScale = 1f;

        [MenuItem("Tools/Windows/Scenes", priority = 0)]
        public static void ShowWindow()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_Scene");
        
            var window = GetWindow<SceneManagementWindow>();
            window.minSize = new Vector2(250, 300);
            window.titleContent = new GUIContent("Scenes", icon);
        }

        private void OnGUI()
        {
            RenderTimeScaleControls();
            EditorGUILayout.Space();
            RenderSceneList();
        }

        private void RenderTimeScaleControls()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
    
            GUILayout.Label("TimeScale", GUILayout.Width(65));
            
            timeScale = EditorGUILayout.Slider(timeScale, 0f, 10f, GUILayout.ExpandWidth(true));
    
            if (GUILayout.Button("Apply", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                Time.timeScale = timeScale;
            }
    
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                GUI.FocusControl(null);
                timeScale = 1f;
                Time.timeScale = 1f;
                Repaint();
            }
    
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                GUI.FocusControl(null);
                timeScale = Time.timeScale;
                Repaint();
            }
    
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void RenderSceneList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Get scenes from build settings
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int loadedSceneCount = SceneManager.sceneCount;

            if (scenes.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes found in Build Settings", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    EditorBuildSettingsScene scene = scenes[i];
                    string scenePath = scene.path;
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                    EditorGUILayout.BeginHorizontal();
                
                    // Scene status
                    Scene sceneObject = SceneManager.GetSceneByPath(scenePath);
                    bool isLoaded = sceneObject.isLoaded;

                    // Action buttons
                    GUI.enabled = !isLoaded;
                    if (GUILayout.Button(new GUIContent(sceneName, $"Load Scene: {scenePath}"), GUILayout.Height(25)))
                    {
                        SceneManagement.LoadScene(scenePath);
                    }

                    if (GUILayout.Button("Add", GUILayout.Width(45), GUILayout.Height(25)))
                    {
                        SceneManagement.LoadSceneAdditive(scenePath);
                    }

                    // Disable Unload if only one scene is loaded
                    GUI.enabled = isLoaded && loadedSceneCount > 1;
                    if (GUILayout.Button("Unload", GUILayout.Width(60), GUILayout.Height(25)))
                    {
                        SceneManagement.UnloadScene(scenePath);
                    }

                    var labelBuilder = new System.Text.StringBuilder();
                    if (sceneObject.isDirty)
                        labelBuilder.Append("*");
                    else 
                        labelBuilder.Append(" ");
                    
                    if (isLoaded) labelBuilder.Append("Loaded");
                    var label = labelBuilder.ToString();

                    EditorGUILayout.LabelField(label, GUILayout.Width(55));
                
                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                    DrawHorizontalLine();
                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHorizontalLine(float height = 1f)
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), Color.gray);
        }

        // Helper class to manage scene loading with error handling
        private static class SceneManagement
        {
            private static bool SaveDirtyScenesPrompt()
            {
                // Get all open scenes
                var dirtyScenesNames = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty)
                    {
                        dirtyScenesNames.Add(scene.name);
                    }
                }

                // If no dirty scenes, return true
                if (dirtyScenesNames.Count == 0)
                    return true;

                // Prompt user with options: Save All, Discard All, Cancel
                int option = EditorUtility.DisplayDialogComplex(
                    "Unsaved Changes",
                    $"The following scenes have unsaved changes:\n\n{string.Join("\n", dirtyScenesNames)}\n\nYour changes will be lost if you don't save them.\n\nWhat would you like to do?",
                    "Save All",
                    "Discard All",
                    "Cancel"
                );

                switch (option)
                {
                    case 0: // Save All
                        for (int i = 0; i < SceneManager.sceneCount; i++)
                        {
                            Scene scene = SceneManager.GetSceneAt(i);
                            if (scene.isDirty)
                            {
                                EditorSceneManager.SaveScene(scene);
                            }
                        }
                        return true;

                    case 1: // Discard All
                        return true;

                    case 2: // Cancel
                        return false;

                    default:
                        return false;
                }
            }


            public static void LoadScene(string scenePath)
            {
                try
                {
                    // Prompt to save dirty scenes
                    if (SaveDirtyScenesPrompt())
                    {
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        Debug.Log($"Scene loaded: {scenePath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load scene {scenePath}: {e.Message}");
                    EditorUtility.DisplayDialog(
                        "Scene Loading Error", 
                        $"Could not load scene:\n{scenePath}\n\nError: {e.Message}", 
                        "OK"
                    );
                }
            }

            public static void LoadSceneAdditive(string scenePath)
            {
                try
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    Debug.Log($"Scene loaded additively: {scenePath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load scene additively {scenePath}: {e.Message}");
                    EditorUtility.DisplayDialog(
                        "Scene Loading Error", 
                        $"Could not load scene additively:\n{scenePath}\n\nError: {e.Message}", 
                        "OK"
                    );
                }
            }

            public static void UnloadScene(string scenePath)
            {
                try
                {
                    Scene scene = SceneManager.GetSceneByPath(scenePath);
                    if (scene.isLoaded)
                    {
                        // Check for unsaved changes
                        if (scene.isDirty)
                        {
                            // Prompt user to save
                            bool saveScene = EditorUtility.DisplayDialog(
                                "Unsaved Changes", 
                                $"Scene '{scene.name}' has unsaved changes. Do you want to save before unloading?", 
                                "Save", 
                                "Discard Changes"
                            );

                            if (saveScene)
                            {
                                // Save the scene
                                EditorSceneManager.SaveScene(scene);
                            }
                        }

                        // Unload the scene
                        EditorSceneManager.CloseScene(scene, true);
                        Debug.Log($"Scene unloaded: {scenePath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to unload scene {scenePath}: {e.Message}");
                    EditorUtility.DisplayDialog(
                        "Scene Unloading Error", 
                        $"Could not unload scene:\n{scenePath}\n\nError: {e.Message}", 
                        "OK"
                    );
                }
            }
        }
    }
}