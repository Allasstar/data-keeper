using System;
using DataKeeper.Extensions;
using UnityEngine;

namespace DataKeeper.Extra
{
    [CreateAssetMenu(menuName = "DataKeeper/SOID", fileName = "SOID")]
    public class SOID : ScriptableObject
    {
        [field: SerializeField] public string ID { get; private set; }

        private void OnEnable()
        {
            if (ID.IsNullOrEmpty())
            {
                ID = Guid.NewGuid().ToString();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("New ID")]
        private void NewID()
        {
            UnityEditor.Undo.RecordObject(this, "Generate New SOID");
            ID = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
