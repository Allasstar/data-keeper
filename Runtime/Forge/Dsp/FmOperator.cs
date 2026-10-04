using Unity.Mathematics;

namespace DataKeeper.Forge.Dsp
{
    // Two-operator FM (phase modulation): sin(carrier + index * sin(modulator)).
    public struct FmOperator
    {
        public float CarrierPhase;
        public float ModulatorPhase;

        public float Next(float phaseIncrement, float ratio, float index)
        {
            var modulator = math.sin(2f * math.PI * ModulatorPhase);
            var output = math.sin(2f * math.PI * CarrierPhase + index * modulator);

            CarrierPhase = math.frac(CarrierPhase + phaseIncrement);
            ModulatorPhase = math.frac(ModulatorPhase + phaseIncrement * ratio);
            return output;
        }
    }
}
