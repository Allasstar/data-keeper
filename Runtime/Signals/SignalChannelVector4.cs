using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Vector4", fileName = "Signal Channel Vector4", order = 6)]
    public class SignalChannelVector4 : SignalChannelBase<Vector4>
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
