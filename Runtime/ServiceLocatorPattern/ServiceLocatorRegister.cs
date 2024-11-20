using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    [DefaultExecutionOrder(-10000)]
    [AddComponentMenu("DataKeeper/Service Locator Register", 0)]
    public class ServiceLocatorRegister : MonoBehaviour
    {
        [SerializeField] private List<ComponentInContext> _register = new();
        
        private void Awake()
        {
            foreach (var c in _register)
            {
                switch (c.contextType)
                {
                    case ServiceLocatorContextType.Global:
                        ServiceLocator.ForGlobal().Reg(c.component);
                        break;
                    case ServiceLocatorContextType.Scene:
                        ServiceLocator.ForSceneOf(this).Reg(c.component);
                        break;
                    case ServiceLocatorContextType.GameObject:
                        ServiceLocator.ForGameObjectOf(this).Reg(c.component);
                        break;
                }
            }
        }
        
        [Serializable]
        public class ComponentInContext
        {
            public ServiceLocatorContextType contextType;
            public Component component;
        }
    }
}
