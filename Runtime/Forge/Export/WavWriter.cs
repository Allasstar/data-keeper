using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;

namespace DataKeeper.Forge.Export
{
    public enum WavFormat
    {
        Pcm16 = 0,
        Pcm24 = 1,
        Float32 = 2,
    }

    public static class WavWriter
    {
        private const ushort FormatPcm = 1;
        private const ushort FormatIeeeFloat = 3;

        public static int BytesPerSample(WavFormat format) => format switch
        {
            WavFormat.Pcm16 => 2,
            WavFormat.Pcm24 => 3,
            _ => 4,
        };

        public static void Write(Stream stream, NativeArray<float> interleaved, int sourceChannels,
            int sampleRate, WavFormat format, int outputChannels)
        {
            var frames = interleaved.Length / sourceChannels;
            var bytesPerSample = BytesPerSample(format);
            var blockAlign = outputChannels * bytesPerSample;
            var dataSize = frames * blockAlign;
            var isFloat = format == WavFormat.Float32;

            // Non-PCM formats need the extended fmt chunk (cbSize) and a fact chunk per the RIFF spec.
            var fmtSize = isFloat ? 18 : 16;
            var factSize = isFloat ? 12 : 0;

            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(4 + 8 + fmtSize + factSize + 8 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(fmtSize);
            writer.Write(isFloat ? FormatIeeeFloat : FormatPcm);
            writer.Write((ushort)outputChannels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * blockAlign);
            writer.Write((ushort)blockAlign);
            writer.Write((ushort)(bytesPerSample * 8));
            if (isFloat) writer.Write((ushort)0);

            if (isFloat)
            {
                writer.Write(Encoding.ASCII.GetBytes("fact"));
                writer.Write(4);
                writer.Write(frames);
            }

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (var frame = 0; frame < frames; frame++)
            {
                for (var channel = 0; channel < outputChannels; channel++)
                    WriteSample(writer, ReadSample(interleaved, frame, channel, sourceChannels, outputChannels), format);
            }
        }

        private static float ReadSample(NativeArray<float> interleaved, int frame, int channel,
            int sourceChannels, int outputChannels)
        {
            var baseIndex = frame * sourceChannels;
            if (outputChannels == sourceChannels) return interleaved[baseIndex + channel];
            if (outputChannels == 1)
            {
                var sum = 0f;
                for (var c = 0; c < sourceChannels; c++) sum += interleaved[baseIndex + c];
                return sum / sourceChannels;
            }

            return interleaved[baseIndex + math.min(channel, sourceChannels - 1)];
        }

        private static void WriteSample(BinaryWriter writer, float sample, WavFormat format)
        {
            switch (format)
            {
                case WavFormat.Pcm16:
                    writer.Write((short)math.round(math.clamp(sample, -1f, 1f) * short.MaxValue));
                    break;
                case WavFormat.Pcm24:
                    var value = (int)math.round(math.clamp(sample, -1f, 1f) * 8388607f);
                    writer.Write((byte)value);
                    writer.Write((byte)(value >> 8));
                    writer.Write((byte)(value >> 16));
                    break;
                default:
                    writer.Write(sample);
                    break;
            }
        }
    }
}
