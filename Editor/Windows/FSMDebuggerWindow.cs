using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.FSM
{
    public class FSMDebuggerWindow : EditorWindow
    {
        private GameObject selectedObject;
        private Vector2 scrollPosition;
        private bool isLocked = false;
        private int historySize = 10;

        [MenuItem("Tools/Windows/FSM Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<FSMDebuggerWindow>("FSM Debugger");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUI.BeginChangeCheck();
            isLocked = EditorGUILayout.Toggle(isLocked, GUILayout.Width(20));
            EditorGUILayout.LabelField("Lock Selection", GUILayout.Width(100));
            EditorGUI.EndChangeCheck();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("History Size", GUILayout.Width(73));
            historySize = EditorGUILayout.IntField(historySize, GUILayout.Width(50));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("Select an object with StateMachine component in Hierarchy", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField($"Selected: {selectedObject.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var components = selectedObject.GetComponents<Component>();
            bool foundAnyStateMachine = false;
            
            foreach (var component in components)
            {
                if (component == null) continue;

                if (FSMDebugger.DrawFSMDebugger(component, historySize))
                {
                    foundAnyStateMachine = true;
                }
            }
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Works only in play mode", MessageType.Info);
            }
            
            if (!foundAnyStateMachine)
            {
                EditorGUILayout.HelpBox("No StateMachine components found on this object", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            
            Repaint();
        }

        private void OnSelectionChange()
        {
            // Only update selected object when not locked
            if (!isLocked && Selection.activeGameObject != null)
            {
                selectedObject = Selection.activeGameObject;
                Repaint();
            }
        }
    }
}