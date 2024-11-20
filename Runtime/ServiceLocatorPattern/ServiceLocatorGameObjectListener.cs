using System;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    public class ServiceLocatorGameObjectListener : MonoBehaviour
    {
        private void Awake()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }

        private Action _onDestroyAction;
        
        public void OnDestroyAction(Action onDestroy)
        {
            _onDestroyAction = onDestroy;
        }
        
        private void OnDestroy()
        {
            _onDestroyAction?.Invoke();
        }
    }
}