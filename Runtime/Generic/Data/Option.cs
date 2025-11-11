using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic.Data
{
    [Serializable]
    public class Option<TValue, TProvider> where TProvider : ScriptableObject, IValueProvider<TValue>
    {
        [SerializeField] private OptionMode mode = OptionMode.Disabled;
        [SerializeField] private TValue localValue = default(TValue);
        [SerializeField] private TProvider globalProvider = null;

        [JsonIgnore] public bool Enabled => mode != OptionMode.Disabled;
        [JsonIgnore] public OptionMode Mode => mode;
        [JsonIgnore] public TValue LocalValue => localValue;
        [JsonIgnore] public TProvider GlobalProvider => globalProvider;

        [JsonIgnore]
        public TValue Value
        {
            get
            {
                switch (mode)
                {
                    case OptionMode.LocalValue:
                        return localValue;
                    case OptionMode.GlobalValue:
                        return globalProvider != null ? globalProvider.GetValue() : default(TValue);
                    case OptionMode.Disabled:
                    default:
                        return default(TValue);
                }
            }
        }

        public Option()
        {
            mode = OptionMode.Disabled;
            localValue = default(TValue);
            globalProvider = null;
        }

        public Option(TValue value)
        {
            mode = OptionMode.LocalValue;
            localValue = value;
            globalProvider = null;
        }

        public Option(TProvider provider)
        {
            mode = OptionMode.GlobalValue;
            localValue = default(TValue);
            globalProvider = provider;
        }

        public void SetDisabled()
        {
            mode = OptionMode.Disabled;
        }

        public void SetLocalValue(TValue value)
        {
            mode = OptionMode.LocalValue;
            localValue = value;
        }

        public void SetGlobalProvider(TProvider provider)
        {
            mode = OptionMode.GlobalValue;
            globalProvider = provider;
        }

        public static implicit operator TValue(Option<TValue, TProvider> option)
        {
            return option.Value;
        }
 
        public bool TryGetValue(out TValue value)
        {
            value = Value;
            return Enabled && (mode != OptionMode.GlobalValue || globalProvider != null);
        }

        public override string ToString()
        {
            if (!Enabled)
                return "Disabled";
        
            return mode == OptionMode.LocalValue 
                ? $"Local: {localValue}" 
                : $"Global: {(globalProvider != null ? globalProvider.GetValue().ToString() : "null")}";
        }
    }
}