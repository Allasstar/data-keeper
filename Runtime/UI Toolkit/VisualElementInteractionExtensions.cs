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
    public static class VisualElementInteractionExtensions
    {
        #region Event Registration Extensions

        public static T SetOnEvent<T, TEvent>(this T element, EventCallback<TEvent> callback)
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

        #region Hover Effects

        public static T SetHoverEffect<T>(this T element, Color? hoverColor = null, float? hoverOpacity = null) where T : VisualElement
        {
            var originalColor = element.resolvedStyle.backgroundColor;
            var originalOpacity = element.resolvedStyle.opacity;
            
            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (hoverColor.HasValue)
                    element.style.backgroundColor = hoverColor.Value;
                if (hoverOpacity.HasValue)
                    element.style.opacity = hoverOpacity.Value;
            });
            
            element.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                element.style.backgroundColor = originalColor;
                element.style.opacity = originalOpacity;
            });
            
            return element;
        }

        public static T SetHoverScale<T>(this T element, float scale = 1.05f) where T : VisualElement
        {
            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                element.style.scale = new Scale(Vector2.one * scale);
            });
            
            element.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                element.style.scale = new Scale(Vector2.one);
            });
            
            return element;
        }

        public static T SetHoverCursor<T>(this T element, Cursor cursor) where T : VisualElement
        {
            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                element.style.cursor = new StyleCursor(cursor);
            });
            
            element.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                element.style.cursor = StyleKeyword.Initial;
            });
            
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

        public static T SetDraggable<T>(this T element) where T : VisualElement
        {
            Vector3 originalPosition = Vector3.zero;
            bool isDragging = false;

            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0) // Left mouse button
                {
                    originalPosition = evt.localPosition;
                    isDragging = true;
                    element.CaptureMouse();
                    evt.StopPropagation();
                }
            });

            element.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    var delta = evt.localPosition - originalPosition;
                    var currentTranslate = element.style.translate.value;
                    element.style.translate = new Translate(
                        currentTranslate.x.value + delta.x,
                        currentTranslate.y.value + delta.y
                    );
                }
            });

            element.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    element.ReleaseMouse();
                }
            });

            return element;
        }

        public static T SetResizable<T>(this T element, float minWidth = 50f, float minHeight = 50f) where T : VisualElement
        {
            var resizeHandle = new VisualElement();
            resizeHandle.style.position = Position.Absolute;
            resizeHandle.style.right = 0;
            resizeHandle.style.bottom = 0;
            resizeHandle.style.width = 20;
            resizeHandle.style.height = 20;
            resizeHandle.style.backgroundColor = Color.gray;
            // resizeHandle.style.cursor = new StyleCursor(UnityEngine.UIElements.Cursor.Load("resize-up-right"));

            bool isResizing = false;
            Vector2 startMousePosition = Vector2.zero;
            Vector2 startElementSize = Vector2.zero;

            resizeHandle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    isResizing = true;
                    startMousePosition = evt.position;
                    startElementSize = new Vector2(element.resolvedStyle.width, element.resolvedStyle.height);
                    resizeHandle.CaptureMouse();
                    evt.StopPropagation();
                }
            });

            resizeHandle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (isResizing)
                {
                    var delta = evt.position.ToVector2XY() - startMousePosition;
                    var newWidth = Mathf.Max(startElementSize.x + delta.x, minWidth);
                    var newHeight = Mathf.Max(startElementSize.y + delta.y, minHeight);
                    
                    element.style.width = newWidth;
                    element.style.height = newHeight;
                }
            });

            resizeHandle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (isResizing)
                {
                    isResizing = false;
                    resizeHandle.ReleaseMouse();
                }
            });

            element.Add(resizeHandle);
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

        public static T SetInteractable<T>(this T element, bool interactable = true) where T : VisualElement
        {
            element.SetEnabled(interactable);
            element.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            return element;
        }

        public static T SetClickThrough<T>(this T element) where T : VisualElement
        {
            element.pickingMode = PickingMode.Ignore;
            return element;
        }

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