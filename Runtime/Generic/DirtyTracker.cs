using System;
using System.Collections.Generic;
using System.Linq;

namespace DataKeeper.Generic
{
    /// <summary>
    /// A generic dirty tracking utility that can work with any types and custom comparison methods.
    /// </summary>
    /// <typeparam name="TValue">The type of the value being tracked</typeparam>
    /// <typeparam name="TCompare">The type used for comparison (can be same as TValue or different)</typeparam>
    public class DirtyTracker<TValue, TCompare>
    {
        private TValue _currentValue;
        private TCompare _lastComparedValue;
        private bool _isDirty;
        
        private readonly Func<TValue, TCompare> _valueToCompareConverter;
        private readonly Func<TCompare, TCompare, bool> _comparer;
        private readonly Action<TValue> _onDirtyCallback;
        private readonly bool _autoUpdateOnDirty;

        /// <summary>
        /// Gets whether the value is currently dirty
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Gets the current value
        /// </summary>
        public TValue CurrentValue => _currentValue;

        /// <summary>
        /// Constructor for DirtyTracker
        /// </summary>
        /// <param name="initialValue">Initial value to track</param>
        /// <param name="valueToCompareConverter">Function to convert TValue to TCompare for comparison</param>
        /// <param name="comparer">Function to compare two TCompare values (returns true if equal)</param>
        /// <param name="onDirtyCallback">Optional callback to execute when value becomes dirty</param>
        /// <param name="autoUpdateOnDirty">Whether to automatically update the base value when dirty</param>
        public DirtyTracker(
            TValue initialValue,
            Func<TValue, TCompare> valueToCompareConverter,
            Func<TCompare, TCompare, bool> comparer,
            Action<TValue> onDirtyCallback = null,
            bool autoUpdateOnDirty = true)
        {
            _valueToCompareConverter = valueToCompareConverter ?? throw new ArgumentNullException(nameof(valueToCompareConverter));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _onDirtyCallback = onDirtyCallback;
            _autoUpdateOnDirty = autoUpdateOnDirty;
            
            _currentValue = initialValue;
            _lastComparedValue = _valueToCompareConverter(initialValue);
            _isDirty = false;
        }

        /// <summary>
        /// Updates the value and checks if it's dirty
        /// </summary>
        /// <param name="newValue">The new value to set</param>
        /// <returns>True if the value is dirty (changed)</returns>
        public bool SetValue(TValue newValue)
        {
            var newCompareValue = _valueToCompareConverter(newValue);
            _isDirty = !_comparer(_lastComparedValue, newCompareValue);

            if (_isDirty)
            {
                _currentValue = newValue;
                
                if (_autoUpdateOnDirty)
                {
                    _lastComparedValue = newCompareValue;
                }
                
                _onDirtyCallback?.Invoke(_currentValue);
            }

            return _isDirty;
        }

        /// <summary>
        /// Manually marks the tracker as dirty
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            _onDirtyCallback?.Invoke(_currentValue);
        }

        /// <summary>
        /// Manually clears the dirty flag and updates the base comparison value
        /// </summary>
        public void ClearDirty()
        {
            _isDirty = false;
            _lastComparedValue = _valueToCompareConverter(_currentValue);
        }

        /// <summary>
        /// Forces an update of the base comparison value without changing dirty state
        /// </summary>
        public void UpdateBaseValue()
        {
            _lastComparedValue = _valueToCompareConverter(_currentValue);
        }
    }

    /// <summary>
    /// Simplified version for when TValue and TCompare are the same type
    /// </summary>
    /// <typeparam name="T">The type of the value being tracked</typeparam>
    public class DirtyTracker<T> : DirtyTracker<T, T>
    {
        public DirtyTracker(
            T initialValue,
            Func<T, T, bool> comparer,
            Action<T> onDirtyCallback = null,
            bool autoUpdateOnDirty = true)
            : base(initialValue, x => x, comparer, onDirtyCallback, autoUpdateOnDirty)
        {
        }
    }

    /// <summary>
    /// Static factory methods for common scenarios
    /// </summary>
    public static class DirtyTracker
    {
        /// <summary>
        /// Creates a DirtyTracker for reference types using ReferenceEquals
        /// </summary>
        public static DirtyTracker<T> ForReference<T>(T initialValue, Action<T> onDirtyCallback = null, bool autoUpdateOnDirty = true)
            where T : class
        {
            return new DirtyTracker<T>(initialValue, ReferenceEquals, onDirtyCallback, autoUpdateOnDirty);
        }

        /// <summary>
        /// Creates a DirtyTracker for value types using Equals
        /// </summary>
        public static DirtyTracker<T> ForValue<T>(T initialValue, Action<T> onDirtyCallback = null, bool autoUpdateOnDirty = true)
            where T : struct
        {
            return new DirtyTracker<T>(initialValue, (a, b) => a.Equals(b), onDirtyCallback, autoUpdateOnDirty);
        }

        /// <summary>
        /// Creates a DirtyTracker for collections using SequenceEqual
        /// </summary>
        public static DirtyTracker<List<T>> ForList<T>(List<T> initialValue, Action<List<T>> onDirtyCallback = null, bool autoUpdateOnDirty = true)
        {
            return new DirtyTracker<List<T>>(initialValue, 
                (a, b) => a?.SequenceEqual(b ?? new List<T>()) ?? b == null, 
                onDirtyCallback, 
                autoUpdateOnDirty);
        }

        /// <summary>
        /// Creates a DirtyTracker with custom comparer
        /// </summary>
        public static DirtyTracker<T> WithComparer<T>(T initialValue, IEqualityComparer<T> comparer, Action<T> onDirtyCallback = null, bool autoUpdateOnDirty = true)
        {
            return new DirtyTracker<T>(initialValue, comparer.Equals, onDirtyCallback, autoUpdateOnDirty);
        }
    }
}