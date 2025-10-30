using UnityEngine;
using UnityEngine.Events;

namespace DataKeeper.Extra
{
    /// <summary>
    /// Exposes Unity lifecycle methods as UnityEvents that can be configured in the Inspector.
    /// Attach this component to any GameObject to hook up lifecycle events without writing code.
    /// </summary>
    [AddComponentMenu("DataKeeper/Extra/Lifecycle Events")]
    public class LifecycleEvents : MonoBehaviour
    {
        [Header("Initialization Events")]
        [Tooltip("Called when the script instance is being loaded")]
        public UnityEvent OnAwakeEvent;
    
        [Tooltip("Called on the frame when the script is enabled, before any Update methods")]
        public UnityEvent OnStartEvent;
    
        [Tooltip("Called when the object becomes enabled and active")]
        public UnityEvent OnEnableEvent;

        [Header("Deactivation Events")]
        [Tooltip("Called when the object becomes disabled")]
        public UnityEvent OnDisableEvent;
    
        [Tooltip("Called when the MonoBehaviour will be destroyed")]
        public UnityEvent OnDestroyEvent;

        [Header("Application Events")]
        public UnityEvent<bool> OnApplicationFocusEvent;
        public UnityEvent<bool> OnApplicationPauseEvent;

        // Lifecycle Methods
        private void Awake()
        {
            OnAwakeEvent?.Invoke();
        }

        private void Start()
        {
            OnStartEvent?.Invoke();
        }

        private void OnEnable()
        {
            OnEnableEvent?.Invoke();
        }

        private void OnDisable()
        {
            OnDisableEvent?.Invoke();
        }

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }

        // Application Events
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