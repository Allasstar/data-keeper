using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DataKeeper.ActCore
{
    public static class Act
    {
        private static ActEngine _actEngine;

        private static ActEngine Engine
        {
            get
            {
                if(!IsInitialized) Init();
                return _actEngine;
            }
        }

        public static bool IsInitialized { get; private set; } = false;
        public static UnityEvent OnApplicationQuitEvent => Engine.OnApplicationQuitEvent;
        public static UnityEvent<bool> OnApplicationFocusEvent => Engine.OnApplicationFocusEvent;
        public static UnityEvent<bool> OnApplicationPauseEvent => Engine.OnApplicationPauseEvent;
        
        public static UnityEvent<Scene, LoadSceneMode> OnSceneLoadedEvent => Engine.OnSceneLoadedEvent;
        public static UnityEvent<Scene> OnSceneUnloadedEvent => Engine.OnSceneUnloadedEvent;
        
        public static UnityEvent OnUpdateEvent => Engine.OnUpdateEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset()
        {
            IsInitialized = false;
        }
        
        public static void Init()
        {
            if (_actEngine != null) return;

            IsInitialized = true;
            GameObject gameObject = new GameObject("[ActEngine]");
            _actEngine = gameObject.AddComponent<ActEngine>();
            Object.DontDestroyOnLoad(gameObject);
        }
        
        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            return Engine.StartCoroutine(coroutine);
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            if (!IsEngineExist()) return;
            Engine.TryStopCoroutine(coroutine);
        }
        
        public static void StopAllCoroutine()
        {
            if (!IsEngineExist()) return;
            Engine.StopAllCoroutines();
        }

        public static bool IsEngineExist()
        {
            return IsInitialized && Engine != null && Engine.gameObject != null && Engine.gameObject.activeInHierarchy;
        }

        public static Coroutine OneSecondUpdate(Action callback) 
            => Engine.OneSecondUpdate(callback);

        public static Coroutine One(float duration, Action<float> value = null, Action onComplete = null) 
            => Engine.One(duration, value, onComplete);
        public static Coroutine Int(int from, int to, float duration, Action<int> value = null, Action onComplete = null) 
            => Engine.Int(from, to, duration, value, onComplete);

        public static Coroutine Delta(float duration, Action<float> delta = null, Action onComplete = null) 
            => Engine.Delta(duration, delta, onComplete);

        public static Coroutine Float(float from, float to, float duration, Action<float> value = null, Action onComplete = null) 
            => Engine.Float(from, to, duration, value, onComplete);
        public static Coroutine Float(float from, float to, float duration, Func<float, float, float, FloatEase> ease, Action<float> value = null, Action onComplete = null) 
            => Engine.Float(from, to, duration, ease, value, onComplete);
        
        public static Coroutine Period(float from, float to, float duration, float callbackPeriod, Action<float> value = null, Action callback = null, Action onComplete = null) 
            => Engine.Period(from, to, duration, callbackPeriod, value, callback, onComplete);
        
        public static Coroutine Timer(float duration, Action<float> value = null, Action onComplete = null) 
            => Engine.Timer(duration, value, onComplete);
        
        public static Coroutine DeltaValue(float value, float duration, Action<float> deltaOfValue = null, Action onComplete = null)
            => Engine.DeltaValue(value, duration, deltaOfValue, onComplete);
        
        /// <summary>
        /// If time less than 0 - wait 0 seconds. If time equals 0 - wait 1 frame. If time greater than 0 - wait in seconds.
        /// </summary>
        /// <param name="time">in seconds.</param>
        /// <param name="callback">callback on timeout.</param>
        public static Coroutine DelayedCall(float time, Action callback) 
            => Engine.DelayedCall(time, callback);

        public static Coroutine WaitWhile(Func<bool> wait, Action callback) 
            => Engine.WaitWhile(wait, callback);
        public static Coroutine WaitUntil(Func<bool> wait, Action callback) 
            => Engine.WaitUntil(wait, callback);

        // New Chain methods
        /// <summary>
        /// Start a new ActChain with the default engine
        /// </summary>
        public static ActChain StartActChain()
        {
            return ActChain.Create(Engine);
        }
    }
}