using System;
using System.Collections.Generic;

namespace DataKeeper.Signals
{
    public abstract class SignalBase
    {
        [field: NonSerialized]
        public List<Delegate> Listeners { get; private set; }= new List<Delegate>();

        public void RemoveAllListeners()
        {
            lock (Listeners)
            {
                Listeners.Clear();
            }
        }

        protected void AddListener(Delegate listener)
        {
            lock (Listeners)
            {
                Listeners.Add(listener);
            }
        }

        protected void RemoveListener(Delegate listener)
        {
            lock (Listeners)
            {
                Listeners.Remove(listener);
            }
        }

        protected void InvokeListeners(params object[] parameters)
        {
            Delegate[] listenersSnapshot;
            lock (Listeners)
            {
                listenersSnapshot = Listeners.ToArray();
            }

            foreach (var listener in listenersSnapshot)
            {
                try
                {
                    listener.DynamicInvoke(parameters);
                }
                catch (Exception ex)
                {
                    // Optionally log the exception or handle it as needed.
                    Console.WriteLine($"Listener invocation failed: {ex}");
                }
            }
        }
    }

    public class Signal : SignalBase
    {
        public void Invoke()
        {
            InvokeListeners();
        }

        public void AddListener(Action listener)
        {
            AddListener((Delegate)listener);
        }

        public void RemoveListener(Action listener)
        {
            RemoveListener((Delegate)listener);
        }
    }

    public class Signal<T0> : SignalBase
    {
        public void Invoke(T0 value)
        {
            InvokeListeners(value);
        }

        public void AddListener(Action<T0> listener)
        {
            AddListener((Delegate)listener);
        }

        public void RemoveListener(Action<T0> listener)
        {
            RemoveListener((Delegate)listener);
        }
    }

    public class Signal<T0, T1> : SignalBase
    {
        public void Invoke(T0 value0, T1 value1)
        {
            InvokeListeners(value0, value1);
        }

        public void AddListener(Action<T0, T1> listener)
        {
            AddListener((Delegate)listener);
        }

        public void RemoveListener(Action<T0, T1> listener)
        {
            RemoveListener((Delegate)listener);
        }
    }

    public class Signal<T0, T1, T2> : SignalBase
    {
        public void Invoke(T0 value0, T1 value1, T2 value2)
        {
            InvokeListeners(value0, value1, value2);
        }

        public void AddListener(Action<T0, T1, T2> listener)
        {
            AddListener((Delegate)listener);
        }

        public void RemoveListener(Action<T0, T1, T2> listener)
        {
            RemoveListener((Delegate)listener);
        }
    }

    public class Signal<T0, T1, T2, T3> : SignalBase
    {
        public void Invoke(T0 value0, T1 value1, T2 value2, T3 value3)
        {
            InvokeListeners(value0, value1, value2, value3);
        }

        public void AddListener(Action<T0, T1, T2, T3> listener)
        {
            AddListener((Delegate)listener);
        }

        public void RemoveListener(Action<T0, T1, T2, T3> listener)
        {
            RemoveListener((Delegate)listener);
        }
    }

    public class Signal<T0, T1, T2, T3, T4> : SignalBase
    {
        public void Invoke(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4)
        {
            InvokeListeners(value0, value1, value2, value3, value4);
        }

        public void AddListener(Action<T0, T1, T2, T3, T4> listener)
        {
            AddListener((Delegate)listener);
        }

        public void RemoveListener(Action<T0, T1, T2, T3, T4> listener)
        {
            RemoveListener((Delegate)listener);
        }
    }
}
