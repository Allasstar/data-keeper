using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DataKeeper.ActCore
{
    public class ActEngine : MonoBehaviour
    {
        public readonly UnityEvent OnApplicationQuitEvent = new UnityEvent();
        public readonly UnityEvent<bool> OnApplicationFocusEvent = new UnityEvent<bool>();
        public readonly UnityEvent<bool> OnApplicationPauseEvent = new UnityEvent<bool>();
        
        public readonly UnityEvent<Scene, LoadSceneMode> OnSceneLoadedEvent = new UnityEvent<Scene, LoadSceneMode>();
        public readonly UnityEvent<Scene> OnSceneUnloadedEvent = new UnityEvent<Scene>();
        
        public readonly UnityEvent OnUpdateEvent = new UnityEvent();

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Update()
        {
            OnUpdateEvent?.Invoke();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            
            OnApplicationQuitEvent.RemoveAllListeners();
            OnApplicationFocusEvent.RemoveAllListeners();
            OnApplicationPauseEvent.RemoveAllListeners();
                
            OnSceneLoadedEvent.RemoveAllListeners();
            OnSceneUnloadedEvent.RemoveAllListeners();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            OnSceneLoadedEvent?.Invoke(scene, mode);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            OnSceneUnloadedEvent?.Invoke(scene);
        }
        
        private void OnApplicationQuit()
        {
            OnApplicationQuitEvent?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            OnApplicationFocusEvent?.Invoke(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnApplicationPauseEvent?.Invoke(pauseStatus);
        }
    }
}