using System.Collections.Generic;
using DataKeeper.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Editor.UI
{
    [CustomEditor(typeof(MaxSizeLayoutElement), true)]
    [CanEditMultipleObjects]
    public class MaxSizeLayoutElementEditor : UnityEditor.Editor
    {
        private static readonly GUIContent MaxWidthLabel = new GUIContent("Max Width");
        private static readonly GUIContent MaxHeightLabel = new GUIContent("Max Height");
        private static readonly GUIContent MaxWidthFractionLabel = new GUIContent("Max Width (x Canvas)");
        private static readonly GUIContent MaxHeightFractionLabel = new GUIContent("Max Height (x Canvas)");
        private static readonly GUIContent ResolvedLabel = new GUIContent("Resolved Max");

        private static readonly List<Component> Elements = new List<Component>();

        private SerializedProperty m_HorizontalMode;
        private SerializedProperty m_MaxWidth;
        private SerializedProperty m_VerticalMode;
        private SerializedProperty m_MaxHeight;
        private SerializedProperty m_LayoutPriority;

        protected virtual void OnEnable()
        {
            m_HorizontalMode = serializedObject.FindProperty("m_HorizontalMode");
            m_MaxWidth = serializedObject.FindProperty("m_MaxWidth");
            m_VerticalMode = serializedObject.FindProperty("m_VerticalMode");
            m_MaxHeight = serializedObject.FindProperty("m_MaxHeight");
            m_LayoutPriority = serializedObject.FindProperty("m_LayoutPriority");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawAxis(m_HorizontalMode, m_MaxWidth, MaxWidthLabel, MaxWidthFractionLabel);
            DrawAxis(m_VerticalMode, m_MaxHeight, MaxHeightLabel, MaxHeightFractionLabel);
            EditorGUILayout.PropertyField(m_LayoutPriority);

            if (!serializedObject.isEditingMultipleObjects)
            {
                MaxSizeLayoutElement element = (MaxSizeLayoutElement)target;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Vector2Field(ResolvedLabel,
                        new Vector2(element.GetMaxSize(0), element.GetMaxSize(1)));
                }

                DrawSourceWarning(element);
                DrawParentWarnings(element);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawAxis(SerializedProperty mode, SerializedProperty value, GUIContent absoluteLabel, GUIContent fractionLabel)
        {
            EditorGUILayout.PropertyField(mode);

            if (mode.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(value);
                EditorGUI.indentLevel--;
                return;
            }

            MaxSizeLayoutElement.MaxMode current = (MaxSizeLayoutElement.MaxMode)mode.enumValueIndex;
            if (current == MaxSizeLayoutElement.MaxMode.Unconstrained) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(value,
                current == MaxSizeLayoutElement.MaxMode.CanvasFraction ? fractionLabel : absoluteLabel);
            EditorGUI.indentLevel--;
        }

        private static void DrawSourceWarning(MaxSizeLayoutElement element)
        {
            if (element.GetMaxSize(0) < 0f && element.GetMaxSize(1) < 0f) return;

            element.GetComponents(typeof(ILayoutElement), Elements);

            bool hasSource = false;
            for (int i = 0; i < Elements.Count; i++)
            {
                if (ReferenceEquals(Elements[i], element)) continue;
                hasSource = true;
                break;
            }

            Elements.Clear();
            if (hasSource) return;

            EditorGUILayout.HelpBox(
                "Nothing on this object reports a size, so there is nothing to cap - a maximum is a ceiling, not a source. Add a Layout Element, a Text, an Image or a Layout Group.",
                MessageType.Info);
        }

        private static void DrawParentWarnings(MaxSizeLayoutElement element)
        {
            bool cappedWidth = element.GetMaxSize(0) >= 0f;
            bool cappedHeight = element.GetMaxSize(1) >= 0f;
            if (!cappedWidth && !cappedHeight) return;

            Transform parent = element.transform.parent;
            if (parent == null) return;

            if (parent.TryGetComponent(out HorizontalOrVerticalLayoutGroup group))
            {
                if ((cappedWidth && !group.childControlWidth) || (cappedHeight && !group.childControlHeight))
                {
                    EditorGUILayout.HelpBox(
                        "The parent layout group has Child Control Size off for a capped axis, so it reads this object's Rect Transform directly and never queries the layout element. Turn Child Control Size on for that axis.",
                        MessageType.Warning);
                }

                if ((cappedWidth && group.childForceExpandWidth) || (cappedHeight && group.childForceExpandHeight))
                {
                    EditorGUILayout.HelpBox(
                        "The parent layout group has Child Force Expand on for a capped axis. Force expand raises the flexible size back to 1, which overrides the cap. Turn it off for that axis.",
                        MessageType.Warning);
                }

                return;
            }

            if (parent.GetComponent<GridLayoutGroup>() != null || parent.GetComponent<AutoGridLayoutGroup>() != null)
            {
                EditorGUILayout.HelpBox(
                    "The parent is a grid, which gives every child the cell size. The cap has no effect - set the cell size on the grid instead.",
                    MessageType.Warning);
            }
        }
    }
}
