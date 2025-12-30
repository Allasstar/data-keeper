using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DataKeeper.Helpers
{
    public static class ReflectionHelper
    {
        private const string BackingFieldSuffix = ">k__BackingField";
        private const string OpenBracket = "<";
    
        public static string ExtractFieldName(FieldInfo fieldInfo) => ExtractFieldName((MemberInfo)fieldInfo);

        public static List<FieldInfo> GetAllFields(object target)
        {
            return target.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .ToList();
        }

        public static List<FieldInfo> GetSerializedFields(object target)
        {
            return GetAllFields(target)
                .Where(field => field.GetCustomAttribute<SerializeField>() != null)
                .ToList();
        }

        public static List<T> GetFieldsOfType<T>(object target)
        {
            var fields = GetAllFields(target);
            var result = new List<T>();
            foreach (var field in fields)
            {
                if (typeof(T).IsAssignableFrom(field.FieldType))
                {
                    var value = field.GetValue(target);
                    if (value != null) result.Add((T)value);
                }
            }
            return result;
        }
    
        public static FieldInfo[] GetFieldsWithAttribute(object obj, Type attribute)
        {
            return obj.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.GetCustomAttributes(attribute, true).Length > 0)
                .ToArray();
        }
    
        public static PropertyInfo[] GetPropertiesWithAttribute(object obj, Type attribute)
        {
            return obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttributes(attribute, true).Length > 0)
                .ToArray();
        }
        
        public static MemberInfo GetFieldOrProperty(Type type, string name)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var field = type.GetField(name, flags);
            if (field != null)
                return field;

            var prop = type.GetProperty(name, flags);
            if (prop != null)
                return prop;

            var backingField = type.GetField($"<{name}>k__BackingField", flags);
            return backingField;
        }
    
        public static object GetMemberValue(object source, string memberName)
        {
            if (source == null || string.IsNullOrEmpty(memberName)) return null;
            Type objectType = source.GetType();
            var field = objectType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field.GetValue(source);
            var property = objectType.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property?.GetValue(source);
        }
    
        public static object GetMemberField(object source, string memberName)
        {
            if (source == null || string.IsNullOrEmpty(memberName)) return null;
            Type objectType = source.GetType();
            var field = objectType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(source);
        }
    
        public static object GetMemberProperty(object source, string memberName)
        {
            if (source == null || string.IsNullOrEmpty(memberName)) return null;
            Type objectType = source.GetType();
            var property = objectType.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property?.GetValue(source);
        }
    
        public static object DeepCloneObject(object source)
        {
            if (source == null) return null;
            var newObject = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(source.GetType());
            var fields = source.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields) field.SetValue(newObject, field.GetValue(source));
            return newObject;
        }

        public static string ExtractFieldName(MemberInfo memberInfo)
        {
            return memberInfo.Name
                .Replace(OpenBracket, string.Empty)
                .Replace(BackingFieldSuffix, string.Empty);
        }

        public static List<MemberInfo> GetAllMembers(object target)
        {
            if (target == null) return new List<MemberInfo>();
            var type = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            
            var fields = type.GetFields(flags).Cast<MemberInfo>();
            var props = type.GetProperties(flags).Where(p => p.GetIndexParameters().Length == 0).Cast<MemberInfo>();
            
            return fields.Union(props).ToList();
        }

        public static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo f) return f.FieldType;
            if (member is PropertyInfo p) return p.PropertyType;
            return null;
        }

        public static object GetValue(object target, MemberInfo member)
        {
            if (member is FieldInfo f) return f.GetValue(target);
            if (member is PropertyInfo p) return p.GetValue(target);
            return null;
        }

        public static void SetMemberValue(object target, MemberInfo member, object value)
        {
            if (member is FieldInfo field)
            {
                field.SetValue(target, value);
            }
            else if (member is PropertyInfo prop)
            {
                // The 'true' argument allows finding private setters
                var setter = prop.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(target, new[] { value });
                }
                else
                {
                    // Fallback for auto-properties without any setter (getter-only)
                    var backingField = target.GetType().GetField($"<{prop.Name}>{BackingFieldSuffix}", 
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    backingField?.SetValue(target, value);
                }
            }
        }
    }
}