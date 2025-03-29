using System;
using System.Collections.Generic;
using DataKeeper.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace DataKeeper.Between.Core
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
        
        public static float ApplyEasing(float t, EaseType easeType) => easeType switch
        {
            // Quadratic
            EaseType.EaseInQuad => t * t,
            EaseType.EaseOutQuad => 1 - (1 - t) * (1 - t),
            EaseType.EaseInOutQuad => (float)(t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2),

            // Cubic
            EaseType.EaseInCubic => t * t * t,
            EaseType.EaseOutCubic => 1 + (--t) * t * t,
            EaseType.EaseInOutCubic => t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1,

            // Quartic
            EaseType.EaseInQuart => t * t * t * t,
            EaseType.EaseOutQuart => 1 - (--t) * t * t * t,
            EaseType.EaseInOutQuart => t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t,

            // Quintic
            EaseType.EaseInQuint => t * t * t * t * t,
            EaseType.EaseOutQuint => 1 + (--t) * t * t * t * t,
            EaseType.EaseInOutQuint => t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t,

            // Sine
            EaseType.EaseInSine => (float)(1 - Math.Cos(t * Math.PI / 2)),
            EaseType.EaseOutSine => (float)Math.Sin(t * Math.PI / 2),
            EaseType.EaseInOutSine => (float)(-0.5 * (Math.Cos(Math.PI * t) - 1)),

            // Exponential
            EaseType.EaseInExpo => (float)(t == 0 ? 0 : Math.Pow(2, 10 * (t - 1))),
            EaseType.EaseOutExpo => (float)(Mathf.Approximately(t, 1) ? 1 : 1 - Math.Pow(2, -10 * t)),
            EaseType.EaseInOutExpo => (float)(t == 0 ? 0 : Mathf.Approximately(t, 1) ? 1 : t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2 : (2 - Math.Pow(2, -20 * t + 10)) / 2),

            // Circular
            EaseType.EaseInCirc => (float)(1 - Math.Sqrt(1 -  Math.Pow(t, 2))),
            EaseType.EaseOutCirc => (float)Math.Sqrt(1 - Math.Pow(t - 1, 2)),
            EaseType.EaseInOutCirc => (float)(t < 0.5f ? (1 - Math.Sqrt(1 - Math.Pow(2 * t, 2))) / 2 : Math.Sqrt(1 - Math.Pow(-2 * t + 2, 2)) + 1) / 2,

            // Bounce
            EaseType.EaseInBounce => 1 - ApplyEasing(1 - t, EaseType.EaseOutBounce),
            EaseType.EaseOutBounce => t switch
            {
                < 1 / 2.75f => 7.5625f * t * t,
                < 2 / 2.75f => 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f,
                < 2.5f / 2.75f => 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f,
                _ => 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f
            },
            EaseType.EaseInOutBounce => t < 0.5f 
                ? ApplyEasing(t * 2, EaseType.EaseInBounce) * 0.5f 
                : ApplyEasing(t * 2 - 1, EaseType.EaseOutBounce) * 0.5f + 0.5f,

            // Back
            EaseType.EaseInBack => t * t * ((1.70158f + 1) * t - 1.70158f),
            EaseType.EaseOutBack => (t -= 1) * t * ((1.70158f + 1) * t + 1.70158f) + 1,
            EaseType.EaseInOutBack => t < 0.5f
                ? (t * 2) * (t * 2) * ((1.70158f * 1.525f + 1) * (t * 2) - 1.70158f * 1.525f) / 2
                : ((t * 2 - 2) * (t * 2 - 2) * ((1.70158f * 1.525f + 1) * (t * 2 - 2) + 1.70158f * 1.525f) + 2) / 2,

            // Elastic
            EaseType.EaseInElastic => t == 0 ? 0 : Mathf.Approximately(t, 1) ? 1 : -(float)Math.Pow(2, 10 * (t - 1)) * (float)Math.Sin((t - 1.1f) * 5 * Math.PI),
            EaseType.EaseOutElastic => t == 0 ? 0 : Mathf.Approximately(t, 1) ? 1 : (float)Math.Pow(2, -10 * t) * (float)Math.Sin((t - 0.1f) * 5 * Math.PI) + 1,
            EaseType.EaseInOutElastic => t switch
            {
                0 => 0,
                1 => 1,
                _ => t < 0.5
                    ? -(float)(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125) * (2 * Math.PI) / 4.5)) / 2
                    : (float)(Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125) * (2 * Math.PI) / 4.5)) / 2 + 1
            },

            // Linear
            _ => t
        };
    }
}