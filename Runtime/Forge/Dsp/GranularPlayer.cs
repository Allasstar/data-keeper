using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace DataKeeper.Forge.Dsp
{
    // Asynchronous granular playback with Hann-windowed grains. Grain state lives in a fixed
    // list on the stack, so a voice allocates nothing.
    public struct GranularPlayer
    {
        public const int MaxGrains = 128;

        // Grain start times wander by this fraction of the interval so grains never lock into
        // an audible pulse at the density rate.
        private const float IntervalJitter = 0.25f;

        private struct Grain
        {
            public float Position;
            public float Step;
            public int Age;
            public int Length;
        }

        private FixedList4096Bytes<Grain> _grains;
        private Random _random;
        private float _countdown;

        // Read head in source samples, counted from the playback start.
        public float Playhead;

        public GranularPlayer(uint seed, float start)
        {
            _grains = default;
            _random = new Random(math.max(1u, seed ^ 0x6A41u));
            _countdown = 0f;
            Playhead = start;
        }

        // `step` is the pitched read speed for new grains; `baseStep` moves the read head in real time.
        public float Next(NativeArray<float> data, int offset, int length, float baseStep, float step,
            int grainFrames, float interval, float spray, float pitchRandom, bool reverse, bool cubic)
        {
            _countdown -= 1f;
            if (_countdown <= 0f)
            {
                _countdown += interval * (1f + _random.NextFloat(-IntervalJitter, IntervalJitter));
                var position = Playhead + _random.NextFloat(-1f, 1f) * spray;
                var detune = AudioMath.SemitonesToRatio(_random.NextFloat(-1f, 1f) * pitchRandom);
                if (_grains.Length < MaxGrains)
                    _grains.Add(new Grain { Position = position, Step = step * detune, Length = grainFrames });
            }

            Playhead += baseStep;

            var sum = 0f;
            for (var g = _grains.Length - 1; g >= 0; g--)
            {
                var grain = _grains[g];
                var window = 0.5f - 0.5f * math.cos(2f * math.PI * grain.Age / grain.Length);
                var readAt = reverse ? length - 1 - grain.Position : grain.Position;
                sum += window * SamplePlayer.ReadAt(data, offset, length, readAt, cubic);

                grain.Position += grain.Step;
                grain.Age++;
                if (grain.Age >= grain.Length) _grains.RemoveAtSwapBack(g);
                else _grains[g] = grain;
            }

            // Overlapping grains are uncorrelated, so they add in power: scale by 1 / sqrt(overlap).
            return sum * math.rsqrt(math.max(1f, grainFrames / interval));
        }
    }
}
