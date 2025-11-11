using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Signals
{
    public abstract class SignalBase<TDelegate> where TDelegate : Delegate
    {
        [field: NonSerialized]
        protected List<TDelegate> Listeners { get; } = new List<TDelegate>();

        public void RemoveAllListeners()
        {
            lock (Listeners)
            {
                Listeners.Clear();
            }
        }

        protected void AddListener(TDelegate listener)
        {
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
    }

    public class Signal : SignalBase<Action>
    {
        public void AddListener(Action listener)
        {
            AddListener((Action)listener);
        }

        public void RemoveListener(Action listener)
        {
            RemoveListener((Action)listener);
        }

        public void Invoke()
        {
            Action[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }
        }
    }

    public class Signal<T0> : SignalBase<Action<T0>>
    {
        public void AddListener(Action<T0> listener)
        {
            AddListener(listener);
        }

        public void RemoveListener(Action<T0> listener)
        {
            RemoveListener(listener);
        }

        public void Invoke(T0 value)
        {
            Action<T0>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }
        }
    }

    public class Signal<T0, T1> : SignalBase<Action<T0, T1>>
    {
        public void AddListener(Action<T0, T1> listener)
        {
            AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1> listener)
        {
            RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1)
        {
            Action<T0, T1>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener(value0, value1);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }
        }
    }

    public class Signal<T0, T1, T2> : SignalBase<Action<T0, T1, T2>>
    {
        public void AddListener(Action<T0, T1, T2> listener)
        {
            AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1, T2> listener)
        {
            RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1, T2 value2)
        {
            Action<T0, T1, T2>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener(value0, value1, value2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }
        }
    }

    public class Signal<T0, T1, T2, T3> : SignalBase<Action<T0, T1, T2, T3>>
    {
        public void AddListener(Action<T0, T1, T2, T3> listener)
        {
            AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1, T2, T3> listener)
        {
            RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1, T2 value2, T3 value3)
        {
            Action<T0, T1, T2, T3>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener(value0, value1, value2, value3);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }
        }
    }

    public class Signal<T0, T1, T2, T3, T4> : SignalBase<Action<T0, T1, T2, T3, T4>>
    {
        public void AddListener(Action<T0, T1, T2, T3, T4> listener)
        {
            AddListener(listener);
        }

        public void RemoveListener(Action<T0, T1, T2, T3, T4> listener)
        {
            RemoveListener(listener);
        }

        public void Invoke(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4)
        {
            Action<T0, T1, T2, T3, T4>[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener(value0, value1, value2, value3, value4);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Listener invocation failed: {ex}");
                }
            }
        }
    }
}