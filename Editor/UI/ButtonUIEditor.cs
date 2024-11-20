using DataKeeper.UI;
using UnityEditor;
using UnityEditor.UI;

namespace DataKeeper.Editor.UI
{
    [CustomEditor(typeof(ButtonUI), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the Button Component.
    ///   Extend this class to write a custom editor for an Button-derived component.
    /// </summary>
    public class ButtonEditor : SelectableUIEditor
    {
        SerializedProperty m_textProperty;
        SerializedProperty m_OnClickProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_textProperty = serializedObject.FindProperty("label");
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(m_textProperty);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_OnClickProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
