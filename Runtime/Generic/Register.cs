using System;
using System.Collections.Generic;
using System.Linq;
using Object = System.Object;

namespace DataKeeper.Generic
{
    public class Register<TValue>
    {
        public struct PendingResolution
        {
            public Object target;
            public Type targetType;
            public string id;
        }
        
        protected readonly Dictionary<string, TValue> _container = new Dictionary<string, TValue>();
        protected List<PendingResolution> _pendingResolutionList = new List<PendingResolution>();

        public int Count => _container.Count;
        public IReadOnlyDictionary<string, TValue> All => _container;
        public IReadOnlyList<PendingResolution> AllPendingResolution => _pendingResolutionList;
        
        public virtual bool Contains<T>() => _container.ContainsKey(typeof(T).Name);
        public virtual bool Contains(string id) => _container.ContainsKey(id);

        public Register()
        {
        }
        
        public T Find<T>(Func<T, bool> predicate) where T : class, TValue
        {
            return _container.Values
                .OfType<T>()
                .FirstOrDefault(predicate);
        }

        public IEnumerable<T> FindAll<T>(Func<T, bool> predicate) where T : class, TValue
        {
            return _container.Values
                .OfType<T>()
                .Where(predicate);
        }
        
        public void Reg(TValue value, string id)
        {
            _container[id] = value;
            Resolver();
        }
        
        public void Reg<T>(TValue value, string id) where T : TValue
        {
            _container[id] = value;
            Resolver();
        }
    
        public void Reg<T>(TValue value) where T : TValue
        {
            _container[typeof(T).Name] = value;
            Resolver();
        }
        
        public void Reg(TValue value)
        {
            _container[value.GetType().Name] = value;
            Resolver();
        }

        private void Resolver()
        {
            for (var i = _pendingResolutionList.Count - 1; i >= 0; i--)
            {
                var pending = _pendingResolutionList[i];
                if (_container.TryGetValue(pending.id, out var value))
                {
                    pending.target = value;
                    _pendingResolutionList.RemoveAt(i);
                }
            }
        }
        
        public TValue Get(Type type, string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                return _container.GetValueOrDefault(id);
            }

            // Try get by exact type name first
            if (_container.TryGetValue(type.Name, out var exactMatch))
            {
                return exactMatch;
            }

            // If type is interface or abstract, look for implementation
            if (type.IsInterface || type.IsAbstract)
            {
                TValue match = default;
                foreach (var v in _container.Values)
                {
                    if (v != null && type.IsInstanceOfType(v))
                    {
                        match = v;
                        break;
                    }
                }
                return match;
            }

            return default;
        }

        public T Get<T>() where T : class, TValue
        {
            return Get(typeof(T)) as T;
        }

        public T Get<T>(string id) where T : class, TValue
        {
            return Get(typeof(T), id) as T;
        }

        public UnityLazy<T> GetLazy<T>() where T : class, TValue
        {
            return new UnityLazy<T>(Get<T>);
        }
        
        public UnityLazy<T> GetLazy<T>(string id) where T : class, TValue
        {
            return new UnityLazy<T>(() => Get<T>(id));
        }
        
        public Register<TValue> Resolve<T>(ref T outValue) where T : class, TValue
        {
            Resolve(ref outValue, typeof(T).Name);
            return this;
        }
        
        public Register<TValue> Resolve<T>(ref T outValue, string id) where T : class, TValue
        {
            if (_container.TryGetValue(id, out var value))
            {
                outValue = value as T;
            }
            else
            {
                _pendingResolutionList.Add(new PendingResolution()
                {
                    target = outValue,
                    targetType = typeof(T),
                    id = id
                });
            }
            
            return this;
        }
        
        public bool TryGet<T>(out T outValue) where T : class, TValue
        {
            outValue = _container.TryGetValue(typeof(T).Name, out var value) ? value as T : null;
            return outValue != null;
        }
        
        public bool TryGet<T>(out T outValue, string id) where T : class, TValue
        {
            outValue = _container.TryGetValue(id, out var value) ? value as T : null;
            return outValue != null;
        }
        
        public bool Remove<T>() => _container.Remove(typeof(T).Name);
        public bool Remove(string id) => _container.Remove(id);
        
        public void Clear() => _container.Clear();
        public void ClearNull()
        {
            var keysToRemove = _container.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _container.Remove(key);
            }
        }
    }
}