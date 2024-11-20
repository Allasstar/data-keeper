using System;

public class Singleton<T>
{
    private static T _instance;
    public static T Instance => _instance ??= CreateInstance();
    
    private static T CreateInstance()
    {
        return Activator.CreateInstance<T>();
    }
}
