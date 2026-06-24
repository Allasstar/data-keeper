using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Rect", fileName = "Signal Channel Rect", order = 3)]
    public class SignalChannelRect : SignalChannelBase<Rect>
    {
#if UNITY_EDITOR
        [ContextMenu("Log Listeners")]
        private void ShowListeners()
        {
            foreach (var listener in Signal.Listeners)
            {
                Debug.Log($"Listener: {listener.Target}.{listener.Method.Name}");
            }
        }
#endif
    }
}
