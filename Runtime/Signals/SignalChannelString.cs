using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel String", fileName = "Signal Channel String", order = 5)]
    public class SignalChannelString : SignalChannelBase<string>
    {
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
