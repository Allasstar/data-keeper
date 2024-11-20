using System.IO;
using UnityEngine;

namespace DataKeeper.Helpers
{
    public class FolderHelper
    {
        public static void CreateFolders(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                string[] folders = directoryPath.Split(Path.DirectorySeparatorChar);

                string currentPath = "";

                foreach (string folder in folders)
                {
                    currentPath = Path.Combine(currentPath, folder);

                    if (!Directory.Exists(currentPath))
                    {
                        Directory.CreateDirectory(currentPath);
                        Debug.Log("Created folder: " + currentPath);
                    }
                }
            }
        }

        public static bool AllFoldersExist(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                string[] folders = directoryPath.Split(Path.DirectorySeparatorChar);

                string currentPath = "";

                foreach (string folder in folders)
                {
                    currentPath = Path.Combine(currentPath, folder);

                    if (!Directory.Exists(currentPath))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
