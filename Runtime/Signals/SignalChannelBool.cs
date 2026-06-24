using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Bool", fileName = "Signal Channel Bool", order = 2)]
    public class SignalChannelBool : SignalChannelBase<bool>
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
