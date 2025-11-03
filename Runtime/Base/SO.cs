using UnityEngine;

namespace DataKeeper.Base
{
    public abstract class SO : ScriptableObject
    {
        public abstract void Initialize();

#if UNITY_EDITOR
        [ContextMenu("Save")]
        private void Save()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
