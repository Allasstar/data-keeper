using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.Signals
{
    public abstract class SignalBase<TDelegate> where TDelegate : Delegate
    {
        public readonly List<TDelegate> Listeners = new List<TDelegate>(4);
        private TDelegate[] _invokeBuffer = new TDelegate[4];

        /// <summary>
        /// Copies listeners into a reusable buffer without heap allocation.
        /// Returns the number of listeners copied.
        /// </summary>
        protected int CopyToBuffer()
        {
            int count = Listeners.Count;
            if (count == 0) return 0;

            if (_invokeBuffer.Length < count)
                _invokeBuffer = new TDelegate[count * 2];

            Listeners.CopyTo(_invokeBuffer, 0);
            return count;
        }

        /// <summary>
        /// Clears buffer slots after invocation to prevent stale delegate GC roots.
        /// </summary>
        protected void ClearBuffer(int count)
        {
            Array.Clear(_invokeBuffer, 0, count);
        }

        protected TDelegate[] InvokeBuffer => _invokeBuffer;

        protected void AddListenerInternal(TDelegate listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener), "Cannot add a null listener.");

            Listeners.Add(listener);
        }

        protected void RemoveListenerInternal(TDelegate listener)
        {
            Listeners.Remove(listener);
        }

        public void RemoveAllListeners()
        {
            Listeners.Clear();
        }

        public int ListenerCount => Listeners.Count;
    }

    // -------------------------------------------------------------------------
    // Signal (no args)
    // -------------------------------------------------------------------------

    public class Signal : SignalBase<Action>
    {
        public void AddListener(Action listener)    => AddListenerInternal(listener);
        public void RemoveListener(Action listener) => RemoveListenerInternal(listener);

        public static Signal operator +(Signal signal, Action listener)
        {
            signal.AddListener(listener);
            return signal;
        }

        public static Signal operator -(Signal signal, Action listener)
        {
            signal.RemoveListener(listener);
            return signal;
        }

        public void Invoke()
        {
            int count = CopyToBuffer();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var listener = InvokeBuffer[i];
                if (listener == null) continue;
                try
                {
                    listener();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Signal] Listener invocation failed: {ex}");
                }
            }

            ClearBuffer(count);
        }
    }

    // -------------------------------------------------------------------------
    // Signal<T0>
    // -------------------------------------------------------------------------

    public class Signal<T0> : SignalBase<Action<T0>>
    {
        public void AddListener(Action<T0> listener)    => AddListenerInternal(listener);
        public void RemoveListener(Action<T0> listener) => RemoveListenerInternal(listener);

        public static Signal<T0> operator +(Signal<T0> signal, Action<T0> listener)
        {
            signal.AddListener(listener);
            return signal;
        }

        public static Signal<T0> operator -(Signal<T0> signal, Action<T0> listener)
        {
            signal.RemoveListener(listener);
            return signal;
        }

        public void Invoke(T0 arg0)
        {
            int count = CopyToBuffer();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var listener = InvokeBuffer[i];
                if (listener == null) continue;
                try
                {
                    listener(arg0);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Signal<T0>] Listener invocation failed: {ex}");
                }
            }

            ClearBuffer(count);
        }
    }

    // -------------------------------------------------------------------------
    // Signal<T0, T1>
    // -------------------------------------------------------------------------

    public class Signal<T0, T1> : SignalBase<Action<T0, T1>>
    {
        public void AddListener(Action<T0, T1> listener)    => AddListenerInternal(listener);
        public void RemoveListener(Action<T0, T1> listener) => RemoveListenerInternal(listener);

        public static Signal<T0, T1> operator +(Signal<T0, T1> signal, Action<T0, T1> listener)
        {
            signal.AddListener(listener);
            return signal;
        }

        public static Signal<T0, T1> operator -(Signal<T0, T1> signal, Action<T0, T1> listener)
        {
            signal.RemoveListener(listener);
            return signal;
        }

        public void Invoke(T0 arg0, T1 arg1)
        {
            int count = CopyToBuffer();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var listener = InvokeBuffer[i];
                if (listener == null) continue;
                try
                {
                    listener(arg0, arg1);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Signal<T0,T1>] Listener invocation failed: {ex}");
                }
            }

            ClearBuffer(count);
        }
    }

    // -------------------------------------------------------------------------
    // Signal<T0, T1, T2>
    // -------------------------------------------------------------------------

    public class Signal<T0, T1, T2> : SignalBase<Action<T0, T1, T2>>
    {
        public void AddListener(Action<T0, T1, T2> listener)    => AddListenerInternal(listener);
        public void RemoveListener(Action<T0, T1, T2> listener) => RemoveListenerInternal(listener);

        public static Signal<T0, T1, T2> operator +(Signal<T0, T1, T2> signal, Action<T0, T1, T2> listener)
        {
            signal.AddListener(listener);
            return signal;
        }

        public static Signal<T0, T1, T2> operator -(Signal<T0, T1, T2> signal, Action<T0, T1, T2> listener)
        {
            signal.RemoveListener(listener);
            return signal;
        }

        public void Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            int count = CopyToBuffer();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var listener = InvokeBuffer[i];
                if (listener == null) continue;
                try
                {
                    listener(arg0, arg1, arg2);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Signal<T0,T1,T2>] Listener invocation failed: {ex}");
                }
            }

            ClearBuffer(count);
        }
    }

    // -------------------------------------------------------------------------
    // Signal<T0, T1, T2, T3>
    // -------------------------------------------------------------------------

    public class Signal<T0, T1, T2, T3> : SignalBase<Action<T0, T1, T2, T3>>
    {
        public void AddListener(Action<T0, T1, T2, T3> listener)    => AddListenerInternal(listener);
        public void RemoveListener(Action<T0, T1, T2, T3> listener) => RemoveListenerInternal(listener);

        public static Signal<T0, T1, T2, T3> operator +(Signal<T0, T1, T2, T3> signal, Action<T0, T1, T2, T3> listener)
        {
            signal.AddListener(listener);
            return signal;
        }

        public static Signal<T0, T1, T2, T3> operator -(Signal<T0, T1, T2, T3> signal, Action<T0, T1, T2, T3> listener)
        {
            signal.RemoveListener(listener);
            return signal;
        }

        public void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            int count = CopyToBuffer();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var listener = InvokeBuffer[i];
                if (listener == null) continue;
                try
                {
                    listener(arg0, arg1, arg2, arg3);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Signal<T0,T1,T2,T3>] Listener invocation failed: {ex}");
                }
            }

            ClearBuffer(count);
        }
    }

    // -------------------------------------------------------------------------
    // Signal<T0, T1, T2, T3, T4>
    // -------------------------------------------------------------------------

    public class Signal<T0, T1, T2, T3, T4> : SignalBase<Action<T0, T1, T2, T3, T4>>
    {
        public void AddListener(Action<T0, T1, T2, T3, T4> listener)    => AddListenerInternal(listener);
        public void RemoveListener(Action<T0, T1, T2, T3, T4> listener) => RemoveListenerInternal(listener);

        public static Signal<T0, T1, T2, T3, T4> operator +(Signal<T0, T1, T2, T3, T4> signal, Action<T0, T1, T2, T3, T4> listener)
        {
            signal.AddListener(listener);
            return signal;
        }

        public static Signal<T0, T1, T2, T3, T4> operator -(Signal<T0, T1, T2, T3, T4> signal, Action<T0, T1, T2, T3, T4> listener)
        {
            signal.RemoveListener(listener);
            return signal;
        }

        public void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            int count = CopyToBuffer();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var listener = InvokeBuffer[i];
                if (listener == null) continue;
                try
                {
                    listener(arg0, arg1, arg2, arg3, arg4);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Signal<T0,T1,T2,T3,T4>] Listener invocation failed: {ex}");
                }
            }

            ClearBuffer(count);
        }
    }
}