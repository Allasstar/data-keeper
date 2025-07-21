using System;
using DataKeeper.FSM;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.FSM
{
    public static class FSMDebugger
    {
        public static bool DrawFSMDebugger(Object target)
        {
            try
            {
                var targetType = target.GetType();
                var fields = targetType.GetFields(System.Reflection.BindingFlags.NonPublic | 
                                                  System.Reflection.BindingFlags.Public | 
                                                  System.Reflection.BindingFlags.Instance);

                bool foundStateMachine = false;
                foreach (var field in fields)
                {
                    if (field?.FieldType == null) continue;
        
                    if (field.FieldType.IsGenericType && 
                        field.FieldType.GetGenericTypeDefinition() == typeof(StateMachine<,>))
                    {
                        var stateMachine = field.GetValue(target);
                        if (stateMachine != null)
                        {
                            DrawStateMachineDebug(stateMachine, target);
                            foundStateMachine = true;
                        }
                    }
                }
                
                return foundStateMachine;
            }
            catch (Exception e)
            {
                Debug.LogError($"FSMDebugger error: {e.Message}");
                return false;
            }
        }

        private static void DrawStateMachineDebug(object stateMachine, Object component)
        {
            if (stateMachine == null) return;

            try
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField($"Component: {component.GetType().Name} > {nameof(stateMachine)}", EditorStyles.boldLabel);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("State Machine Debug", EditorStyles.boldLabel);

                var type = stateMachine.GetType();
                if (type == null) return;
                
                // Current State
                DrawStateProperty(stateMachine, "CurrentStateType", "Current State \u25b6");

                // Previous State
                DrawStateProperty(stateMachine, "PreviousStateType", "Previous State \u25c1");

                // History
                DrawHistory(stateMachine);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
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

        private static bool showAllStates = false;
        private static bool showHistory = false;
        private static void DrawHistory(object stateMachine)
        {
            showAllStates = EditorGUILayout.Foldout(showAllStates, "States", true);
            if (showAllStates)
            {
                States(stateMachine);
            }
            
            showHistory = EditorGUILayout.Foldout(showHistory, "State History", true);
            if (showHistory)
            {
                History(stateMachine);
            }
        }

        private static void States(object stateMachine)
        {
            try
            {
                var getAllStatesMethod = stateMachine.GetType().GetMethod("GetAllStates");
                if (getAllStatesMethod == null) return;
                
                var allStates = getAllStatesMethod.Invoke(stateMachine, null) as string[];
                if (allStates == null || allStates.Length == 0) return;
                
                var curState = "";
                var prevState = "";

                var propertyCurrentStateType = stateMachine.GetType().GetProperty("CurrentStateType");
                if (propertyCurrentStateType != null)
                {
                    curState = propertyCurrentStateType.GetValue(stateMachine).ToString();
                }
            
                var propertyPreviousStateType = stateMachine.GetType().GetProperty("PreviousStateType");
                if (propertyPreviousStateType != null)
                {
                    prevState = propertyPreviousStateType.GetValue(stateMachine).ToString();
                }
                
                EditorGUILayout.Space();

                foreach (var state in allStates)
                {
                    var status = "";
                    if (state.Equals(curState))
                    {
                        status = "\u25b6";
                    }
                    else if (state.Equals(prevState))
                    {
                        status = "\u25c1";
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(status, GUILayout.Width(20));
                    EditorGUILayout.LabelField(state);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.3f, 0.3f, 0.3f));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error drawing states: {e.Message}");
            }
        }

        private static void History(object stateMachine)
        {
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

                for (int i = history.Length - 1; i >= 0; i--)
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