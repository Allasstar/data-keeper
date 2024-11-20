using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomEditor(typeof(ScriptableObject), true)]
    public class ButtonSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PropertyGUI.DrawButtons(target);
        }
    }
}
