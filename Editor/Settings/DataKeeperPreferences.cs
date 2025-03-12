using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace DataKeeper.Editor.Settings
{
    static class DataKeeperPreferences
    {
        [SettingsProvider]
        public static SettingsProvider CreateDataKeeperPreferences()
        {
            var provider = new SettingsProvider("Preferences/Data Keeper", SettingsScope.User)
            {
                label = "Data Keeper",
                guiHandler = (searchContext) =>
                {
                    EnhanceHierarchyIconUI();
                },
                
                // Keywords to help find these preferences when searching
                keywords = new HashSet<string>(new[] { "hierarchy", "icon", "component", "enhance", "data keeper" })
            };
            
            return provider;
        }
        
        private static void EnhanceHierarchyIconUI()
        {
            // Load the current value
            bool currentValue = DataKeeperEditorPref.EnhanceHierarchyIconPref.Value;
            
            // Create a toggle for enabling/disabling the feature
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle("Enhance Hierarchy Icon", currentValue);
            
            if (EditorGUI.EndChangeCheck())
            {
                DataKeeperEditorPref.EnhanceHierarchyIconPref.Value = newValue;
                
                // Force Unity to repaint the hierarchy window
                EditorApplication.RepaintHierarchyWindow();
            }
            
            // Add some space
            EditorGUILayout.Space();
            
            // Add a help box with additional information
            EditorGUILayout.HelpBox(
                "When enabled, this will show component icons next to GameObjects in the hierarchy view.", 
                MessageType.Info);
        }
    }
}