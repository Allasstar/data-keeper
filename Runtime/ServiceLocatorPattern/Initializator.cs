using DataKeeper.Base;
using UnityEngine;

namespace DataKeeper.ServiceLocatorPattern
{
    public static class Initializator
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoadRuntimeMethod()
        {
            var sos = Resources.LoadAll<SO>("");
            foreach (var so in sos)
            {
                so.Initialize();
            }
        }
    }
}
