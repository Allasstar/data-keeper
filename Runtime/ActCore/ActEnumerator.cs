using System;
using System.Collections;
using UnityEngine;

namespace DataKeeper.ActCore
{
    public static class ActEnumerator
    {
        public static IEnumerator WaitUntil(Func<bool> wait, Action callback)
        {
            yield return new WaitUntil(wait);
            callback?.Invoke();
        }
    
        public static IEnumerator WaitWhile(Func<bool> wait, Action callback)
        {
            yield return new WaitWhile(wait);
            callback?.Invoke();
        }
    
        public static IEnumerator OneSecondUpdate(Action callback)
        {
            var wait = new WaitForSeconds(1);
            
            while (true)
            {
                callback?.Invoke();
                yield return wait;
            }
        }
    
        public static IEnumerator WaitSeconds(float time, Action callback)
        {
            if (time == 0)
            {
                yield return null;
            }
            else if (time > 0)
            {
                yield return new WaitForSeconds(time);
            }
            callback?.Invoke();
        }
    
        public static IEnumerator Int(int from, int to, float duration, Action<int> value, Action onComplete)
        {
            var time = 0f;
            value?.Invoke(from);
            
            while (time <= duration)
            {
                yield return new WaitForEndOfFrame();
                time += Time.deltaTime;
                
                value?.Invoke(Lerp.Int(from, to, time / duration));
            }
            
            value?.Invoke(to);
            onComplete?.Invoke();
        }
    
        public static IEnumerator Delta(float duration, Action<float> delta, Action onComplete)
        {
            var time = 0f;
            
            while (time <= duration)
            {
                yield return null;
                time += Time.deltaTime;
                delta?.Invoke(Time.deltaTime);
            }
            
            onComplete?.Invoke();
        }
        
        public static IEnumerator DeltaValue(float value, float duration, Action<float> deltaOfValue, Action onComplete = null)
        {
            float elapsedTime = 0f;
            float lastValue = 0f;

            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;

                float currentValue = Mathf.Lerp(0, value, elapsedTime / duration);
                float delta = currentValue - lastValue;

                if (delta != 0)
                {
                    deltaOfValue?.Invoke(delta);
                }

                lastValue = currentValue;
            }

            float finalDelta = value - lastValue;
            if (finalDelta != 0)
            {
                deltaOfValue?.Invoke(finalDelta);
            }

            onComplete?.Invoke();
        }
        
        public static IEnumerator Float(float from, float to, float duration, Action<float> value, Action onComplete)
        {
            var time = 0f;
            value?.Invoke(from);
            
            while (time <= duration)
            {
                yield return null;
                time += Time.deltaTime;
                
                value?.Invoke(Lerp.Float(from, to, time / duration));
            }
            
            value?.Invoke(to);
            onComplete?.Invoke();
        }
        
        public static IEnumerator Float(float from, float to, float duration, Func<float, float, float, FloatEase> ease, Action<float> value, Action onComplete)
        {
            var time = 0f;
            value?.Invoke(from);
            
            while (time <= duration)
            {
                yield return null;
                time += Time.deltaTime;
                
                value?.Invoke(ease(time / duration, from, to));
            }
            
            value?.Invoke(to);
            onComplete?.Invoke();
        }
        
        public static IEnumerator Period(float from, float to, float duration, float callbackPeriod, Action<float> value, Action callback, Action onComplete)
        {
            var time = 0f;
            var lastCallbackTime = 0f;
            value?.Invoke(from);
    
            while (time <= duration)
            {
                yield return null;
                time += Time.deltaTime;
        
                float currentValue = Lerp.Float(from, to, time / duration);
                value?.Invoke(currentValue);
        
                if (time - lastCallbackTime >= callbackPeriod)
                {
                    callback?.Invoke();
                    lastCallbackTime = time;
                }
            }
    
            callback?.Invoke();
            value?.Invoke(to);
            onComplete?.Invoke();
        }

        public static IEnumerator Timer(float duration, Action<float> value, Action onComplete)
        {
            var time = 0f;
            value?.Invoke(duration);
            
            while (time <= duration)
            {
                yield return null;
                time += Time.deltaTime;
                
                value?.Invoke(Lerp.Float(duration, 0, time / duration));
            }
            
            value?.Invoke(0);
            onComplete?.Invoke();
        }
    }
}
