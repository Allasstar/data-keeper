using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace DataKeeper.Forge.Render
{
    // Layers arrive already gained and panned, so mixing is a plain sum over each voice span.
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
    public struct MixJob : IJob
    {
        public int FrameCount;
        public int LayerCount;
        [ReadOnly] public NativeArray<LayerRenderParams> Layers;
        [ReadOnly] public NativeArray<float> LayerBuffer;
        public NativeArray<float> Output;

        public void Execute()
        {
            var sampleCount = FrameCount * 2;
            for (var i = 0; i < sampleCount; i++) Output[i] = 0f;

            for (var layer = 0; layer < LayerCount; layer++)
            {
                var p = Layers[layer];
                if (p.Gain <= 0f) continue;

                var offset = layer * sampleCount;
                for (var i = p.StartFrame * 2; i < p.EndFrame * 2; i++)
                    Output[i] += LayerBuffer[offset + i];
            }
        }
    }
}
