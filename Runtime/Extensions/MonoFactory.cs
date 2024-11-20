using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class MonoFactory 
    {
        public static GameObject Create(string gameObjectName, bool dontDestroyOnLoad = false)
        {
            var go = new GameObject(gameObjectName);
            if (dontDestroyOnLoad == true) go.DontDestroyOnLoad();
            return go;
        }
        
        public static T Create<T>(string gameObjectName, Transform parent = null) where T : MonoBehaviour
        {
            var go = new GameObject(gameObjectName);
            if (parent != null) go.SetParent(parent);
            return go.AddComponent<T>();
        }
        
        public static GameObject Create<T>(string gameObjectName, out T component) where T : MonoBehaviour
        {
            var go = new GameObject(gameObjectName);
            component = go.AddComponent<T>();
            return go;
        }

        public static GameObject DontDestroyOnLoad(this GameObject go)
        {
            Object.DontDestroyOnLoad(go);
            return go;
        }
        
        public static GameObject SetParent(this GameObject go, GameObject parent)
        {
            go.transform.SetParent(parent.transform);
            return go;
        }
        
        public static GameObject SetParent(this GameObject go, Transform parent)
        {
            go.transform.SetParent(parent);
            return go;
        }

        public static void Destroy(this GameObject go)
        {
            Object.Destroy(go);
        }
        
        public static void Destroy(this GameObject go, float delay)
        {
            Object.Destroy(go, delay);
        }
    }
}
