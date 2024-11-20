using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class ComponentExtension
    {
        public static void SetParentGameObjectActive(this Component component, bool isActive)
        {
            if (component == null) return;
            component.transform.parent.gameObject.SetActive(isActive);
        }
        
        public static void SetGameObjectActive(this Component component, bool isActive)
        {
            if (component == null) return;
            component.gameObject.SetActive(isActive);
        }

        public static bool TryGetComponent<T>(this Component component, out T result) where T : Component
        {
            result = component.GetComponent<T>();
            return result != null;
        }
        
        public static bool TryGetComponent<T>(this GameObject go, out T result) where T : Component
        {
            result = go.GetComponent<T>();
            return result != null;
        }

        public static bool HasComponent<T>(this Component component) where T : Component
        {
            return component.GetComponent<T>() != null;
        }
        
        public static bool HasComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() != null;
        }

        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            var existingComponent = component.GetComponent<T>();
            return existingComponent != null ? existingComponent : component.gameObject.AddComponent<T>();
        }
        
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var existingComponent = go.GetComponent<T>();
            return existingComponent != null ? existingComponent : go.gameObject.AddComponent<T>();
        }
    }
}