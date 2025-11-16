using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    public static partial class ServiceLocator
    {
        private static BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly Queue<(object Target, MemberInfo Member, ResolveAttribute Attribute)> _unresolvedQueue
            = new Queue<(object, MemberInfo, ResolveAttribute)>();

        public static void Resolve(object target)
        {
            var type = target.GetType();
            var members = new List<MemberInfo>();

            // Get all fields and properties with Inject attribute
            members.AddRange(type.GetFields(_bindingFlags));
            members.AddRange(type.GetProperties(_bindingFlags));

            foreach (var member in members)
            {
                var injectAttribute = member.GetCustomAttribute<ResolveAttribute>();
                if (injectAttribute == null) continue;

                if (!TryResolveValue(target, member, injectAttribute))
                {
                    _unresolvedQueue.Enqueue((target, member, injectAttribute));
                }
            }
        }

        private static bool TryResolveValue(object target, MemberInfo member, ResolveAttribute attribute)
        {
            Type memberType = member is FieldInfo field ? field.FieldType : ((PropertyInfo)member).PropertyType;
            object value = null;

            if (attribute.Context == ContextType.Any)
            {
                // Try resolve in order: Global > Scene > GameObject > Table
                value = TryResolveGlobal(memberType, attribute.ID) ??
                        TryResolveScene(target, memberType, attribute.ID) ??
                        TryResolveGameObject(target, memberType, attribute.ID) ??
                        (string.IsNullOrEmpty(attribute.TableName)
                            ? null
                            : TryResolveTable(memberType, attribute.ID, attribute.TableName));
            }
            else
            {
                value = attribute.Context switch
                {
                    ContextType.Global => TryResolveGlobal(memberType, attribute.ID),
                    ContextType.Scene => TryResolveScene(target, memberType, attribute.ID),
                    ContextType.GameObject => TryResolveGameObject(target, memberType, attribute.ID),
                    ContextType.Table => TryResolveTable(memberType, attribute.ID, attribute.TableName),
                    _ => null
                };
            }

            if (value != null)
            {
                SetMemberValue(target, member, value);
                return true;
            }

            return false;
        }

        private static object TryResolveGlobal(Type type, string id)
        {
            return string.IsNullOrEmpty(id) ? GlobalRegister.Get(type) : GlobalRegister.Get(type, id);
        }

        private static object TryResolveScene(object target, Type type, string id)
        {
            if (target is Component component)
            {
                var register = ForSceneOf(component);
                return string.IsNullOrEmpty(id) ? register.Get(type) : register.Get(type, id);
            }

            return null;
        }

        private static object TryResolveGameObject(object target, Type type, string id)
        {
            if (target is Component component)
            {
                var register = ForGameObjectOf(component);
                return string.IsNullOrEmpty(id) ? register.Get(type) : register.Get(type, id);
            }

            return null;
        }

        private static object TryResolveTable(Type type, string id, string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return null;

            var register = ForTableOf(tableName);
            return string.IsNullOrEmpty(id) ? register.Get(type) : register.Get(type, id);
        }

        private static void SetMemberValue(object target, MemberInfo member, object value)
        {
            try
            {
                switch (member)
                {
                    case FieldInfo field:
                        field.SetValue(target, value);
                        break;
                    case PropertyInfo property:
                        property.SetValue(target, value);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set value for {member.Name} on {target.GetType().Name}: {e.Message}");
            }
        }

        // For future
        private static void TryResolveQueue()
        {
            var currentQueueCount = _unresolvedQueue.Count;
            for (int i = 0; i < currentQueueCount; i++)
            {
                var (target, member, attribute) = _unresolvedQueue.Dequeue();
                if (!TryResolveValue(target, member, attribute))
                {
                    _unresolvedQueue.Enqueue((target, member, attribute));
                }
            }
        }
    }
}