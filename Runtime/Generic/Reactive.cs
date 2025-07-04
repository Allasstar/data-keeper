using System;
using System.Collections.Generic;
using DataKeeper.Signals;
using Newtonsoft.Json;
using Unity.Properties;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public class Reactive<T> : IReactive
    {
        [SerializeField, DontCreateProperty]
        private T value;
    
        [NonSerialized]
        public Signal<T> OnValueChanged = new Signal<T>();

        public static implicit operator T(Reactive<T> instance)
        {
            return instance.value;
        }
        
        public Reactive()
        {
            this.value = default(T);
        }

        public Reactive(T value)
        {
            this.value = value;
        }

        [CreateProperty]
        public T Value
        {
            get => this.value;
       
            set
            {
                this.value = value;
                this.OnValueChanged?.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public T UniqueValue
        {
            get => Value;
       
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, this.value))
                    return;
                
                Value = value;
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
    
    public interface IReactive
    {
        public void Invoke();
    }
}