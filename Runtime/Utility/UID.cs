using System;

namespace DataKeeper.Utility
{
    /// <summary>
    /// Utility class for generating various types of unique identifiers.
    /// </summary>
    public static class UID
    {
        /// <summary>
        /// Generates a standard RFC 4122 version 4 GUID.
        /// </summary>
        /// <returns>A new <see cref="Guid"/>.</returns>
        /// <remarks>
        /// Collision probability: Extremely low (standard 128-bit GUID).
        /// </remarks>
        public static Guid GuidId() => Guid.NewGuid();

        /// <summary>
        /// Generates a 32-bit integer ID by taking the first 4 bytes of a new GUID.
        /// </summary>
        /// <returns>A random 32-bit integer.</returns>
        /// <remarks>
        /// Collision probability: High compared to GUID (1 in 2^32). Use only for non-critical IDs where collisions are handled or unlikely in small sets.
        /// </remarks>
        public static int Int32Id() => BitConverter.ToInt32(GuidId().ToByteArray(), 0);

        /// <summary>
        /// Generates a 64-bit long ID by taking the first 8 bytes of a new GUID.
        /// </summary>
        /// <returns>A random 64-bit long.</returns>
        /// <remarks>
        /// Collision probability: Moderate (1 in 2^64). Much safer than <see cref="Int32Id"/> but less unique than <see cref="GuidId"/>.
        /// </remarks>
        public static long Int64Id() => BitConverter.ToInt64(GuidId().ToByteArray(), 0);

        /// <summary>
        /// Generates a 32-bit integer ID using the hash code of a new GUID.
        /// </summary>
        /// <returns>A 32-bit integer hash.</returns>
        /// <remarks>
        /// Collision probability: High (1 in 2^32), similar to <see cref="Int32Id"/>.
        /// </remarks>
        public static int HashId() => GuidId().GetHashCode();
        
        
        static readonly int DevId = Fnv(Environment.MachineName + "|" + Environment.UserName) & 1023;

        static long last;
        static int seq;

        /// <summary>
        /// Generates a 64-bit time-ordered ID inspired by Snowflake IDs.
        /// </summary>
        /// <returns>A unique 64-bit long ID.</returns>
        /// <remarks>
        /// Structure:
        /// - 42 bits: Timestamp (milliseconds since Unix epoch).
        /// - 10 bits: Device ID (derived from MachineName and UserName, max 1024 unique devices).
        /// - 12 bits: Sequence number (max 4096 IDs per millisecond per device).
        /// 
        /// Collision probability: 
        /// - Zero on a single device (guaranteed by sequence and timestamp wait).
        /// - Low across multiple devices, unless two devices happen to have the same 10-bit <see cref="DevId"/> (1 in 1024 chance) and generate IDs in the same millisecond.
        /// </remarks>
        public static long LongId()
        {
            long t = DateTime.UtcNow.Ticks / 10_000;

            if (t == last) seq++; else { seq = 0; last = t; }

            while (seq >= 4096)
            {
                t = DateTime.UtcNow.Ticks / 10_000;
                if (t != last) { seq = 0; last = t; break; }
            }

            return (t << 22) | ((long)DevId << 12) | (uint)seq;
        }

        /// <summary>
        /// Calculates a 32-bit Fowler–Noll–Vo (FNV-1a) hash of the input string.
        /// </summary>
        /// <param name="s">The string to hash.</param>
        /// <returns>A 32-bit integer hash.</returns>
        public static int Fnv(string s)
        {
            unchecked
            {
                int h = -2128831035;
                foreach (var c in s)
                    h = (h ^ c) * 16777619;
                return h;
            }
        }
    }
}
