namespace DataKeeper.Debugger
{
    using UnityEngine;

    public struct DebugPrintStyle
    {
        public const float FADE_OUT_TIME = 1f;
        public const int FONT_SIZE = 25;
        public const float LOG_DURATION = 3f;
        public const float ERROR_DURATION = 6f;
        public static readonly Color BACKGROUND_COLOR = new Color(0f, 0f, 0f, 0.3f);
        public static readonly Color LOG_COLOR = new Color(FADE_OUT_TIME, FADE_OUT_TIME, FADE_OUT_TIME, FADE_OUT_TIME);
        public static readonly Color ERROR_COLOR = Color.orange;
        
        public float Duration;
        public float FadeOutTime;

        public Color Color;
        public int FontSize;

        public bool UseBackground;
        public Color BackgroundColor;

        public bool HasCopyButton;

        public static DebugPrintStyle NewStyle(float duration, float fadeOutTime, Color color, int fontSize,
            bool hasCopyButton, bool useBackground, Color backgroundColor)
        {
            return new DebugPrintStyle
            {
                Duration = duration,
                FadeOutTime = fadeOutTime,
                Color = color,
                FontSize = fontSize,
                UseBackground = useBackground,
                BackgroundColor = backgroundColor,
                HasCopyButton = hasCopyButton,
            };
        }

        public static DebugPrintStyle LogStyle(Color color, float duration = LOG_DURATION, int fontSize = FONT_SIZE, bool hasCopyButton = false)
        {
            return new DebugPrintStyle
            {
                Duration = duration,
                FadeOutTime = FADE_OUT_TIME,
                Color = color,
                FontSize = fontSize,
                UseBackground = true,
                BackgroundColor = BACKGROUND_COLOR,
                HasCopyButton = hasCopyButton,
            };
        }
        
        public static DebugPrintStyle ErrorStyle(Color color, float duration = ERROR_DURATION, int fontSize = FONT_SIZE, bool hasCopyButton = true)
        {
            return new DebugPrintStyle
            {
                Duration = duration,
                FadeOutTime = FADE_OUT_TIME,
                Color = color,
                FontSize = fontSize,
                UseBackground = true,
                BackgroundColor = BACKGROUND_COLOR,
                HasCopyButton = hasCopyButton,
            };
        }

        public static DebugPrintStyle DefaultLog => new DebugPrintStyle
        {
            Duration = LOG_DURATION,
            FadeOutTime = FADE_OUT_TIME,
            Color = LOG_COLOR,
            FontSize = FONT_SIZE,
            UseBackground = true,
            BackgroundColor = BACKGROUND_COLOR,
            HasCopyButton = false,
        };
        
        public static DebugPrintStyle DefaultLogWithCopy => new DebugPrintStyle
        {
            Duration = LOG_DURATION,
            FadeOutTime = FADE_OUT_TIME,
            Color = LOG_COLOR,
            FontSize = FONT_SIZE,
            UseBackground = true,
            BackgroundColor = BACKGROUND_COLOR,
            HasCopyButton = true,
        };

        public static DebugPrintStyle DefaultError => new DebugPrintStyle
        {
            Duration = ERROR_DURATION,
            FadeOutTime = FADE_OUT_TIME,
            Color = ERROR_COLOR,
            FontSize = FONT_SIZE,
            UseBackground = true,
            BackgroundColor = BACKGROUND_COLOR,
            HasCopyButton = true,
        };
    }
}