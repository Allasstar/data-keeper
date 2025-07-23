using System;

namespace DataKeeper.FSM
{
    /// <summary>
    /// Base class for states in the FSM
    /// </summary>
    public abstract class State<TState, TSelfType> where TState : Enum where TSelfType : class
    {
        public StateMachine<TState, TSelfType> StateMachine { get; private set; }
        public TSelfType Self { get; private set; }

        public virtual void Initialize(StateMachine<TState, TSelfType> machine)
        {
            StateMachine = machine;
            Self = machine.Self;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void OnUpdate()
        {
        }
        
        public virtual void OnLateUpdate()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }
        
        public virtual void OnAnimatorMove()
        {
        }
    }
}