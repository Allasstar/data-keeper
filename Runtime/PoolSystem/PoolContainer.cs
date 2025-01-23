using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.PoolSystem
{
    public class PoolContainer<T> where T : Component
    {
        private static Dictionary<int, Transform> PoolParentDictionary = new Dictionary<int, Transform>();
        private static Dictionary<int, List<T>> PoolInactiveDictionary = new Dictionary<int, List<T>>();

        private int _poolPrefabID;
        private string _poolPrefabName;
        
        private Transform GetPoolParent()
        {
            Transform poolParent;
          
            if (PoolParentDictionary.ContainsKey(_poolPrefabID) && PoolParentDictionary[_poolPrefabID] != null)
            {
                poolParent = PoolParentDictionary[_poolPrefabID];
            }
            else
            {
                poolParent = new GameObject().transform;
                Object.DontDestroyOnLoad(poolParent);
                poolParent.name = $":: [{typeof(T).Name}] > {_poolPrefabName}";
                PoolParentDictionary[_poolPrefabID] = poolParent;
            }
            
            return poolParent;
        }

        private List<T> GetPoolInactive()
        {
            List<T> poolInactive;
            if (PoolInactiveDictionary.ContainsKey(_poolPrefabID))
            {
                poolInactive = PoolInactiveDictionary[_poolPrefabID];
                poolInactive.RemoveAll(r => r == null || r.gameObject == null);
            }
            else
            {
                poolInactive = new List<T>();
                PoolInactiveDictionary[_poolPrefabID] = poolInactive;
            }

            return poolInactive;
        }
        
        private List<T> GetPoolActive()
        {
            return new List<T>();
        }

        public PoolContainer(int poolPrefabID, string poolPrefabName)
        {
            _poolPrefabID = poolPrefabID;
            _poolPrefabName = poolPrefabName;
        }

        public void Deconstruct(out Transform poolParent, out List<T> poolInactive, out List<T> poolActive)
        {
            poolParent = GetPoolParent();
            poolInactive = GetPoolInactive();
            poolActive = GetPoolActive();
        }
        
        public void Deconstruct(out Transform poolParent)
        {
            poolParent = GetPoolParent();
        }
    }
}