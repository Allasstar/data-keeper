using System;
using System.Collections.Generic;
using System.Linq;

namespace DataKeeper.Generic
{
    public class Register<TValue>
    {
        protected readonly Dictionary<string, TValue> _container = new Dictionary<string, TValue>();

        public int Count => _container.Count;
        public IReadOnlyDictionary<string, TValue> All => _container;
        
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
        }
    
        public void Reg<T>(TValue value) where T : TValue
        {
            _container[typeof(T).Name] = value;
        }
        
        public void Reg(TValue value)
        {
            _container[value.GetType().Name] = value;
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
        
        public Register<TValue> Resolve<T>(out T outValue) where T : class, TValue
        {
            outValue = _container.TryGetValue(typeof(T).Name, out var value) ? value as T : null;
            return this;
        }
        
        public Register<TValue> Resolve<T>(out T outValue, string id) where T : class, TValue
        {
            outValue = _container.TryGetValue(id, out var value) ? value as T : null;
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