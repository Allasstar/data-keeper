#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.GameTagSystem
{
    /// <summary>
    /// Removes the dead <c>GameTagDatabase</c> asset left in ProjectSettings by an earlier
    /// tag-system experiment — its script no longer exists, so the asset is an unreferenced orphan.
    /// </summary>
    public static class GameTagMigration
    {
        private const string OrphanPath = "ProjectSettings/GameTagDatabase.asset";

        [MenuItem("Tools/DataKeeper/Game Tags/Remove Orphaned GameTagDatabase")]
        public static void RemoveOrphan()
        {
            if (!File.Exists(OrphanPath))
            {
                Debug.Log("[GameTag] No orphaned GameTagDatabase.asset found.");
                return;
            }

            File.Delete(OrphanPath);
            if (File.Exists(OrphanPath + ".meta")) File.Delete(OrphanPath + ".meta");
            AssetDatabase.Refresh();
            Debug.Log("[GameTag] Removed orphaned ProjectSettings/GameTagDatabase.asset (dead script).");
        }
    }
}
#endif
