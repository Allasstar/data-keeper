using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.FSM
{
    public class StateMachine<TState, TTarget> where TState : Enum where TTarget : class
    {
#if UNITY_EDITOR
        private FSMHistory<TState> stateHistory = new FSMHistory<TState>();
        public TransitionRecordHistory[] GetStateHistory() => stateHistory.GetHistory();
        
        private string[] allStates;
        public string[] GetAllStates()
        {
            if (allStates == null || allStates.Length == 0 || allStates.Length != states.Keys.Count)
            {
                allStates = Enum.GetNames(typeof(TState));
            }
            
            return allStates;
        }
#endif
        
        private Dictionary<TState, State<TState, TTarget>> states = new Dictionary<TState, State<TState, TTarget>>();
        private List<Transition<TState>> transitions = new List<Transition<TState>>();
        private List<Transition<TState>> anyStateTransitions = new List<Transition<TState>>();

        private State<TState, TTarget> currentState;
        public TTarget Target { get; private set; }
        public TState CurrentStateType { get; private set; }
        public TState PreviousStateType { get; private set; }

        public StateMachine(TTarget target)
        {
            Target = target;
        }
        
        public void AddState(TState stateType, State<TState, TTarget> state)
        {
            states[stateType] = state;
            state.Initialize(this);
        }

        public void SetInitialState(TState initialState)
        {
            if (states.TryGetValue(initialState, out State<TState, TTarget> state))
            {
                CurrentStateType = initialState;
                currentState = state;
                currentState.OnEnter();
            }
            else
            {
                Debug.LogError($"State {initialState} not found in state machine!");
            }
        }

        public Transition<TState> AddTransition(TState fromState, TState toState, Func<bool> condition)
        {
            var transition = new Transition<TState>(fromState, toState, condition, null, 0);
            transitions.Add(transition);
            return transition;
        }

        public Transition<TState> AddAnyStateTransition(TState toState, Func<bool> condition)
        {
            var transition = new Transition<TState>(default, toState, condition, null, 0);
            anyStateTransitions.Add(transition);
            return transition;
        }

        private bool TryTransition(Transition<TState> transition)
        {
            if (!transition.IsCooldownComplete()) return false;
            if (!transition.Condition()) return false;

            transition.LastTransitionTime = Time.time;
            transition.OnTransitionCallback?.Invoke();
            ChangeState(transition.ToState);
            return true;
        }

        public void ChangeState(TState newStateType)
        {
            if (!states.ContainsKey(newStateType))
            {
                Debug.LogError($"State {newStateType} not found in state machine!");
                return;
            }

            PreviousStateType = CurrentStateType;
            State<TState, TTarget> newState = states[newStateType];

            currentState?.OnExit();
            CurrentStateType = newStateType;
            currentState = newState;
            currentState.OnEnter();

#if UNITY_EDITOR
            stateHistory.RecordTransition(PreviousStateType, CurrentStateType);
#endif
        }

        public void Update()
        {
            // Check "any state" transitions first
            foreach (var transition in anyStateTransitions)
            {
                if (TryTransition(transition)) return;
            }

            // Check regular transitions
            foreach (var transition in transitions)
            {
                if (transition.FromState.Equals(CurrentStateType) && TryTransition(transition))
                    return;
            }

            currentState?.OnUpdate();
        }

        public void FixedUpdate()
        {
            currentState?.OnFixedUpdate();
        }
    }
}