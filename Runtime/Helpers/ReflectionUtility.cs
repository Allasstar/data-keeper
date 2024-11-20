using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public static class ReflectionUtility
{
    public static List<FieldInfo> GetAllFields(object target)
    {
        return target.GetType()
            .GetFields(BindingFlags.Instance | 
                      BindingFlags.NonPublic | 
                      BindingFlags.Public)
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
                if (value != null)
                {
                    result.Add((T)value);
                }
            }
        }

        return result;
    }
}