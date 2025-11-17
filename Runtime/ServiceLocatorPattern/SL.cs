using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    /// <summary>
    /// Provides shorthand methods for accessing various service locator registers.
    /// This utility class simplifies the process of accessing global, scene, game object, and table-specific service registers.
    /// </summary>
    public static class SL
    {
        /// <summary>
        /// Gets the global service register that maintains services available throughout the entire application.
        /// </summary>
        /// <returns>The global service register</returns>
        public static Register<object> G() => ServiceLocator.ForGlobal();
        
        /// <summary>
        /// Gets a scene-specific service register for the specified scene.
        /// Scene registers are automatically cleaned up when the scene is unloaded.
        /// </summary>
        /// <param name="sceneName">Name of the scene to get the register for</param>
        /// <returns>The scene-specific service register</returns>
        public static Register<object> S(string sceneName) => ServiceLocator.ForSceneOf(sceneName);
        
        /// <summary>
        /// Gets a GameObject-specific service register for the specified GameObject.
        /// GameObject registers are automatically cleaned up when the GameObject is destroyed.
        /// </summary>
        /// <param name="go">The GameObject to get the register for</param>
        /// <returns>The GameObject-specific service register</returns>
        public static Register<object> GO(GameObject gameObject) => ServiceLocator.ForGameObjectOf(gameObject);
        
        /// <summary>
        /// Gets a table-specific service register for the specified table name.
        /// Tables provide an additional organizational layer for grouping related services.
        /// </summary>
        /// <param name="tableName">Name of the table to get the register for</param>
        /// <returns>The table-specific service register</returns>
        public static Register<object> T(string tableName) => ServiceLocator.ForTableOf(tableName);
    }
}