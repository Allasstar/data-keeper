using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/ColorBlock Provider", fileName = "ColorBlock Provider")]
    public class ColorBlockProvider : ScriptableObject, IValueProvider<ColorBlock>
    {
        [SerializeField] private ColorBlock value = ColorBlock.defaultColorBlock;
    
        public ColorBlock GetValue() => value;
    
        public void SetValue(ColorBlock newValue) => value = newValue;
    }
}