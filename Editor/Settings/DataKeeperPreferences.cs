using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

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
                    EnhanceHierarchyPrefabIconUI();
                },
                
                // Keywords to help find these preferences when searching
                keywords = new HashSet<string>(new[] { "hierarchy", "icon", "component", "enhance", "data keeper" })
            };
            
            return provider;
        }
        
        private static void EnhanceHierarchyIconUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Enhance:");
            EditorGUI.indentLevel++;

            // Load the current value
            bool currentValue = DataKeeperEditorPref.EnhanceHierarchyIconPref.Value;
            
            // Create a toggle for enabling/disabling the feature
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle("Hierarchy Icon", currentValue);
            
            if (EditorGUI.EndChangeCheck())
            {
                DataKeeperEditorPref.EnhanceHierarchyIconPref.Value = newValue;
                
                // Force Unity to repaint the hierarchy window
                EditorApplication.RepaintHierarchyWindow();
            }
            
            EditorGUILayout.HelpBox(
                "When enabled, this will show component icons next to GameObjects in the hierarchy view.", 
                MessageType.None);
        }
        
        private static void EnhanceHierarchyPrefabIconUI()
        {
            EditorGUI.BeginDisabledGroup(!DataKeeperEditorPref.EnhanceHierarchyIconPref.Value);

            EditorGUILayout.Space();

            // Load the current value
            bool currentValue = DataKeeperEditorPref.EnhanceHierarchyPrefabIconPref.Value;
            
            // Create a toggle for enabling/disabling the feature
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle("Hierarchy Prefab", currentValue);
            
            if (EditorGUI.EndChangeCheck())
            {
                DataKeeperEditorPref.EnhanceHierarchyPrefabIconPref.Value = newValue;
                EditorApplication.RepaintHierarchyWindow();
            }
            
            EditorGUILayout.HelpBox(
                "When enabled, this will show small icon on top of Prefab icon.", 
                MessageType.None);
            
            EditorGUI.EndDisabledGroup();
        }
    }
}