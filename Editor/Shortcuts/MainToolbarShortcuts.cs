using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace DataKeeper.Editor.Shortcuts
{
    public static class MainToolbarShortcuts
    {
        [MainToolbarElement("Data Keeper/Save Project", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateSaveProjectButton()
        {
            var icon = EditorGUIUtility.IconContent("SaveAs").image as Texture2D;
            return new MainToolbarButton(new MainToolbarContent(icon, "Save all modified assets and dirty scenes"), SaveProject);
        }

        [MainToolbarElement("Data Keeper/Reload Domain", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateReloadDomainButton()
        {
            var icon = EditorGUIUtility.IconContent("Refresh").image as Texture2D;
            return new MainToolbarButton(new MainToolbarContent(icon, "Force a script domain reload"), ReloadDomain);
        }

        public static void SaveProject()
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Project saved.");
        }

        public static void ReloadDomain()
        {
            EditorUtility.RequestScriptReload();
            Debug.Log("Domain reload requested.");
        }
    }
}
