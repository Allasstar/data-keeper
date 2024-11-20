using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonMonoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PropertyGUI.DrawButtons(target);
        }
    }
}
