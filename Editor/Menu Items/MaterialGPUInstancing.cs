using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.MenuItems
{
    public class MaterialGPUInstancing
    {
        [MenuItem("Tools/Materials GPU Instancing/Enable", priority = 10)]
        static void EnableGPUInstancing()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int changedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null && mat.enableInstancing == false)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    changedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Enabled GPU Instancing on {changedCount} materials.");
        }
        
        [MenuItem("Tools/Materials GPU Instancing/Disable")]
        static void DisableGPUInstancing()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int changedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null && mat.enableInstancing)
                {
                    mat.enableInstancing = false;
                    EditorUtility.SetDirty(mat);
                    changedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Disable GPU Instancing on {changedCount} materials.");
        }
    }
}
