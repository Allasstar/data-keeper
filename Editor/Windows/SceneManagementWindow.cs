using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.Windows
{
   
    public class SceneManagementWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        [MenuItem("Tools/Scene Management Window")]
        public static void ShowWindow()
        {
            GetWindow<SceneManagementWindow>("Scene Manager");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
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
                
                    // Index
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(20));
                
                    // Scene name in bold
                    EditorGUILayout.LabelField(new GUIContent(
                        sceneName, 
                        "Scene path: " + scenePath
                    ), EditorStyles.boldLabel, GUILayout.Width(120));

                    // Scene status
                    bool isLoaded = SceneManager.GetSceneByPath(scenePath).isLoaded;

                    // Action buttons
                    GUI.enabled = !isLoaded;
                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        SceneManagement.LoadScene(scenePath);
                    }

                    if (GUILayout.Button("Add", GUILayout.Width(45)))
                    {
                        SceneManagement.LoadSceneAdditive(scenePath);
                    }

                    // Disable Unload if only one scene is loaded
                    GUI.enabled = isLoaded && loadedSceneCount > 1;
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        SceneManagement.UnloadScene(scenePath);
                    }

                    EditorGUILayout.LabelField(
                        isLoaded ? "Loaded" : "",
                        GUILayout.Width(80)
                    );
                
                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndScrollView();
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

                // Prompt user to save multiple dirty scenes
                bool saveAll = EditorUtility.DisplayDialog(
                    "Unsaved Changes", 
                    $"The following scenes have unsaved changes:\n\n{string.Join("\n", dirtyScenesNames)}\n\nDo you want to save all changed scenes?", 
                    "Save All and Load",
                    "Cancel Load"
                );

                if (saveAll)
                {
                    // Save all dirty scenes
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.isDirty)
                        {
                            EditorSceneManager.SaveScene(scene);
                        }
                    }
                }

                return saveAll;
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