using System;
using System.Runtime.CompilerServices;
using UnityEngine;

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
        /// Only compiled in DEBUG or Unity Editor.
        /// </summary>
        public static T DoDebug<T>(
            this T value,
            Func<T, string> messageProvider,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            string msg = messageProvider?.Invoke(value);
            Debug.Log($"{msg} >> {member} → [{System.IO.Path.GetFileName(file)}:{line}]");
            return value;
        }

        /// <summary>
        /// Logs debug information only if the condition is true.
        /// Includes caller file, method, and line.
        /// Only compiled in DEBUG or Unity Editor.
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
                Debug.Log($"{msg} >> {member} → [{System.IO.Path.GetFileName(file)}:{line}]");
            }

            return value;
        }

        // ─────────────── TRANSFORMATION ───────────────

        /// <summary>
        /// Applies a transformation function to the value and returns the result.
        /// </summary>
        public static TOut Map<TIn, TOut>(this TIn value, Func<TIn, TOut> func) => func(value);
    }
}
