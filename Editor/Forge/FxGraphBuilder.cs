using System;
using DataKeeper.Forge;
using DataKeeper.Forge.Dsp;
using DataKeeper.Forge.Dsp.Fx;
using Unity.Collections;
using UnityEngine;

namespace DataKeeper.Editor.Forge
{
    // Runs the real FX code on short synthetic signals, so each graph shows what the effect does
    // with its current settings whatever the layers sound like (FUI-D16). A low sample rate keeps
    // a 4 s reverb response cheap enough to redraw while a knob is dragged.
    public sealed class FxGraphBuilder : IDisposable
    {
        private const int SampleRate = 4000;
        private const int MaxFrames = (int)(SfxRecipe.MaxLengthMs * SampleRate / 1000f);

        private const float ReverbRangeDb = 60f;

        private const float TransientWindowMs = 300f;
        private const float TransientAttackMs = 1f;
        private const float TransientDecayMs = 60f;

        private const float LimiterWindowMs = 600f;
        private const float LimiterQuiet = 0.2f;
        private const float LimiterBurst = 2f;
        private const float LimiterBurstStartMs = 60f;
        private const float LimiterBurstEndMs = 100f;
        private const float LimiterFloorDb = -30f;
        private const float LimiterTopDb = 6f;

        private NativeArray<float> _buffer;
        private NativeArray<float> _wet;
        private NativeArray<float> _gain;
        private NativeArray<float> _reverbMemory;
        private NativeArray<float> _delayLine;

        public FxGraphBuilder()
        {
            _buffer = new NativeArray<float>(MaxFrames * 2, Allocator.Persistent);
            _wet = new NativeArray<float>(MaxFrames * 2, Allocator.Persistent);
            _gain = new NativeArray<float>(MaxFrames, Allocator.Persistent);
            _reverbMemory = new NativeArray<float>(Reverb.MemorySize(SampleRate), Allocator.Persistent);
            _delayLine = new NativeArray<float>(
                StereoDelay.LineSize(FxParams.DelayFramesFor(DelaySettings.MaxTimeMs, SampleRate)), Allocator.Persistent);
        }

        public void Dispose()
        {
            _buffer.Dispose();
            _wet.Dispose();
            _gain.Dispose();
            _reverbMemory.Dispose();
            _delayLine.Dispose();
        }

        // Disabled effects are skipped: their modules are collapsed, and turning one on is a
        // recipe change that renders and refills.
        public void Fill(FxChain fx, float lengthMs, FxGraphElement transient, FxGraphElement distortion,
            FxGraphElement delay, FxGraphElement reverb, FxGraphElement limiter)
        {
            var p = FxParams.From(fx, SampleRate);
            if (p.TransientEnabled) FillTransient(transient, p);
            if (p.DistortionEnabled) FillDistortion(distortion, p);
            if (p.DelayEnabled) FillDelay(delay, p, lengthMs);
            if (p.ReverbEnabled) FillReverb(reverb, p, lengthMs);
            if (p.LimiterEnabled) FillLimiter(limiter, p);
        }

        private static void FillDistortion(FxGraphElement graph, in FxParams p)
        {
            graph.Bipolar = true;
            var reference = graph.Reference(2);
            reference[0] = -1f;
            reference[1] = 1f;

            var trace = graph.Trace(FxGraphElement.Capacity);
            for (var i = 0; i < trace.Length; i++)
            {
                var x = (float)i / (trace.Length - 1) * 2f - 1f;
                trace[i] = Mathf.Lerp(x, Distortion.Shape(x * p.DistortionDrive, p.DistortionMode), p.DistortionMix);
            }

            graph.MarkDirtyRepaint();
        }

        private void FillDelay(FxGraphElement graph, in FxParams p, float lengthMs)
        {
            var frames = Frames(lengthMs);
            Impulse(frames);
            StereoDelay.Process(_buffer, frames, _delayLine, p.DelayFrames, p.DelayFeedback, p.DelayMix, p.DelayPingPong);

            graph.Reference(0);
            Peaks(graph.Trace(FxGraphElement.Capacity), frames);
            graph.MarkDirtyRepaint();
        }

