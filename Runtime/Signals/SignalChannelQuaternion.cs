using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Quaternion", fileName = "Signal Channel Quaternion", order = 7)]
    public class SignalChannelQuaternion : SignalChannelBase<Quaternion>
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
