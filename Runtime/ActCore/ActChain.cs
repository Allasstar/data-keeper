using System;
using System.Collections;
using System.Collections.Generic;
using DataKeeper.PoolSystem;
using UnityEngine;

namespace DataKeeper.ActCore
{
    public class ActChain : CustomYieldInstruction
    {
        private static readonly PoolObject<ActChain> Chain = 
            new PoolObject<ActChain>(() => new ActChain(), chain => chain.Clear());
        
        private static readonly PoolObject<ActChainStep> Step = 
            new PoolObject<ActChainStep>(() => new ActChainStep(), step => step.Clear());

        private Queue<ActChainStep> _stepQueue = new Queue<ActChainStep>();
        private bool _isRunning = true;
        private MonoBehaviour _executor;

        public override bool keepWaiting => _isRunning;

        internal static ActChain Create(MonoBehaviour executor)
        {
            var chain = Chain.Get();
            chain.Setup(executor);
            return chain;
        }

        private void Setup(MonoBehaviour executor)
        {
            _isRunning = true;
            _executor = executor;
            _executor.StartCoroutine(ExecuteChain());
        }

        private void Clear()
        {
            _executor = null;
            _stepQueue.Clear();
            _isRunning = false;
        }

        private IEnumerator ExecuteChain()
        {
            yield return null;

            while (_stepQueue.Count > 0)
            {
                var step = _stepQueue.Dequeue();
                var coroutine = step.Execute(_executor);
                
                if (coroutine != null)
                    yield return coroutine;
                
                Step.Release(step);
            }

            _isRunning = false;
            Chain.Release(this);
        }

        public ActChain Play(IEnumerator routine)
        {
            var step = Step.Get().SetupCoroutine(routine);
            _stepQueue.Enqueue(step);
            return this;
        }

        public ActChain Wait(float seconds)
        {
            var step = Step.Get().SetupWait(seconds);
            _stepQueue.Enqueue(step);
            return this;
        }

        public ActChain Call(Action action)
        {
            var step = Step.Get().SetupAction(action);
            _stepQueue.Enqueue(step);
            return this;
        }

        public ActChain WaitWhile(Func<bool> condition)
        {
            var step = Step.Get().SetupWaitWhile(condition);
            _stepQueue.Enqueue(step);
            return this;
        }

        public ActChain WaitUntil(Func<bool> condition)
        {
            var step = Step.Get().SetupWaitUntil(condition);
            _stepQueue.Enqueue(step);
            return this;
        }

        public ActChain Parallel(params IEnumerator[] routines)
        {
            var step = Step.Get().SetupParallel(routines);
            _stepQueue.Enqueue(step);
            return this;
        }

        public ActChain Sequential(params IEnumerator[] routines)
        {
            foreach (var routine in routines)
            {
                var step = Step.Get().SetupCoroutine(routine);
                _stepQueue.Enqueue(step);
            }
            return this;
        }

        // Integration with existing Act methods
        public ActChain One(float duration, Action<float> value = null, Action onComplete = null)
        {
            return Play(ActEnumerator.One(duration, value, onComplete));
        }

        public ActChain Float(float from, float to, float duration, Action<float> value = null, Action onComplete = null)
        {
            return Play(ActEnumerator.Float(from, to, duration, value, onComplete));
        }

        public ActChain Int(int from, int to, float duration, Action<int> value = null, Action onComplete = null)
        {
            return Play(ActEnumerator.Int(from, to, duration, value, onComplete));
        }

        public ActChain Timer(float duration, Action<float> value = null, Action onComplete = null)
        {
            return Play(ActEnumerator.Timer(duration, value, onComplete));
        }

        public ActChain Delta(float duration, Action<float> delta = null, Action onComplete = null)
        {
            return Play(ActEnumerator.Delta(duration, delta, onComplete));
        }

        public ActChain DelayedCall(float time, Action callback)
        {
            return Play(ActEnumerator.WaitSeconds(time, callback));
        }
    }

    internal class ActChainStep
    {
        private StepType _type;
        private IEnumerator _coroutine;
        private IEnumerator[] _parallelRoutines;
        private Action _action;
        private float _waitTime;
        private Func<bool> _condition;

        public Coroutine Execute(MonoBehaviour executor)
        {
            switch (_type)
            {
                case StepType.Action:
                    _action?.Invoke();
                    return null;
                
                case StepType.Coroutine:
                    return executor.StartCoroutine(_coroutine);
                
                case StepType.Wait:
                    return executor.StartCoroutine(WaitRoutine());
                
                case StepType.WaitWhile:
                    return executor.StartCoroutine(WaitWhileRoutine());
                
                case StepType.WaitUntil:
                    return executor.StartCoroutine(WaitUntilRoutine());
                
                case StepType.Parallel:
                    return executor.StartCoroutine(ParallelRoutine(executor));
                
                default:
                    return null;
            }
        }

        public ActChainStep SetupCoroutine(IEnumerator coroutine)
        {
            _type = StepType.Coroutine;
            _coroutine = coroutine;
            return this;
        }

        public ActChainStep SetupAction(Action action)
        {
            _type = StepType.Action;
            _action = action;
            return this;
        }

        public ActChainStep SetupWait(float seconds)
        {
            _type = StepType.Wait;
            _waitTime = seconds;
            return this;
        }

        public ActChainStep SetupWaitWhile(Func<bool> condition)
        {
            _type = StepType.WaitWhile;
            _condition = condition;
            return this;
        }

        public ActChainStep SetupWaitUntil(Func<bool> condition)
        {
            _type = StepType.WaitUntil;
            _condition = condition;
            return this;
        }

        public ActChainStep SetupParallel(IEnumerator[] routines)
        {
            _type = StepType.Parallel;
            _parallelRoutines = routines;
            return this;
        }

        public void Clear()
        {
            _coroutine = null;
            _parallelRoutines = null;
            _action = null;
            _condition = null;
            _waitTime = 0f;
        }

        private IEnumerator WaitRoutine()
        {
            if (_waitTime <= 0)
                yield return null;
            else
                yield return ActEnumerator.GetWaitForSeconds(_waitTime);
        }

        private IEnumerator WaitWhileRoutine()
        {
            while (_condition())
                yield return null;
        }

        private IEnumerator WaitUntilRoutine()
        {
            while (!_condition())
                yield return null;
        }

        private IEnumerator ParallelRoutine(MonoBehaviour executor)
        {
            int totalRoutines = _parallelRoutines.Length;
            int completedCount = 0;

            foreach (var routine in _parallelRoutines)
            {
                executor.StartCoroutine(RunParallelRoutine(routine, () => completedCount++));
            }

            while (completedCount < totalRoutines)
                yield return null;
        }

        private IEnumerator RunParallelRoutine(IEnumerator routine, Action onComplete)
        {
            yield return routine;
            onComplete?.Invoke();
        }

        private enum StepType
        {
            Action,
            Coroutine,
            Wait,
            WaitWhile,
            WaitUntil,
            Parallel
        }
    }
}