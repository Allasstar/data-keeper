using System;

namespace DataKeeper.BlackboardSystem
{
    public interface IBlackboardOwner
    {
        Blackboard Blackboard { get; }
    }
}
