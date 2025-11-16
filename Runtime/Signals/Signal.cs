using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Signals
{
    public abstract class SignalBase<TDelegate> where TDelegate : Delegate
    {
        [field: NonSerialized]
        public List<TDelegate> Listeners { get; } = new List<TDelegate>();

        public void RemoveAllListeners()
        {
            lock (Listeners)
            {
                Listeners.Clear();
            }
        }

        protected void AddListener(TDelegate listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener), "Cannot add null listener.");
            }

            lock (Listeners)
            {
                Listeners.Add(listener);
            }
        }

        protected void RemoveListener(TDelegate listener)
        {
            lock (Listeners)
            {
                Listeners.Remove(listener);
            }
        }

        protected void CleanupNullListeners()
        {
            lock (Listeners)
            {
                Listeners.RemoveAll(l => l == null);
            }
        }
    }

    public class Signal : SignalBase<Action>
    {
        public void AddListener(Action listener)
        {
            base.AddListener(listener);
        }

        public void RemoveListener(Action listener)
        {
            base.RemoveListener(listener);
        }

        public void Invoke()
        {
            Action[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            bool hasNull = false;
            foreach (var listener in listenersSnapshot)
            {
                if (listener == null)
                {
                    hasNull = true;
                    continue;
                }

                try
                {
                    listener();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }

            if (hasNull)
            {
                CleanupNullListeners();
            }
        }
    }

    public class Signal<T0> : SignalBase<Action<T0>>
    {
        public void AddListener(Action<T0> listener)
        {
            base.AddListener(listener);
        }

        public void RemoveListener(Action<T0> listener)
        {
            base.RemoveListener(listener);
        }

        public void Invoke(T0 value)
        {
            Action<T0>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            bool hasNull = false;
            foreach (var listener in listenersSnapshot)
            {
                if (listener == null)
                {
                    hasNull = true;
                    continue;
                }

                try
                {
                    listener(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }

            if (hasNull)
            {
                CleanupNullListeners();
            }
        }
    }

    public class Signal<T0, T1> : SignalBase<Action<T0, T1>>
    {
        public void AddListener(Action<T0, T1> listener)
        {
            base.AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1> listener)
        {
            base.RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1)
        {
            Action<T0, T1>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            bool hasNull = false;
            foreach (var listener in listenersSnapshot)
            {
                if (listener == null)
                {
                    hasNull = true;
                    continue;
                }

                try
                {
                    listener(value0, value1);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }

            if (hasNull)
            {
                CleanupNullListeners();
            }
        }
    }

    public class Signal<T0, T1, T2> : SignalBase<Action<T0, T1, T2>>
    {
        public void AddListener(Action<T0, T1, T2> listener)
        {
            base.AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1, T2> listener)
        {
            base.RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1, T2 value2)
        {
            Action<T0, T1, T2>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            bool hasNull = false;
            foreach (var listener in listenersSnapshot)
            {
                if (listener == null)
                {
                    hasNull = true;
                    continue;
                }

                try
                {
                    listener(value0, value1, value2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }

            if (hasNull)
            {
                CleanupNullListeners();
            }
        }
    }

    public class Signal<T0, T1, T2, T3> : SignalBase<Action<T0, T1, T2, T3>>
    {
        public void AddListener(Action<T0, T1, T2, T3> listener)
        {
            base.AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1, T2, T3> listener)
        {
            base.RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1, T2 value2, T3 value3)
        {
            Action<T0, T1, T2, T3>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            bool hasNull = false;
            foreach (var listener in listenersSnapshot)
            {
                if (listener == null)
                {
                    hasNull = true;
                    continue;
                }

                try
                {
                    listener(value0, value1, value2, value3);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }

            if (hasNull)
            {
                CleanupNullListeners();
            }
        }
    }

    public class Signal<T0, T1, T2, T3, T4> : SignalBase<Action<T0, T1, T2, T3, T4>>
    {
        public void AddListener(Action<T0, T1, T2, T3, T4> listener)
        {
            base.AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1, T2, T3, T4> listener)
        {
            base.RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4)
        {
            Action<T0, T1, T2, T3, T4>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            bool hasNull = false;
            foreach (var listener in listenersSnapshot)
            {
                if (listener == null)
                {
                    hasNull = true;
                    continue;
                }

                try
                {
                    listener(value0, value1, value2, value3, value4);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }

            if (hasNull)
            {
                CleanupNullListeners();
            }
        }
    }
}