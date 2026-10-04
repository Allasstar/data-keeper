using System.IO;
using System.Text;

namespace DataKeeper.Forge.Export
{
    public struct WavData
    {
        public int SampleRate;
        public int Channels;
        public WavFormat Format;
        public float[] Samples;
    }

    public static class WavReader
    {
        public static WavData Read(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, true);

            if (ReadTag(reader) != "RIFF") throw new InvalidDataException("Not a RIFF file.");
            reader.ReadInt32();
            if (ReadTag(reader) != "WAVE") throw new InvalidDataException("Not a WAVE file.");

            var data = new WavData();
            var bitsPerSample = 0;
            var formatTag = 0;

            while (stream.Position + 8 <= stream.Length)
            {
                var tag = ReadTag(reader);
                var size = reader.ReadInt32();
                var next = stream.Position + size + (size & 1);

                if (tag == "fmt ")
                {
                    formatTag = reader.ReadUInt16();
                    data.Channels = reader.ReadUInt16();
                    data.SampleRate = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadUInt16();
                    bitsPerSample = reader.ReadUInt16();
                }
                else if (tag == "data")
                {
                    data.Format = formatTag == 3 ? WavFormat.Float32
                        : bitsPerSample == 24 ? WavFormat.Pcm24
                        : WavFormat.Pcm16;
                    data.Samples = ReadSamples(reader, size / (bitsPerSample / 8), data.Format);
                }

                stream.Position = next;
            }

            return data;
        }

        private static float[] ReadSamples(BinaryReader reader, int count, WavFormat format)
        {
            var samples = new float[count];
            for (var i = 0; i < count; i++)
            {
                switch (format)
                {
                    case WavFormat.Pcm16:
                        samples[i] = reader.ReadInt16() / (float)short.MaxValue;
                        break;
                    case WavFormat.Pcm24:
                        var b0 = reader.ReadByte();
                        var b1 = reader.ReadByte();
                        var b2 = reader.ReadByte();
                        var value = (b0 | (b1 << 8) | (b2 << 16)) << 8 >> 8;
                        samples[i] = value / 8388607f;
                        break;
                    default:
                        samples[i] = reader.ReadSingle();
                        break;
                }
            }

            return samples;
        }

        private static string ReadTag(BinaryReader reader) => Encoding.ASCII.GetString(reader.ReadBytes(4));
    }
}
