using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Float Provider", fileName = "Float Provider")]
    public class FloatProvider : ScriptableObject, IValueProvider<float>
    {
        [SerializeField] private float value = 0f;
    
        public float GetValue() => value;
    
        public void SetValue(float newValue) => value = newValue;
    }
}