using System;
using DataKeeper.ActCore;
using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class MonoBehaviourExtension
    {
        public static bool TryStopCoroutine(this MonoBehaviour monoBehaviour, Coroutine routine)
        {
            if (routine == null) return false;
            monoBehaviour.StopCoroutine(routine);
            return true;
        }
        
        public static Coroutine OneSecondUpdate(this MonoBehaviour monoBehaviour, Action callback) 
            => monoBehaviour.StartCoroutine(ActEnumerator.OneSecondUpdate(callback));

        public static Coroutine One(this MonoBehaviour monoBehaviour, float duration, Action<float> value = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Float(0f, 1f, duration, value, onComplete));
        
        public static Coroutine Int(this MonoBehaviour monoBehaviour, int from, int to, float duration, Action<int> value = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Int(from, to, duration, value, onComplete));

        public static Coroutine Delta(this MonoBehaviour monoBehaviour, float duration, Action<float> delta = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Delta(duration, delta, onComplete));

        public static Coroutine Float(this MonoBehaviour monoBehaviour, float from, float to, float duration, Action<float> value = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Float(from, to, duration, value, onComplete));
        
        public static Coroutine Float(this MonoBehaviour monoBehaviour, float from, float to, float duration, Func<float, float, float, FloatEase> ease, Action<float> value = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Float(from, to, duration, ease, value, onComplete));

        public static Coroutine Period(this MonoBehaviour monoBehaviour, float from, float to, float duration, float callbackPeriod, Action<float> value = null, Action callback = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Period(from, to, duration, callbackPeriod, value, callback, onComplete));

        public static Coroutine Timer(this MonoBehaviour monoBehaviour, float duration, Action<float> value = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.Timer(duration, value, onComplete));

        public static Coroutine DeltaValue(this MonoBehaviour monoBehaviour, float value, float duration, Action<float> deltaOfValue = null, Action onComplete = null) 
            => monoBehaviour.StartCoroutine(ActEnumerator.DeltaValue(value, duration, deltaOfValue, onComplete));

        /// <summary>
        /// If time less than 0 - wait 0 seconds. If time equals 0 - wait 1 frame. If time greater than 0 - wait in seconds.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to start the coroutine on.</param>
        /// <param name="time">in seconds.</param>
        /// <param name="callback">callback on timeout.</param>
        public static Coroutine DelayedCall(this MonoBehaviour monoBehaviour, float time, Action callback) 
            => monoBehaviour.StartCoroutine(ActEnumerator.WaitSeconds(time, callback));

        public static Coroutine WaitWhile(this MonoBehaviour monoBehaviour, Func<bool> wait, Action callback) 
            => monoBehaviour.StartCoroutine(ActEnumerator.WaitWhile(wait, callback));
        
        public static Coroutine WaitUntil(this MonoBehaviour monoBehaviour, Func<bool> wait, Action callback) 
            => monoBehaviour.StartCoroutine(ActEnumerator.WaitUntil(wait, callback));
    }
}