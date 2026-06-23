using System;
using UnityEngine;

public class Singleton<T>
{
    private static T _instance;
    public static T Instance => _instance ??= CreateInstance();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _instance = default;
    }
    
    private static T CreateInstance()
    {
        return Activator.CreateInstance<T>();
    }
}
