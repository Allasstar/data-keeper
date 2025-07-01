using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Vector3 Provider", fileName = "Vector3 Provider")]
    public class Vector3Provider : ScriptableObject, IValueProvider<Vector3>
    {
        [SerializeField] private Vector3 value = Vector3.zero;
    
        public Vector3 GetValue() => value;
    
        public void SetValue(Vector3 newValue) => value = newValue;
    }
}