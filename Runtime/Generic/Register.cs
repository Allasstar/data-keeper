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
            _container[typeof(TValue).Name] = value;
        }
        
        public T Get<T>() where T : class, TValue
        {
            return _container.TryGetValue(typeof(T).Name, out var value) ? value as T : null;
        }
    
        public T Get<T>(string id) where T : class, TValue
        {
            return _container.TryGetValue(id, out var value) ? value as T : null;
        }
        
        public Register<TValue> Get<T>(out T outValue) where T : class, TValue
        {
            outValue = _container.TryGetValue(typeof(T).Name, out var value) ? value as T : null;
            return this;
        }
        
        public Register<TValue> Get<T>(string id, out T outValue) where T : class, TValue
        {
            outValue = _container.TryGetValue(id, out var value) ? value as T : null;
            return this;
        }
        
        public bool TryGet<T>(out T outValue) where T : class, TValue
        {
            outValue = _container.TryGetValue(typeof(T).Name, out var value) ? value as T : null;
            return outValue != null;
        }
        
        public bool TryGet<T>(string id, out T outValue) where T : class, TValue
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