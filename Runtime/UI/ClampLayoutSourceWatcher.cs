using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    // A size source sits outside the layout branch that reads it, and MarkLayoutForRebuild only ever walks
    // parents, so a change on the source never reaches the clamp by itself. This rides along on the source and
    // carries the notification across. ClampLayoutElement adds and removes it; it is never authored.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [RequireComponent(typeof(RectTransform))]
    public class ClampLayoutSourceWatcher : UIBehaviour, ILayoutElement
    {
        private readonly List<ClampLayoutElement> m_Listeners = new List<ClampLayoutElement>();

        private RectTransform m_Rect;

        // -1 so the first sample always reports a change.
        private Vector2 m_LastMin = new Vector2(-1f, -1f);
        private Vector2 m_LastPreferred = new Vector2(-1f, -1f);

        public static void Watch(RectTransform source, ClampLayoutElement listener)
        {
            ClampLayoutSourceWatcher watcher = source.GetComponent<ClampLayoutSourceWatcher>();

            if (watcher == null)
            {
                watcher = source.gameObject.AddComponent<ClampLayoutSourceWatcher>();
                watcher.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!watcher.m_Listeners.Contains(listener)) watcher.m_Listeners.Add(listener);
        }

        public static void Unwatch(RectTransform source, ClampLayoutElement listener)
        {
            if (source == null) return;

            ClampLayoutSourceWatcher watcher = source.GetComponent<ClampLayoutSourceWatcher>();
            if (watcher == null) return;

            watcher.m_Listeners.Remove(listener);
            if (watcher.m_Listeners.Count > 0) return;

            if (Application.isPlaying) Destroy(watcher);
            else DestroyImmediate(watcher);
        }

        protected override void Awake()
        {
            base.Awake();
            m_Rect = (RectTransform)transform;
        }

        // Inert to LayoutUtility: every size is -1, so it is skipped before its priority is ever considered.
        // ILayoutElement is implemented only to be called back when the source recalculates.
        public float minWidth => -1f;
#if UNITY_6000_6_OR_NEWER
        public float maxWidth => -1f;
#endif
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => -1f;
#if UNITY_6000_6_OR_NEWER
        public float maxHeight => -1f;
#endif
        public float preferredHeight => -1f;
        public float flexibleHeight => -1f;
        public int layoutPriority => int.MinValue;

        public void CalculateLayoutInputHorizontal()
        {
        }

        // The last calculation callback of a layout pass, so both axes are settled by now. A source resized by
        // its own fitter reports through OnRectTransformDimensionsChange instead; one whose reported size moved
        // without its rect moving only shows up here.
        public void CalculateLayoutInputVertical()
        {
            SampleAndNotify();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SampleAndNotify();
        }

        // Gated on the measured values rather than firing on every signal: a source the clamp indirectly sizes
        // would otherwise mark a rebuild every frame forever.
        private void SampleAndNotify()
        {
            Vector2 min = new Vector2(LayoutUtility.GetMinWidth(m_Rect), LayoutUtility.GetMinHeight(m_Rect));
            Vector2 preferred = new Vector2(LayoutUtility.GetPreferredWidth(m_Rect), LayoutUtility.GetPreferredHeight(m_Rect));

            if (min == m_LastMin && preferred == m_LastPreferred) return;

            m_LastMin = min;
            m_LastPreferred = preferred;

            for (int i = m_Listeners.Count - 1; i >= 0; i--)
            {
                ClampLayoutElement listener = m_Listeners[i];

                if (listener == null)
                {
                    m_Listeners.RemoveAt(i);
                    continue;
                }

                listener.SetDirty();
            }
        }
    }
}
