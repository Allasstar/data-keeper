using System.Text;
using DataKeeper.Editor.Windows.AssetCommander;
using NUnit.Framework;

namespace DataKeeper.Tests.Editor.AssetCommander
{
    public class XxHash64Tests
    {
        // Published xxHash64 vectors (seed 0). If these drift, duplicate detection silently
        // stops matching caches written by an earlier version.
        [TestCase("", 0xEF46DB3751D8E999UL)]
        [TestCase("abc", 0x44BC2CF5AD770999UL)]
        [TestCase("Nobody inspects the spammish repetition", 0xFBCEA83C8A378BF1UL)]
        public void Digest_MatchesReferenceVectors(string input, ulong expected)
        {
            var bytes = Encoding.ASCII.GetBytes(input);

            Assert.That(XxHash64.Compute(bytes, 0, bytes.Length), Is.EqualTo(expected));
        }

        [Test]
        public void Digest_IsIndependentOfAppendChunking()
        {
            var data = new byte[1024];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)i;

            ulong oneShot = XxHash64.Compute(data, 0, data.Length);
            Assert.That(oneShot, Is.EqualTo(0x6F3914F18FE4DF57UL));

            foreach (int step in new[] { 1, 3, 7, 8, 31, 32, 33, 512 })
            {
                var hash = new XxHash64();
                for (int offset = 0; offset < data.Length; offset += step)
                    hash.Append(data, offset, System.Math.Min(step, data.Length - offset));

                Assert.That(hash.Digest(), Is.EqualTo(oneShot), $"step {step}");
            }
        }

        [Test]
        public void Reset_ReturnsTheHashToItsInitialState()
        {
            var bytes = Encoding.ASCII.GetBytes("abc");
            var hash = new XxHash64();

            hash.Append(bytes, 0, bytes.Length);
            hash.Reset();
            hash.Append(bytes, 0, bytes.Length);

            Assert.That(hash.Length, Is.EqualTo(3));
            Assert.That(hash.Digest(), Is.EqualTo(0x44BC2CF5AD770999UL));
        }
    }
}
