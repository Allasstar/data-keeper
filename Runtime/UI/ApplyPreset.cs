using System;
using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Apply Preset", 0)]
    public class ApplyPreset : MonoBehaviour
    {
#if UNITY_EDITOR

        [SerializeField] private TargetPreset[] _targetPresetArray;
        
        private void OnValidate()
        {
            ForceApply();
        }

        [Button("Force Apply", 10)]
        private void ForceApply()
        {
            if(_targetPresetArray == null || _targetPresetArray.Length == 0) return;

            foreach (var targetPreset in _targetPresetArray)
            {
                if(targetPreset.Target == null || targetPreset.Preset == null) return;
                if (!targetPreset.Preset.IsValid())
                {
                    return;
                }
            
                if (!targetPreset.Preset.CanBeAppliedTo(targetPreset.Target))
                {
                    targetPreset.Preset = null;
                    return;
                }
                
                Apply(targetPreset.Target, targetPreset.Preset);
            }
        }

        private void Apply(UnityEngine.Object target, UnityEditor.Presets.Preset preset)
        {
            if (preset.DataEquals(target)) return;
            preset.ApplyTo(target);
        }
        
        [Serializable]
        public class TargetPreset
        {
            [field: SerializeField] public Component Target { get; private set; }
            [field: SerializeField] public UnityEditor.Presets.Preset Preset { get; set; }
        }
#endif
    }
}