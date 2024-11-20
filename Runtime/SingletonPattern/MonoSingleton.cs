using DataKeeper.Extensions;
using UnityEngine;

namespace DataKeeper.SingletonPattern
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        public static T Instance => _instance ??= CreateInstance();

        private static Transform _container;
        private static Transform Container => _container ??= CreateContainer();
     
        
        private static T CreateInstance()
        {
            return MonoFactory.Create<T>($"{typeof(T).Name} (Singleton)", Container);
        }

        private static Transform CreateContainer()
        {
            return MonoFactory.Create($"[Singletons]", true).transform;
        }
    }
}

