namespace DataKeeper.DebugPrinter
{
    using UnityEngine;

    public struct DebugPrintStyle
    {
        public float Duration;      // fully visible time
        public float FadeOutTime;   // fade duration

        public Color Color;
        public int FontSize;

        public bool UseBackground;
        public Color BackgroundColor;

        public static DebugPrintStyle DefaultLog => new DebugPrintStyle
        {
            Duration = 3f,
            FadeOutTime = 2f,
            Color = Color.white,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = new Color(0f, 0f, 0f, 0.3f),
        };

        public static DebugPrintStyle DefaultError => new DebugPrintStyle
        {
            Duration = 6f,
            FadeOutTime = 2f,
            Color = Color.orange,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = new Color(0.3f, 0f, 0f, 0.3f),
        };
    }
}