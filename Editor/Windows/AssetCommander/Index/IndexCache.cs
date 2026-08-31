using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Binary snapshot of the parsed index under Library/. Every path and version string is
    // passed in rather than read from Application.*, because load runs on a worker.
    public static class IndexCache
    {
        private const int Magic = 0x43414B44; // "DKAC"
        private const int FormatVersion = 1;

        public static Dictionary<string, AssetRecord> TryLoad(string filePath, string unityVersion)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                           1 << 16, FileOptions.SequentialScan))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadInt32() != Magic) return null;
                    if (reader.ReadInt32() != FormatVersion) return null;

                    // A minor editor upgrade can change how assets serialise, so a stamp
                    // mismatch is a full rebuild rather than a partial reconcile.
                    if (reader.ReadString() != unityVersion) return null;

                    int guidCount = reader.ReadInt32();
                    if (guidCount < 0) return null;

                    var guids = new string[guidCount];
                    for (int i = 0; i < guidCount; i++) guids[i] = reader.ReadString();

                    int recordCount = reader.ReadInt32();
                    if (recordCount < 0) return null;

                    var records = new Dictionary<string, AssetRecord>(recordCount, StringComparer.Ordinal);
                    for (int i = 0; i < recordCount; i++)
                    {
                        var record = new AssetRecord
                        {
                            Guid = guids[reader.ReadInt32()],
                            Path = reader.ReadString(),
                            Kind = (AssetKind)reader.ReadByte(),
                            Size = reader.ReadInt64(),
                            ContentHash = reader.ReadUInt64(),
                            LastWriteTicks = reader.ReadInt64(),
                            MetaWriteTicks = reader.ReadInt64(),
                            DependencyGuids = ReadGuidArray(reader, guids),
                            ScriptGuids = ReadGuidArray(reader, guids),
                        };

                        records[record.Guid] = record;
                    }

                    return records;
                }
            }
            // A truncated or hand-mangled cache must degrade to a rebuild, never brick the
            // window; every failure mode here means the same thing.
            // IOException covers the truncated-file case via EndOfStreamException.
            catch (IOException)
            {
                return null;
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static bool Save(string filePath, string unityVersion, ICollection<AssetRecord> records)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                var guidIndices = new Dictionary<string, int>(records.Count * 2, StringComparer.Ordinal);
                var guidTable = new List<string>(records.Count * 2);

                foreach (var record in records)
                {
                    Intern(record.Guid, guidIndices, guidTable);
                    foreach (var dependency in record.DependencyGuids) Intern(dependency, guidIndices, guidTable);
                    foreach (var script in record.ScriptGuids) Intern(script, guidIndices, guidTable);
                }

                // Written beside the target and moved into place, so a crash mid-write leaves
                // the previous cache intact instead of a truncated one.
                var temporary = filePath + ".tmp";

                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                           1 << 16, FileOptions.SequentialScan))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write(Magic);
                    writer.Write(FormatVersion);
                    writer.Write(unityVersion);

                    writer.Write(guidTable.Count);
                    foreach (var guid in guidTable) writer.Write(guid);

                    writer.Write(records.Count);
                    foreach (var record in records)
                    {
                        writer.Write(guidIndices[record.Guid]);
                        writer.Write(record.Path ?? "");
                        writer.Write((byte)record.Kind);
                        writer.Write(record.Size);
                        writer.Write(record.ContentHash);
                        writer.Write(record.LastWriteTicks);
                        writer.Write(record.MetaWriteTicks);
                        WriteGuidArray(writer, record.DependencyGuids, guidIndices);
                        WriteGuidArray(writer, record.ScriptGuids, guidIndices);
                    }
                }

                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(temporary, filePath);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public static void Delete(string filePath)
        {
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (IOException)
            {
                // Nothing to do: the next save overwrites it anyway.
            }
        }

        private static void Intern(string guid, Dictionary<string, int> indices, List<string> table)
        {
            if (string.IsNullOrEmpty(guid) || indices.ContainsKey(guid)) return;

            indices.Add(guid, table.Count);
            table.Add(guid);
        }

        private static void WriteGuidArray(BinaryWriter writer, string[] guids, Dictionary<string, int> indices)
        {
            writer.Write(guids.Length);
            foreach (var guid in guids) writer.Write(indices[guid]);
        }

        private static string[] ReadGuidArray(BinaryReader reader, string[] guids)
        {
            int count = reader.ReadInt32();
            if (count <= 0) return AssetRecord.NoGuids;

            var result = new string[count];
            for (int i = 0; i < count; i++) result[i] = guids[reader.ReadInt32()];

            return result;
        }
    }
}
