using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataKeeper.Editor.Attributes;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class StaticClassEditorWindow : EditorWindow
    {
        private string selectedNamespace;
        private List<string> namespaces = new List<string>();
        private List<Type> staticClasses = new List<Type>();
        private Type selectedClass;
        private MemberInfo selectedMember;
        private string jsonData;
        private string jsonDataOriginal;
        private AdvancedDropdownState namespaceDropdownState = new AdvancedDropdownState();
        private AdvancedDropdownState classDropdownState = new AdvancedDropdownState();
        private AdvancedDropdownState memberDropdownState = new AdvancedDropdownState();

        [MenuItem("Tools/Windows/Static Class Editor", priority = 3)]
        public static void ShowWindow()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_editicon.sml");

            var window = GetWindow<StaticClassEditorWindow>();
            window.titleContent = new GUIContent("Static Class Editor", icon);
        }

        private void OnEnable()
        {
            LoadNamespaces();
        }

        private void LoadNamespaces()
        {
            namespaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && type.IsAbstract && type.IsSealed && !string.IsNullOrEmpty(type.Namespace))
                .Select(type => type.Namespace)
                .Distinct()
                .OrderBy(ns => ns)
                .ToList();
        }

        private void LoadStaticClasses()
        {
            if (string.IsNullOrEmpty(selectedNamespace)) return;

            staticClasses = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsClass && type.IsAbstract && type.IsSealed && type.Namespace == selectedNamespace
                select type).ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal();
            DrawNamespaceSelector();
            DrawClassSelector();
            DrawMemberSelector();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            DrawJsonEditor();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNamespaceSelector()
        {
            if (GUILayout.Button(string.IsNullOrEmpty(selectedNamespace) ? "Select Namespace" : selectedNamespace, 
                    EditorStyles.toolbarPopup, GUILayout.Width(250)))
            {
                var dropdown = new SearchableEnumDropdown(namespaceDropdownState, namespaces.ToArray(), index =>
                {
                    selectedNamespace = namespaces[index];
                    LoadStaticClasses();
                    selectedClass = null;
                    selectedMember = null;
                });
                dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }
        }

        private void DrawClassSelector()
        {
            if (GUILayout.Button(selectedClass?.Name ?? "Select Class", 
                    EditorStyles.toolbarPopup, GUILayout.Width(250)))
            {
                var dropdown = new SearchableEnumDropdown(classDropdownState, staticClasses.Select(c => c.Name).ToArray(), index =>
                {
                    selectedClass = staticClasses[index];
                    selectedMember = null;
                });
                dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }
        }

        private void DrawMemberSelector()
        {
            var members = selectedClass == null ? Array.Empty<string>() 
                : selectedClass.GetMembers(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m is FieldInfo || m is PropertyInfo)
                .Select(m => m.Name).ToArray();

            if (GUILayout.Button(selectedMember?.Name ?? "Select Member", 
                    EditorStyles.toolbarPopup, GUILayout.Width(250)))
            {
                var dropdown = new SearchableEnumDropdown(memberDropdownState, members, index =>
                {
                    selectedMember = selectedClass.GetMember(members[index], BindingFlags.Static | BindingFlags.Public).FirstOrDefault();
                    if (selectedMember is FieldInfo field)
                    {
                        jsonData = JsonConvert.SerializeObject(field.GetValue(null), Formatting.Indented);
                    }
                    else if (selectedMember is PropertyInfo property)
                    {
                        jsonData = JsonConvert.SerializeObject(property.GetValue(null), Formatting.Indented);
                    }
                    
                    jsonDataOriginal = (string)jsonData.Clone();
                });
                dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }
        }

        private void DrawJsonEditor()
        {
            if (selectedMember == null) return;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Editing: {selectedMember.Name}", EditorStyles.boldLabel);

            jsonData = EditorGUILayout.TextArea(jsonData, GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Changes"))
            {
                ApplyJsonChanges();
            }
            
            if (GUILayout.Button("Restore Original"))
            {
                jsonData = (string)jsonDataOriginal.Clone();
                GUI.FocusControl(null);
                Repaint();
            }
            
            // if (GUILayout.Button("Reset to Default Object"))
            // {
            //     ResetToDefault();
            // }
           
            EditorGUILayout.EndHorizontal();
        
            EditorGUILayout.EndVertical();
        }

        private void ApplyJsonChanges()
        {
            try
            {
                if (selectedMember is FieldInfo field)
                {
                    object value = JsonConvert.DeserializeObject(jsonData, field.FieldType);
                    field.SetValue(null, value);
                }
                else if (selectedMember is PropertyInfo property && property.CanWrite)
                {
                    object value = JsonConvert.DeserializeObject(jsonData, property.PropertyType);
                    property.SetValue(null, value);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void ResetToDefault()
        {
            try
            {
                if (selectedMember is FieldInfo field)
                {
                    field.SetValue(null, Activator.CreateInstance(field.FieldType));
                    jsonData = JsonConvert.SerializeObject(field.GetValue(null), Formatting.Indented);
                }
                else if (selectedMember is PropertyInfo property && property.CanWrite)
                {
                    property.SetValue(null, Activator.CreateInstance(property.PropertyType));
                    jsonData = JsonConvert.SerializeObject(property.GetValue(null), Formatting.Indented);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}
