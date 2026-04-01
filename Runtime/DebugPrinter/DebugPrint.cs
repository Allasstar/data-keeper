using UnityEngine;

namespace DataKeeper.DebugPrinter
{
    public static class DebugPrint
    {
        public static bool IsEnabledPrint = true;

        private static bool _isEnabledSystemErrors = false;
        public static bool IsEnabledSystemErrors
        {
            get => _isEnabledSystemErrors;
            set
            {
                _isEnabledSystemErrors = value; 
                Initialize();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Application.logMessageReceived -= LogListener;
            if(!_isEnabledSystemErrors) return;
            Application.logMessageReceived += LogListener;
        }

        private static void LogListener(string condition, string stackTrace, LogType type)
        {
            if(!_isEnabledSystemErrors) return;
            if (type == LogType.Error || type == LogType.Exception)
            {
                if (IsEnabledSystemErrors)
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
