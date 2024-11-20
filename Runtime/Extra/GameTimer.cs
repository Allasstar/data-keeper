using System;
using DataKeeper.ActCore;
using DataKeeper.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace DataKeeper.Extra
{
    public class GameTimer : JsonData<GameTimer>, IDisposable
    {
        // JsonRequired
        [JsonRequired] public TimerType TimerType { get; private set; }
        [JsonRequired] public int Time { get; private set; }
        [JsonRequired] public int StartedAt { get; private set; }
        [JsonRequired] public  int EndedAt { get; private set; }
        [JsonRequired] public  bool IsStarted { get; private set; }
        [JsonRequired] public  bool IsEnded { get; private set; }
    
    
        // JsonIgnore
        [JsonIgnore] public bool IsRunning => EndedAt > TimeHelper.CurrentTimeInSec();
        [JsonIgnore] public int RemainingTime => (int)(EndedAt - TimeHelper.CurrentTimeInSec());


        [JsonIgnore] public readonly UnityEvent<int> OnRemainingTime = new UnityEvent<int>();
        [JsonIgnore] public readonly UnityEvent OnTimeOut = new UnityEvent();
    
        [JsonIgnore] private string Key => $"game_timer_{TimerType}";
        [JsonIgnore] private Coroutine _coroutine;
    
    
        public GameTimer(TimerType timerType)
        {
            TimerType = timerType;
        }

        public void Start(int time)
        {
            Time = time;
            StartedAt = TimeHelper.CurrentTimeInSec();
            EndedAt = TimeHelper.CurrentTimeInSec(Time);
            IsStarted = true;
            IsEnded = false;
        
            Save();

            Act.StopCoroutine(_coroutine);
            _coroutine = Act.OneSecondUpdate(Tick);
        }

        public void Stop()
        {
            EndedAt = TimeHelper.CurrentTimeInSec();
        }

        public void Save()
        {
            var json = ToJSON();
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    
        public TimerLoadResultType Status()
        {
            if (IsStarted)
            {
                if (IsEnded)
                {
                    return TimerLoadResultType.IsEnded;
                }
            
                return TimerLoadResultType.IsStarted;
            }

            return TimerLoadResultType.IsNull;
        }

        public TimerLoadResultType TryLoad()
        {
            var json = PlayerPrefs.GetString(Key, "");
            if(string.IsNullOrEmpty(json)) return TimerLoadResultType.IsNull;

            var timer = FromJSON(json);

            if(timer == null) return TimerLoadResultType.IsNull;
            CopyTimerData(timer);

            if (IsEnded) return TimerLoadResultType.IsEnded;
        
            if (!IsRunning && IsStarted)
            {
                IsEnded = true;
                Save();
                OnTimeOut?.Invoke();
                return TimerLoadResultType.IsEnded;
            }
        
            Act.StopCoroutine(_coroutine);
            _coroutine = Act.OneSecondUpdate(Tick);
            return TimerLoadResultType.IsStarted;
        }

        public (int amount, int timeToNext) OfflineStatus()
        {
            (int amount, int timeToNext) value = (-1, -1);
        
            var startRegen = StartedAt;
            var curTime = TimeHelper.CurrentTimeInSec();
        
            var amount = 0;
        
            while (startRegen + Time < curTime)
            {
                startRegen += Time;
                amount++;
            }

            value.amount = amount;
            value.timeToNext = Mathf.Max((startRegen + Time) - curTime, 0);
            return value;
        }

        private void CopyTimerData(GameTimer timer)
        {
            TimerType = timer.TimerType;
            Time = timer.Time;
            StartedAt = timer.StartedAt;
            EndedAt = timer.EndedAt;
            IsStarted = timer.IsStarted;
            IsEnded = timer.IsEnded;
        }

        private void Tick()
        {
            var time = RemainingTime;
            OnRemainingTime?.Invoke(time);
            if (time <= 0)
            {
                IsEnded = true;
                Save();
                Act.DelayedCall(0f, () => OnTimeOut?.Invoke());
                Act.StopCoroutine(_coroutine);  
            }
        }

        public void SilentStopAndClearSave()
        {
            PlayerPrefs.SetString(Key, "");
            PlayerPrefs.Save();
            IsStarted = false;
            IsEnded = false;
            StartedAt = 0;
            EndedAt = 0;
            Time = 0;
            Act.StopCoroutine(_coroutine);
        }

        public void Dispose()
        {
            Act.StopCoroutine(_coroutine);
        }
    }

    public enum TimerType
    {
        None = -1,
        Lives = 1,
    }

    public enum TimerLoadResultType
    {
        IsNull = 0,
        IsEnded = 1,
        IsStarted = 2,
    }
}