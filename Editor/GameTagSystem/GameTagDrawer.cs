using DataKeeper.GameTagSystem;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.GameTagSystem
{
    [CustomPropertyDrawer(typeof(GameTag))]
    public class GameTagDrawer : PropertyDrawer
    {
        private const string None = "(none)";
        private static readonly Color MissingTint = new Color(1f, 0.55f, 0.6f);

        private static GameTagRegistry Registry => GameTagRegistry.Default;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idProp = property.FindPropertyRelative("_id");
            int id = idProp.intValue;

            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            var registry = Registry;
            string path = (id != GameTagRegistry.NONE && registry != null) ? registry.GetPath(id) : null;
            bool missing = id != GameTagRegistry.NONE && string.IsNullOrEmpty(path);

            string display = id == GameTagRegistry.NONE ? None : missing ? $"[missing #{id}]" : path;

            var prev = GUI.contentColor;
            if (missing) GUI.contentColor = MissingTint;

            if (GUI.Button(buttonRect, display, EditorStyles.popup))
            {
                GameTagPickerWindow.Show(registry, id, selectedId =>
                {
                    idProp.intValue = selectedId;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            if (missing) GUI.contentColor = prev;
        }
    }
}
