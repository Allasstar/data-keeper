using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
    public class ButtonSODrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PropertyGUI.DrawButtons(target);
        }
    }
}
