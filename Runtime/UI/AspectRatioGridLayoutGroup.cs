using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Aspect Ratio Grid Layout Group")]
    public class AspectRatioGridLayoutGroup : LayoutGroup
    {
        public enum LayoutType
        {
            FixedRows = 0,
            FixedColumns = 1
        }

        public LayoutType layoutType = LayoutType.FixedRows;
        [Min(1)] public int fixedCount = 1;
        public float aspectRatio = 1f;
        public Vector2 spacing = Vector2.zero;

        private float m_CellWidth;
        private float m_CellHeight;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            
            int childCount = rectChildren.Count;
            if (childCount == 0) return;

            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            int rows = fixedCount;
            int columns = fixedCount;
            
            if (layoutType == LayoutType.FixedRows)
            {
                columns = Mathf.CeilToInt((float)childCount / fixedCount);
            }
            else // FixedColumns
            {
                rows = Mathf.CeilToInt((float)childCount / fixedCount);
            }

            float totalSpacingWidth = spacing.x * (columns - 1);
            float totalSpacingHeight = spacing.y * (rows - 1);

            if (layoutType == LayoutType.FixedRows)
            {
                m_CellHeight = (parentHeight - padding.vertical - totalSpacingHeight) / rows;
                m_CellWidth = m_CellHeight * aspectRatio;
            }
            else // FixedColumns
            {
                m_CellWidth = (parentWidth - padding.horizontal - totalSpacingWidth) / columns;
                m_CellHeight = m_CellWidth / aspectRatio;
            }

            for (int i = 0; i < childCount; i++)
            {
                int rowIndex = layoutType == LayoutType.FixedRows ? i % rows : i / columns;
                int columnIndex = layoutType == LayoutType.FixedRows ? i / rows : i % columns;

                var item = rectChildren[i];

                var xPos = padding.left + (m_CellWidth + spacing.x) * columnIndex;
                var yPos = padding.top + (m_CellHeight + spacing.y) * rowIndex;

                SetChildAlongAxis(item, 0, xPos, m_CellWidth);
                SetChildAlongAxis(item, 1, yPos, m_CellHeight);
            }

            // Set the preferred width and height
            SetLayoutInputForAxis(
                padding.horizontal + (m_CellWidth * columns) + (spacing.x * (columns - 1)),
                padding.horizontal + (m_CellWidth * columns) + (spacing.x * (columns - 1)),
                -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int childCount = rectChildren.Count;
            if (childCount == 0) return;

            int rows = fixedCount;
            int columns = fixedCount;
            
            if (layoutType == LayoutType.FixedRows)
            {
                columns = Mathf.CeilToInt((float)childCount / fixedCount);
            }
            else // FixedColumns
            {
                rows = Mathf.CeilToInt((float)childCount / fixedCount);
            }

            float totalSpacingHeight = spacing.y * (rows - 1);

            float minHeight = padding.vertical + (m_CellHeight * rows) + totalSpacingHeight;
            float preferredHeight = minHeight;

            SetLayoutInputForAxis(minHeight, preferredHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }
    }
}