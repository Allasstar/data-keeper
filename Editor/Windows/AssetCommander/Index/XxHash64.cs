using System;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Streaming xxHash64 (seed 0). Streaming rather than one-shot because the indexer reads
    // every asset in 64 KB chunks with an overlap window for the guid scanner — the hash has
    // to see each byte exactly once, in order, while the scanner re-reads the overlap.
    // Not in the plan's file list; the plan asked for "xxHash64 over file bytes" without
    // saying where it lives.
    public sealed class XxHash64
    {
        private const ulong P1 = 11400714785074694791UL;
        private const ulong P2 = 14029467366897019727UL;
        private const ulong P3 = 1609587929392839161UL;
        private const ulong P4 = 9650029242287828579UL;
        private const ulong P5 = 2870177450012600261UL;

        private readonly byte[] _partial = new byte[32];

        private ulong _v1, _v2, _v3, _v4;
        private int _partialLength;
        private long _totalLength;

        public XxHash64()
        {
            Reset();
        }

        public long Length => _totalLength;

        public void Reset()
        {
            unchecked
            {
                _v1 = P1 + P2;
                _v2 = P2;
                _v3 = 0UL;
                _v4 = 0UL - P1;
            }

            _partialLength = 0;
            _totalLength = 0;
        }

        public void Append(byte[] data, int offset, int count)
        {
            if (count <= 0) return;

            _totalLength += count;
            int end = offset + count;

            if (_partialLength > 0)
            {
                int need = 32 - _partialLength;
                if (count < need)
                {
                    Buffer.BlockCopy(data, offset, _partial, _partialLength, count);
                    _partialLength += count;
                    return;
                }

                Buffer.BlockCopy(data, offset, _partial, _partialLength, need);
                ProcessBlock(_partial, 0);
                offset += need;
                _partialLength = 0;
            }

            while (offset + 32 <= end)
            {
                ProcessBlock(data, offset);
                offset += 32;
            }

            int rest = end - offset;
            if (rest > 0)
            {
                Buffer.BlockCopy(data, offset, _partial, 0, rest);
                _partialLength = rest;
            }
        }

        public ulong Digest()
        {
            unchecked
            {
                ulong h;
                if (_totalLength >= 32)
                {
                    h = Rotl(_v1, 1) + Rotl(_v2, 7) + Rotl(_v3, 12) + Rotl(_v4, 18);
                    h = MergeRound(h, _v1);
                    h = MergeRound(h, _v2);
                    h = MergeRound(h, _v3);
                    h = MergeRound(h, _v4);
                }
                else
                {
                    h = P5;
                }

                h += (ulong)_totalLength;

                int i = 0;
                while (i + 8 <= _partialLength)
                {
                    h ^= Round(0UL, Read64(_partial, i));
                    h = Rotl(h, 27) * P1 + P4;
                    i += 8;
                }

                if (i + 4 <= _partialLength)
                {
                    h ^= Read32(_partial, i) * P1;
                    h = Rotl(h, 23) * P2 + P3;
                    i += 4;
                }

                while (i < _partialLength)
                {
                    h ^= _partial[i] * P5;
                    h = Rotl(h, 11) * P1;
                    i++;
                }

                h ^= h >> 33;
                h *= P2;
                h ^= h >> 29;
                h *= P3;
                h ^= h >> 32;
                return h;
            }
        }

        public static ulong Compute(byte[] data, int offset, int count)
        {
            var hash = new XxHash64();
            hash.Append(data, offset, count);
            return hash.Digest();
        }

        private void ProcessBlock(byte[] d, int o)
        {
            _v1 = Round(_v1, Read64(d, o));
            _v2 = Round(_v2, Read64(d, o + 8));
            _v3 = Round(_v3, Read64(d, o + 16));
            _v4 = Round(_v4, Read64(d, o + 24));
        }

        private static ulong Round(ulong acc, ulong input)
        {
            unchecked
            {
                acc += input * P2;
                acc = Rotl(acc, 31);
                acc *= P1;
                return acc;
            }
        }

        private static ulong MergeRound(ulong acc, ulong val)
        {
            unchecked
            {
                acc ^= Round(0UL, val);
                return acc * P1 + P4;
            }
        }

        private static ulong Rotl(ulong value, int bits) => (value << bits) | (value >> (64 - bits));

        private static ulong Read64(byte[] d, int o) =>
            d[o]
            | ((ulong)d[o + 1] << 8)
            | ((ulong)d[o + 2] << 16)
            | ((ulong)d[o + 3] << 24)
            | ((ulong)d[o + 4] << 32)
            | ((ulong)d[o + 5] << 40)
            | ((ulong)d[o + 6] << 48)
            | ((ulong)d[o + 7] << 56);

        private static ulong Read32(byte[] d, int o) =>
            d[o]
            | ((ulong)d[o + 1] << 8)
            | ((ulong)d[o + 2] << 16)
            | ((ulong)d[o + 3] << 24);
    }
}
