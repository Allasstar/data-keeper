using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/LayerMask Provider", fileName = "LayerMask Provider")]
    public class LayerMaskProvider : ScriptableObject, IValueProvider<LayerMask>
    {
        [SerializeField] private LayerMask value = 0;
    
        public LayerMask GetValue() => value;
    
        public void SetValue(LayerMask newValue) => value = newValue;
    }
}