using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Rigidbody2D Provider", fileName = "Rigidbody2D Provider")]
    public class Rigidbody2DProvider : ScriptableObject, IValueProvider<Rigidbody2D>
    {
        [SerializeField] private Rigidbody2D value;
    
        public Rigidbody2D GetValue() => value;
    
        public void SetValue(Rigidbody2D newValue) => value = newValue;
    }
}