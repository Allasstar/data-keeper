using System;
using System.Collections.Generic;
using DataKeeper.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace DataKeeper.Ease
{
    public static class TweenCore
    {
        private struct TweenUpdateSystem { }

        private static HashSet<Action> actions = new HashSet<Action>();
        private static List<Action> remove = new List<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Initialize()
        {
            actions.Clear();
            remove.Clear();

            PlayerLoopSystem tweenSystem = new PlayerLoopSystem
            {
                type = typeof(TweenUpdateSystem),
                updateDelegate = ExecuteActions
            };

            PlayerLoopHelper.InsertSystemIntoPlayerLoop(typeof(Update), tweenSystem);

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorApplicationOnplayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            
            void EditorApplicationOnplayModeStateChanged(PlayModeStateChange state)
            {
                if (state != PlayModeStateChange.ExitingPlayMode) return;
                PlayerLoopHelper.RemoveSystemFromPlayerLoop(typeof(Update), typeof(TweenUpdateSystem));
                actions.Clear();
                remove.Clear();
            }
#endif
        }
        
        public static void AddToUpdate(Action action)
        {
            actions.Add(action);
        }
        
        public static void RemoveFromUpdate(Action action)
        {
            remove.Add(action);
        }

        private static void ExecuteActions()
        {
            if (!Application.isPlaying)
                return;
            
            foreach (var action in remove)
            {
                actions.Remove(action);
            }
            remove.Clear();
            
            foreach (var action in actions)
            {
                action?.Invoke();
            }
        }
    }
}