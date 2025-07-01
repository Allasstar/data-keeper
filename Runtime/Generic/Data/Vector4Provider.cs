using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Vector4 Provider", fileName = "Vector4 Provider")]
    public class Vector4Provider : ScriptableObject, IValueProvider<Vector4>
    {
        [SerializeField] private Vector4 value = Vector4.zero;
    
        public Vector4 GetValue() => value;
    
        public void SetValue(Vector4 newValue) => value = newValue;
    }
}