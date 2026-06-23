using UnityEngine;

namespace DataKeeper.BlackboardSystem
{
    [AddComponentMenu("DataKeeper/Blackboard/Blackboard"), DefaultExecutionOrder(-100000)]
    public class BlackboardBehaviour : MonoBehaviour, IBlackboardOwner
    {
        [SerializeField] private Blackboard _blackboard = new Blackboard();

        public Blackboard Blackboard => _blackboard;

        private void Awake()
        {
            _blackboard.Initialize();
        }
    }
}
