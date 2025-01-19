using System;
using UnityEngine;

namespace DataKeeper.FSM
{
    public class Transition<TState> where TState : Enum
    {
        public TState FromState { get; private set; }
        public TState ToState { get; private set; }
        public Func<bool> Condition { get; private set; }
        public Action OnTransitionCallback { get; private set; }
        public float CooldownTime { get; private set; }
        public float LastTransitionTime { get; set; }

        public Transition(TState fromState, TState toState, Func<bool> condition, Action onTransition = null, float cooldown = 0f)
        {
            FromState = fromState;
            ToState = toState;
            Condition = condition;
            OnTransitionCallback = onTransition;
            CooldownTime = cooldown;
            LastTransitionTime = -cooldown; // Allow immediate first transition
        }

        public Transition<TState> OnTransition(Action onTransition)
        {
            OnTransitionCallback = onTransition;
            return this;
        }
        
        public Transition<TState> Cooldown(float cooldown)
        {
            CooldownTime = cooldown;
            LastTransitionTime = -cooldown; // Allow immediate first transition
            return this;
        }

        public bool IsCooldownComplete()
        {
            if (CooldownTime == 0f) return true;
            return Time.time - LastTransitionTime >= CooldownTime;
        }
    }
}