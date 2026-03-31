using UnityEngine;

namespace DataKeeper.DebugPrinter
{
    public static class DebugPrint
    {
        public static bool IsEnabled = true;

        private static bool _isEnabledListenErrors = true;
        public static bool IsEnabledListenErrors
        {
            get => _isEnabledListenErrors;
            set
            {
                _isEnabledListenErrors = value; 
                Initialize();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Application.logMessageReceived -= LogListener;
            if(!_isEnabledListenErrors) return;
            Application.logMessageReceived += LogListener;
        }

        private static void LogListener(string condition, string stackTrace, LogType type)
        {
            if(!_isEnabledListenErrors) return;
            if (type == LogType.Error || type == LogType.Exception)
            {
                if (IsEnabledListenErrors)
                    Error(condition);
            }
        }
        
        public static void Log(string message, DebugPrintStyle? style = null)
        {
            if (!IsEnabled || !Application.isPlaying) return;
            DebugPrintSystem.Instance.Add(message, false, style);
        }

        public static void Error(string message, DebugPrintStyle? style = null)
        {
            if (!IsEnabled || !Application.isPlaying) return;
            DebugPrintSystem.Instance.Add(message, true, style);
        }
    }
}
