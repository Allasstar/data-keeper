using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataKeeper.Generic
{
    /// <summary>
    /// Shared Json.NET settings used by <see cref="DataFile{T}"/>, <see cref="JsonData{T}"/> and <see cref="ReactivePref{T}"/>.
    /// UnityEngine structs (Vector3, Quaternion, Color, ...) are serialized by fields only, so read-only
    /// properties like <c>Vector3.normalized</c> neither bloat the output nor cause self-reference loops.
    /// Assign or mutate <see cref="Settings"/> (e.g. add converters) before the first save/load to customize.
    /// </summary>
    public static class DataKeeperJson
    {
        private static JsonSerializerSettings _settings;

        public static JsonSerializerSettings Settings
        {
            get => _settings ??= CreateDefaultSettings();
            set => _settings = value;
        }

        public static JsonSerializerSettings CreateDefaultSettings() => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new UnityTypeContractResolver()
        };

        public static string Serialize(object value, Formatting formatting = Formatting.None)
            => JsonConvert.SerializeObject(value, formatting, Settings);

        public static T Deserialize<T>(string json)
            => JsonConvert.DeserializeObject<T>(json, Settings);
    }

    /// <summary>
    /// Serializes UnityEngine value types by their fields (public and private) instead of properties.
    /// </summary>
    public class UnityTypeContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            if (IsUnityValueType(objectType))
            {
                var fields = objectType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var members = new List<MemberInfo>(fields.Length);
                members.AddRange(fields);
                return members;
            }

            return base.GetSerializableMembers(objectType);
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            // Fields mode makes private fields readable/writable (needed for e.g. Bounds, Vector3Int).
            return base.CreateProperties(type, IsUnityValueType(type) ? MemberSerialization.Fields : memberSerialization);
        }

        private static bool IsUnityValueType(Type type)
            => type.IsValueType && !type.IsEnum && !type.IsPrimitive
               && type.Namespace != null && type.Namespace.StartsWith("UnityEngine");
    }
}
