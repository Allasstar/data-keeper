using UnityEngine;

namespace DataKeeper.DebugPrinter
{
    public static class DebugPrint
    {
        public static bool IsEnabledPrint = true;

        private static bool _isEnabledLogMessageReceived = false;
        public static bool IsEnabledLogMessageReceived
        {
            get => _isEnabledLogMessageReceived;
            set
            {
                _isEnabledLogMessageReceived = value; 
                Initialize();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Application.logMessageReceived -= LogListener;
            if(!_isEnabledLogMessageReceived) return;
            Application.logMessageReceived += LogListener;
        }

        private static void LogListener(string condition, string stackTrace, LogType type)
        {
            if(!_isEnabledLogMessageReceived) return;
            if (type == LogType.Error || type == LogType.Exception)
            {
                if (IsEnabledLogMessageReceived)
                    Error(condition);
            }
        }
        
        public static void Log(string message, DebugPrintStyle? style = null)
        {
            if (!IsEnabledPrint || !Application.isPlaying) return;
            DebugPrintSystem.Instance.Add(message, false, style);
        }

        public static void Error(string message, DebugPrintStyle? style = null)
        {
            if (!IsEnabledPrint || !Application.isPlaying) return;
            DebugPrintSystem.Instance.Add(message, true, style);
        }
    }
}
