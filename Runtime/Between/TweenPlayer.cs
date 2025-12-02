using System;
using System.Text;
using DataKeeper.Between.Core;
using DataKeeper.MathFunc;
using UnityEngine;

namespace DataKeeper.Between
{
    public class TweenPlayer
    {
        public float FromValue { get; private set; }
        public float ToValue { get; private set; }
        public float DurationValue { get; private set; }
        public EaseType EaseType { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool IsCompleted { get; private set; }
        
        private float currentTime;
        private float currentValue;

        public Action<float> OnValueCallback { get; private set; }
        public Action<float> OnProgressCallback { get; private set; }
        public Action OnCompleteCallback { get; private set; }

        #region Constructor
        
        public TweenPlayer(float duration = 1f)
        {
            FromValue = 0;
            ToValue = 1;
            DurationValue = duration;
            EaseType = EaseType.Linear;
        }
        
        public TweenPlayer(float to, float duration)
        {
            FromValue = 0;
            ToValue = to;
            DurationValue = duration;
            EaseType = EaseType.Linear;
        }
        
        public TweenPlayer(float from, float to, float duration)
        {
            FromValue = from;
            ToValue = to;
            DurationValue = duration;
            EaseType = EaseType.Linear;
        }
        
        #endregion

        public TweenPlayer From(float from)
        {
            FromValue = from;
            return this;
        }
        
        public TweenPlayer To(float to)
        {
            ToValue = to;
            return this;
        }
        
        public TweenPlayer Duration(float duration)
        {
            DurationValue = duration;
            return this;
        }
        
        public TweenPlayer Ease(EaseType easeType)
        {
            EaseType = easeType;
            return this;
        }
        
        public TweenPlayer OnValue(Action<float> callback)
        {
            OnValueCallback = callback;
            return this;
        }
        
        public TweenPlayer OnProgress(Action<float> callback)
        {
            OnProgressCallback = callback;
            return this;
        }
        
        public TweenPlayer OnComplete(Action callback)
        {
            OnCompleteCallback = callback;
            return this;
        }
            
        private void Update()
        {
            if (!IsPlaying) return;
            
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / DurationValue);
            
            currentValue = Mathf.Lerp(FromValue, ToValue, Easing.Apply(progress, EaseType));
            
            OnValueCallback ?.Invoke(currentValue);
            OnProgressCallback ?.Invoke(progress);
            
            if (progress >= 1f)
            {
                IsCompleted = true;
                Stop();
                OnCompleteCallback ?.Invoke();
            }
        }

        #region Override

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(128);
            sb.Append("TweenPlayer [");
            sb.Append("From: ").Append(FromValue).Append(", ");
            sb.Append("To: ").Append(ToValue).Append(", ");
            sb.Append("Duration: ").Append(DurationValue).Append(", ");
            sb.Append("Ease: ").Append(EaseType).Append(", ");
            sb.Append("IsPlaying: ").Append(IsPlaying).Append(", ");
            sb.Append("IsCompleted: ").Append(IsCompleted).Append(", ");
            sb.Append("Progress: ").AppendFormat("{0:P}", currentTime / DurationValue).Append("]");
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if(obj is not TweenPlayer other) return false;

            return FromValue.Equals(other.FromValue) &&
                   ToValue.Equals(other.ToValue) &&
                   DurationValue.Equals(other.DurationValue) &&
                   EaseType == other.EaseType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                hashCode = hashCode * 23 + FromValue.GetHashCode();
                hashCode = hashCode * 23 + ToValue.GetHashCode();
                hashCode = hashCode * 23 + DurationValue.GetHashCode();
                hashCode = hashCode * 23 + EaseType.GetHashCode();
                return hashCode;
            }
        }

        #endregion

        #region Play Controls

        public TweenPlayer Start()
        {
            Reset();
            IsPlaying = true;
            TweenCore.AddToUpdate(Update);
            return this;
        }
    
        public TweenPlayer Stop()
        {
            IsPlaying = false;
            TweenCore.RemoveFromUpdate(Update);
            return this;
        }
        
        public TweenPlayer Pause()
        {
            IsPlaying = false;
            return this;
        }

        public TweenPlayer Resume()
        {
            if (!IsCompleted)
            {
                IsPlaying = true;
            }
            return this;
        }
        
        public TweenPlayer Reset()
        {
            currentTime = 0f;
            IsCompleted = false;
            currentValue = FromValue;
            return this;
        }
        
        public TweenPlayer Restart()
        {
            Reset();
            return Start();
        }
        
        #endregion
    }
}