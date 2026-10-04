using System.IO;
using DataKeeper.Forge.Export;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class WavRoundTripTests
    {
        private const int SampleRate = 44100;
        private const int Frames = 1000;

        [Test]
        public void WriteThenRead_PreservesHeaderAndSamples(
            [Values] WavFormat format,
            [Values(1, 2)] int outputChannels)
        {
            var source = new NativeArray<float>(Frames * 2, Allocator.Temp);
            for (var i = 0; i < Frames; i++)
            {
                source[i * 2] = Mathf.Sin(i * 0.05f) * 0.9f;
                source[i * 2 + 1] = Mathf.Cos(i * 0.031f) * -0.7f;
            }

            using var stream = new MemoryStream();
            WavWriter.Write(stream, source, 2, SampleRate, format, outputChannels);
            stream.Position = 0;
            var wav = WavReader.Read(stream);

            Assert.AreEqual(SampleRate, wav.SampleRate);
            Assert.AreEqual(outputChannels, wav.Channels);
            Assert.AreEqual(format, wav.Format);
            Assert.AreEqual(Frames * outputChannels, wav.Samples.Length);

            var tolerance = format switch
            {
                WavFormat.Pcm16 => 1f / 32767f,
                WavFormat.Pcm24 => 2f / 8388607f,
                _ => 0f,
            };

            for (var frame = 0; frame < Frames; frame++)
            {
                for (var c = 0; c < outputChannels; c++)
                {
                    var expected = outputChannels == 2
                        ? source[frame * 2 + c]
                        : (source[frame * 2] + source[frame * 2 + 1]) / 2f;
                    Assert.AreEqual(expected, wav.Samples[frame * outputChannels + c], tolerance);
                }
            }

            source.Dispose();
        }

        [Test]
        public void Write_ClampsOutOfRangeForPcm()
        {
            using var source = new NativeArray<float>(new[] { 2f, -3f }, Allocator.Temp);
            using var stream = new MemoryStream();

            WavWriter.Write(stream, source, 1, SampleRate, WavFormat.Pcm16, 1);
            stream.Position = 0;
            var wav = WavReader.Read(stream);

            Assert.AreEqual(1f, wav.Samples[0], 1e-6f);
            Assert.AreEqual(-1f, wav.Samples[1], 1e-6f);
        }
    }
}
