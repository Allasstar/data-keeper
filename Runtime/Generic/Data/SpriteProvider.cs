using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Sprite Provider", fileName = "Sprite Provider")]
    public class SpriteProvider : ScriptableObject, IValueProvider<Sprite>
    {
        [SerializeField] private Sprite value = null;
    
        public Sprite GetValue() => value;
    
        public void SetValue(Sprite newValue) => value = newValue;
    }
}