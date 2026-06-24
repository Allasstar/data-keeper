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
            _blackboard.Clear();
            _blackboard.Initialize();
            _initialized = true;
        }
    }
}
