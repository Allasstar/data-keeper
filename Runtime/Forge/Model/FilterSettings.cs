using System;

namespace DataKeeper.Forge
{
    [Serializable]
    public struct FilterSettings
    {
        public FilterType Type;
        public float CutoffHz;
        public float Resonance;

        public static FilterSettings Default => new()
        {
            Type = FilterType.Off,
            CutoffHz = 2000f,
            Resonance = 0.1f,
        };
    }
}
