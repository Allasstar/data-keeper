using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [Serializable]
    public class Optional<TValue, TProvider> where TProvider : ScriptableObject, IValueProvider<TValue>
    {
        [SerializeField] private OptionalMode mode = OptionalMode.Disabled;
        [SerializeField] private TValue localValue = default(TValue);
        [SerializeField] private TProvider globalProvider = null;

        [JsonIgnore] public bool Enabled => mode != OptionalMode.Disabled;
        [JsonIgnore] public OptionalMode Mode => mode;
        [JsonIgnore] public TValue LocalValue => localValue;
        [JsonIgnore] public TProvider GlobalProvider => globalProvider;

        [JsonIgnore]
        public TValue Value
        {
            get
            {
                switch (mode)
                {
                    case OptionalMode.LocalValue:
                        return localValue;
                    case OptionalMode.GlobalValue:
                        return globalProvider != null ? globalProvider.GetValue() : default(TValue);
                    case OptionalMode.Disabled:
                    default:
                        return default(TValue);
                }
            }
        }

        public Optional()
        {
            mode = OptionalMode.Disabled;
            localValue = default(TValue);
            globalProvider = null;
        }

        public Optional(TValue value)
        {
            mode = OptionalMode.LocalValue;
            localValue = value;
            globalProvider = null;
        }

        public Optional(TProvider provider)
        {
            mode = OptionalMode.GlobalValue;
            localValue = default(TValue);
            globalProvider = provider;
        }

        public void SetDisabled()
        {
            mode = OptionalMode.Disabled;
        }

        public void SetLocalValue(TValue value)
        {
            mode = OptionalMode.LocalValue;
            localValue = value;
        }

        public void SetGlobalProvider(TProvider provider)
        {
            mode = OptionalMode.GlobalValue;
            globalProvider = provider;
        }

        public static implicit operator TValue(Optional<TValue, TProvider> optional)
        {
            return optional.Value;
        }
 
        public bool TryGetValue(out TValue value)
        {
            value = Value;
            return Enabled && (mode != OptionalMode.GlobalValue || globalProvider != null);
        }

        public override string ToString()
        {
            if (!Enabled)
                return "Disabled";
        
            return mode == OptionalMode.LocalValue 
                ? $"Local: {localValue}" 
                : $"Global: {(globalProvider != null ? globalProvider.GetValue().ToString() : "null")}";
        }
    }
}