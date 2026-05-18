using DataKeeper.UI;
using UnityEditor;
using UnityEditor.UI;

namespace DataKeeper.Editor.UI
{
    [CustomEditor(typeof(ToggleUI), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the ToggleUI Component.
    ///   Extend this class to write a custom editor for an Toggle-derived component.
    /// </summary>
    public class ToggleUIEditor : SelectableUIEditor
    {
        SerializedProperty m_IsOnProperty;
        
        SerializedProperty m_IconProperty;
        SerializedProperty m_IconSpriteProperty;
        SerializedProperty m_IconColorProperty;
        
        SerializedProperty m_LabelProperty;
        SerializedProperty m_LabelColorProperty;
        SerializedProperty m_LabelFontStyleProperty;
        SerializedProperty m_LabelTextProperty;
        
        SerializedProperty m_OnValueChangedProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_IsOnProperty = serializedObject.FindProperty("m_IsOn");
            
            m_IconProperty = serializedObject.FindProperty("icon");
            m_IconSpriteProperty = serializedObject.FindProperty("_iconSprite");
            m_IconColorProperty = serializedObject.FindProperty("_iconColor");
            
            m_LabelProperty = serializedObject.FindProperty("label");
            m_LabelColorProperty = serializedObject.FindProperty("_labelColor");
            m_LabelFontStyleProperty = serializedObject.FindProperty("_labelFontStyle");
            m_LabelTextProperty = serializedObject.FindProperty("_labelText");
            
            m_OnValueChangedProperty = serializedObject.FindProperty("onValueChanged");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_IsOnProperty);
            
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_IconProperty);
            EditorGUILayout.PropertyField(m_IconSpriteProperty);
            EditorGUILayout.PropertyField(m_IconColorProperty);
            
            EditorGUILayout.PropertyField(m_LabelProperty);
            EditorGUILayout.PropertyField(m_LabelTextProperty);
            EditorGUILayout.PropertyField(m_LabelColorProperty);
            EditorGUILayout.PropertyField(m_LabelFontStyleProperty);
           

            EditorGUILayout.Space();

            // Draw the event notification options
            EditorGUILayout.PropertyField(m_OnValueChangedProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
