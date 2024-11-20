using DataKeeper.Attributes;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    /// <summary>
    /// This class contain custom drawer for ReadOnly attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Unity method for drawing GUI in Editor
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="property">Property.</param>
        /// <param name="label">Label.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Disabling edit for property
            GUI.enabled = false;

            // Drawing Property
            EditorGUI.PropertyField(position, property, label);

            // Enabling edit for property
            GUI.enabled = true;
        }
    }
}
