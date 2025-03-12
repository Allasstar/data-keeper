using System;
using System.Collections.Generic;
using DataKeeper.Extensions;
using DataKeeper.Signals;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Generic
{
    [Serializable]
    public class ReactiveEditorPref<T>
    {
        [SerializeField]
        private T value;
    
        public T DefaultValue { get; private set; }
    
        public string Key { get; private set; }
        
    
        [NonSerialized]
        public Signal<T> OnValueChanged = new Signal<T>();

        private bool _isLoaded = false;

        public ReactiveEditorPref(T defaultValue, string key)
        {
            this.Key = key;
            DefaultValue = defaultValue;
        }
        
        public void Load()
        {
            switch (DefaultValue)
            {
                case int i:
                    value = (T)(object)EditorPrefs.GetInt(Key, i);
                    break;
                case string s:
                    value = (T)(object)EditorPrefs.GetString(Key, s);
                    break;
                case float f:
                    value = (T)(object)EditorPrefs.GetFloat(Key, f);
                    break;
                case bool b:
                    var res = EditorPrefs.GetBool(Key, b);
                    value = (T)(object)res;
                    break;
                case Vector2 v2:
                    value = (T)(object)v2.FromString(EditorPrefs.GetString(Key, "(0.0, 0.0)"));
                    break;
                case Vector3 v3:
                    value = (T)(object)v3.FromString(EditorPrefs.GetString(Key, "(0.0, 0.0, 0.0)"));
                    break;
                case Vector4 v4:
                    value = (T)(object)v4.FromString(EditorPrefs.GetString(Key, "(0.0, 0.0, 0.0, 0.0)"));
                    break;
                case Color col:
                    value = (T)(object)col.FromString(EditorPrefs.GetString(Key, "RGBA(0.0, 0.0, 0.0, 0.0)"));
                    break;
                case Rect rect:
                    value = (T)(object)rect.FromString(EditorPrefs.GetString(Key, "(x:0.0, y:0.0, width:0.0, height:0.00)"));
                    break;
                default:
                    var defaultJson = JsonConvert.SerializeObject(DefaultValue);
                    var json = EditorPrefs.GetString(Key, defaultJson);
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
                Save();
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
            
            if(callOnAddListener) OnValueChanged.Invoke(Value);
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
                    EditorPrefs.SetInt(Key, i);
                    break;
                case string s:
                    EditorPrefs.SetString(Key, s);
                    break;
                case float f:
                    EditorPrefs.SetFloat(Key, f);
                    break;
                case bool b:
                    EditorPrefs.SetBool(Key, b);
                    break;
                case Vector2 v2:
                    EditorPrefs.SetString(Key, v2.ToString());
                    break;
                case Vector3 v3:
                    EditorPrefs.SetString(Key, v3.ToString());
                    break;
                case Vector4 v4:
                    EditorPrefs.SetString(Key, v4.ToString());
                    break;
                case Color col:
                    EditorPrefs.SetString(Key, col.ToString());
                    break;
                case Rect rec:
                    EditorPrefs.SetString(Key, rec.ToString());
                    break;
                default:
                    EditorPrefs.SetString(Key, JsonConvert.SerializeObject(value));
                    break;
            }
        }
    }
}
