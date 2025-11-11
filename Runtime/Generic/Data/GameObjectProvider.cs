using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [CreateAssetMenu(menuName = "DataKeeper/Value Provider/GameObject Provider", fileName = "GameObject Provider")]
    public class GameObjectProvider : ScriptableObject, IValueProvider<GameObject>
    {
        [SerializeField] private GameObject value;
    
        public GameObject GetValue() => value;
    
        public void SetValue(GameObject newValue) => value = newValue;
    }
}