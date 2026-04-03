using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameTag))]
public class GameTagDrawer : PropertyDrawer
{
    private const string None = "(none)";
    private static GameTagRegistry _registry;

    static GameTagDrawer()
    {
        _registry = Resources.Load<GameTagRegistry>("GameTagRegistry");
        if (_registry == null)
        {
            _registry = ScriptableObject.CreateInstance<GameTagRegistry>();
            const string path = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(_registry, $"{path}/GameTagRegistry.asset");
            AssetDatabase.SaveAssets();
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProp  = property.FindPropertyRelative("_value");
        var labelRect  = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
            position.width - EditorGUIUtility.labelWidth, position.height);

        EditorGUI.LabelField(labelRect, label);

        string current = valueProp.stringValue;
        if (GUI.Button(buttonRect, string.IsNullOrEmpty(current) ? None : current, EditorStyles.popup))
        {
            GameTagPickerWindow.Show(
                _registry,
                current != null ? new[] { current } : Array.Empty<string>(),
                multiSelect: false,
                selected =>
                {
                    valueProp.stringValue = selected.Count > 0 ? selected[0] : string.Empty;
                    property.serializedObject.ApplyModifiedProperties();
                });
        }
    }
}