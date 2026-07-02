using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DataKeeper.Generic
{
    /// <summary>
    /// Default <see cref="IStorageProvider"/>: plain files under <see cref="Application.persistentDataPath"/>.
    /// Serves as the reference implementation for custom (cloud) providers.
    /// </summary>
    public sealed class LocalFileStorage : IStorageProvider
    {
        // Cached because Application.persistentDataPath is main-thread-only.
        private readonly string _root = Application.persistentDataPath;

        private string FullPath(string key) => $"{_root}/{key}";

        public bool Exists(string key) => File.Exists(FullPath(key));

        public byte[] ReadBytes(string key)
        {
            string path = FullPath(key);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void WriteBytes(string key, byte[] data)
        {
            string path = FullPath(key);
            EnsureFolder(path);
            File.WriteAllBytes(path, data);
        }

        public void Delete(string key)
        {
            string path = FullPath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public string[] ListDirectories(string prefix)
        {
            string root = FullPath(prefix);
            if (!Directory.Exists(root)) return Array.Empty<string>();

            string[] dirs = Directory.GetDirectories(root);
            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = Path.GetFileName(dirs[i]);
            }
            return dirs;
        }

        public void DeleteAll(string prefix)
        {
            string path = FullPath(prefix);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        public async Task<byte[]> ReadBytesAsync(string key)
        {
            string path = FullPath(key);
            if (!File.Exists(path)) return null;

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                byte[] buffer = new byte[stream.Length];
                int read = 0;
                while (read < buffer.Length)
                {
                    read += await stream.ReadAsync(buffer, read, buffer.Length - read);
                }
                return buffer;
            }
        }

        public async Task WriteBytesAsync(string key, byte[] data)
        {
            string path = FullPath(key);
            EnsureFolder(path);

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        private static void EnsureFolder(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
