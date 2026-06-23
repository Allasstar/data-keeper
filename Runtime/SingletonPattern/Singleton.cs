using System;

namespace DataKeeper.SingletonPattern
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = CreateInstance();
                return _instance;
            }
        }

        private static T CreateInstance()
        {
            return new T();
        }
    }
}
