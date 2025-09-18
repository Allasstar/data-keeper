using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for hierarchy management, classes, and utility functions
    /// </summary>
    public static class UTKHierarchyExtensions
    {
        #region Hierarchy Management

        public static T SetChildOf<T>(this T element, VisualElement parent) where T : VisualElement
        {
            parent.Add(element);
            return element;
        }

        public static T AddChild<T>(this T element, params VisualElement[] children) where T : VisualElement
        {
            foreach (var child in children)
                element.Add(child);
            return element;
        }

        public static T AddChildren<T>(this T element, System.Collections.Generic.IEnumerable<VisualElement> children) where T : VisualElement
        {
            foreach (var child in children)
                element.Add(child);
            return element;
        }

        public static T InsertChild<T>(this T element, int index, VisualElement child) where T : VisualElement
        {
            element.Insert(index, child);
            return element;
        }

        public static T RemoveChild<T>(this T element, VisualElement child) where T : VisualElement
        {
            element.Remove(child);
            return element;
        }

        public static T RemoveAllChildren<T>(this T element) where T : VisualElement
        {
            element.Clear();
            return element;
        }

        public static T RemoveFromHierarchy<T>(this T element) where T : VisualElement
        {
            element.RemoveFromHierarchy();
            return element;
        }

        public static T BringToFront<T>(this T element) where T : VisualElement
        {
            element.BringToFront();
            return element;
        }

        public static T SendToBack<T>(this T element) where T : VisualElement
        {
            element.SendToBack();
            return element;
        }

        #endregion

        #region CSS Class Management

        public static T AddClass<T>(this T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }

        public static T AddClasses<T>(this T element, params string[] classNames) where T : VisualElement
        {
            foreach (var className in classNames)
                element.AddToClassList(className);
            return element;
        }

        public static T RemoveClass<T>(this T element, string className) where T : VisualElement
        {
            element.RemoveFromClassList(className);
            return element;
        }

        public static T RemoveClasses<T>(this T element, params string[] classNames) where T : VisualElement
        {
            foreach (var className in classNames)
                element.RemoveFromClassList(className);
            return element;
        }

        public static T ToggleInClassList<T>(this T element, string className) where T : VisualElement
        {
            element.ToggleInClassList(className);
            return element;
        }

        public static T EnableInClassList<T>(this T element, string className, bool enabled) where T : VisualElement
        {
            element.EnableInClassList(className, enabled);
            return element;
        }

        public static T RemoveAllUnityClasses<T>(this T element) where T : VisualElement
        {
            var unityClasses = element.GetClasses()
                .Where(className => className.StartsWith("unity"))
                .ToList();

            foreach (string className in unityClasses)
                element.RemoveFromClassList(className);

            foreach (VisualElement child in element.hierarchy.Children())
                child.RemoveAllUnityClasses();

            return element;
        }

        public static bool HasClass<T>(this T element, string className) where T : VisualElement
        {
            return element.ClassListContains(className);
        }

        #endregion

        #region Safe Area and Screen Utilities

        public static T SetSafeArea<T>(this T element, bool apply = true) where T : VisualElement
        {
            Rect safeArea = apply ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);

            element.style.paddingLeft = safeArea.x;
            element.style.paddingBottom = safeArea.y;
            element.style.paddingRight = Screen.width - (safeArea.x + safeArea.width);
            element.style.paddingTop = Screen.height - (safeArea.y + safeArea.height);

            return element;
        }

        public static T SetSafeAreaBorder<T>(this T element, bool apply = true) where T : VisualElement
        {
            Rect safeArea = apply ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);

            element.style.borderLeftWidth = safeArea.x;
            element.style.borderBottomWidth = safeArea.y;
            element.style.borderRightWidth = Screen.width - (safeArea.x + safeArea.width);
            element.style.borderTopWidth = Screen.height - (safeArea.y + safeArea.height);

            return element;
        }

        #endregion

        #region Debug and Development Utilities

        public static T DebugLogAllUnityClasses<T>(this T element) where T : VisualElement
        {
            foreach (string className in element.GetClasses().Where(className => className.StartsWith("unity")))
                Debug.Log($"{element.name}<{element.GetType().Name}> :: class: {className}");

            foreach (VisualElement child in element.hierarchy.Children())
                child.DebugLogAllUnityClasses();

            return element;
        }

        public static T DebugBorder<T>(this T element, Color? color = null) where T : VisualElement
        {
            var debugColor = color ?? Color.red;
            element.style.borderLeftWidth = 1;
            element.style.borderRightWidth = 1;
            element.style.borderTopWidth = 1;
            element.style.borderBottomWidth = 1;
            element.style.borderLeftColor = debugColor;
            element.style.borderRightColor = debugColor;
            element.style.borderTopColor = debugColor;
            element.style.borderBottomColor = debugColor;
            return element;
        }

        public static T DebugBackground<T>(this T element, Color? color = null) where T : VisualElement
        {
            var debugColor = color ?? new Color(1f, 0f, 0f, 0.3f);
            element.style.backgroundColor = debugColor;
            return element;
        }

        public static T DebugLogInfo<T>(this T element, string message = "") where T : VisualElement
        {
            var info = string.IsNullOrEmpty(message) 
                ? $"Element: {element.name} ({element.GetType().Name})"
                : $"{message} - Element: {element.name} ({element.GetType().Name})";
            Debug.Log(info);
            return element;
        }

        #endregion

        #region Layout Debugging

        public static T ForceUpdate<T>(this T element) where T : VisualElement
        {
            element.schedule.Execute(() =>
            {
                var fakeOldRect = Rect.zero;
                var fakeNewRect = element.layout;

                using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = element.contentContainer;
                element.contentContainer.SendEvent(evt);
            });

            return element;
        }

        public static T MarkDirtyRepaint<T>(this T element) where T : VisualElement
        {
            element.MarkDirtyRepaint();
            return element;
        }

        public static T ScheduleAction<T>(this T element, Action action, long delayMs = 0) where T : VisualElement
        {
            element.schedule.Execute(action).StartingIn(delayMs);
            return element;
        }

        public static T ScheduleRepeating<T>(this T element, Action action, long intervalMs) where T : VisualElement
        {
            element.schedule.Execute(action).Every(intervalMs);
            return element;
        }

        #endregion

        #region State Management

        public static T SaveState<T>(this T element, string key, object state) where T : VisualElement
        {
            if (element.userData == null)
                element.userData = new Dictionary<string, object>();
            
            if (element.userData is Dictionary<string, object> dict)
            {
                dict[key] = state;
            }
            
            return element;
        }

        public static TState GetState<T, TState>(this T element, string key, TState defaultValue = default) where T : VisualElement
        {
            if (element.userData is Dictionary<string, object> dict)
            {
                return dict.TryGetValue(key, out var value) && value is TState state ? state : defaultValue;
            }
            
            return defaultValue;
        }

        public static T ClearState<T>(this T element, string key = null) where T : VisualElement
        {
            if (element.userData is Dictionary<string, object> dict)
            {
                if (key == null)
                    dict.Clear();
                else
                    dict.Remove(key);
            }
            
            return element;
        }

        #endregion

        #region Performance Utilities

        public static T SetGenerateVisualContent<T>(this T element, Action<MeshGenerationContext> generateVisualContent) where T : VisualElement
        {
            element.generateVisualContent += generateVisualContent;
            return element;
        }

        #endregion
    }
}