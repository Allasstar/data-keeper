using System;
using System.Collections.Generic;
using DataKeeper.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Clamp Layout Element")]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class ClampLayoutElement : UIBehaviour, ILayoutElement
    {
        // LayoutElement reports at 1, Text / TMP_Text / LayoutGroup at 0. LayoutUtility resolves equal
        // priorities to the largest value, so a size can only ever be clamped from above all of them.
        private const int DefaultLayoutPriority = 2;

        private static readonly Func<ILayoutElement, float> MinWidthOf = e => e.minWidth;
        private static readonly Func<ILayoutElement, float> MinHeightOf = e => e.minHeight;
        private static readonly Func<ILayoutElement, float> PreferredWidthOf = e => e.preferredWidth;
        private static readonly Func<ILayoutElement, float> PreferredHeightOf = e => e.preferredHeight;

        [SerializeField] private Optional<float> m_MinWidth = new Optional<float>(100f, false);
        [SerializeField] private Optional<float> m_MaxWidth = new Optional<float>(300f, false);
        [SerializeField] private Optional<float> m_MinHeight = new Optional<float>(100f, false);
        [SerializeField] private Optional<float> m_MaxHeight = new Optional<float>(300f, false);
        [SerializeField] private Optional<RectTransform> m_SizeSource;
        [SerializeField] private int m_LayoutPriority = DefaultLayoutPriority;

        // The six ILayoutElement size members are explicit: this component's own minWidth is the authored
        // bound, while the interface reports what the object should measure once that bound is applied.
        public Optional<float> minWidth { get => m_MinWidth; set => SetProperty(ref m_MinWidth, Sanitize(value)); }
        public Optional<float> maxWidth { get => m_MaxWidth; set => SetProperty(ref m_MaxWidth, Sanitize(value)); }
        public Optional<float> minHeight { get => m_MinHeight; set => SetProperty(ref m_MinHeight, Sanitize(value)); }
        public Optional<float> maxHeight { get => m_MaxHeight; set => SetProperty(ref m_MaxHeight, Sanitize(value)); }
        public Optional<RectTransform> sizeSource
        {
            get => m_SizeSource;
            set { if (SetProperty(ref m_SizeSource, value)) SyncSourceWatcher(); }
        }
        public int layoutPriority { get => m_LayoutPriority; set => SetProperty(ref m_LayoutPriority, value); }

        private static readonly List<Component> s_Groups = new List<Component>();
        private static readonly List<RectTransform> s_Measuring = new List<RectTransform>();

        private readonly List<Component> m_Elements = new List<Component>();

        private RectTransform m_WatchedSource;
        private int m_MeasuredFrame = -1;

        protected ClampLayoutElement()
        {
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        // GetPreferredSize takes the larger of the separately resolved min and preferred, so a clamp that
        // skips the minimum is silently undone by it.
        float ILayoutElement.minWidth => Report(0, MinWidthOf);
        float ILayoutElement.minHeight => Report(1, MinHeightOf);
        float ILayoutElement.preferredWidth => Report(0, PreferredWidthOf);
        float ILayoutElement.preferredHeight => Report(1, PreferredHeightOf);

#if UNITY_6000_6_OR_NEWER
        // uGUI reads a maximum of its own since Unity 6.6. Reporting the authored bound here lets a layout
        // group and a ContentSizeFitter honour it directly, on top of the sizes already clamped above.
        float ILayoutElement.maxWidth => GetMaxSize(0);
        float ILayoutElement.maxHeight => GetMaxSize(1);
#endif

        // A layout group adds flexible space on top of the preferred size on its main axis, and treats a
        // flexible child as having no upper bound on its cross axis. Either way an upper bound needs it
        // gone - a lower bound does not, so this follows the maximum alone.
        float ILayoutElement.flexibleWidth => GetMaxSize(0) < 0f ? -1f : 0f;
        float ILayoutElement.flexibleHeight => GetMaxSize(1) < 0f ? -1f : 0f;

        public float GetMinSize(int axis) => BoundOf(axis == 0 ? m_MinWidth : m_MinHeight);
        public float GetMaxSize(int axis) => BoundOf(axis == 0 ? m_MaxWidth : m_MaxHeight);

        private static float BoundOf(Optional<float> bound) => bound.Enabled ? Mathf.Max(0f, bound.Value) : -1f;

        private static Optional<float> Sanitize(Optional<float> bound) =>
            bound.Enabled ? bound.WithValue(Mathf.Max(0f, bound.Value)) : bound;

        // Everything resolves lazily in the getters rather than in CalculateLayoutInput*, because components
        // on one GameObject calculate in component order - a sibling text may not have measured itself yet.
        // A parent only reads these properties once every component on this object is done. Laziness settles
        // this object; a size source needs MeasureSource on top, being outside the pass that reaches here.
        private float Report(int axis, Func<ILayoutElement, float> property)
        {
            float min = GetMinSize(axis);
            float max = GetMaxSize(axis);
            if (min < 0f && max < 0f) return -1f;

            float value = Resolve(property);

            // Minimum is applied last, so it wins where the two contradict and content is never crushed
            // below the floor. The editor flags the contradiction.
            if (max >= 0f) value = Mathf.Min(value, max);
            if (min >= 0f) value = Mathf.Max(value, min);

            return value;
        }

        public RectTransform GetSizeSource()
        {
            if (!m_SizeSource.Enabled) return null;

            RectTransform source = m_SizeSource.Value;
            return source == null || source == transform ? null : source;
        }

        // LayoutUtility.GetLayoutProperty, minus this component, so the clamp reads what the object would
        // have reported without it.
        //
        // A size source measures a rect this object does not control instead. A scroll view needs that: its
        // content has to be free to overflow, so the content cannot be a layout child here, and a layout
        // group is the only thing that would otherwise make the content's size visible from up here.
        private float Resolve(Func<ILayoutElement, float> property)
        {
            RectTransform source = GetSizeSource();
            if (source != null)
            {
                MeasureSource(source);
                return Mathf.Max(0f, LayoutUtility.GetLayoutProperty(source, property, 0f));
            }

            GetComponents(typeof(ILayoutElement), m_Elements);

            float result = 0f;
            int maxPriority = int.MinValue;

            for (int i = 0; i < m_Elements.Count; i++)
            {
                ILayoutElement element = (ILayoutElement)m_Elements[i];
                if (ReferenceEquals(element, this)) continue;
                if (element is Behaviour behaviour && !behaviour.isActiveAndEnabled) continue;
                if (element.layoutPriority < maxPriority) continue;

                float value = property(element);
                if (value < 0f) continue;

                if (element.layoutPriority > maxPriority)
                {
                    maxPriority = element.layoutPriority;
                    result = value;
                }
                else if (value > result)
                {
                    result = value;
                }
            }

            m_Elements.Clear();
            return result;
        }

        // A source is measured by a layout pass this object is not part of, and CanvasUpdateRegistry orders that
        // queue by hierarchy depth alone - so a source below this object is still holding the previous pass'
        // numbers by the time the getters above read it, and the watcher only ever repairs that a pass late.
        // Settling the source's own layout root first takes the read off that ordering; a source that is itself
        // a clamp settles its own source on the way in, so a chain of them resolves depth-first.
        //
        // The frame gate keeps the four getters of one pass to a single rebuild. The static list cuts a cycle of
        // sources, where the value from before the cycle closed is the only answer that terminates.
        private void MeasureSource(RectTransform source)
        {
            if (m_MeasuredFrame == Time.frameCount) return;
            m_MeasuredFrame = Time.frameCount;

            RectTransform root = LayoutRootOf(source);
            if (s_Measuring.Contains(root)) return;

            s_Measuring.Add(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            s_Measuring.RemoveAt(s_Measuring.Count - 1);

            // Rebuilding the source routes back through the watcher and reopens the gate. What it reported is
            // what was just measured, so close it again rather than rebuild the same thing twice this pass.
            m_MeasuredFrame = Time.frameCount;
        }

        // The walk LayoutRebuilder.MarkLayoutForRebuild does: the topmost ancestor still driven by an enabled
        // layout group is the rect uGUI would have queued, and rebuilding below it only gets overwritten.
        private static RectTransform LayoutRootOf(RectTransform rect)
        {
            RectTransform root = rect;

            for (RectTransform parent = rect.parent as RectTransform; parent != null; parent = parent.parent as RectTransform)
            {
                parent.GetComponents(typeof(ILayoutGroup), s_Groups);

                bool controlled = false;
                for (int i = 0; i < s_Groups.Count; i++)
                {
                    if (s_Groups[i] is Behaviour behaviour && behaviour.isActiveAndEnabled)
                    {
                        controlled = true;
                        break;
                    }
                }

                s_Groups.Clear();
                if (!controlled) break;

                root = parent;
            }

            return root;
        }

        private bool SetProperty<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            field = value;
            SetDirty();
            return true;
        }

        public void SetDirty()
        {
            m_MeasuredFrame = -1;

            if (!IsActive()) return;

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        // Nothing in uGUI links a source to this object, so the source has to report back. Resolved through
        // GetSizeSource so a source that is off, empty or this object itself is never watched.
        private void SyncSourceWatcher()
        {
            RectTransform source = IsActive() ? GetSizeSource() : null;
            if (m_WatchedSource == source) return;

            if (m_WatchedSource != null) ClampLayoutSourceWatcher.Unwatch(m_WatchedSource, this);

            m_WatchedSource = source;

            if (m_WatchedSource != null) ClampLayoutSourceWatcher.Watch(m_WatchedSource, this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SyncSourceWatcher();
            SetDirty();
        }

        protected override void OnDisable()
        {
            SyncSourceWatcher();

            // enabled is already false by now, so IsActive() - and SetDirty with it - is a no-op. Unity's own
            // LayoutElement loses the rebuild here for that reason; Graphic marks it unconditionally instead.
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);

            base.OnDisable();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_MinWidth = Sanitize(m_MinWidth);
            m_MaxWidth = Sanitize(m_MaxWidth);
            m_MinHeight = Sanitize(m_MinHeight);
            m_MaxHeight = Sanitize(m_MaxHeight);
            SetDirty();

            // AddComponent is not allowed to run inside OnValidate.
            UnityEditor.EditorApplication.delayCall += SyncSourceWatcherDelayed;
        }

        private void SyncSourceWatcherDelayed()
        {
            if (this == null) return;

            SyncSourceWatcher();
        }
#endif
    }
}
