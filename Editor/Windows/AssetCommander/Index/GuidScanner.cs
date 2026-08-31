using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // A Unity guid as its exact 128-bit value, so the scanner can dedupe before it
    // materialises a string. Hashing the hex text into a 64-bit key would be cheaper still,
    // but a collision would silently drop a real dependency and surface as a false "unused
    // asset" — the value is exact, so it cannot.
    public readonly struct GuidKey : IEquatable<GuidKey>
    {
        private const string HexDigits = "0123456789abcdef";

        public readonly ulong Hi;
        public readonly ulong Lo;

        public GuidKey(ulong hi, ulong lo)
        {
            Hi = hi;
            Lo = lo;
        }

        public bool Equals(GuidKey other) => Hi == other.Hi && Lo == other.Lo;

        public override bool Equals(object obj) => obj is GuidKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                ulong mixed = Hi ^ (Lo * 0x9E3779B97F4A7C15UL);
                return (int)(mixed ^ (mixed >> 32));
            }
        }

        // Lowercase hex, matching what AssetDatabase.AssetPathToGUID returns — the scanner
        // accepts uppercase in YAML and normalises here, so both sides of every lookup agree.
        public override string ToString()
        {
            var chars = new char[32];
            for (int i = 0; i < 16; i++) chars[15 - i] = HexDigits[(int)((Hi >> (i * 4)) & 0xF)];
            for (int i = 0; i < 16; i++) chars[31 - i] = HexDigits[(int)((Lo >> (i * 4)) & 0xF)];
            return new string(chars);
        }

        public static bool TryParse(string text, out GuidKey key)
        {
            key = default;
            if (string.IsNullOrEmpty(text) || text.Length != 32) return false;

            ulong hi = 0, lo = 0;
            for (int i = 0; i < 32; i++)
            {
                int v = GuidScanner.HexValue((byte)text[i]);
                if (v < 0) return false;
                if (i < 16) hi = (hi << 4) | (uint)v;
                else lo = (lo << 4) | (uint)v;
            }

            key = new GuidKey(hi, lo);
            return true;
        }
    }

    // Per-worker scratch, reused across every file that worker touches, so a 50k-asset
    // rebuild allocates two hash sets per thread rather than two per asset.
    public sealed class ScanScratch
    {
        public readonly HashSet<GuidKey> Dependencies = new HashSet<GuidKey>();
        public readonly HashSet<GuidKey> ScriptRefs = new HashSet<GuidKey>();
        public readonly XxHash64 Hash = new XxHash64();

        public void Reset()
        {
            Dependencies.Clear();
            ScriptRefs.Clear();
            Hash.Reset();
        }

        public string[] DependencyStrings(string excludeGuid) => ToArray(Dependencies, excludeGuid);

        public string[] ScriptStrings() => ToArray(ScriptRefs, null);

        private static string[] ToArray(HashSet<GuidKey> set, string excludeGuid)
        {
            if (set.Count == 0) return AssetRecord.NoGuids;

            var result = new List<string>(set.Count);
            foreach (var key in set)
            {
                var text = key.ToString();
                if (text != excludeGuid) result.Add(text);
            }

            return result.Count == 0 ? AssetRecord.NoGuids : result.ToArray();
        }
    }

    public static class GuidScanner
    {
        public const int DefaultChunkSize = 64 * 1024;

        // Past this, hashing stops and only the length still distinguishes two files. Reading
        // a multi-GB asset end to end would dominate a rebuild, and two assets agreeing on
        // both their first 64 MB and their exact length are not a duplicate-detection problem.
        public const long MaxHashBytes = 64L * 1024 * 1024;

        // "guid: " plus 32 hex digits.
        private const int MatchLength = 6 + 32;

        // Furthest back the "m_Script:" owning a guid can sit: "  m_Script: {fileID: 11500000, "
        // is 31 bytes, and 64 leaves room for deeper indentation.
        public const int LookBack = 64;

        // Carried between chunks so a match straddling a read boundary is whole in the next
        // buffer, lookback included.
        public const int OverlapBytes = LookBack + MatchLength;

        public static void ScanBuffer(byte[] buffer, int length, HashSet<GuidKey> dependencies,
            HashSet<GuidKey> scriptRefs)
        {
            int last = length - MatchLength;
            for (int i = 0; i <= last; i++)
            {
                if (buffer[i] != (byte)'g') continue;
                if (buffer[i + 1] != (byte)'u' || buffer[i + 2] != (byte)'i' || buffer[i + 3] != (byte)'d'
                    || buffer[i + 4] != (byte)':' || buffer[i + 5] != (byte)' ') continue;

                int hex = i + 6;
                if (!TryReadHex32(buffer, hex, out var key)) continue;

                // A 33rd hex digit means this is some longer token, not a Unity guid.
                int after = hex + 32;
                if (after < length && HexValue(buffer[after]) >= 0)
                {
                    i = after;
                    continue;
                }

                dependencies?.Add(key);
                if (scriptRefs != null && HasScriptPrefix(buffer, i)) scriptRefs.Add(key);

                i = after - 1;
            }
        }

        public static bool ScanFile(string absolutePath, ScanScratch scratch, bool forceGuidScan,
            bool hashContent, int chunkSize = DefaultChunkSize)
        {
            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(chunkSize + OverlapBytes);

            try
            {
                using (var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                {
                    int carry = 0;
                    long hashed = 0;
                    bool scanGuids = forceGuidScan;
                    bool sniffed = false;

                    while (true)
                    {
                        int read = stream.Read(buffer, carry, chunkSize);
                        if (read <= 0) break;

                        if (hashContent && hashed < MaxHashBytes)
                        {
                            int n = (int)Math.Min(read, MaxHashBytes - hashed);
                            scratch.Hash.Append(buffer, carry, n);
                            hashed += n;
                        }

                        int total = carry + read;

                        if (!sniffed)
                        {
                            sniffed = true;
                            if (!scanGuids) scanGuids = LooksLikeYaml(buffer, total);
                        }

                        if (scanGuids) ScanBuffer(buffer, total, scratch.Dependencies, scratch.ScriptRefs);
                        else if (!hashContent || hashed >= MaxHashBytes) break;

                        carry = Math.Min(OverlapBytes, total);
                        Buffer.BlockCopy(buffer, total - carry, buffer, 0, carry);
                    }
                }

                return true;
            }
            // A worker that throws would abort the whole Parallel.ForEach and lose the
            // rebuild, and assets do get locked or deleted mid-scan by the importer.
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        internal static int HexValue(byte b)
        {
            if (b >= (byte)'0' && b <= (byte)'9') return b - (byte)'0';
            if (b >= (byte)'a' && b <= (byte)'f') return b - (byte)'a' + 10;
            if (b >= (byte)'A' && b <= (byte)'F') return b - (byte)'A' + 10;
            return -1;
        }

        private static bool TryReadHex32(byte[] buffer, int offset, out GuidKey key)
        {
            ulong hi = 0, lo = 0;

            for (int i = 0; i < 16; i++)
            {
                int v = HexValue(buffer[offset + i]);
                if (v < 0)
                {
                    key = default;
                    return false;
                }

                hi = (hi << 4) | (uint)v;
            }

            for (int i = 16; i < 32; i++)
            {
                int v = HexValue(buffer[offset + i]);
                if (v < 0)
                {
                    key = default;
                    return false;
                }

                lo = (lo << 4) | (uint)v;
            }

            key = new GuidKey(hi, lo);
            return true;
        }

        private static bool HasScriptPrefix(byte[] buffer, int guidStart)
        {
            int stop = Math.Max(0, guidStart - LookBack);
            for (int i = guidStart - 1; i >= stop; i--)
            {
                byte b = buffer[i];
                if (b == (byte)'\n' || b == (byte)'\r') return false;
                if (b != (byte)'m' || i + 9 > guidStart) continue;

                if (buffer[i + 1] == (byte)'_' && buffer[i + 2] == (byte)'S' && buffer[i + 3] == (byte)'c'
                    && buffer[i + 4] == (byte)'r' && buffer[i + 5] == (byte)'i' && buffer[i + 6] == (byte)'p'
                    && buffer[i + 7] == (byte)'t' && buffer[i + 8] == (byte)':') return true;
            }

            return false;
        }

        private static bool LooksLikeYaml(byte[] buffer, int length) =>
            length >= 5 && buffer[0] == (byte)'%' && buffer[1] == (byte)'Y' && buffer[2] == (byte)'A'
            && buffer[3] == (byte)'M' && buffer[4] == (byte)'L';
    }
}
