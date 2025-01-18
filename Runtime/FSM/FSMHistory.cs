using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.FSM
{
    public struct TransitionRecordHistory
    {
        public string FromState { get; }
        public string ToState { get; }
        public float TimeStamp { get; }

        public TransitionRecordHistory(string fromState, string toState)
        {
            FromState = fromState;
            ToState = toState;
            TimeStamp = Time.time;
        }
    }
    
    public class FSMHistory<TState> where TState : Enum
    {
        private const int MAX_HISTORY = 10;
        private Queue<TransitionRecordHistory> stateHistory;

        public FSMHistory()
        {
            stateHistory = new Queue<TransitionRecordHistory>();
        }

        public void RecordTransition(TState fromState, TState toState)
        {
            stateHistory.Enqueue(new TransitionRecordHistory(fromState.ToString(), toState.ToString()));
            if (stateHistory.Count > MAX_HISTORY)
            {
                stateHistory.Dequeue();
            }
        }

        public TransitionRecordHistory[] GetHistory()
        {
            return stateHistory.ToArray();
        }

        public void Clear()
        {
            stateHistory.Clear();
        }
    }
}
