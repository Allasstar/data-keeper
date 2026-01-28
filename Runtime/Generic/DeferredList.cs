using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    /// <summary>
    /// A thread-safe list wrapper that defers additions and removals until ApplyChanges() is called.
    /// Prevents modification exceptions during iteration.
    /// </summary>
    [Serializable]
    public class DeferredList<T> : IList<T>
    {
        [SerializeField, JsonIgnore] private List<T> _items = new();
        [SerializeField, JsonIgnore] private List<T> _toAdd = new();
        [SerializeField, JsonIgnore] private List<T> _toRemove = new();

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public int Count => _items.Count;
        
        public bool IsReadOnly => false;

        /// <summary>
        /// Applies all pending additions and removals.
        /// Call this BEFORE iteration to ensure changes are applied safely.
        /// </summary>
        public void ApplyChanges()
        {
            foreach (var item in _toRemove)
            {
                _items.Remove(item);
            }
            _toRemove.Clear();

            foreach (var item in _toAdd)
            {
                _items.Add(item);
            }
            _toAdd.Clear();
        }
        
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }
        
        public bool ContainsToAdd(T item)
        {
            return _toAdd.Contains(item);
        }
        
        public bool ContainsToRemove(T item)
        {
            return _toRemove.Contains(item);
        }

        public void Add(T item)
        {
            if (item == null) return;
            _toAdd.Add(item);
        }

        public bool Remove(T item)
        {
            if (item == null) return false;
            _toRemove.Add(item);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _toRemove.Add(_items[index]);
        }

        public void Insert(int index, T item)
        {
            if (item == null) return;
            
            Add(item);
        }

        public void Clear()
        {
            _items.Clear();
            _toAdd.Clear();
            _toRemove.Clear();
        }

        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}