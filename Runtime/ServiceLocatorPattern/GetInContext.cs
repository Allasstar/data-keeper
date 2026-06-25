using System;
using DataKeeper.Attributes;
using DataKeeper.GameTagSystem;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    /// <summary>
    /// Resolves a service register from a chosen context — the read-side counterpart to
    /// <see cref="ServiceLocatorRegister.RegInContext"/>. Pick a concrete context in the
    /// inspector and call <see cref="Get{T}"/> to pull a registered service.
    /// </summary>
    [Serializable]
    public abstract class GetInContext
    {
        [field: SerializeField] public Optional<GameTag> ComponentID { get; private set; }

        public abstract ContextType GetContextType();
        public abstract Register<object> GetRegister();

        public T Get<T>() where T : class
        {
            var register = GetRegister();
            if (register == null) return null;
            return ComponentID.Enabled ? register.Get<T>(ComponentID.Value.Path) : register.Get<T>();
        }
    }

    [Serializable]
    public class GetInGlobalContext : GetInContext
    {
        public override ContextType GetContextType() => ContextType.Global;

        public override Register<object> GetRegister() => ServiceLocator.ForGlobal();
    }

    [Serializable]
    public class GetInSceneContext : GetInContext
    {
        [field: SerializeField, ObjectComponentPicker] public Component Source { get; private set; }

        public override ContextType GetContextType() => ContextType.Scene;

        public override Register<object> GetRegister()
            => Source != null ? ServiceLocator.ForSceneOf(Source) : null;
    }

    [Serializable]
    public class GetInGameObjectContext : GetInContext
    {
        [field: SerializeField, ObjectComponentPicker] public Component Source { get; private set; }

        public override ContextType GetContextType() => ContextType.GameObject;

        public override Register<object> GetRegister()
            => Source != null ? ServiceLocator.ForGameObjectOf(Source) : null;
    }

    [Serializable]
    public class GetInTableContext : GetInContext
    {
        [field: SerializeField] public GameTag TableName { get; private set; }

        public override ContextType GetContextType() => ContextType.Table;

        public override Register<object> GetRegister() => ServiceLocator.ForTableOf(TableName.Path);
    }
}
