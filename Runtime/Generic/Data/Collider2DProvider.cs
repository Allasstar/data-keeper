using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Collider2D Provider", fileName = "Collider2D Provider")]
    public class Collider2DProvider : ScriptableObject, IValueProvider<Collider2D>
    {
        [SerializeField] private Collider2D value;
    
        public Collider2D GetValue() => value;
    
        public void SetValue(Collider2D newValue) => value = newValue;
    }
}