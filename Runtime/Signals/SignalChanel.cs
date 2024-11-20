using System;
using UnityEngine;

namespace DataKeeper.Signals
{
   [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Chanel", fileName = "Signal Chanel", order = 0)]
   public class SignalChanel : ScriptableObject
   {
      /// <summary>
      /// On List Changed event.
      /// </summary>
      [field: NonSerialized]
      public Signal Signal { get; private set; } = new Signal();

      /// <summary>
      /// Add Listener.
      /// </summary>
      /// <param name="call"></param>
      public void AddListener(Action call)
      {
         Signal.AddListener(call);
      }
    
      /// <summary>
      /// Remove Listener.
      /// </summary>
      /// <param name="call"></param>
      public void RemoveListener(Action call)
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
      public void Invoke()
      {
         this.Signal?.Invoke();
      }
          
#if UNITY_EDITOR
      [ContextMenu("Log Listeners")]
      private void ShowListeners()
      {
         foreach (var listener in Signal.Listeners)
         {
            Debug.Log( $"Listener: {listener.Target}.{listener.Method.Name}");
         }
      }
#endif
   }
}
