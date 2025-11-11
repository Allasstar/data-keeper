using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Transform Provider", fileName = "Transform Provider")]
    public class TransformProvider : ScriptableObject, IValueProvider<Transform>
    {
        [SerializeField] private Transform value;
    
        public Transform GetValue() => value;
    
        public void SetValue(Transform newValue) => value = newValue;
    }
}