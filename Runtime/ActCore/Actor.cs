using System;
using UnityEngine;

namespace DataKeeper.ActCore
{
    public class Actor : MonoBehaviour
    {
        public bool TryStopCoroutine(Coroutine routine)
        {
            if (routine == null) return false;
            StopCoroutine(routine);
            return true;
        }
        
        public Coroutine OneSecondUpdate(Action callback) 
            => StartCoroutine(ActEnumerator.OneSecondUpdate(callback));
    
        public Coroutine One(float duration, Action<float> value = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.Float(0f, 1f, duration, value, onComplete));
        public Coroutine Int(int from, int to, float duration, Action<int> value = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.Int(from, to, duration, value, onComplete));
    
        public Coroutine Delta(float duration, Action<float> delta = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.Delta(duration, delta, onComplete));
    
        public Coroutine Float(float from, float to, float duration, Action<float> value = null, Action onComplete = null) 
            => 
            StartCoroutine(ActEnumerator.Float(from, to, duration, value, onComplete));
        public Coroutine Float(float from, float to, float duration, Func<float, float, float, FloatEase> ease, Action<float> value = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.Float(from, to, duration, ease, value, onComplete));
    
        public Coroutine Period(float from, float to, float duration, float callbackPeriod, Action<float> value = null, Action callback = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.Period(from, to, duration, callbackPeriod, value, callback, onComplete));
    
        public Coroutine Timer(float duration, Action<float> value = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.Timer(duration, value, onComplete));
    
        public Coroutine DeltaValue(float value, float duration, Action<float> deltaOfValue = null, Action onComplete = null) 
            => StartCoroutine(ActEnumerator.DeltaValue(value, duration, deltaOfValue, onComplete));
    
        /// <summary>
        /// If time less than 0 - wait 0 seconds. If time equals 0 - wait 1 frame. If time greater than 0 - wait in seconds.
        /// </summary>
        /// <param name="time">in seconds.</param>
        /// <param name="callback">callback on timeout.</param>
        public Coroutine DelayedCall(float time, Action callback) 
            => StartCoroutine(ActEnumerator.WaitSeconds(time, callback));
    
        public Coroutine WaitWhile(Func<bool> wait, Action callback) 
            => StartCoroutine(ActEnumerator.WaitWhile(wait, callback));
        public Coroutine WaitUntil(Func<bool> wait, Action callback) 
            => StartCoroutine(ActEnumerator.WaitUntil(wait, callback));
    }
}
