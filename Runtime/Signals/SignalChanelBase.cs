using System;
using UnityEngine;

namespace DataKeeper.Signals
{
   public class SignalChanelBase<T> : ScriptableObject
   {
      /// <summary>
      /// On List Changed event.
      /// </summary>
      [field: NonSerialized]
      public Signal<T> Signal { get; private set; } = new Signal<T>();

      /// <summary>
      /// Add Listener.
      /// </summary>
      /// <param name="call"></param>
      public void AddListener(Action<T> call)
      {
         Signal.AddListener(call);
      }
    
      /// <summary>
      /// Remove Listener.
      /// </summary>
      /// <param name="call"></param>
      public void RemoveListener(Action<T> call)
      {
         Signal.RemoveListener(call);
      }

      /// <summary>
      /// Remove All Listeners.
      /// </summary>
      public void RemoveAllListeners()
      {
         Signal.RemoveAllListeners();
      }
      
      /// <summary>
      /// Invoke all registered callbacks (runtime and persistent).
      /// </summary>
      public void Invoke(T value)
      {
         this.Signal?.Invoke(value);
      }
   }
}
