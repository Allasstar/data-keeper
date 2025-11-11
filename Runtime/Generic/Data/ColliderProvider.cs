using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Collider Provider", fileName = "Collider Provider")]
    public class ColliderProvider : ScriptableObject, IValueProvider<Collider>
    {
        [SerializeField] private Collider value;
    
        public Collider GetValue() => value;
    
        public void SetValue(Collider newValue) => value = newValue;
    }
}