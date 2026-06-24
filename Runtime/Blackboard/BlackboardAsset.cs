using UnityEngine;

namespace DataKeeper.BlackboardSystem
{
    /// <summary>
    /// ScriptableObject analog of <see cref="BlackboardBehaviour"/>: a shared, asset-based
    /// blackboard owner. Reference it anywhere an <see cref="IBlackboardOwner"/> is required
    /// (e.g. the BlackboardProvider value providers) to share one blackboard across scenes.
    ///
    /// Initialized lazily on first <see cref="Blackboard"/> access. Because a ScriptableObject's
    /// in-memory state survives between editor play sessions when Domain Reload is disabled, the
    /// init flag is reset on load and on entering play mode so entries are re-applied each run.
    /// </summary>
    [CreateAssetMenu(menuName = "DataKeeper/Blackboard/Blackboard Asset", fileName = "Blackboard Asset")]
    public class BlackboardAsset : ScriptableObject, IBlackboardOwner
    {
        [SerializeField] private Blackboard _blackboard = new Blackboard();

        private bool _initialized;

        public Blackboard Blackboard
        {
            get
            {
                if (!_initialized) Initialize();
                return _blackboard;
            }
        }

        /// <summary>Applies the configured entries onto the blackboard and marks it ready.</summary>
        public void Initialize()
        {
            _blackboard.Initialize();
            _initialized = true;
        }

        /// <summary>Drops runtime state so the next <see cref="Blackboard"/> access re-applies entries.</summary>
        public void ResetState()
        {
            _blackboard.Clear();
            _initialized = false;
        }

        private void OnEnable()
        {
            _initialized = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnDisable() => UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        // OnEnable isn't guaranteed to fire on Play when Domain Reload is off, so reset
        // explicitly when leaving edit mode to avoid carrying state into the next run.
        private void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingEditMode)
                ResetState();
        }
#endif
    }
}
