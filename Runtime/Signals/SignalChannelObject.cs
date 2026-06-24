using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Object", fileName = "Signal Channel Object", order = 1)]
    public class SignalChannelObject : SignalChannelBase<object>
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
