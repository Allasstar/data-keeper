using DataKeeper.Extensions;
using UnityEngine;

namespace DataKeeper.SingletonPattern
{
    public class MonoSingleton<T> : MonoBehaviour where T : Component
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
            return MonoFactory.Create<T>($"{typeof(T).Name} (Singleton)", MonoSingletonContainer.Get().transform);
        }
    }
}
