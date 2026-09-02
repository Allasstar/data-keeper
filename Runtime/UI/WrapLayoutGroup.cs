using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Wrap Layout Group")]
    public class WrapLayoutGroup : LayoutGroup
    {
        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        [SerializeField] private Axis mainAxis = Axis.Horizontal;
        [SerializeField] private Vector2 spacing = Vector2.zero;
        [SerializeField] private bool childForceExpandWidth = false;
        [SerializeField] private bool childForceExpandHeight = false;

        public Axis MainAxis { get => mainAxis; set => SetProperty(ref mainAxis, value); }
        public Vector2 Spacing { get => spacing; set => SetProperty(ref spacing, value); }
        public bool ChildForceExpandWidth { get => childForceExpandWidth; set => SetProperty(ref childForceExpandWidth, value); }
        public bool ChildForceExpandHeight { get => childForceExpandHeight; set => SetProperty(ref childForceExpandHeight, value); }

        private readonly List<Vector2> m_Sizes = new List<Vector2>();
        private readonly List<Vector2> m_Positions = new List<Vector2>();
        private readonly List<int> m_LineStarts = new List<int>();

        private float m_ContentCross;

        private bool IsHorizontal => mainAxis == Axis.Horizontal;
        private int MainAxisIndex => IsHorizontal ? 0 : 1;
        private int CrossAxisIndex => IsHorizontal ? 1 : 0;
        private float MainSpacing => IsHorizontal ? spacing.x : spacing.y;
        private float CrossSpacing => IsHorizontal ? spacing.y : spacing.x;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            if (IsHorizontal)
            {
                // Where the lines break is only known once the width is final, so the reported width is
                // the unwrapped single line one - what the layout would need to not wrap at all.
                GetChildExtremes(0, out float largest, out float total);
                SetLayoutInput(largest + padding.horizontal, total + padding.horizontal, -1, 0);
            }
            else
            {
                // A vertical flow wraps on the height, which is final only in SetLayoutVertical, so the
                // width reported here follows the previous height and settles on the next rebuild.
                FlowMain();
                FlowCross();
                SetLayoutInput(m_ContentCross + padding.horizontal, m_ContentCross + padding.horizontal, -1, 0);
            }
        }

        public override void SetLayoutHorizontal()
        {
            NormalizeChildAnchors();

            if (IsHorizontal) FlowMain();
            ApplyAxis(0);
        }

        public override void CalculateLayoutInputVertical()
        {
            if (IsHorizontal)
            {
                // Children finished their own vertical calculation against the width set above, so this
                // is the first phase where their heights are worth measuring.
                FlowCross();
                SetLayoutInput(m_ContentCross + padding.vertical, m_ContentCross + padding.vertical, -1, 1);
            }
            else
            {
                GetChildExtremes(1, out float largest, out float total);
                SetLayoutInput(largest + padding.vertical, total + padding.vertical, -1, 1);
            }
        }

        public override void SetLayoutVertical()
        {
            if (!IsHorizontal)
            {
                // Own height and the children's heights are both final only now.
                FlowMain();
                FlowCross();
                ApplyAxis(0);
            }

            ApplyAxis(1);
        }

        // A layout group collapses its children onto a single anchor point, and a stretched child loses
        // the size it was showing when that happens - bake it into sizeDelta before the first placement.
        private void NormalizeChildAnchors()
        {
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child.anchorMin == child.anchorMax) continue;

                Vector2 size = child.rect.size;
                child.anchorMin = Vector2.up;
                child.anchorMax = Vector2.up;
                child.sizeDelta = size;
            }
        }

        // The child's own rect is what gets packed; its preferred size only stands in for a child that has
        // no usable rect, so a zero sized child still lands somewhere sensible instead of on top of its neighbour.
        private static float MeasureChild(RectTransform child, int axis)
        {
            float rectSize = axis == 0 ? child.rect.width : child.rect.height;
            if (rectSize > 0f) return rectSize;

            return axis == 0
                ? LayoutUtility.GetPreferredWidth(child)
                : LayoutUtility.GetPreferredHeight(child);
        }

        private void GetChildExtremes(int axis, out float largestChild, out float total)
        {
            largestChild = 0f;
            total = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                float size = MeasureChild(rectChildren[i], axis);
                largestChild = Mathf.Max(largestChild, size);
                total += size;
            }

            total += Mathf.Max(0, rectChildren.Count - 1) * (axis == 0 ? spacing.x : spacing.y);
        }

        private void FlowMain()
        {
            m_Sizes.Clear();
            m_Positions.Clear();
            m_LineStarts.Clear();

            int count = rectChildren.Count;
            if (count == 0) return;

            int axis = MainAxisIndex;
            float available = IsHorizontal
                ? rectTransform.rect.width - padding.horizontal
                : rectTransform.rect.height - padding.vertical;
            float mainSpacing = MainSpacing;

            float cursor = 0f;
            int lineStart = 0;
            m_LineStarts.Add(0);

            for (int i = 0; i < count; i++)
            {
                float main = MeasureChild(rectChildren[i], axis);

                // A line always keeps its first child, otherwise an oversized child would wrap forever.
                if (i > lineStart && cursor + main > available + 0.001f)
                {
                    LayoutLineMain(lineStart, i, cursor - mainSpacing, available);
                    lineStart = i;
                    m_LineStarts.Add(i);
                    cursor = 0f;
                }

                Vector2 size = Vector2.zero;
                size[axis] = main;
                m_Sizes.Add(size);
                m_Positions.Add(Vector2.zero);

                cursor += main + mainSpacing;
            }

            LayoutLineMain(lineStart, count, cursor - mainSpacing, available);
        }

        private void LayoutLineMain(int start, int end, float lineSize, float available)
        {
            int axis = MainAxisIndex;
            bool expand = IsHorizontal ? childForceExpandWidth : childForceExpandHeight;
            float alignment = GetAlignmentOnAxis(axis);
            float mainSpacing = MainSpacing;

            float surplus = Mathf.Max(0f, available - lineSize);
            float extraPerChild = expand ? surplus / (end - start) : 0f;
            float cursor = (IsHorizontal ? padding.left : padding.top) + (expand ? 0f : surplus * alignment);

            for (int i = start; i < end; i++)
            {
                Vector2 position = m_Positions[i];

                // Force expand spreads the line's leftover space over the children's slots; the child keeps
                // its own size and is aligned inside the slot it got.
                float slot = m_Sizes[i][axis] + extraPerChild;
                position[axis] = cursor + (slot - m_Sizes[i][axis]) * alignment;
                m_Positions[i] = position;

                cursor += slot + mainSpacing;
            }
        }

        private void FlowCross()
        {
            int count = rectChildren.Count;
            if (m_Sizes.Count != count) FlowMain();

            m_ContentCross = 0f;
            if (count == 0) return;

            int axis = CrossAxisIndex;
            float alignment = GetAlignmentOnAxis(axis);
            float crossSpacing = CrossSpacing;

            float linePosition = 0f;

            for (int line = 0; line < m_LineStarts.Count; line++)
            {
                int start = m_LineStarts[line];
                int end = line + 1 < m_LineStarts.Count ? m_LineStarts[line + 1] : count;

                float lineSize = 0f;
                for (int i = start; i < end; i++)
                {
                    Vector2 size = m_Sizes[i];
                    size[axis] = MeasureChild(rectChildren[i], axis);
                    m_Sizes[i] = size;
                    lineSize = Mathf.Max(lineSize, size[axis]);
                }

                for (int i = start; i < end; i++)
                {
                    Vector2 position = m_Positions[i];
                    position[axis] = linePosition + (lineSize - m_Sizes[i][axis]) * alignment;
                    m_Positions[i] = position;
                }

                linePosition += lineSize + crossSpacing;
            }

            m_ContentCross = linePosition - crossSpacing;
        }

        private void ApplyAxis(int axis)
        {
            int count = rectChildren.Count;
            if (m_Positions.Count != count)
            {
                FlowMain();
                FlowCross();
            }

            // Cross axis positions are stored relative to the content box, the main axis ones are final.
            float crossOffset = axis == CrossAxisIndex ? GetStartOffset(axis, m_ContentCross) : 0f;

            // Only the position is set - the child's size stays its own, and stays editable.
            for (int i = 0; i < count; i++)
            {
                SetChildAlongAxis(rectChildren[i], axis, m_Positions[i][axis] + crossOffset);
            }
        }

        // SetLayoutInputForAxis gained a maximum between min and preferred in Unity 6.6. This group has no
        // upper bound of its own, so it reports the unbounded default there.
        private void SetLayoutInput(float min, float preferred, float flexible, int axis)
        {
#if UNITY_6000_6_OR_NEWER
            SetLayoutInputForAxis(min, LayoutUtility.DefaultMaxSize, preferred, flexible, axis);
#else
            SetLayoutInputForAxis(min, preferred, flexible, axis);
#endif
        }

    }
}
