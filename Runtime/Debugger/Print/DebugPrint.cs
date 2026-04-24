using System.Collections.Generic;

namespace UnityEngine
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_PRINT
    
    public static class DebugPrint
    {
        private const int MAX_MESSAGES = 50;
        
        public static List<Message> Messages { get; private set; }
        
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Messages = new ();
            Application.logMessageReceived -= LogListener;
            if(!_isEnabledSystemErrors) return;
            Application.logMessageReceived += LogListener;
        }
        
        private static void Add(string text, bool isError, DebugPrintStyle? overrideStyle)
        {
            var style = overrideStyle ?? (isError
                ? DebugPrintStyle.DefaultError
                : DebugPrintStyle.DefaultLog);

            if (Messages.Count >= MAX_MESSAGES)
                Messages.RemoveAt(0);

            Messages.Add(new Message
            {
                Text = text,
                Duration = style.Duration,
                FadeOutTime = style.FadeOutTime,
                TimeLeft = style.Duration + style.FadeOutTime,

                Color = style.Color,
                FontSize = style.FontSize,
                IsError = isError,

                UseBackground = style.UseBackground,
                BackgroundColor = style.BackgroundColor,
                HasCopyButton = style.HasCopyButton,
            });
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
            if (!Application.isPlaying) return;
            Add(message, false, style);
            DebugPrintSystem.Instance.Ping();
        }

        public static void Error(string message, DebugPrintStyle? style = null)
        {
            if (!Application.isPlaying) return;
            Add(message, true, style);
            DebugPrintSystem.Instance.Ping();
        }
    }
    
    public class Message
    {
        public string Text;

        public float TimeLeft;
        public float Duration;
        public float FadeOutTime;

        public Color Color;
        public int FontSize;
        public bool IsError;

        public bool UseBackground;
        public Color BackgroundColor;
            
        public bool HasCopyButton;
    }
    
#else
    public static class DebugPrint
    {
        public static void Log(string message, DebugPrintStyle? style = null)
        {
           
        }

        public static void Error(string message, DebugPrintStyle? style = null)
        {
           
        }
    }
    
    public struct DebugPrintStyle {}
#endif
}
