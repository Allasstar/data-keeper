using System;
using System.Collections.Generic;
using DataKeeper.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    /// <summary>
    /// Handle to an active reactive-to-UI binding. Dispose to unbind early;
    /// otherwise the binding is released automatically when the bound target is destroyed.
    /// </summary>
    public sealed class UIBinding : IDisposable
    {
        private Action _unbind;

        internal UIBinding(Action unbind) => _unbind = unbind;

        public void Dispose()
        {
            _unbind?.Invoke();
            _unbind = null;
        }
    }

    [AddComponentMenu("")]
    [ExecuteAlways]
    internal sealed class UIBindingLifetime : MonoBehaviour
    {
        private readonly List<UIBinding> _bindings = new List<UIBinding>();

        internal void Add(UIBinding binding) => _bindings.Add(binding);

        private void OnDestroy()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Dispose();
            }
            _bindings.Clear();
        }
    }

    /// <summary>
    /// Binds <see cref="IReactive{T}"/> sources (<see cref="Reactive{T}"/>, <see cref="ReactivePref{T}"/>)
    /// to uGUI elements. The current value is pushed on bind; bindings auto-release when the target is destroyed.
    /// </summary>
    public static class ReactiveBindings
    {
        // --- Text ---

        /// <summary>One-way: value → label text (via <paramref name="format"/> or ToString).</summary>
        public static UIBinding BindTo<T>(this IReactive<T> source, TMP_Text label, Func<T, string> format = null)
        {
            Action<T> apply = format == null
                ? v => { if (label) label.text = v != null ? v.ToString() : string.Empty; }
                : v => { if (label) label.text = format(v); };

            source.AddListener(apply, callOnAddListener: true);
            return Attach(label, new UIBinding(() => source.RemoveListener(apply)));
        }

        // --- Slider ---

        /// <summary>Value ↔ slider. Two-way by default; UI edits write back via <see cref="IReactive{T}.UniqueValue"/>.</summary>
        public static UIBinding BindTo(this IReactive<float> source, Slider slider, bool twoWay = true)
        {
            Action<float> apply = v => { if (slider) slider.SetValueWithoutNotify(v); };
            source.AddListener(apply, callOnAddListener: true);

            UnityAction<float> onUI = null;
            if (twoWay)
            {
                onUI = v => source.UniqueValue = v;
                slider.onValueChanged.AddListener(onUI);
            }

            return Attach(slider, new UIBinding(() =>
            {
                source.RemoveListener(apply);
                if (onUI != null && slider) slider.onValueChanged.RemoveListener(onUI);
            }));
        }

        /// <summary>Int value ↔ slider (UI values are rounded on write-back; pair with <c>wholeNumbers</c>).</summary>
        public static UIBinding BindTo(this IReactive<int> source, Slider slider, bool twoWay = true)
        {
            Action<int> apply = v => { if (slider) slider.SetValueWithoutNotify(v); };
            source.AddListener(apply, callOnAddListener: true);

            UnityAction<float> onUI = null;
            if (twoWay)
            {
                onUI = v => source.UniqueValue = Mathf.RoundToInt(v);
                slider.onValueChanged.AddListener(onUI);
            }

            return Attach(slider, new UIBinding(() =>
            {
                source.RemoveListener(apply);
                if (onUI != null && slider) slider.onValueChanged.RemoveListener(onUI);
            }));
        }

        // --- Toggle ---

        /// <summary>Bool value ↔ toggle. Two-way by default.</summary>
        public static UIBinding BindTo(this IReactive<bool> source, Toggle toggle, bool twoWay = true)
        {
            Action<bool> apply = v => { if (toggle) toggle.SetIsOnWithoutNotify(v); };
            source.AddListener(apply, callOnAddListener: true);

            UnityAction<bool> onUI = null;
            if (twoWay)
            {
                onUI = v => source.UniqueValue = v;
                toggle.onValueChanged.AddListener(onUI);
            }

            return Attach(toggle, new UIBinding(() =>
            {
                source.RemoveListener(apply);
                if (onUI != null && toggle) toggle.onValueChanged.RemoveListener(onUI);
            }));
        }

        // --- Input field ---

        /// <summary>String value ↔ input field. Two-way by default (writes back on every edit).</summary>
        public static UIBinding BindTo(this IReactive<string> source, TMP_InputField input, bool twoWay = true)
        {
            Action<string> apply = v => { if (input) input.SetTextWithoutNotify(v); };
            source.AddListener(apply, callOnAddListener: true);

            UnityAction<string> onUI = null;
            if (twoWay)
            {
                onUI = v => source.UniqueValue = v;
                input.onValueChanged.AddListener(onUI);
            }

            return Attach(input, new UIBinding(() =>
            {
                source.RemoveListener(apply);
                if (onUI != null && input) input.onValueChanged.RemoveListener(onUI);
            }));
        }

        // --- Graphics ---

        /// <summary>One-way: float (0..1) → <see cref="Image.fillAmount"/>.</summary>
        public static UIBinding BindToFill(this IReactive<float> source, Image image)
        {
            Action<float> apply = v => { if (image) image.fillAmount = v; };
            source.AddListener(apply, callOnAddListener: true);
            return Attach(image, new UIBinding(() => source.RemoveListener(apply)));
        }

        /// <summary>One-way: sprite → <see cref="Image.sprite"/>.</summary>
        public static UIBinding BindTo(this IReactive<Sprite> source, Image image)
        {
            Action<Sprite> apply = v => { if (image) image.sprite = v; };
            source.AddListener(apply, callOnAddListener: true);
            return Attach(image, new UIBinding(() => source.RemoveListener(apply)));
        }

        /// <summary>One-way: color → <see cref="Graphic.color"/> (Image, TMP_Text, RawImage, ...).</summary>
        public static UIBinding BindToColor(this IReactive<Color> source, Graphic graphic)
        {
            Action<Color> apply = v => { if (graphic) graphic.color = v; };
            source.AddListener(apply, callOnAddListener: true);
            return Attach(graphic, new UIBinding(() => source.RemoveListener(apply)));
        }

        // --- State ---

        /// <summary>One-way: bool → <see cref="GameObject.SetActive"/>.</summary>
        public static UIBinding BindToActive(this IReactive<bool> source, GameObject target)
        {
            Action<bool> apply = v => { if (target) target.SetActive(v); };
            source.AddListener(apply, callOnAddListener: true);
            return Attach(target, new UIBinding(() => source.RemoveListener(apply)));
        }

        /// <summary>One-way: bool → <see cref="Selectable.interactable"/>.</summary>
        public static UIBinding BindToInteractable(this IReactive<bool> source, Selectable selectable)
        {
            Action<bool> apply = v => { if (selectable) selectable.interactable = v; };
            source.AddListener(apply, callOnAddListener: true);
            return Attach(selectable, new UIBinding(() => source.RemoveListener(apply)));
        }

        /// <summary>One-way: float → <see cref="CanvasGroup.alpha"/>.</summary>
        public static UIBinding BindToAlpha(this IReactive<float> source, CanvasGroup group)
        {
            Action<float> apply = v => { if (group) group.alpha = v; };
            source.AddListener(apply, callOnAddListener: true);
            return Attach(group, new UIBinding(() => source.RemoveListener(apply)));
        }

        // --- Custom ---

        /// <summary>
        /// One-way binding with a custom apply action, tied to <paramref name="owner"/>'s lifetime.
        /// </summary>
        public static UIBinding Bind<T>(this IReactive<T> source, Component owner, Action<T> apply)
        {
            source.AddListener(apply, callOnAddListener: true);
            return Attach(owner, new UIBinding(() => source.RemoveListener(apply)));
        }

        // --- Lifetime ---

        private static UIBinding Attach(Component target, UIBinding binding) => Attach(target.gameObject, binding);

        private static UIBinding Attach(GameObject target, UIBinding binding)
        {
            if (!target.TryGetComponent(out UIBindingLifetime lifetime))
            {
                lifetime = target.AddComponent<UIBindingLifetime>();
                lifetime.hideFlags = HideFlags.HideInInspector;
            }
            lifetime.Add(binding);
            return binding;
        }
    }
}
