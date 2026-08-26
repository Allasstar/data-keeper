using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Max Size Layout Element")]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class MaxSizeLayoutElement : UIBehaviour, ILayoutElement
    {
        public enum MaxMode
        {
            Unconstrained = 0,
            Absolute = 1,
            CanvasFraction = 2
        }

        // LayoutElement reports at 1, Text / TMP_Text / LayoutGroup at 0. LayoutUtility resolves equal
        // priorities to the largest value, so a size can only ever be clamped from above all of them.
        private const int DefaultLayoutPriority = 2;

        private static readonly Func<ILayoutElement, float> MinWidthOf = e => e.minWidth;
        private static readonly Func<ILayoutElement, float> MinHeightOf = e => e.minHeight;
        private static readonly Func<ILayoutElement, float> PreferredWidthOf = e => e.preferredWidth;
        private static readonly Func<ILayoutElement, float> PreferredHeightOf = e => e.preferredHeight;

        [SerializeField] private MaxMode m_HorizontalMode = MaxMode.Unconstrained;
        [SerializeField, Min(0f)] private float m_MaxWidth = 300f;
        [SerializeField] private MaxMode m_VerticalMode = MaxMode.Unconstrained;
        [SerializeField, Min(0f)] private float m_MaxHeight = 300f;
        [SerializeField] private int m_LayoutPriority = DefaultLayoutPriority;

        public MaxMode horizontalMode { get => m_HorizontalMode; set => SetProperty(ref m_HorizontalMode, value); }
        public MaxMode verticalMode { get => m_VerticalMode; set => SetProperty(ref m_VerticalMode, value); }
        public float maxWidth { get => m_MaxWidth; set => SetProperty(ref m_MaxWidth, Mathf.Max(0f, value)); }
        public float maxHeight { get => m_MaxHeight; set => SetProperty(ref m_MaxHeight, Mathf.Max(0f, value)); }
        public int layoutPriority { get => m_LayoutPriority; set => SetProperty(ref m_LayoutPriority, value); }

        private readonly List<Component> m_Elements = new List<Component>();

        [NonSerialized] private Canvas m_Canvas;

        protected MaxSizeLayoutElement()
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
        public float minWidth => Report(0, MinWidthOf);
        public float minHeight => Report(1, MinHeightOf);
        public float preferredWidth => Report(0, PreferredWidthOf);
        public float preferredHeight => Report(1, PreferredHeightOf);

        // A layout group adds flexible space on top of the preferred size on its main axis, and treats a
        // flexible child as having no upper bound on its cross axis. Either way the clamp needs it gone.
        public float flexibleWidth => GetMaxSize(0) < 0f ? -1f : 0f;
        public float flexibleHeight => GetMaxSize(1) < 0f ? -1f : 0f;

        public float GetMaxSize(int axis)
        {
            MaxMode mode = axis == 0 ? m_HorizontalMode : m_VerticalMode;
            if (mode == MaxMode.Unconstrained) return -1f;

            float value = Mathf.Max(0f, axis == 0 ? m_MaxWidth : m_MaxHeight);
            if (mode == MaxMode.Absolute) return value;

            Canvas canvas = ResolveCanvas();
            if (canvas == null) return -1f;

            Rect canvasRect = ((RectTransform)canvas.transform).rect;
            return (axis == 0 ? canvasRect.width : canvasRect.height) * value;
        }

        // Everything resolves lazily in the getters rather than in CalculateLayoutInput*, because components
        // on one GameObject calculate in component order - a sibling text may not have measured itself yet.
        // A parent only reads these properties once every component on this object is done.
        private float Report(int axis, Func<ILayoutElement, float> property)
        {
            float max = GetMaxSize(axis);
            if (max < 0f) return -1f;

            return Mathf.Min(Resolve(property), max);
        }

        // LayoutUtility.GetLayoutProperty, minus this component, so the clamp reads what the object would
        // have reported without it.
        private float Resolve(Func<ILayoutElement, float> property)
        {
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

        // A fraction of the canvas is safe to clamp against; a fraction of the parent would not be, since
        // the parent size can itself follow this rect.
        private Canvas ResolveCanvas()
        {
            if (m_Canvas == null) m_Canvas = GetComponentInParent<Canvas>();
            return m_Canvas == null ? null : m_Canvas.rootCanvas;
        }

        private void SetProperty<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;
            SetDirty();
        }

        protected void SetDirty()
        {
            if (!IsActive()) return;

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            m_Canvas = null;
            SetDirty();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            m_Canvas = null;
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_MaxWidth = Mathf.Max(0f, m_MaxWidth);
            m_MaxHeight = Mathf.Max(0f, m_MaxHeight);
            SetDirty();
        }
#endif
    }
}
