using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    public static class AudioMath
    {
        public const float ReferenceHz = 440f;
        public const float SilenceDb = -200f;

        public static float DbToLinear(float db) => math.pow(10f, db * 0.05f);

        public static float LinearToDb(float linear) =>
            linear <= 0f ? SilenceDb : 20f * math.log10(linear);

        public static float SemitonesToRatio(float semitones) => math.exp2(semitones / 12f);

        public static float RatioToSemitones(float ratio) => 12f * math.log2(ratio);

        public static float SemitonesToHz(float semitones) => ReferenceHz * SemitonesToRatio(semitones);

        public static float MapLog(float t, float min, float max) => min * math.pow(max / min, t);

        public static float UnmapLog(float value, float min, float max) =>
            math.log(value / min) / math.log(max / min);

        public static float2 ConstantPowerPan(float pan)
        {
            var angle = (math.clamp(pan, -1f, 1f) + 1f) * (math.PI * 0.25f);
            return new float2(math.cos(angle), math.sin(angle));
        }
    }
}
