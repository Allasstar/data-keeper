using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Auto Grid Layout Group")]
    public class AutoGridLayoutGroup : LayoutGroup
    {
        public enum Corner
        {
            UpperLeft = 0,
            UpperRight = 1,
            LowerLeft = 2,
            LowerRight = 3
        }

        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        public enum Constraint
        {
            FixedColumnCount = 0,
            FixedRowCount = 1
        }

        public enum CellSizeMode
        {
            AspectRatio = 0,
            Fill = 1
        }

        private const float MinAspectRatio = 0.0001f;

        [SerializeField] protected Corner m_StartCorner = Corner.UpperLeft;
        [SerializeField] protected Axis m_StartAxis = Axis.Horizontal;
        [SerializeField] protected Constraint m_Constraint = Constraint.FixedColumnCount;
        [SerializeField, Min(1)] protected int m_ConstraintCount = 2;
        [SerializeField] protected CellSizeMode m_CellSizeMode = CellSizeMode.AspectRatio;
        [SerializeField, Min(MinAspectRatio)] protected float m_AspectRatio = 1f;
        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;

        public Corner startCorner { get => m_StartCorner; set => SetProperty(ref m_StartCorner, value); }
        public Axis startAxis { get => m_StartAxis; set => SetProperty(ref m_StartAxis, value); }
        public Constraint constraint { get => m_Constraint; set => SetProperty(ref m_Constraint, value); }
        public int constraintCount { get => m_ConstraintCount; set => SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); }
        public CellSizeMode cellSizeMode { get => m_CellSizeMode; set => SetProperty(ref m_CellSizeMode, value); }
        public float aspectRatio { get => m_AspectRatio; set => SetProperty(ref m_AspectRatio, Mathf.Max(MinAspectRatio, value)); }
        public Vector2 spacing { get => m_Spacing; set => SetProperty(ref m_Spacing, value); }

        public Vector2 cellSize => m_CellSize;
        public int columns => m_Columns;
        public int rows => m_Rows;

        private Vector2 m_CellSize;
        private int m_Columns = 1;
        private int m_Rows = 1;

        // The container size feeds the cell size on these axes, so it must not be reported back as a minimum.
        public bool isWidthDriven => m_CellSizeMode == CellSizeMode.Fill || m_Constraint == Constraint.FixedColumnCount;
        public bool isHeightDriven => m_CellSizeMode == CellSizeMode.Fill || m_Constraint == Constraint.FixedRowCount;

        protected AutoGridLayoutGroup()
        {
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ConstraintCount = Mathf.Max(1, m_ConstraintCount);
            m_AspectRatio = Mathf.Max(MinAspectRatio, m_AspectRatio);
        }
#endif

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            UpdateGrid();
            UpdateCellSize();

            float width = padding.horizontal + m_CellSize.x * m_Columns + m_Spacing.x * (m_Columns - 1);
            SetLayoutInputForAxis(isWidthDriven ? 0f : width, width, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float height = padding.vertical + m_CellSize.y * m_Rows + m_Spacing.y * (m_Rows - 1);
            SetLayoutInputForAxis(isHeightDriven ? 0f : height, height, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            UpdateCellSize();
            PlaceCells();
        }

        public override void SetLayoutVertical()
        {
            // Own height is final only now, and a height driven cell size also feeds the cell width,
            // so both axes are placed again instead of only the vertical one.
            UpdateCellSize();
            PlaceCells();
        }

        private void UpdateGrid()
        {
            int count = rectChildren.Count;

            if (m_Constraint == Constraint.FixedColumnCount)
            {
                m_Columns = Mathf.Max(1, m_ConstraintCount);
                m_Rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)m_Columns - 0.001f));
            }
            else
            {
                m_Rows = Mathf.Max(1, m_ConstraintCount);
                m_Columns = Mathf.Max(1, Mathf.CeilToInt(count / (float)m_Rows - 0.001f));
            }
        }

        private void UpdateCellSize()
        {
            Rect rect = rectTransform.rect;
            float cellWidth = Mathf.Max(0f, (rect.width - padding.horizontal - m_Spacing.x * (m_Columns - 1)) / m_Columns);
            float cellHeight = Mathf.Max(0f, (rect.height - padding.vertical - m_Spacing.y * (m_Rows - 1)) / m_Rows);

            if (m_CellSizeMode == CellSizeMode.Fill)
            {
                m_CellSize.x = cellWidth;
                m_CellSize.y = cellHeight;
                return;
            }

            float aspect = Mathf.Max(MinAspectRatio, m_AspectRatio);

            if (m_Constraint == Constraint.FixedColumnCount)
            {
                m_CellSize.x = cellWidth;
                m_CellSize.y = cellWidth / aspect;
            }
            else
            {
                m_CellSize.y = cellHeight;
                m_CellSize.x = cellHeight * aspect;
            }
        }

        private void PlaceCells()
        {
            int count = rectChildren.Count;
            if (count == 0) return;

            int cellsPerMainAxis;
            int actualColumns;
            int actualRows;

            if (m_StartAxis == Axis.Horizontal)
            {
                cellsPerMainAxis = m_Columns;
                actualColumns = Mathf.Clamp(m_Columns, 1, count);
                actualRows = Mathf.Clamp(m_Rows, 1, Mathf.CeilToInt(count / (float)cellsPerMainAxis));
            }
            else
            {
                cellsPerMainAxis = m_Rows;
                actualRows = Mathf.Clamp(m_Rows, 1, count);
                actualColumns = Mathf.Clamp(m_Columns, 1, Mathf.CeilToInt(count / (float)cellsPerMainAxis));
            }

            float requiredWidth = actualColumns * m_CellSize.x + (actualColumns - 1) * m_Spacing.x;
            float requiredHeight = actualRows * m_CellSize.y + (actualRows - 1) * m_Spacing.y;

            float startX = GetStartOffset(0, requiredWidth);
            float startY = GetStartOffset(1, requiredHeight);

            int cornerX = (int)m_StartCorner % 2;
            int cornerY = (int)m_StartCorner / 2;

            for (int i = 0; i < count; i++)
            {
                int columnIndex = m_StartAxis == Axis.Horizontal ? i % cellsPerMainAxis : i / cellsPerMainAxis;
                int rowIndex = m_StartAxis == Axis.Horizontal ? i / cellsPerMainAxis : i % cellsPerMainAxis;

                if (cornerX == 1) columnIndex = actualColumns - 1 - columnIndex;
                if (cornerY == 1) rowIndex = actualRows - 1 - rowIndex;

                RectTransform child = rectChildren[i];
                SetChildAlongAxis(child, 0, startX + (m_CellSize.x + m_Spacing.x) * columnIndex, m_CellSize.x);
                SetChildAlongAxis(child, 1, startY + (m_CellSize.y + m_Spacing.y) * rowIndex, m_CellSize.y);
            }
        }
    }
}
