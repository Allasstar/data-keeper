using System;
using System.Collections.Generic;
using DataKeeper.Signals;
using Newtonsoft.Json;
using Unity.Properties;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public class DirtyTracker<T> : IReactive
    {
        [SerializeField, DontCreateProperty]
        private T value;
    
        [NonSerialized]
        public Signal<T> OnValueChanged = new Signal<T>();
        [NonSerialized]
        public Action Callback;
        
         [NonSerialized] private Func<T, T, bool> comparer;

        public static implicit operator T(DirtyTracker<T> instance)
        {
            return instance.value;
        }

        public DirtyTracker(T value, Func<T, T, bool> comparer = null)
        {
            this.value = value;

            if (comparer == null)
            {
                this.comparer = (a, b) => EqualityComparer<T>.Default.Equals(a, b);
            }
            else
            {
                this.comparer = comparer;
            }
        }

        public void SetCallback(Action callback)
        {
            Callback = callback;
        }

        [CreateProperty]
        public T Value
        {
            get => value;
       
            set
            {
                if (comparer(value, this.value))
                    return;
                
                this.value = value;
                this.Callback?.Invoke();
                this.OnValueChanged?.Invoke(value);
            }
        }
    
        [JsonIgnore]
        public T SilentValue
        {
            get => this.value;
            set => this.value = value;
        }
        
        public void Invoke()
        {
            this.Callback?.Invoke();
            this.OnValueChanged?.Invoke(value);
        }

        public void SilentChange(T value)
        {
            this.value = value;
        }

        public void AddListener(Action<T> call, bool callOnAddListener = false)
        {
            OnValueChanged.AddListener(call);
            
            if(callOnAddListener) call.Invoke(value);
        }
    
        public void RemoveListener(Action<T> call)
        {
            OnValueChanged.RemoveListener(call);
        }

        public void RemoveAllListeners()
        {
            OnValueChanged.RemoveAllListeners();
        }
    
        public override string ToString()
        {
            return value.ToString();
        }

        public void Clear()
        {
            value = default(T);
        }
    }
}