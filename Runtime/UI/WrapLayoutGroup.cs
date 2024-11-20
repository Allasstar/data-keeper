using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [ExecuteInEditMode]
    [AddComponentMenu("DataKeeper/UI/Wrap Layout Group")]
    public class WrapLayoutGroup : LayoutGroup
    {
        public enum Axis { Horizontal, Vertical }
        public Axis mainAxis = Axis.Horizontal;
        
        public Vector2 spacing = Vector2.zero;
        public bool childForceExpandWidth = false;
        public bool childForceExpandHeight = false;


        private float m_ContentWidth;
        private float m_ContentHeight;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        
            if (mainAxis == Axis.Horizontal)
                CalculateLayoutHorizontal();
            else
                CalculateLayoutVertical();
        }

        public override void SetLayoutHorizontal()
        {
            SetLayout(0);
        }

        public override void SetLayoutVertical()
        {
            SetLayout(1);
        }

        public override void CalculateLayoutInputVertical()
        {
            if (mainAxis == Axis.Horizontal)
                SetLayoutInputForAxis(m_ContentHeight, m_ContentHeight, 0, 1);
            else
                SetLayoutInputForAxis(m_ContentWidth, m_ContentWidth, 0, 0);
        }

        private void CalculateLayoutHorizontal()
        {
            float width = rectTransform.rect.width - padding.left - padding.right;
            float x = padding.left;
            float y = padding.top;
            float rowHeight = 0f;
            m_ContentHeight = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                var item = rectChildren[i];
            
                var itemWidth = LayoutUtility.GetPreferredWidth(item);
                var itemHeight = LayoutUtility.GetPreferredHeight(item);

                if (x + itemWidth > width)
                {
                    x = padding.left;
                    y += rowHeight + spacing.y;
                    rowHeight = 0;
                }

                rowHeight = Mathf.Max(rowHeight, itemHeight);
                x += itemWidth + spacing.x;
            }

            m_ContentHeight = y + rowHeight + padding.bottom;
        }

        private void CalculateLayoutVertical()
        {
            float height = rectTransform.rect.height - padding.top - padding.bottom;
            float x = padding.left;
            float y = padding.top;
            float columnWidth = 0f;
            m_ContentWidth = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                var item = rectChildren[i];
            
                var itemWidth = LayoutUtility.GetPreferredWidth(item);
                var itemHeight = LayoutUtility.GetPreferredHeight(item);

                if (y + itemHeight > height)
                {
                    y = padding.top;
                    x += columnWidth + spacing.x;
                    columnWidth = 0;
                }

                columnWidth = Mathf.Max(columnWidth, itemWidth);
                y += itemHeight + spacing.y;
            }

            m_ContentWidth = x + columnWidth + padding.right;
        }

        private void SetLayout(int axis)
        {
            float width = rectTransform.rect.width - padding.left - padding.right;
            float height = rectTransform.rect.height - padding.top - padding.bottom;
            float x = padding.left;
            float y = padding.top;
            float rowHeight = 0f;
            float columnWidth = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                var item = rectChildren[i];
            
                var itemWidth = LayoutUtility.GetPreferredWidth(item);
                var itemHeight = LayoutUtility.GetPreferredHeight(item);

                if (mainAxis == Axis.Horizontal)
                {
                    if (x + itemWidth > width)
                    {
                        x = padding.left;
                        y += rowHeight + spacing.y;
                        rowHeight = 0;
                    }

                    SetChildAlongAxis(item, 0, x, itemWidth);
                    SetChildAlongAxis(item, 1, y, itemHeight);

                    rowHeight = Mathf.Max(rowHeight, itemHeight);
                    x += itemWidth + spacing.x;
                }
                else
                {
                    if (y + itemHeight > height)
                    {
                        y = padding.top;
                        x += columnWidth + spacing.x;
                        columnWidth = 0;
                    }

                    SetChildAlongAxis(item, 0, x, itemWidth);
                    SetChildAlongAxis(item, 1, y, itemHeight);

                    columnWidth = Mathf.Max(columnWidth, itemWidth);
                    y += itemHeight + spacing.y;
                }
            }
        }
    }
}