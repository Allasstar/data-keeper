using System;
using UnityEngine;

namespace DataKeeper.FSM
{
    public class Transition<TState> where TState : Enum
    {
        public TState FromState { get; private set; }
        public TState ToState { get; private set; }
        public Func<bool> Condition { get; private set; }
        public Action OnTransition { get; private set; }
        public float Cooldown { get; private set; }
        public float LastTransitionTime { get; set; }

        public Transition(TState fromState, TState toState, Func<bool> condition, Action onTransition = null, float cooldown = 0f)
        {
            FromState = fromState;
            ToState = toState;
            Condition = condition;
            OnTransition = onTransition;
            Cooldown = cooldown;
            LastTransitionTime = -cooldown; // Allow immediate first transition
        }

        public bool IsCooldownComplete()
        {
            if (Cooldown == 0f) return true;
            return Time.time - LastTransitionTime >= Cooldown;
        }
    }
}