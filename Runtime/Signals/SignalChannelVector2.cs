using UnityEngine;

namespace DataKeeper.Signals
{
    [CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Channel Vector2", fileName = "Signal Channel Vector2", order = 4)]
    public class SignalChannelVector2 : SignalChannelBase<Vector2>
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
