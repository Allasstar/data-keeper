using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DataKeeper.Editor.MenuItems
{
    public class MissingScriptFinder : EditorWindow
    {
        [MenuItem("Tools/Find Missing Scripts in Scene", priority = 1)]
        public static void FindMissingScripts()
        {
            // Find all GameObjects in the scene
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> objectsWithMissingScripts = new List<GameObject>();

            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();
                
                foreach (Component component in components)
                {
                    // Check if the component is null, which indicates a missing script
                    if (component == null)
                    {
                        objectsWithMissingScripts.Add(go);
                        Debug.LogWarning($"Missing script found on GameObject: {go.name}", go);
                        break;
                    }
                }
            }

            // Optional: Display results in console
            if (objectsWithMissingScripts.Count > 0)
            {
                Debug.Log($"!!! Found {objectsWithMissingScripts.Count} GameObjects with missing scripts.");

                foreach (var objectsWithMissingScript in objectsWithMissingScripts)
                {
                    Debug.Log($"Found {objectsWithMissingScript.gameObject.name} GameObjects with missing scripts.", objectsWithMissingScript);
                }
            }
            else
            {
                Debug.Log("No missing scripts found in the scene.");
            }
        }
    }
}
