using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    public class JsonData<T>
    {
        public string ToJSON(Formatting formatting = Formatting.None)
        {
            try
            {
                return JsonConvert.SerializeObject(this, formatting);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return null;
        }

        public T FromJSON(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return default;
        }
    }
}