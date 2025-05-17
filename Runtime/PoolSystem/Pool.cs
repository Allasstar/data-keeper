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
        [SerializeField] private Optional<Transform> _container = new Optional<Transform>();
        [SerializeField] private Optional<int> _prewarm = new Optional<int>();
        [SerializeField] private Optional<int> _maxActive = new Optional<int>();
        
        private bool _isInitialized;
        private Transform _poolContainer;
        private List<T> _poolInactive  = new List<T>();
        private List<T> _poolActive  = new List<T>();

        public T GetPoolPrefab() => _poolPrefab;
        public Transform GetPoolContainer() => PoolContainer;
        public int GetPoolPrefabID() => _poolPrefab.GetHashCode();
        public string GetPoolPrefabName() => _poolPrefab.name;
        public bool IsInitialized() => _isInitialized;
        public List<T> GetAllInactive() => _poolInactive;
        public List<T> GetAllActive() => _poolActive;
        
        private Transform PoolContainer
        {
            get
            {
                if(_container.Enabled) return _container.Value;
                
                if(_poolContainer == null) 
                {
                    _poolContainer = new GameObject($":: [{typeof(T).Name}] > {GetPoolPrefabName()}").transform;
                    Object.DontDestroyOnLoad(_poolContainer.gameObject);
                }
            
                return _poolContainer;
            }   
        }

        public Pool()
        {
            
        }
        
        public Pool(T prefab, int prewarm, int maxActive = -1)
        {
            _poolPrefab = prefab;
            _prewarm = new Optional<int>(prewarm, prewarm > 0);
            _maxActive = new Optional<int>(maxActive, maxActive > 0);
        }
        
       public virtual void Initialize()
       {
           if(_isInitialized) return;
           _isInitialized = true;
           
           if (!_prewarm.Enabled) return;

           if (_poolInactive.Count >= _prewarm.Value) return;

           var count = _prewarm.Value - _poolInactive.Count;
           for (int i = 0; i < count; i++)
           {
               Create();
           }
       }
       
       public virtual T Create()
       {
           var obj = Object.Instantiate(_poolPrefab, PoolContainer);
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
           poolObject.transform.SetParent(PoolContainer);
           
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

       public virtual void DestroyAllActive()
       {
           foreach (var component in _poolActive)
           {
               Object.Destroy(component.gameObject);
           }
           
           _poolActive.Clear();
       }
       
       public virtual void DestroyAllInactive()
       {
           foreach (var component in _poolInactive)
           {
               Object.Destroy(component.gameObject);
           }
           
           _poolInactive.Clear();
       }
       
       public virtual void DestroyAll()
       {
           DestroyAllInactive();
           DestroyAllActive();
       }
    }
}
