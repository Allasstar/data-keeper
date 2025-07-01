using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Texture Provider", fileName = "Texture Provider")]
    public class TextureProvider : ScriptableObject, IValueProvider<Texture>
    {
        [SerializeField] private Texture value = null;
    
        public Texture GetValue() => value;
    
        public void SetValue(Texture newValue) => value = newValue;
    }
}