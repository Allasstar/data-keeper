using System;
using DataKeeper.Extensions;
using UnityEngine;

namespace DataKeeper.Generic
{
    /// <summary>
    /// Lazy implementation
    /// </summary>
    /// <typeparam name="T">The type of object that is being lazily initialized</typeparam>
    [Serializable]
    public class UnityLazy<T> where T : class
    {
        [SerializeField] private T _value;
        
        private Func<T> _valueFactory;

        /// <summary>
        /// Gets a value indicating whether the lazy instance has been initialized
        /// </summary>
        public bool IsValueInitialized => !_value.IsNull();
    
        /// <summary>
        /// Gets the lazily initialized value
        /// </summary>
        public T Value
        {
            get
            {
                if (!IsValueInitialized)
                {
                    _value = _valueFactory?.Invoke();
                }
            
                return _value;
            }
        }
    
        /// <summary>
        /// Initialize with a factory function
        /// </summary>
        /// <param name="valueFactory">Function that creates the value</param>
        public UnityLazy(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }
    
        /// <summary>
        /// Initialize for component lookup on target GameObject
        /// </summary>
        /// <param name="target">Target GameObject to search</param>
        /// <param name="includeChildren">If true, searches in children. If false, searches only target. If null, uses FindObjectOfType</param>
        public UnityLazy(GameObject target, bool includeChildren = false)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!typeof(Component).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException("T must be a Component");
            }
        
            if (includeChildren)
            {
                _valueFactory = target.GetComponentInChildren<T>;
            }
            else
            {
                _valueFactory = target.GetComponent<T>;
            }
        }
        
        /// <summary>
        /// Initialize for component lookup on target GameObject
        /// </summary>
        /// <param name="target">Target GameObject to search</param>
        /// <param name="includeChildren">If true, searches in children. If false, searches only target. If null, uses FindObjectOfType</param>
        public UnityLazy(Component target, bool includeChildren = false)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!typeof(Component).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException("T must be a Component");
            }
        
            if (includeChildren)
            {
                _valueFactory = target.GetComponentInChildren<T>;
            }
            else
            {
                _valueFactory = target.GetComponent<T>;
            }
        }
    
        /// <summary>
        /// Forces re-initialization on next access
        /// </summary>
        public void Reset()
        {
            _value = null;
        }
    
        /// <summary>
        /// Implicit conversion to T
        /// </summary>
        public static implicit operator T(UnityLazy<T> unityLazy) => unityLazy?.Value;
    
        /// <summary>
        /// Implicit conversion to bool (checks if value exists)
        /// </summary>
        public static implicit operator bool(UnityLazy<T> unityLazy) => unityLazy?.IsValueInitialized ?? false;
    
        public override string ToString() => IsValueInitialized ? _value.ToString() : "Value not created";
    }
}
