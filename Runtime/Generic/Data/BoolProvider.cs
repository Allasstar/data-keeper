using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Bool Provider", fileName = "Bool Provider")]
    public class BoolProvider : ScriptableObject, IValueProvider<bool>
    {
        [SerializeField] private bool value = false;
    
        public bool GetValue() => value;
    
        public void SetValue(bool newValue) => value = newValue;
    }
}