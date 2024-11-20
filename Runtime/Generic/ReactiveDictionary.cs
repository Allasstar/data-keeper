using System;
using System.Collections;
using System.Collections.Generic;
using DataKeeper.Signals;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    public enum DictionaryChangedEvent
    {
        Added = 0,
        Removed = 1,
        Updated = 2,
        Cleared = 3
    }

    [Serializable]
    public class ReactiveDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        [SerializeField, JsonIgnore] private Dictionary<TKey, TValue> m_Dictionary;

        /// <summary>
        /// On Dictionary Changed event.
        /// </summary>
        [NonSerialized]
        public Signal<TKey, TValue, DictionaryChangedEvent> OnDictionaryChanged = new Signal<TKey, TValue, DictionaryChangedEvent>();

        /// <summary>
        /// Add Listener.
        /// </summary>
        /// <param name="call"></param>
        public void AddListener(Action<TKey, TValue, DictionaryChangedEvent> call)
        {
            OnDictionaryChanged.AddListener(call);
        }

        /// <summary>
        /// Remove Listener.
        /// </summary>
        /// <param name="call"></param>
        public void RemoveListener(Action<TKey, TValue, DictionaryChangedEvent> call)
        {
            OnDictionaryChanged.RemoveListener(call);
        }

        /// <summary>
        /// Remove All Listeners.
        /// </summary>
        public void RemoveAllListeners()
        {
            OnDictionaryChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public ReactiveDictionary()
        {
            m_Dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Allocation size.</param>
        public ReactiveDictionary(int capacity)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="collection">Input dictionary.</param>
        public ReactiveDictionary(IDictionary<TKey, TValue> collection)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(collection);
        }

        /// <summary>
        /// Accessor.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>The value associated with the provided key.</returns>
        public TValue this[TKey key]
        {
            get => m_Dictionary[key];
            set
            {
                if (m_Dictionary.ContainsKey(key))
                {
                    OnDictionaryChanged?.Invoke(key, m_Dictionary[key], DictionaryChangedEvent.Updated);
                    m_Dictionary[key] = value;
                }
                else
                {
                    m_Dictionary[key] = value;
                    OnDictionaryChanged?.Invoke(key, value, DictionaryChangedEvent.Added);
                }
            }
        }

        /// <summary>
        /// Keys in the dictionary.
        /// </summary>
        public ICollection<TKey> Keys => m_Dictionary.Keys;

        /// <summary>
        /// Values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values => m_Dictionary.Values;

        /// <summary>
        /// Number of elements in the dictionary.
        /// </summary>
        public int Count => m_Dictionary.Count;

        /// <summary>
        /// Is the dictionary read only?
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Add a key-value pair to the dictionary.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void Add(TKey key, TValue value)
        {
            m_Dictionary.Add(key, value);
            OnDictionaryChanged?.Invoke(key, value, DictionaryChangedEvent.Added);
        }

        /// <summary>
        /// Add a key-value pair to the dictionary.
        /// </summary>
        /// <param name="item">Key-value pair.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Remove an item from the dictionary.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>True if the key was successfully removed. False otherwise.</returns>
        public bool Remove(TKey key)
        {
            if (m_Dictionary.TryGetValue(key, out var value))
            {
                bool ret = m_Dictionary.Remove(key);
                if (ret)
                    OnDictionaryChanged?.Invoke(key, value, DictionaryChangedEvent.Removed);
                return ret;
            }
            return false;
        }

        /// <summary>
        /// Remove an item from the dictionary.
        /// </summary>
        /// <param name="item">Key-value pair to remove.</param>
        /// <returns>True if the item was successfully removed. False otherwise.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        /// <summary>
        /// Clear the dictionary.
        /// </summary>
        public void Clear()
        {
            m_Dictionary.Clear();
            OnDictionaryChanged?.Invoke(default, default, DictionaryChangedEvent.Cleared);
        }

        /// <summary>
        /// Check if an element is present in the dictionary.
        /// </summary>
        /// <param name="key">Key to test against.</param>
        /// <returns>True if the key is in the dictionary.</returns>
        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Check if an element is present in the dictionary.
        /// </summary>
        /// <param name="item">Key-value pair to test against.</param>
        /// <returns>True if the key-value pair is in the dictionary.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_Dictionary.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(m_Dictionary[item.Key], item.Value);
        }

        /// <summary>
        /// Copy items in the dictionary to an array.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Starting index.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)m_Dictionary).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Try to get the value associated with a key.
        /// </summary>
        /// <param name="key">Key to look up.</param>
        /// <param name="value">Out parameter for the value.</param>
        /// <returns>True if the key is found, false otherwise.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        /// <returns>The dictionary enumerator.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        /// <returns>The dictionary enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        /// <summary>
        /// Manually Invoke Update Event
        /// </summary>
        /// <param name="index"></param>
        public void InvokeUpdateEvent(TKey key)
        {
            if(!m_Dictionary.ContainsKey(key)) return;
            
            OnDictionaryChanged?.Invoke(key, m_Dictionary[key], DictionaryChangedEvent.Updated);
        }
    }
}
