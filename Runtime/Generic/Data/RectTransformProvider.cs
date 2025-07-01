using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/RectTransform Provider", fileName = "RectTransform Provider")]
    public class RectTransformProvider : ScriptableObject, IValueProvider<RectTransform>
    {
        [SerializeField] private RectTransform value = null;
    
        public RectTransform GetValue() => value;
    
        public void SetValue(RectTransform newValue) => value = newValue;
    }
}