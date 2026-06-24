using DataKeeper.Editor.Drawer;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomEditor(typeof(Component), true), CanEditMultipleObjects]
    public class ComponentDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PropertyGUI.DrawInspector(serializedObject);
            PropertyGUI.DrawButtons(target);
        }
    }
}
