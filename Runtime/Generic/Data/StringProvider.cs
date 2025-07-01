using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/String Provider", fileName = "String Provider")]
    public class StringProvider : ScriptableObject, IValueProvider<string>
    {
        [SerializeField] private string value = "";
    
        public string GetValue() => value;
    
        public void SetValue(string newValue) => value = newValue;
    }
}