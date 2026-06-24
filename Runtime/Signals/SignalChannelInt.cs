using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Int", fileName = "Signal Channel Int", order = 3)]
    public class SignalChannelInt : SignalChannelBase<int>
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
