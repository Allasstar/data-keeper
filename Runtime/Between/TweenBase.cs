using System;
using DataKeeper.Between.Core;
using UnityEngine;

namespace DataKeeper.Between
{
    public abstract class TweenBase<T, TV>
    {
        protected T target;
        protected TV startValue;
        protected TV endValue;

        protected float duration;
        protected float speed;
        private float elapsed;
        private EaseType easeType;
        private bool isLooping;
        private int currentLoop;
        private int totalLoops;
        private LoopType loopType;
        private bool isReverse;
        private Action onComplete;
        
        public bool IsComplete { get; private set; }
        public bool IsPaused { get; private set; }

        protected TweenBase(T target)
        {
            this.target = target;
            this.duration = 0.3f;
            this.easeType = EaseType.Linear;
            this.isLooping = false;
            this.totalLoops = 0;
            this.currentLoop = 0;
            this.loopType = LoopType.None;
            this.isReverse = false;
            IsComplete = false;
        }

        public TweenBase<T, TV> From(TV startValue)
        {
            this.startValue = startValue;
            return this;
        }
    
        public TweenBase<T, TV> To(TV endValue)
        {
            this.endValue = endValue;
            return this;
        }
    
        public TweenBase<T, TV> Duration(float duration)
        {
            this.duration = duration;
            return this;
        }

        public abstract TweenBase<T, TV> Speed(float speed);
    
        public TweenBase<T, TV> Loop(int loops = -1, LoopType loopType = LoopType.None)
        {
            this.loopType = loopType;
            this.totalLoops = loops;
            this.currentLoop = 0;
            this.isReverse = false;
            this.isLooping = loopType != LoopType.None && loops != 0;
            return this;
        }
    
        public TweenBase<T, TV> Ease(EaseType easeType)
        {
            this.easeType = easeType;
            return this;
        }
        
        public TweenBase<T, TV> OnComplete(Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }
    
        public virtual void Update()
        {
            if(IsPaused) return;
        
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t >= 1f)
            {
                if (isLooping && (totalLoops == -1 || currentLoop < totalLoops))
                {
                    HandleLoops();
                    return;
                }
                
                t = 1f;
                
                SetTargetValue(endValue);
                
                // Only invoke onComplete when the entire tween (including all loops) is finished
                if (!isLooping || (totalLoops != -1 && currentLoop >= totalLoops))
                {
                    InvokeOnComplete();
                }
                return;
            }

            t = TweenCore.ApplyEasing(t, easeType);
        
            if (loopType == LoopType.PingPong && isReverse)
            {
                t = 1 - t;
            }

            LerpValueAndSetTargetValue(t);
        }
        
        protected abstract void SetTargetValue(TV value);

        protected abstract void LerpValueAndSetTargetValue(float value);

        private void InvokeOnComplete()
        {
            IsComplete = true;
            TweenCore.RemoveFromUpdate(Update);
            onComplete?.Invoke();
        }
    
        protected virtual void HandleLoops()
        {
            elapsed = 0;
        
            if (totalLoops != -1)
            {
                currentLoop++;
            }
        
            switch (loopType)
            {
                case LoopType.None:
                    break;
                
                case LoopType.Restart:
                    HandleRestartLoop(startValue, endValue);
                    break;
                
                case LoopType.PingPong:
                    HandlePingPongLoop(startValue, endValue);
                    break;
                
                case LoopType.Incremental:
                    HandleIncrementLoop(startValue, endValue);
                    break;
            }
        }

        protected virtual void HandleRestartLoop(TV start, TV end)
        {
            SetTargetValue(start);
        }
        
        protected virtual void HandlePingPongLoop(TV start, TV end)
        {
            isReverse = !isReverse;
            if (isReverse)
            {
                SetTargetValue(end);
            }
            else
            {
                SetTargetValue(start);
            }
        }

        protected abstract void HandleIncrementLoop(TV start, TV end);

        private void Reset()
        {
            this.elapsed = 0;
            this.currentLoop = 0;
            this.isReverse = false;
            IsComplete = false;
            IsPaused = false;
        }
        
        public TweenBase<T, TV> Start()
        {
            Reset();
            TweenCore.AddToUpdate(Update);
            return this;
        }
    
        public TweenBase<T, TV> Stop()
        {
            Reset();
            TweenCore.RemoveFromUpdate(Update);
            return this;
        }
        
        public TweenBase<T, TV> Pause()
        {
            IsPaused = true;
            return this;
        }
        
        public TweenBase<T, TV> Unpause()
        {
            IsPaused = false;
            return this;
        }
    }
}