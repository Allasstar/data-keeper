using DataKeeper.Extensions;
using UnityEngine;

namespace DataKeeper.SingletonPattern
{
    internal static class MonoSingletonContainer
    {
        private static GameObject _container;
        internal static GameObject Get()
        {
            if (_container == null)
                _container = MonoFactory.Create("[Singletons]", true);
            return _container;
        }
    }
}