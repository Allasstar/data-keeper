using System;

namespace DataKeeper.FSM
{
    /// <summary>
    /// Base class for states in the FSM
    /// </summary>
    public abstract class State<TState, TTarget> where TState : Enum where TTarget : class
    {
        protected StateMachine<TState, TTarget> stateMachine;

        public virtual void Initialize(StateMachine<TState, TTarget> machine)
        {
            stateMachine = machine;
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

        public virtual void OnFixedUpdate()
        {
        }
    }
}