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
    
    [Serializable]
    public class FSMHistory<TState> where TState : Enum
    {
        [SerializeField] private int _historySize = 10;
        private Queue<TransitionRecordHistory> _stateHistory;

        public FSMHistory()
        {
            _stateHistory = new Queue<TransitionRecordHistory>();
        }

        public void RecordTransition(TState fromState, TState toState)
        {
            _stateHistory.Enqueue(new TransitionRecordHistory(fromState.ToString(), toState.ToString()));
            if (_stateHistory.Count > _historySize)
            {
                _stateHistory.Dequeue();
            }
        }

        public Queue<TransitionRecordHistory> GetQueueHistory() => _stateHistory;

        public TransitionRecordHistory[] GetHistory()
        {
            return _stateHistory.ToArray();
        }

        public void Clear()
        {
            _stateHistory.Clear();
        }
    }
}
