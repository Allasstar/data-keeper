using System;
using DataKeeper.FSM;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.FSM
{
    public static class FSMDebugger
    {
        private const int MAX_DISPLAYED_HISTORY = 10;

        public static void DrawFSMDebuger(Object target)
        {
            try
            {
                var targetType = target.GetType();
                var fields = targetType.GetFields(System.Reflection.BindingFlags.NonPublic | 
                                                  System.Reflection.BindingFlags.Public | 
                                                  System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field?.FieldType == null) continue;
                
                    if (field.FieldType.IsGenericType && 
                        field.FieldType.GetGenericTypeDefinition() == typeof(StateMachine<,>))
                    {
                        var stateMachine = field.GetValue(target);
                        if (stateMachine != null)
                        {
                            DrawStateMachineDebug(stateMachine);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"FSMDebugger error: {e.Message}");
            }
        }

        private static void DrawStateMachineDebug(object stateMachine)
        {
            if (stateMachine == null) return;

            try
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("State Machine Debug", EditorStyles.boldLabel);

                var type = stateMachine.GetType();
                if (type == null) return;

                // Current State
                DrawStateProperty(stateMachine, "CurrentStateType", "Current State");

                // Previous State
                DrawStateProperty(stateMachine, "PreviousStateType", "Previous State");

                // History
                DrawHistory(stateMachine);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error drawing FSM debug: {e.Message}");
            }
        }

        private static void DrawStateProperty(object stateMachine, string propertyName, string label)
        {
            try
            {
                var property = stateMachine.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(stateMachine);
                    if (value != null)
                    {
                        EditorGUILayout.LabelField(label, value.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error drawing state property {propertyName}: {e.Message}");
            }
        }

        private static bool showHistory = false;
        private static void DrawHistory(object stateMachine)
        {
            showHistory = EditorGUILayout.Foldout(showHistory, "State History", true);
            if(!showHistory) return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("From", EditorStyles.boldLabel,GUILayout.Width(100));
            EditorGUILayout.LabelField("To", EditorStyles.boldLabel,GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            try
            {
                var getHistoryMethod = stateMachine.GetType().GetMethod("GetStateHistory");
                if (getHistoryMethod == null) return;

                var history = getHistoryMethod.Invoke(stateMachine, null) as Array;
                if (history == null || history.Length == 0) return;

                for (int i = history.Length - 1; i >= Mathf.Max(0, history.Length - MAX_DISPLAYED_HISTORY); i--)
                {
                    var record = (TransitionRecordHistory)history.GetValue(i);

                    // First row green, others alternating light/dark
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.3f, 0.3f, 0.3f));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{record.TimeStamp:F}s", GUILayout.Width(100));
                    EditorGUILayout.LabelField(record.FromState, GUILayout.Width(100));
                    EditorGUILayout.LabelField(record.ToState, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error drawing history: {e.Message}");
            }
        }
    }
}