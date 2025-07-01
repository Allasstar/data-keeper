using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Vector2 Provider", fileName = "Vector2 Provider")]
    public class Vector2Provider : ScriptableObject, IValueProvider<Vector2>
    {
        [SerializeField] private Vector2 value = Vector2.zero;
    
        public Vector2 GetValue() => value;
    
        public void SetValue(Vector2 newValue) => value = newValue;
    }
}