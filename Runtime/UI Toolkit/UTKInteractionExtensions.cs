using System;
using DataKeeper.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace DataKeeper.UIToolkit
{
    /// <summary>
    /// Extensions for interaction, events, and manipulators
    /// </summary>
    public static class UTKInteractionExtensions
    {
        #region Event Registration Extensions

        public static T SetRegisterCallback<T, TEvent>(this T element, EventCallback<TEvent> callback)
            where T : VisualElement
            where TEvent : EventBase<TEvent>, new()
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnClick<T>(this T element, Action callback) where T : VisualElement
        {
            element.RegisterCallback<ClickEvent>(_ => callback?.Invoke());
            return element;
        }

        public static T SetOnClick<T>(this T element, EventCallback<ClickEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnPointerDown<T>(this T element, EventCallback<PointerDownEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnPointerUp<T>(this T element, EventCallback<PointerUpEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnPointerEnter<T>(this T element, EventCallback<PointerEnterEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnPointerLeave<T>(this T element, EventCallback<PointerLeaveEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnPointerMove<T>(this T element, EventCallback<PointerMoveEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnFocusIn<T>(this T element, EventCallback<FocusInEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnFocusOut<T>(this T element, EventCallback<FocusOutEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnKeyDown<T>(this T element, EventCallback<KeyDownEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnKeyUp<T>(this T element, EventCallback<KeyUpEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        public static T SetOnGeometryChanged<T>(this T element, EventCallback<GeometryChangedEvent> callback) where T : VisualElement
        {
            element.RegisterCallback(callback);
            return element;
        }

        #endregion


        #region Manipulator Extensions

        public static T SetManipulator<T, TManipulator>(this T element, TManipulator manipulator)
            where T : VisualElement
            where TManipulator : IManipulator
        {
            element.AddManipulator(manipulator);
            return element;
        }

        public static T SetClickable<T>(this T element, Action callback) where T : VisualElement
        {
            element.AddManipulator(new Clickable(callback));
            return element;
        }

        public static T SetClickable<T>(this T element, Action<EventBase> callback) where T : VisualElement
        {
            element.AddManipulator(new Clickable(callback));
            return element;
        }

        #endregion

        #region Focus and Selection Extensions

        public static T SetFocusable<T>(this T element, bool focusable = true) where T : VisualElement
        {
            element.focusable = focusable;
            return element;
        }

        public static T SetTabIndex<T>(this T element, int tabIndex) where T : VisualElement
        {
            element.tabIndex = tabIndex;
            return element;
        }

        public static T FocusElement<T>(this T element) where T : VisualElement
        {
            element.Focus();
            return element;
        }

        public static T BlurElement<T>(this T element) where T : VisualElement
        {
            element.Blur();
            return element;
        }

        #endregion

        #region Animation and Transition Extensions

        public static T AnimateTo<T>(this T element, float duration, Action<T> setter) where T : VisualElement
        {
            var startTime = Time.time;
            
            element.schedule.Execute(() =>
            {
                var elapsed = Time.time - startTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                
                setter?.Invoke(element);
                
                if (progress >= 1f)
                {
                    return; // Stop the animation
                }
            }).Every(16); // ~60 FPS
            
            return element;
        }

        public static T FadeIn<T>(this T element, float duration = 0.3f) where T : VisualElement
        {
            var startOpacity = element.resolvedStyle.opacity;
            var startTime = Time.time;
            
            element.schedule.Execute(() =>
            {
                var elapsed = Time.time - startTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var opacity = Mathf.Lerp(startOpacity, 1f, progress);
                
                element.style.opacity = opacity;
                
                if (progress >= 1f)
                {
                    return; // Stop the animation
                }
            }).Every(16);
            
            return element;
        }

        public static T FadeOut<T>(this T element, float duration = 0.3f) where T : VisualElement
        {
            var startOpacity = element.resolvedStyle.opacity;
            var startTime = Time.time;
            
            element.schedule.Execute(() =>
            {
                var elapsed = Time.time - startTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var opacity = Mathf.Lerp(startOpacity, 0f, progress);
                
                element.style.opacity = opacity;
                
                if (progress >= 1f)
                {
                    element.style.display = DisplayStyle.None;
                    return; // Stop the animation
                }
            }).Every(16);
            
            return element;
        }

        public static T SlideIn<T>(this T element, Vector2 direction, float duration = 0.3f) where T : VisualElement
        {
            var startPosition = direction * 100f; // Start 100 pixels away
            element.style.translate = new Translate(startPosition.x, startPosition.y);
            element.style.display = DisplayStyle.Flex;
            
            var startTime = Time.time;
            
            element.schedule.Execute(() =>
            {
                var elapsed = Time.time - startTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var currentPosition = Vector2.Lerp(startPosition, Vector2.zero, progress);
                
                element.style.translate = new Translate(currentPosition.x, currentPosition.y);
                
                if (progress >= 1f)
                {
                    return; // Stop the animation
                }
            }).Every(16);
            
            return element;
        }

        #endregion

        #region Utility Interaction Methods

        public static T SetPickingMode<T>(this T element, PickingMode mode) where T : VisualElement
        {
            element.pickingMode = mode;
            return element;
        }

        public static T SetUserData<T>(this T element, object userData) where T : VisualElement
        {
            element.userData = userData;
            return element;
        }

        public static TData GetUserData<T, TData>(this T element) where T : VisualElement
        {
            return element.userData is TData data ? data : default(TData);
        }

        #endregion
    }
}