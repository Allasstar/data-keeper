using UnityEngine;

namespace DataKeeper.DebugPrinter
{
    public struct DebugPrintStyle
    {
        public float Duration;
        public Color Color;
        public int FontSize;

        public bool UseBackground;
        public Color BackgroundColor;

        public bool UseShadow;
        public Color ShadowColor;

        public static DebugPrintStyle DefaultLog => new DebugPrintStyle
        {
            Duration = 3f,
            Color = Color.white,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = new Color(0f, 0f, 0f, 0.3f),
            UseShadow = true,
            ShadowColor = Color.black
        };

        public static DebugPrintStyle DefaultError => new DebugPrintStyle
        {
            Duration = 6f,
            Color = Color.crimson,
            FontSize = 25,
            UseBackground = true,
            BackgroundColor = new Color(0.5f, 0f, 0f, 0.3f),
            UseShadow = true,
            ShadowColor = Color.black
        };
    }
}