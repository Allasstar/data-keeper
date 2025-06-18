using System;
using System.Collections.Generic;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    [DefaultExecutionOrder(-10000)]
    [AddComponentMenu("DataKeeper/Service Locator Register", 0)]
    public class ServiceLocatorRegister : MonoBehaviour
    {
        [SerializeReference, SerializeReferenceSelector] private List<RegInContext> _register = new();
        
        private void Awake()
        {
            foreach (var c in _register)
            {
               c?.Register(gameObject);
            }
        }
        
        [Serializable]
        public abstract class RegInContext
        {
            [field: SerializeField] public Optional<string> ComponentID { get; private set; }
            [field: SerializeField] public Component Component { get; private set; }
            
            public abstract ContextType GetContextType();
            public abstract void Register(GameObject owner);
        }
        
        [Serializable]
        public class RegInGlobalContext : RegInContext
        {
            public override ContextType GetContextType() => ContextType.Global;

            public override void Register(GameObject owner)
            {
                if (ComponentID.Enabled)
                {
                    ServiceLocator.ForGlobal().Reg(Component, ComponentID.Value);
                }
                else
                {
                    ServiceLocator.ForGlobal().Reg(Component);
                }
            }
        }
        
        [Serializable]
        public class RegInSceneContext : RegInContext
        {
            public override ContextType GetContextType() => ContextType.Scene;

            public override void Register(GameObject owner)
            {
                if (ComponentID.Enabled)
                {
                    ServiceLocator.ForSceneOf(owner).Reg(Component, ComponentID.Value);
                }
                else
                {
                    ServiceLocator.ForSceneOf(owner).Reg(Component);
                }
            }
        }
        
        [Serializable]
        public class RegInGameObjectContext : RegInContext
        {
            public override ContextType GetContextType() => ContextType.GameObject;

            public override void Register(GameObject owner)
            {
                if (ComponentID.Enabled)
                {
                    ServiceLocator.ForGameObjectOf(owner).Reg(Component, ComponentID.Value);
                }
                else
                {
                    ServiceLocator.ForGameObjectOf(owner).Reg(Component);
                }
            }
        }
        
        [Serializable]
        public class RegInTableContext : RegInContext
        {
            [field: SerializeField] public string TableName { get; private set; }

            public override ContextType GetContextType() => ContextType.Table;

            public override void Register(GameObject owner)
            {
                if (ComponentID.Enabled)
                {
                    ServiceLocator.ForTableOf(TableName).Reg(Component, ComponentID.Value);
                }
                else
                {
                    ServiceLocator.ForTableOf(TableName).Reg(Component);
                }
            }
        }
    }
}
