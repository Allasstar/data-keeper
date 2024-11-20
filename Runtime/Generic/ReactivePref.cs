using System;
using DataKeeper.Extensions;
using DataKeeper.Signals;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public class ReactivePref<T> : IReactivePref
    {
        [SerializeField]
        private T value;
    
        public T DefaultValue { get; private set; }
    
        public string Key { get; private set; }
        
    
        [NonSerialized]
        public Signal<T> OnValueChanged = new Signal<T>();

        private readonly bool _autoSave;
        private bool _isLoaded = false;

        public ReactivePref(T defaultValue, string key, bool autoSave = true)
        {
            this.Key = key;
            DefaultValue = defaultValue;
            _autoSave = autoSave;
        }
        
        public void Load()
        {
            switch (DefaultValue)
            {
                case int i:
                    value = (T)(object)PlayerPrefs.GetInt(Key, i);
                    break;
                case string s:
                    value = (T)(object)PlayerPrefs.GetString(Key, s);
                    break;
                case float f:
                    value = (T)(object)PlayerPrefs.GetFloat(Key, f);
                    break;
                case bool b:
                    var res = PlayerPrefs.GetInt(Key, b ? 1 : 0) == 1;
                    value = (T)(object)res;
                    break;
                case Vector2 v2:
                    value = (T)(object)v2.FromString(PlayerPrefs.GetString(Key, "(0.0, 0.0)"));
                    break;
                case Vector3 v3:
                    value = (T)(object)v3.FromString(PlayerPrefs.GetString(Key, "(0.0, 0.0, 0.0)"));
                    break;
                case Vector4 v4:
                    value = (T)(object)v4.FromString(PlayerPrefs.GetString(Key, "(0.0, 0.0, 0.0, 0.0)"));
                    break;
                case Color col:
                    value = (T)(object)col.FromString(PlayerPrefs.GetString(Key, "RGBA(0.0, 0.0, 0.0, 0.0)"));
                    break;
                case Rect rect:
                    value = (T)(object)rect.FromString(PlayerPrefs.GetString(Key, "(x:0.0, y:0.0, width:0.0, height:0.00)"));
                    break;
                default:
                    var defaultJson = JsonConvert.SerializeObject(DefaultValue);
                    var json = PlayerPrefs.GetString(Key, defaultJson);
                    value = JsonConvert.DeserializeObject<T>(json);
                    break;
            }
        }

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
            
            if(callOnAddListener) OnValueChanged.Invoke(value);
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

        public void Reset()
        {
            value = DefaultValue;
        }
    
        public void Save()
        {
            switch (value)
            {
                case int i:
                    PlayerPrefs.SetInt(Key, i);
                    break;
                case string s:
                    PlayerPrefs.SetString(Key, s);
                    break;
                case float f:
                    PlayerPrefs.SetFloat(Key, f);
                    break;
                case bool b:
                    PlayerPrefs.SetInt(Key, b ? 1 : 0);
                    break;
                case Vector2 v2:
                    PlayerPrefs.SetString(Key, v2.ToString());
                    break;
                case Vector3 v3:
                    PlayerPrefs.SetString(Key, v3.ToString());
                    break;
                case Vector4 v4:
                    PlayerPrefs.SetString(Key, v4.ToString());
                    break;
                case Color col:
                    PlayerPrefs.SetString(Key, col.ToString());
                    break;
                case Rect rec:
                    PlayerPrefs.SetString(Key, rec.ToString());
                    break;
                default:
                    PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(value));
                    break;
            }
       
            PlayerPrefs.Save();
        }
    }

    public interface IReactivePref : IReactive
    {
        public void Save();
        public void Load();
    }
}