using System.Collections.Generic;
using DataKeeper.Generic;
using DataKeeper.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Editor.UI
{
    [CustomEditor(typeof(ClampLayoutElement), true)]
    [CanEditMultipleObjects]
    public class ClampLayoutElementEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ResolvedMinLabel = new GUIContent("Resolved Min");
        private static readonly GUIContent ResolvedMaxLabel = new GUIContent("Resolved Max");

        private static readonly List<Component> Elements = new List<Component>();

        private SerializedProperty m_MinWidth;
        private SerializedProperty m_MaxWidth;
        private SerializedProperty m_MinHeight;
        private SerializedProperty m_MaxHeight;
        private SerializedProperty m_SizeSource;
        private SerializedProperty m_LayoutPriority;

        protected virtual void OnEnable()
        {
            m_MinWidth = serializedObject.FindProperty("m_MinWidth");
            m_MaxWidth = serializedObject.FindProperty("m_MaxWidth");
            m_MinHeight = serializedObject.FindProperty("m_MinHeight");
            m_MaxHeight = serializedObject.FindProperty("m_MaxHeight");
            m_SizeSource = serializedObject.FindProperty("m_SizeSource");
            m_LayoutPriority = serializedObject.FindProperty("m_LayoutPriority");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_MinWidth);
            EditorGUILayout.PropertyField(m_MaxWidth);
            EditorGUILayout.PropertyField(m_MinHeight);
            EditorGUILayout.PropertyField(m_MaxHeight);
            EditorGUILayout.PropertyField(m_SizeSource);
            EditorGUILayout.PropertyField(m_LayoutPriority);

            if (!serializedObject.isEditingMultipleObjects)
            {
                ClampLayoutElement element = (ClampLayoutElement)target;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Vector2Field(ResolvedMinLabel,
                        new Vector2(element.GetMinSize(0), element.GetMinSize(1)));
                    EditorGUILayout.Vector2Field(ResolvedMaxLabel,
                        new Vector2(element.GetMaxSize(0), element.GetMaxSize(1)));
                }

                DrawContradictionWarning(element);
                DrawSizeSourceWarnings(element);
                DrawSourceWarning(element);
                DrawParentWarnings(element);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawContradictionWarning(ClampLayoutElement element)
        {
            if (!Contradicts(element, 0) && !Contradicts(element, 1)) return;

            EditorGUILayout.HelpBox(
                "A minimum is larger than the maximum on the same axis. The minimum wins, so the maximum has no effect on that axis.",
                MessageType.Warning);
        }

        private static bool Contradicts(ClampLayoutElement element, int axis)
        {
            float min = element.GetMinSize(axis);
            float max = element.GetMaxSize(axis);
            return min >= 0f && max >= 0f && min > max;
        }

        private static void DrawSizeSourceWarnings(ClampLayoutElement element)
        {
            Optional<RectTransform> authored = element.sizeSource;
            if (!authored.Enabled) return;

            RectTransform source = authored.Value;

            if (source == null)
            {
                EditorGUILayout.HelpBox(
                    "Size Source is on but empty, so the size is still measured from this object.",
                    MessageType.Info);
                return;
            }

            if (source == element.transform)
            {
                EditorGUILayout.HelpBox(
                    "Size Source points at this object's own Rect Transform, which would measure itself. It is ignored.",
                    MessageType.Error);
                return;
            }

            if (element.transform.IsChildOf(source))
            {
                EditorGUILayout.HelpBox(
                    "Size Source is an ancestor of this object. Its size follows this one, so measuring it feeds the result back into itself.",
                    MessageType.Error);
                return;
            }

            if (source.IsChildOf(element.transform) && element.GetComponent<LayoutGroup>() != null)
            {
                EditorGUILayout.HelpBox(
                    "This object has a layout group, so it resizes the source to fit the bounded size and the source can never overflow. Remove the layout group and anchor the source instead - that is the point of a size source.",
                    MessageType.Warning);
            }
        }

        private static void DrawSourceWarning(ClampLayoutElement element)
        {
            if (element.GetMaxSize(0) < 0f && element.GetMaxSize(1) < 0f) return;
            if (element.GetSizeSource() != null) return;

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
                "Nothing on this object reports a size, so there is nothing to cap - a maximum is a ceiling, not a source. Add a Layout Element, a Text, an Image or a Layout Group. A minimum works on its own.",
                MessageType.Info);
        }

        private static void DrawParentWarnings(ClampLayoutElement element)
        {
            bool boundWidth = element.GetMinSize(0) >= 0f || element.GetMaxSize(0) >= 0f;
            bool boundHeight = element.GetMinSize(1) >= 0f || element.GetMaxSize(1) >= 0f;
            if (!boundWidth && !boundHeight) return;

            Transform parent = element.transform.parent;
            if (parent == null) return;

            if (parent.TryGetComponent(out HorizontalOrVerticalLayoutGroup group))
            {
                if ((boundWidth && !group.childControlWidth) || (boundHeight && !group.childControlHeight))
                {
                    EditorGUILayout.HelpBox(
                        "The parent layout group has Child Control Size off for a bounded axis, so it reads this object's Rect Transform directly and never queries the layout element. Turn Child Control Size on for that axis.",
                        MessageType.Warning);
                }

                bool cappedWidth = element.GetMaxSize(0) >= 0f;
                bool cappedHeight = element.GetMaxSize(1) >= 0f;

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
                    "The parent is a grid, which gives every child the cell size. The bounds have no effect - set the cell size on the grid instead.",
                    MessageType.Warning);
            }
        }
    }
}
