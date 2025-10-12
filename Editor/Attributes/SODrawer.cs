using DataKeeper.Editor.Drawer;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
    public class SODrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DictionaryInspectorGUI.DrawDictionaries(target);
            PropertyGUI.DrawButtons(target);
        }
    }
}
