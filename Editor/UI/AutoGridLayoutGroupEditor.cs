using DataKeeper.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Editor.UI
{
    [CustomEditor(typeof(AutoGridLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class AutoGridLayoutGroupEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ColumnsLabel = new GUIContent("Columns");
        private static readonly GUIContent RowsLabel = new GUIContent("Rows");
        private static readonly GUIContent CellSizeLabel = new GUIContent("Cell Size");

        private SerializedProperty m_Padding;
        private SerializedProperty m_ChildAlignment;
        private SerializedProperty m_StartCorner;
        private SerializedProperty m_StartAxis;
        private SerializedProperty m_Spacing;
        private SerializedProperty m_Constraint;
        private SerializedProperty m_ConstraintCount;
        private SerializedProperty m_CellSizeMode;
        private SerializedProperty m_AspectRatio;

        protected virtual void OnEnable()
        {
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_StartCorner = serializedObject.FindProperty("m_StartCorner");
            m_StartAxis = serializedObject.FindProperty("m_StartAxis");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_Constraint = serializedObject.FindProperty("m_Constraint");
            m_ConstraintCount = serializedObject.FindProperty("m_ConstraintCount");
            m_CellSizeMode = serializedObject.FindProperty("m_CellSizeMode");
            m_AspectRatio = serializedObject.FindProperty("m_AspectRatio");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_ChildAlignment);
            EditorGUILayout.PropertyField(m_StartCorner);
            EditorGUILayout.PropertyField(m_StartAxis);
            EditorGUILayout.PropertyField(m_Spacing);

            EditorGUILayout.PropertyField(m_Constraint);
            EditorGUI.indentLevel++;
            DrawConstraintCounts();
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(m_CellSizeMode);
            if (!m_CellSizeMode.hasMultipleDifferentValues &&
                (AutoGridLayoutGroup.CellSizeMode)m_CellSizeMode.enumValueIndex == AutoGridLayoutGroup.CellSizeMode.AspectRatio)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AspectRatio);
                EditorGUI.indentLevel--;
            }

            if (!serializedObject.isEditingMultipleObjects)
            {
                AutoGridLayoutGroup grid = (AutoGridLayoutGroup)target;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Vector2Field(CellSizeLabel, grid.cellSize);
                }

                DrawFitterWarning(grid);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawFitterWarning(AutoGridLayoutGroup grid)
        {
            ContentSizeFitter fitter = grid.GetComponent<ContentSizeFitter>();
            if (fitter == null || !fitter.enabled) return;

            bool clash = (grid.isWidthDriven && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained) ||
                         (grid.isHeightDriven && fitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained);

            if (!clash) return;

            EditorGUILayout.HelpBox(
                "The cell size is derived from the container on that axis, so a Content Size Fitter there feeds the grid its own size and it can never grow. Leave the driven axis Unconstrained.",
                MessageType.Warning);
        }

        private void DrawConstraintCounts()
        {
            if (m_Constraint.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(m_ConstraintCount);
                return;
            }

            bool fixedColumns = (AutoGridLayoutGroup.Constraint)m_Constraint.enumValueIndex ==
                                AutoGridLayoutGroup.Constraint.FixedColumnCount;

            EditorGUILayout.PropertyField(m_ConstraintCount, fixedColumns ? ColumnsLabel : RowsLabel);
        }
    }
}
