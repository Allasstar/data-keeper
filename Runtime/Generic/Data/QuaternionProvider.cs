using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Quaternion Provider", fileName = "Quaternion Provider")]
    public class QuaternionProvider : ScriptableObject, IValueProvider<Quaternion>
    {
        [SerializeField] private Quaternion value = Quaternion.identity;
    
        public Quaternion GetValue() => value;
    
        public void SetValue(Quaternion newValue) => value = newValue;
    }
}