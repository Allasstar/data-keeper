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
        SerializedProperty m_GroupProperty;
        
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
            m_GroupProperty = serializedObject.FindProperty("m_Group");
            
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
            EditorGUILayout.PropertyField(m_GroupProperty);
            
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
    
    // Does not work as expected, instead used ForceUpdate() in OnValidate()
    /*
    public class ToggleUIEditor : SelectableEditor
    {
        SerializedProperty m_IsOnProperty;
        SerializedProperty m_GroupProperty;
        
        SerializedProperty m_IconProperty;
        SerializedProperty m_IconSpriteProperty;
        SerializedProperty m_IconColorProperty;
        
        SerializedProperty m_TextProperty;
        SerializedProperty m_TextColorProperty;
        
        SerializedProperty m_OnValueChangedProperty;
    
        protected override void OnEnable()
        {
            base.OnEnable();
    
            m_IsOnProperty = serializedObject.FindProperty("m_IsOn");
            m_GroupProperty = serializedObject.FindProperty("m_Group");
            
            m_IconProperty = serializedObject.FindProperty("icon");
            m_IconSpriteProperty = serializedObject.FindProperty("_iconSprite");
            m_IconColorProperty = serializedObject.FindProperty("_iconColor");
            
            m_TextProperty = serializedObject.FindProperty("text");
            m_TextColorProperty = serializedObject.FindProperty("_textColor");
            
            m_OnValueChangedProperty = serializedObject.FindProperty("onValueChanged");
        }
    
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
    
            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_IsOnProperty);
            if (EditorGUI.EndChangeCheck())
            {
                ToggleUI toggle = serializedObject.targetObject as ToggleUI;
                ToggleUIGroup group = m_GroupProperty.objectReferenceValue as ToggleUIGroup;
                
                toggle.isOn = m_IsOnProperty.boolValue;
                
                if (group != null && toggle.IsActive())
                {
                    if (toggle.isOn || (!group.AnyTogglesOn() && !group.allowSwitchOff))
                    {
                        toggle.isOn = true;
                        group.NotifyToggleOn(toggle);
                    }
                }
                
                toggle.UpdateUI();
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GroupProperty);
            if (EditorGUI.EndChangeCheck())
            {
                ToggleUI toggle = serializedObject.targetObject as ToggleUI;
                ToggleUIGroup group = m_GroupProperty.objectReferenceValue as ToggleUIGroup;
                toggle.group = group;
                // toggle.UpdateUI();
            }
            
            EditorGUILayout.Space();
    
            EditorGUI.BeginChangeCheck();
    
            EditorGUILayout.PropertyField(m_IconProperty);
            EditorGUILayout.PropertyField(m_IconSpriteProperty);
            EditorGUILayout.PropertyField(m_IconColorProperty);
            
            EditorGUILayout.PropertyField(m_TextProperty);
            EditorGUILayout.PropertyField(m_TextColorProperty);
            if (EditorGUI.EndChangeCheck())
            {
                ToggleUI toggle = serializedObject.targetObject as ToggleUI;
                ToggleUIGroup group = m_GroupProperty.objectReferenceValue as ToggleUIGroup;
                
                toggle.isOn = m_IsOnProperty.boolValue;
                
                if (group != null && toggle.IsActive())
                {
                    if (toggle.isOn || (!group.AnyTogglesOn() && !group.allowSwitchOff))
                    {
                        toggle.isOn = true;
                        group.NotifyToggleOn(toggle);
                    }
                }
                
                // toggle.UpdateUI();
            }
    
            EditorGUILayout.Space();
    
            // Draw the event notification options
            EditorGUILayout.PropertyField(m_OnValueChangedProperty);
    
            serializedObject.ApplyModifiedProperties();
        }
    }
    */
}
