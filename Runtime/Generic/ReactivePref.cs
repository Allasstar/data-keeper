using System;
using System.Collections.Generic;
using System.Globalization;
using DataKeeper.Extensions;
using DataKeeper.Signals;
using Newtonsoft.Json;
using Unity.Properties;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public class ReactivePref<T> : IReactivePref, IReactive<T>
    {
        [SerializeField, DontCreateProperty]
        private T value;
    
        public T DefaultValue { get; private set; }
    
        public string Key { get; private set; }
        
    
        [NonSerialized]
        public Signal<T> OnValueChanged = new Signal<T>();

        private readonly bool _autoSave;
        private bool _isLoaded = false;
        private IPrefsStorage _storage;

        /// <summary>
        /// Prefs backend of this pref. Defaults to the global <see cref="DataKeeperStorage.Prefs"/>;
        /// assign (or pass to the constructor) a provider to override it per pref.
        /// </summary>
        public IPrefsStorage Storage
        {
            get => _storage ?? DataKeeperStorage.Prefs;
            set => _storage = value;
        }

        public ReactivePref(T defaultValue, string key, bool autoSave = true, IPrefsStorage storage = null)
        {
            this.Key = key;
            DefaultValue = defaultValue;
            _autoSave = autoSave;
            _storage = storage;
        }

        public void Load()
        {
            IPrefsStorage prefs = Storage;

            switch (DefaultValue)
            {
                case int i:
                    value = (T)(object)prefs.GetInt(Key, i);
                    break;
                case string s:
                    value = (T)(object)prefs.GetString(Key, s);
                    break;
                case float f:
                    value = (T)(object)prefs.GetFloat(Key, f);
                    break;
                case bool b:
                    var res = prefs.GetInt(Key, b ? 1 : 0) == 1;
                    value = (T)(object)res;
                    break;
                case Vector2 v2:
                    value = (T)(object)v2.FromString(prefs.GetString(Key, "(0.0, 0.0)"));
                    break;
                case Vector3 v3:
                    value = (T)(object)v3.FromString(prefs.GetString(Key, "(0.0, 0.0, 0.0)"));
                    break;
                case Vector4 v4:
                    value = (T)(object)v4.FromString(prefs.GetString(Key, "(0.0, 0.0, 0.0, 0.0)"));
                    break;
                case Color col:
                    value = (T)(object)col.FromString(prefs.GetString(Key, "RGBA(0.0, 0.0, 0.0, 0.0)"));
                    break;
                case Rect rect:
                    value = (T)(object)rect.FromString(prefs.GetString(Key, "(x:0.0, y:0.0, width:0.0, height:0.00)"));
                    break;
                default:
                    var defaultJson = JsonConvert.SerializeObject(DefaultValue, DataKeeperJson.Settings);
                    var json = prefs.GetString(Key, defaultJson);
                    value = JsonConvert.DeserializeObject<T>(json, DataKeeperJson.Settings);
                    break;
            }
        }

        [CreateProperty]
        public T Value
        {
            get
            {
                if (!_isLoaded)
                {
                    Load();
                    _isLoaded = true;
                }
                return this.value;
            }

            set
            {
                this.value = value;
                if(_autoSave) Save();
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
            get
            {
                if (!_isLoaded)
                {
                    Load();
                    _isLoaded = true;
                }
                return this.value;
            }
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
            
            if(callOnAddListener) call.Invoke(Value);
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
            return value?.ToString() ?? "null";
        }

        public void Reset()
        {
            value = DefaultValue;
        }
    
        public void Save()
        {
            IPrefsStorage prefs = Storage;

            switch (value)
            {
                case int i:
                    prefs.SetInt(Key, i);
                    break;
                case string s:
                    prefs.SetString(Key, s);
                    break;
                case float f:
                    prefs.SetFloat(Key, f);
                    break;
                case bool b:
                    prefs.SetInt(Key, b ? 1 : 0);
                    break;
                case Vector2 v2:
                    prefs.SetString(Key, v2.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case Vector3 v3:
                    prefs.SetString(Key, v3.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case Vector4 v4:
                    prefs.SetString(Key, v4.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case Color col:
                    prefs.SetString(Key, col.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case Rect rec:
                    prefs.SetString(Key, rec.ToString("R", CultureInfo.InvariantCulture));
                    break;
                default:
                    prefs.SetString(Key, JsonConvert.SerializeObject(value, DataKeeperJson.Settings));
                    break;
            }

            prefs.Save();
        }
    }

    public interface IReactivePref : IReactive
    {
        public void Save();
        public void Load();
    }
}
