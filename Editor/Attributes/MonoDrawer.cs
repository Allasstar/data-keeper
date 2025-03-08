using DataKeeper.Editor.FSM;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    public class MonoDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PropertyGUI.DrawButtons(target);
            FSMDebugger.DrawFSMDebuger(target);
        }
    }
}
