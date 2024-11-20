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

        
        public static void Init()
        {
            if (_actEngine != null) return;

            IsInitialized = true;
            GameObject gameObject = new GameObject("[ActEngine]");
            _actEngine = gameObject.AddComponent<ActEngine>();
            Object.DontDestroyOnLoad(gameObject);
        }
        
        public static Coroutine StartCoroutine(IEnumerator coroutine) => Engine.StartCoroutine(coroutine);
        public static void StopCoroutine(Coroutine coroutine) { if (coroutine != null) Engine.StopCoroutine(coroutine); }

        public static Coroutine OneSecondUpdate(Action callback) => StartCoroutine(ActEnumerator.OneSecondUpdate(callback));

        public static void One(float duration, Action<float> value = null, Action onComplete = null) => StartCoroutine(ActEnumerator.Float(0f, 1f, duration, value, onComplete));
        public static void Int(int from, int to, float duration, Action<int> value = null, Action onComplete = null) => StartCoroutine(ActEnumerator.Int(from, to, duration, value, onComplete));

        
        public static void Delta(float duration, Action<float> delta = null, Action onComplete = null) => StartCoroutine(ActEnumerator.Delta(duration, delta, onComplete));

        public static void Float(float from, float to, float duration, Action<float> value = null, Action onComplete = null) => 
            StartCoroutine(ActEnumerator.Float(from, to, duration, value, onComplete));
        public static void Float(float from, float to, float duration, Func<float, float, float, FloatEase> ease, Action<float> value = null, Action onComplete = null) =>
            StartCoroutine(ActEnumerator.Float(from, to, duration, ease, value, onComplete));
        
        public static void Period(float from, float to, float duration, float callbackPeriod, Action<float> value = null, Action callback = null, Action onComplete = null) =>
            StartCoroutine(ActEnumerator.Period(from, to, duration, callbackPeriod, value, callback, onComplete));
        
        public static void Timer(float duration, Action<float> value = null, Action onComplete = null) =>
            StartCoroutine(ActEnumerator.Timer(duration, value, onComplete));
        
        public static void DeltaValue(float value, float duration, Action<float> deltaOfValue = null, Action onComplete = null) =>
            StartCoroutine(ActEnumerator.DeltaValue(value, duration, deltaOfValue, onComplete));
        
        /// <summary>
        /// If time less than 0 - wait 0 seconds. If time equals 0 - wait 1 frame. If time greater than 0 - wait in seconds.
        /// </summary>
        /// <param name="time">in seconds.</param>
        /// <param name="callback">callback on timeout.</param>
        public static void DelayedCall(float time, Action callback) => StartCoroutine(ActEnumerator.WaitSeconds(time, callback));

        public static Coroutine WaitWhile(Func<bool> wait, Action callback) => StartCoroutine(ActEnumerator.WaitWhile(wait, callback));
        public static Coroutine WaitUntil(Func<bool> wait, Action callback) => StartCoroutine(ActEnumerator.WaitUntil(wait, callback));
        
        
        
    }
}