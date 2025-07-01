using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Color Provider", fileName = "Color Provider")]
    public class ColorProvider : ScriptableObject, IValueProvider<Color>
    {
        [SerializeField] private Color value = Color.white;
    
        public Color GetValue() => value;
    
        public void SetValue(Color newValue) => value = newValue;
    }
}