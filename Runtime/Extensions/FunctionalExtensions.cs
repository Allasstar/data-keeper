using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Extensions
{
    /// <summary>
    /// Provides minimal functional-style helpers:
    /// Do (side effects) and Map (transformations),
    /// plus debug-only logging with automatic caller info.
    /// </summary>
    public static class FunctionalExtensions
    {
        // ─────────────── SIDE EFFECTS ───────────────

        /// <summary>
        /// Executes an action with the current value and returns the same value.
        /// </summary>
        public static T Do<T>(this T value, Action<T> action)
        {
            action?.Invoke(value);
            return value;
        }

        // ─────────────── DEBUG HELPERS ───────────────

        /// <summary>
        /// Logs debug information including the caller file, method, and line.
        /// </summary>
        public static T DoDebug<T>(
            this T value,
            Func<T, string> messageProvider,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            string msg = messageProvider?.Invoke(value);
            Debug.Log(Message(file, member, line, msg));
            
            return value;
        }
        
        /// <summary>
        /// Logs debug information including the context, caller file, method, and line.
        /// </summary>
        public static T DoDebug<T>(
            this T value,
            Func<T, string> messageProvider,
            Object context,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            string msg = messageProvider?.Invoke(value);
            Debug.Log(Message(file, member, line, msg), context);
            
            return value;
        }

        /// <summary>
        /// Logs debug information only if the condition is true.
        /// Includes caller file, method, and line.
        /// </summary>
        public static T DoDebugIf<T>(
            this T value,
            Func<T, bool> condition,
            Func<T, string> messageProvider,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (condition(value))
            {
                string msg = messageProvider?.Invoke(value);
                Debug.Log(Message(file, member, line, msg));
            }

            return value;
        }
        
        /// <summary>
        /// Logs debug information only if the condition is true.
        /// Includes context, caller file, method, and line.
        /// </summary>
        public static T DoDebugIf<T>(
            this T value,
            Func<T, bool> condition,
            Func<T, string> messageProvider,
            Object context,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (condition(value))
            {
                string msg = messageProvider?.Invoke(value);
                Debug.Log(Message(file, member, line, msg), context);
            }

            return value;
        }

        private static string Message(string file, string member, int line, string msg)
        {
            return $"{msg} >> [{System.IO.Path.GetFileName(file)}:{line}] → {member}";
        }

        // ─────────────── TRANSFORMATION ───────────────

        /// <summary>
        /// Applies a transformation function to the value and returns the result.
        /// </summary>
        public static TOut Map<TIn, TOut>(this TIn value, Func<TIn, TOut> func) => func(value);
    }
}
