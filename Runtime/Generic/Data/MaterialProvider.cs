using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Material Provider", fileName = "Material Provider")]
    public class MaterialProvider : ScriptableObject, IValueProvider<Material>
    {
        [SerializeField] private Material value = null;
    
        public Material GetValue() => value;
    
        public void SetValue(Material newValue) => value = newValue;
    }
}