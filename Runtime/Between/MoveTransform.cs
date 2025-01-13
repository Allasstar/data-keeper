using System;
using UnityEngine;

namespace DataKeeper.Between
{
    public class MoveTransform<T> where T : Transform
    {
        private T target;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private float duration;
        private float speed;
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
    
        public MoveTransform(T target)
        {
            this.target = target;
            this.startPosition = target.position;
            this.endPosition = target.position + Vector3.forward;
            this.duration = 0.3f;
            this.easeType = EaseType.Linear;
            this.isLooping = false;
            this.totalLoops = 0;
            this.currentLoop = 0;
            this.loopType = LoopType.None;
            this.isReverse = false;
            IsComplete = false;
        }

        public MoveTransform<T> From(Vector3 startPosition)
        {
            this.startPosition = startPosition;
            return this;
        }
    
        public MoveTransform<T> To(Vector3 endPosition)
        {
            this.endPosition = endPosition;
            return this;
        }
    
        public MoveTransform<T> Duration(float duration)
        {
            this.duration = duration;
            return this;
        }
    
        public MoveTransform<T> Speed(float speed)
        {
            this.speed = speed;
            if (speed > 0)
            {
                this.duration = Vector3.Distance(startPosition, endPosition) / speed;
            }
            return this;
        }
    
        public MoveTransform<T> Loop(int loops = -1, LoopType loopType = LoopType.None)
        {
            this.loopType = loopType;
            this.totalLoops = loops;
            this.currentLoop = 0;
            this.isReverse = false;
            this.isLooping = loopType != LoopType.None && loops != 0;
            return this;
        }
    
        public MoveTransform<T> Ease(EaseType easeType)
        {
            this.easeType = easeType;
            return this;
        }
        
        public MoveTransform<T> OnComplete(Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }
    
        public void Update()
        {
            if(IsPaused) return;
        
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t >= 1f)
            {
                if (isLooping && (totalLoops == -1 || currentLoop < totalLoops))
                {
                    HandleLoopComplete();
                    return;
                }
                
                t = 1f;
                target.position = endPosition;
                
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

            try
            {
                if (float.IsNaN(t) || float.IsInfinity(t))
                {
                    Debug.Log($"t: {t} :: easeType: {easeType}");
                }
                else
                {
                    target.position = Vector3.Lerp(startPosition, endPosition, t);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Catch > easeType: {easeType}");
            }
        }

        private void InvokeOnComplete()
        {
            IsComplete = true;
            TweenCore.RemoveFromUpdate(Update);
            onComplete?.Invoke();
        }
    
        private void HandleLoopComplete()
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
                    target.position = startPosition;
                    break;
                
                case LoopType.PingPong:
                    isReverse = !isReverse;
                    if (isReverse)
                    {
                        target.position = endPosition;
                    }
                    else
                    {
                        target.position = startPosition;
                    }
                    break;
                
                case LoopType.Incremental:
                    Vector3 increment = endPosition - startPosition;
                    startPosition = endPosition;
                    endPosition += increment;
                    target.position = startPosition;
                    break;
            }
        }

        private void Reset()
        {
            this.elapsed = 0;
            this.currentLoop = 0;
            this.isReverse = false;
            IsComplete = false;
            IsPaused = false;
        }
        
        public MoveTransform<T> Start()
        {
            Reset();
            TweenCore.AddToUpdate(Update);
            return this;
        }
    
        public MoveTransform<T> Stop()
        {
            Reset();
            TweenCore.RemoveFromUpdate(Update);
            return this;
        }
        
        public MoveTransform<T> Pause()
        {
            IsPaused = true;
            return this;
        }
        
        public MoveTransform<T> Unpause()
        {
            IsPaused = false;
            return this;
        }
    }
}