        private void FillReverb(FxGraphElement graph, in FxParams p, float lengthMs)
        {
            var frames = Frames(lengthMs);
            Impulse(frames);
            Reverb.Process(_buffer, frames, _wet, _reverbMemory, SampleRate, p.ReverbSize, p.ReverbDamping, 1f);
            // The graph is the tail alone; the dry impulse would set the scale and flatten it.
            _buffer[0] -= 1f;
            _buffer[1] -= 1f;

            graph.Reference(0);
            var trace = graph.Trace(FxGraphElement.Capacity);
            Peaks(trace, frames);

            var peak = 0f;
            foreach (var value in trace) peak = Mathf.Max(peak, value);
            for (var i = 0; i < trace.Length; i++)
                trace[i] = peak > 0f ? Mathf.Clamp01(1f + AudioMath.LinearToDb(trace[i] / peak) / ReverbRangeDb) : 0f;
            graph.MarkDirtyRepaint();
        }

        private void FillTransient(FxGraphElement graph, in FxParams p)
        {
            var frames = Frames(TransientWindowMs);
            var attackFrames = Frames(TransientAttackMs);
            var decayFrames = (float)Frames(TransientDecayMs);
            for (var frame = 0; frame < frames; frame++)
            {
                var envelope = frame < attackFrames
                    ? (frame + 1f) / attackFrames
                    : Mathf.Exp(-(frame - attackFrames) / decayFrames);
                // Alternating sign gives a carrier whose rectified level is exactly the envelope.
                var sample = (frame & 1) == 0 ? envelope : -envelope;
                _buffer[frame * 2] = sample;
                _buffer[frame * 2 + 1] = sample;
            }

            var reference = graph.Reference(FxGraphElement.Capacity);
            Peaks(reference, frames);
            TransientShaper.Process(_buffer, frames, SampleRate, p.TransientAttack, p.TransientSustain);
            var trace = graph.Trace(FxGraphElement.Capacity);
            Peaks(trace, frames);

            // A shared scale, so a boosted attack visibly rises above the input.
            var peak = 0f;
            for (var i = 0; i < trace.Length; i++) peak = Mathf.Max(peak, Mathf.Max(trace[i], reference[i]));
            for (var i = 0; i < trace.Length; i++)
            {
                trace[i] /= peak;
                reference[i] /= peak;
            }

            graph.MarkDirtyRepaint();
        }

        private void FillLimiter(FxGraphElement graph, in FxParams p)
        {
            var frames = Frames(LimiterWindowMs);
            var burstStart = Frames(LimiterBurstStartMs);
            var burstEnd = Frames(LimiterBurstEndMs);
            for (var frame = 0; frame < frames; frame++)
            {
                var level = frame >= burstStart && frame < burstEnd ? LimiterBurst : LimiterQuiet;
                var sample = (frame & 1) == 0 ? level : -level;
                _buffer[frame * 2] = sample;
                _buffer[frame * 2 + 1] = sample;
            }

            var reference = graph.Reference(FxGraphElement.Capacity);
            Peaks(reference, frames);
            Limiter.Process(_buffer, frames, _gain, SampleRate, p.LimiterCeiling, p.LimiterReleaseMs);
            var trace = graph.Trace(FxGraphElement.Capacity);
            Peaks(trace, frames);

            for (var i = 0; i < trace.Length; i++)
            {
                trace[i] = LimiterY(trace[i]);
                reference[i] = LimiterY(reference[i]);
            }

            graph.Marker = LimiterY(p.LimiterCeiling);
            graph.MarkDirtyRepaint();
        }

        private static float LimiterY(float linear) =>
            Mathf.Clamp01((AudioMath.LinearToDb(linear) - LimiterFloorDb) / (LimiterTopDb - LimiterFloorDb));

        private static int Frames(float ms) => Mathf.Clamp(Mathf.RoundToInt(ms * SampleRate / 1000f), 1, MaxFrames);

        private void Impulse(int frames)
        {
            for (var i = 0; i < frames * 2; i++) _buffer[i] = 0f;
            _buffer[0] = 1f;
            _buffer[1] = 1f;
        }

        // Peak of either channel per column, so a short tap or a burst edge is never skipped.
        private void Peaks(Span<float> target, int frames)
        {
            var columns = target.Length;
            for (var column = 0; column < columns; column++)
            {
                var first = (int)((long)column * frames / columns);
                var last = Mathf.Max(first + 1, (int)((long)(column + 1) * frames / columns));
                var peak = 0f;
                for (var frame = first; frame < last && frame < frames; frame++)
                    peak = Mathf.Max(peak, Mathf.Max(Mathf.Abs(_buffer[frame * 2]), Mathf.Abs(_buffer[frame * 2 + 1])));
                target[column] = peak;
            }
        }
    }
}
