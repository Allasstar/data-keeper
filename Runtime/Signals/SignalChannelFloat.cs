using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Float", fileName = "Signal Channel Float", order = 4)]
    public class SignalChannelFloat : SignalChannelBase<float>
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
