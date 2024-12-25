using System;
using System.Collections.Generic;
using DataKeeper.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.PoolSystem
{
    [Serializable]
    public class Pool<T> where T : Component
    {
        [SerializeField] private T _poolPrefab;
        [SerializeField] private Optional<int> _prewarm = new Optional<int>();
        [SerializeField] private Optional<int> _maxActive = new Optional<int>();
        
        private Transform _poolContainer;
        private List<T> _poolInactive;
        private List<T> _poolActive;
        private bool _isInitialized;

        public int GetPoolPrefabID() => _poolPrefab.GetHashCode();
        public string GetPoolPrefabName() => _poolPrefab.name;
        public bool IsInitialized() => _isInitialized;

       public virtual void Initialize()
       {
           if(_isInitialized) return;
           _isInitialized = true;
           
           new PoolContainer<T>(GetPoolPrefabID(), GetPoolPrefabName())
               .Deconstruct(out _poolContainer, out _poolInactive, out _poolActive);

           if (!_prewarm.Enabled) return;

           if (_poolInactive.Count >= _prewarm.Value) return;
           
           for (int i = 0; i < _prewarm.Value - _poolInactive.Count; i++)
           {
               Create();
           }
       }
       
       public virtual T Create()
       {
           var obj = Object.Instantiate(_poolPrefab, _poolContainer);
           obj.gameObject.name = $"{_poolPrefab.name} [{obj.GetInstanceID()}]";
           obj.gameObject.SetActive(false);
           _poolInactive.Add(obj);
           return obj;
       }

       public virtual T Get()
       {
           T poolObject;

           if (_maxActive is { Enabled: true, Value: > 0 } && _poolActive.Count >= _maxActive.Value)
           {
               Release(_poolActive[0]);
           }
           
           if (_poolInactive.Count > 0)
           {
               poolObject = _poolInactive[0];
           }
           else
           {
               poolObject = Create();
           }
           
           _poolInactive.Remove(poolObject);
           _poolActive.Add(poolObject);
           poolObject.gameObject.SetActive(true);
           return poolObject;
       }
       
       public T Get(out Action releaseAction)
       {
           var obj = Get();
           releaseAction = () => Release(obj);
           return obj;
       }

       public virtual void Release(T poolObject)
       {
           poolObject.gameObject.SetActive(false);
           poolObject.transform.SetParent(_poolContainer);
           
           _poolActive.Remove(poolObject);
           
           if(_poolInactive.Contains(poolObject)) return;
           _poolInactive.Add(poolObject);
       }

       public virtual void ReleaseAll()
       {
           foreach (var component in _poolActive.ToArray())
           {
               Release(component);
           }
           
           _poolActive.Clear();
       }
    }
}
