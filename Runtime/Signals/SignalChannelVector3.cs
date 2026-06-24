using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Vector3", fileName = "Signal Channel Vector3", order = 5)]
    public class SignalChannelVector3 : SignalChannelBase<Vector3>
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
