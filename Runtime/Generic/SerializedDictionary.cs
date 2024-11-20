using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField, JsonIgnore] List<Pair> _pairs;

        public void OnAfterDeserialize()
        {
            Clear();
            foreach (var pair in _pairs)
                if (!ContainsKey(pair.key))
                    Add(pair.key, pair.value);
        }

        public void OnBeforeSerialize()
        {
            if (!Application.isPlaying)
                return;

            _pairs.Clear();
            foreach (var (k, v) in this)
                _pairs.Add(new Pair(k, v));
        }

        [System.Serializable]
        struct Pair
        {
            public TKey key;
            public TValue value;

            public Pair(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }
    }
}