using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public static class ReflectionUtility
{
    private const string BackingFieldSuffix = ">k__BackingField";
    private const string OpenBracket = "<";
    
    public static string ExtractFieldName(FieldInfo fieldInfo)
    {
        return fieldInfo.Name
            .Replace(OpenBracket, string.Empty)
            .Replace(BackingFieldSuffix, string.Empty);
    }

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
    
    public static FieldInfo[] GetFieldsWithAttribute(object obj, Type attribute)
    {
        return obj.GetType()
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field => field.GetCustomAttributes(attribute, true).Length > 0)
            .ToArray();
    }
    
    public static PropertyInfo[] GetPropertiesWithAttribute(object obj, Type attribute)
    {
        return obj.GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(prop => prop.GetCustomAttributes(attribute, true).Length > 0)
            .ToArray();
    }
    
    public static object GetMemberValue(object source, string memberName)
    {
        if (source == null || string.IsNullOrEmpty(memberName))
            return null;

        Type objectType = source.GetType();
        var field = objectType.GetField(memberName);

        if (field != null)
            return field.GetValue(source);

        var property = objectType.GetProperty(memberName);
        return property?.GetValue(source);
    }
    
    public static object GetMemberField(object source, string memberName)
    {
        if (source == null || string.IsNullOrEmpty(memberName))
            return null;
        
        Type objectType = source.GetType();
        var property = objectType.GetField(memberName);
        return property?.GetValue(source);
    }
    
    public static object GetMemberProperty(object source, string memberName)
    {
        if (source == null || string.IsNullOrEmpty(memberName))
            return null;

        Type objectType = source.GetType();
        var property = objectType.GetProperty(memberName);
        return property?.GetValue(source);
    }
    
    public static object DeepCloneObject(object source)
    {
        if (source == null) return null;
        
        var newObject = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(source.GetType());
        var fields = source.GetType().GetFields(System.Reflection.BindingFlags.Public | 
                                                System.Reflection.BindingFlags.NonPublic | 
                                                System.Reflection.BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            field.SetValue(newObject, field.GetValue(source));
        }
        
        return newObject;
    }

}