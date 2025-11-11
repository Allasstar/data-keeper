using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Rigidbody Provider", fileName = "Rigidbody Provider")]
    public class RigidbodyProvider : ScriptableObject, IValueProvider<Rigidbody>
    {
        [SerializeField] private Rigidbody value;
    
        public Rigidbody GetValue() => value;
    
        public void SetValue(Rigidbody newValue) => value = newValue;
    }
}