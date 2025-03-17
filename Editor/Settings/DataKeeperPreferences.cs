using System;
using UnityEditor;
using System.Collections.Generic;
using DataKeeper.Editor.Enhance;
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
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Enhance Hierarchy Icon:", EditorStyles.boldLabel);
                    EnhanceHierarchyIconUI();
                    EnhanceHierarchyPrefabIconUI();
                    EditorGUILayout.EndVertical();
                },
                
                // Keywords to help find these preferences when searching
                keywords = new HashSet<string>(new[] { "hierarchy", "icon", "component", "enhance", "data keeper" })
            };
            
            return provider;
        }
        
        private static void EnhanceHierarchyIconUI()
        {
            EditorGUI.indentLevel++;

            // Load the current value
            bool currentValue = DataKeeperEditorPref.EnhanceHierarchyIconPref.Value;
            
            // Create a toggle for enabling/disabling the feature
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle("Enabled", currentValue);
            
            if (EditorGUI.EndChangeCheck())
            {
                DataKeeperEditorPref.EnhanceHierarchyIconPref.Value = newValue;
                
                // Force Unity to repaint the hierarchy window
                EditorApplication.RepaintHierarchyWindow();
            }
        }
        
        private static void EnhanceHierarchyPrefabIconUI()
        {
            EditorGUI.BeginDisabledGroup(!DataKeeperEditorPref.EnhanceHierarchyIconPref.Value);

            // Load the current value
            PrefabHierarchyIcon currentValue = DataKeeperEditorPref.EnhanceHierarchyPrefabIconPref.Value;
            
            // Create a toggle for enabling/disabling the feature
            EditorGUI.BeginChangeCheck();

            var newValue = (PrefabHierarchyIcon)EditorGUILayout.EnumPopup("Prefab", currentValue, GUILayout.MaxWidth(300));
            
            if (EditorGUI.EndChangeCheck())
            {
                DataKeeperEditorPref.EnhanceHierarchyPrefabIconPref.Value = newValue;
                EditorApplication.RepaintHierarchyWindow();
            }
            
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }
    }
}