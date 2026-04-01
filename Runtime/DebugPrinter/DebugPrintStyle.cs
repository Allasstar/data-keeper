namespace DataKeeper.DebugPrinter
{
    using UnityEngine;

    public struct DebugPrintStyle
    {
        public static readonly Color BACKGROUND_COLOR = new Color(0f, 0f, 0f, 0.3f);
        public static readonly Color LOG_COLOR = new Color(1f, 1f, 1f, 1f);
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

        public static DebugPrintStyle LogStyle(Color color, float duration = 3f, int fontSize = 25, bool hasCopyButton = false)
        {
            return new DebugPrintStyle
            {
                Duration = duration,
                FadeOutTime = 1f,
                Color = color,
                FontSize = fontSize,
                UseBackground = true,
                BackgroundColor = BACKGROUND_COLOR,
                HasCopyButton = hasCopyButton,
            };
        }
        
        public static DebugPrintStyle ErrorStyle(Color color, float duration = 6f, int fontSize = 25, bool hasCopyButton = true)
        {
            return new DebugPrintStyle
            {
                Duration = duration,
                FadeOutTime = 1f,
                Color = color,
                FontSize = fontSize,
                UseBackground = true,
                BackgroundColor = BACKGROUND_COLOR,
                HasCopyButton = hasCopyButton,
            };
        }

        public static DebugPrintStyle DefaultLog => new DebugPrintStyle
        {
            Duration = 3f,
            FadeOutTime = 1f,
            Color = LOG_COLOR,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = BACKGROUND_COLOR,
            HasCopyButton = false,
        };
        
        public static DebugPrintStyle DefaultLogWithCopy => new DebugPrintStyle
        {
            Duration = 3f,
            FadeOutTime = 1f,
            Color = LOG_COLOR,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = BACKGROUND_COLOR,
            HasCopyButton = true,
        };

        public static DebugPrintStyle DefaultError => new DebugPrintStyle
        {
            Duration = 6f,
            FadeOutTime = 1f,
            Color = Color.orange,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = BACKGROUND_COLOR,
            HasCopyButton = true,
        };
    }
}