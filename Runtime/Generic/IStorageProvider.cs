using System.Text;
using System.Threading.Tasks;

namespace DataKeeper.Generic
{
    /// <summary>
    /// File/blob storage backend used by <see cref="DataFile{T}"/> and <see cref="SaveManager"/>.
    /// Works with relative keys ("player.json", "slots/slot1/player.json") instead of absolute paths,
    /// so backends with flat namespaces (Steam Remote Storage, Unity Cloud Save, ...) can be plugged in.
    /// Assign a custom implementation to <see cref="DataKeeperStorage.Files"/> (global default) or to an
    /// individual <see cref="DataFile{T}.Storage"/>; the default is <see cref="LocalFileStorage"/>.
    /// </summary>
    /// <remarks>
    /// Implementation notes for custom (cloud) providers:
    /// <list type="bullet">
    /// <item>Keys use '/' as separator. Backends without real folders can treat keys as opaque names
    /// and derive <see cref="ListDirectories"/> / <see cref="DeleteAll"/> from key prefixes.</item>
    /// <item><see cref="ReadBytes"/> returns null (does not throw) when the key is missing.</item>
    /// <item><see cref="WriteBytes"/> creates any missing parent "folders" itself.</item>
    /// <item>For async-only APIs (e.g. Unity Cloud Save) implement the async members and throw
    /// <see cref="System.NotSupportedException"/> from the sync ones — callers must then use the
    /// async save/load variants (<see cref="SaveManager.SaveAllAsync"/> / <see cref="SaveManager.LoadAllAsync"/>).</item>
    /// </list>
    /// </remarks>
    public interface IStorageProvider
    {
        bool Exists(string key);

        /// <summary>Returns the stored bytes, or null when the key does not exist.</summary>
        byte[] ReadBytes(string key);

        void WriteBytes(string key, byte[] data);

        void Delete(string key);

        /// <summary>First-level "directory" names under a prefix, e.g. <c>ListDirectories("slots")</c> returns slot names.</summary>
        string[] ListDirectories(string prefix);

        /// <summary>Deletes every key under the prefix (used for slot deletion).</summary>
        void DeleteAll(string prefix);

        Task<byte[]> ReadBytesAsync(string key);

        Task WriteBytesAsync(string key, byte[] data);
    }

    /// <summary>UTF-8 text convenience on top of the byte-oriented <see cref="IStorageProvider"/>.</summary>
    public static class StorageProviderExtensions
    {
        public static string ReadText(this IStorageProvider storage, string key)
        {
            byte[] bytes = storage.ReadBytes(key);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }

        public static void WriteText(this IStorageProvider storage, string key, string text)
        {
            storage.WriteBytes(key, Encoding.UTF8.GetBytes(text));
        }

        public static async Task<string> ReadTextAsync(this IStorageProvider storage, string key)
        {
            byte[] bytes = await storage.ReadBytesAsync(key);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }

        public static Task WriteTextAsync(this IStorageProvider storage, string key, string text)
        {
            return storage.WriteBytesAsync(key, Encoding.UTF8.GetBytes(text));
        }
    }
}
