using DataKeeper.Editor.Windows.AssetCommander;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.MenuItems
{
    // Was a console dump: one warning per broken object, in a log the user then had to scroll.
    // Asset Commander answers the same question as a selectable, pingable list, so the menu item
    // stays and points there instead.
    public static class MissingScriptFinder
    {
        [MenuItem("Tools/Find Missing Scripts in Scene", priority = 1)]
        public static void FindMissingScripts()
        {
            var scene = SceneManager.GetActiveScene();

            // An unsaved scene has no asset path for a side to point at, so the window falls
            // back to the same question asked of the project's assets.
            var root = string.IsNullOrEmpty(scene.path) ? null : scene.path;

            AssetCommanderWindow.Show(CommanderModes.MissingScriptsId, root);
        }
    }
}
