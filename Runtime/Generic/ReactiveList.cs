using System;
using System.Collections;
using System.Collections.Generic;
using DataKeeper.Extensions;
using DataKeeper.Signals;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    public enum ListChangedEvent
    {
        Added = 0,
        Removed = 1,
        Updated = 2,
        Cleared = 3
    }
    
    [Serializable]
    public class ReactiveList<T> : IList<T>
    {
        [SerializeField, JsonIgnore] private List<T> m_List;
        
        /// <summary>
        /// On List Changed event.
        /// </summary>
        [NonSerialized]
        public Signal<int, T, ListChangedEvent> OnListChanged = new Signal<int, T, ListChangedEvent>();


        /// <summary>
        /// Add Listener.
        /// </summary>
        /// <param name="call"></param>
        public void AddListener(Action<int, T, ListChangedEvent> call)
        {
            OnListChanged.AddListener(call);
        }
    
        /// <summary>
        /// Remove Listener.
        /// </summary>
        /// <param name="call"></param>
        public void RemoveListener(Action<int, T, ListChangedEvent> call)
        {
            OnListChanged.RemoveListener(call);
        }

        /// <summary>
        /// Remove All Listeners.
        /// </summary>
        public void RemoveAllListeners()
        {
            OnListChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public ReactiveList() : this(0)
        {
            m_List = new List<T>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Allocation size.</param>
        public ReactiveList(int capacity)
        {
            m_List = new List<T>(capacity);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="collection">Input list.</param>
        public ReactiveList(IEnumerable<T> collection)
        {
            m_List = new List<T>(collection);
        }

        /// <summary>
        /// Accessor.
        /// </summary>
        /// <param name="index">Item index.</param>
        /// <returns>The item at the provided index.</returns>
        public T this[int index]
        {
            get => m_List[index];
            set
            {
                OnListChanged?.Invoke(index, m_List[index], ListChangedEvent.Removed);
                m_List[index] = value;
                OnListChanged?.Invoke(index, value, ListChangedEvent.Added);
            }
        }

        /// <summary>
        /// Number of elements in the list.
        /// </summary>
        public int Count => m_List.Count;

        /// <summary>
        /// Is the list read only?
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Check if an element is present in the list.
        /// </summary>
        /// <param name="item">Item to test against.</param>
        /// <returns>True if the item is in the list.</returns>
        public bool Contains(T item)
        {
            return m_List.Contains(item);
        }

        /// <summary>
        /// Get the index of an item.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>The index of the item in the list if it exists, -1 otherwise.</returns>
        public int IndexOf(T item)
        {
            return m_List.IndexOf(item);
        }

        /// <summary>
        /// Add an item to the list.
        /// </summary>
        /// <param name="item">Item to add to the list.</param>
        public void Add(T item)
        {
            m_List.Add(item);
            OnListChanged?.Invoke(m_List.IndexOf(item), item, ListChangedEvent.Added);
        }

        /// <summary>
        /// Add multiple objects to the list.
        /// </summary>
        /// <param name="items">Items to add to the list.</param>
        public void Add(params T[] items)
        {
            foreach (var i in items)
                Add(i);
        }

        /// <summary>
        /// Insert an item in the list.
        /// </summary>
        /// <param name="index">Index at which to insert the new item.</param>
        /// <param name="item">Item to insert in the list.</param>
        public void Insert(int index, T item)
        {
            m_List.Insert(index, item);
            OnListChanged?.Invoke(index, item, ListChangedEvent.Added);
        }

        /// <summary>
        /// Remove an item from the list.
        /// </summary>
        /// <param name="item">Item to remove from the list.</param>
        /// <returns>True if the item was successfuly removed. False otherise.</returns>
        public bool Remove(T item)
        {
            int index = m_List.IndexOf(item);
            bool ret = m_List.Remove(item);
            if (ret)
                OnListChanged?.Invoke(index, item, ListChangedEvent.Removed);
            return ret;
        }

        /// <summary>
        /// Remove multiple items from the list.
        /// </summary>
        /// <param name="items">Items to remove from the list.</param>
        /// <returns>The number of removed items.</returns>
        public int Remove(params T[] items)
        {
            if (items == null)
                return 0;

            int count = 0;

            foreach (var i in items)
                count += Remove(i) ? 1 : 0;

            return count;
        }

        /// <summary>
        /// Remove an item at a specific index.
        /// </summary>
        /// <param name="index">Index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            var item = m_List[index];
            m_List.RemoveAt(index);
            OnListChanged?.Invoke(index, item, ListChangedEvent.Removed);
        }

        /// <summary>
        /// Clear the list.
        /// </summary>
        public void Clear()
        {
            m_List.Clear();
            OnListChanged?.Invoke(-1, default, ListChangedEvent.Cleared);
        }

        /// <summary>
        /// Copy items in the list to an array.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Starting index.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            m_List.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        /// <returns>The list enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        /// <returns>The list enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Manually Invoke Update Event
        /// </summary>
        /// <param name="index"></param>
        public void InvokeUpdateEvent(int index)
        {
            if(!m_List.HasIndex(index)) return;
            
            OnListChanged?.Invoke(index, m_List[index], ListChangedEvent.Updated);
        }
        
        /// <summary>
        /// Manually Invoke Update Event
        /// </summary>
        /// <param name="value"></param>
        public void InvokeUpdateEvent(T value)
        {
            if(!m_List.Contains(value)) return;
            
            OnListChanged?.Invoke(m_List.IndexOf(value), value, ListChangedEvent.Updated);
        }
    }
}
