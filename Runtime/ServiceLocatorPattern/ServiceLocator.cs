using System.Collections.Generic;
using DataKeeper.ActCore;
using DataKeeper.Extensions;
using DataKeeper.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataKeeper.ServiceLocatorPattern
{
    public static partial class ServiceLocator
    {
        public static readonly Register<object> GlobalRegister = new Register<object>();
        public static readonly Dictionary<string, Register<object>> SceneRegisters = new Dictionary<string, Register<object>>();
        public static readonly Dictionary<GameObject, Register<object>> GameObjectRegisters = new Dictionary<GameObject, Register<object>>();
        public static readonly Dictionary<string, Register<object>> TableRegisters = new Dictionary<string, Register<object>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Reset()
        {
            GlobalRegister.Clear();
            SceneRegisters.Clear();
            GameObjectRegisters.Clear();
            TableRegisters.Clear();
        }
        
        public static void Reg<T>(T value)
        {
            GlobalRegister.Reg<T>(value);
        }
        
        public static void Reg<T>(T value, string id)
        {
            GlobalRegister.Reg(value, id);
        }
        
        public static Register<object> ForGlobal()
        {
            return GlobalRegister;
        }
        
        public static Register<object> ForSceneOf(Component component)
        {
            return ForSceneOf(component.gameObject.scene.name);
        }
        
        public static Register<object> ForSceneOf(GameObject go)
        {
            return ForSceneOf(go.scene.name);
        }
        
        public static Register<object> ForSceneOf(string sceneName)
        {
            if (!SceneRegisters.ContainsKey(sceneName))
            {
                SceneRegisters[sceneName] = new Register<object>();
                
                Act.OnSceneUnloadedEvent.AddListener(SceneUnloaded);

                void SceneUnloaded(Scene scene)
                {
                    Act.OnSceneUnloadedEvent.RemoveListener(SceneUnloaded);
                    SceneRegisters.Remove(scene.name);
                }
            }
            
            return SceneRegisters[sceneName];
        }
        
        public static Register<object> ForTableOf(string tableName)
        {
            if (!TableRegisters.ContainsKey(tableName))
            {
                TableRegisters[tableName] = new Register<object>();
            }
            
            return TableRegisters[tableName];
        }
        
        public static Register<object> ForGameObjectOf(Component component)
        {
            return ForGameObjectOf(component.gameObject);
        }
        
        public static Register<object> ForGameObjectOf(GameObject go)
        {
            if (!GameObjectRegisters.ContainsKey(go))
            {
                GameObjectRegisters[go] = new Register<object>();

                go.GetOrAddComponent<ServiceLocatorGameObjectListener>()
                    .OnDestroyAction(() => GameObjectRegisters.Remove(go));
            }
            
            return GameObjectRegisters[go];
        }
    }
}
