using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/Int Provider", fileName = "Int Provider")]
    public class IntProvider : ScriptableObject, IValueProvider<int>
    {
        [SerializeField] private int value = 0;
    
        public int GetValue() => value;
    
        public void SetValue(int newValue) => value = newValue;
    }
}