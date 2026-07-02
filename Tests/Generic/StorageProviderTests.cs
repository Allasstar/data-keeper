using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataKeeper.Generic;
using NUnit.Framework;

namespace DataKeeper.Tests.Generic
{
    /// <summary>
    /// Verifies the whole pipeline runs against a custom <see cref="IStorageProvider"/> /
    /// <see cref="IPrefsStorage"/>. The in-memory providers below double as a minimal
    /// example of what a cloud implementation (Steam, Unity Cloud Save, ...) looks like.
    /// </summary>
    public class StorageProviderTests
    {
        [Serializable]
        private class Progress
        {
            public int level;
        }

        private class InMemoryStorage : IStorageProvider
        {
            public readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

            public bool Exists(string key) => Files.ContainsKey(key);

            public byte[] ReadBytes(string key) => Files.TryGetValue(key, out byte[] data) ? data : null;

            public void WriteBytes(string key, byte[] data) => Files[key] = data;

            public void Delete(string key) => Files.Remove(key);

            public string[] ListDirectories(string prefix)
            {
                var names = new HashSet<string>();
                string start = prefix + "/";
                foreach (string key in Files.Keys)
                {
                    if (!key.StartsWith(start)) continue;
                    int end = key.IndexOf('/', start.Length);
                    if (end > start.Length) names.Add(key.Substring(start.Length, end - start.Length));
                }
                var result = new string[names.Count];
                names.CopyTo(result);
                return result;
            }

            public void DeleteAll(string prefix)
            {
                var toRemove = new List<string>();
                foreach (string key in Files.Keys)
                {
                    if (key.StartsWith(prefix + "/")) toRemove.Add(key);
                }
                foreach (string key in toRemove) Files.Remove(key);
            }

            public Task<byte[]> ReadBytesAsync(string key) => Task.FromResult(ReadBytes(key));

            public Task WriteBytesAsync(string key, byte[] data)
            {
                WriteBytes(key, data);
                return Task.CompletedTask;
            }
        }

        private class InMemoryPrefs : IPrefsStorage
        {
            public readonly Dictionary<string, object> Values = new Dictionary<string, object>();
            public int SaveCalls;

            public int GetInt(string key, int defaultValue) => Values.TryGetValue(key, out object v) ? (int)v : defaultValue;
            public void SetInt(string key, int value) => Values[key] = value;
            public float GetFloat(string key, float defaultValue) => Values.TryGetValue(key, out object v) ? (float)v : defaultValue;
            public void SetFloat(string key, float value) => Values[key] = value;
            public string GetString(string key, string defaultValue) => Values.TryGetValue(key, out object v) ? (string)v : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public bool HasKey(string key) => Values.ContainsKey(key);
            public void DeleteKey(string key) => Values.Remove(key);
            public void Save() => SaveCalls++;
        }

        private InMemoryStorage _storage;
        private InMemoryPrefs _prefs;

        [SetUp]
        public void SetUp()
        {
            SaveManager.UnregisterAll();
            SaveManager.ClearMigrations();
            SaveManager.SetSlot(string.Empty);
            SaveManager.DataVersion = 1;

            _storage = new InMemoryStorage();
            _prefs = new InMemoryPrefs();
            DataKeeperStorage.Files = _storage;
            DataKeeperStorage.Prefs = _prefs;
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.UnregisterAll();
            SaveManager.ClearMigrations();
            SaveManager.SetSlot(string.Empty);
            SaveManager.DataVersion = 1;
            DataKeeperStorage.Files = new LocalFileStorage();
            DataKeeperStorage.Prefs = new PlayerPrefsStorage();
        }

        [Test]
        public void DataFile_SavesAndLoads_ThroughCustomStorage()
        {
            var file = new DataFile<Progress>("mem_progress.json", SerializationType.Json, new Progress { level = 3 });
            SaveManager.Register(file);

            SaveManager.SaveAll();

            Assert.IsTrue(_storage.Exists("mem_progress.json"));

            file.Data = new Progress { level = 0 };
            SaveManager.LoadAll();

            Assert.AreEqual(3, file.Data.level);
        }

        [Test]
        public void Slots_WorkThroughCustomStorage()
        {
            var file = new DataFile<Progress>("mem_progress.json", SerializationType.Json, new Progress(), SaveScope.Slot);
            SaveManager.Register(file);

            SaveManager.SetSlot("mem_slot_a");
            file.Data.level = 1;
            SaveManager.SaveAll();

            SaveManager.SetSlot("mem_slot_b");
            file.Data = new Progress { level = 42 };
            SaveManager.SaveAll();

            CollectionAssert.AreEquivalent(new[] { "mem_slot_a", "mem_slot_b" }, SaveManager.GetSlots());
            Assert.IsTrue(SaveManager.SlotExists("mem_slot_a"));

            SaveManager.SetSlot("mem_slot_a");
            SaveManager.LoadAll();
            Assert.AreEqual(1, file.Data.level);

            SaveManager.DeleteSlot("mem_slot_b");
            Assert.IsFalse(SaveManager.SlotExists("mem_slot_b"));
        }

        [Test]
        public void VersionMeta_IsStamped_InCustomStorage()
        {
            SaveManager.SetSlot("mem_slot_a");
            SaveManager.DataVersion = 5;
            SaveManager.SaveAll();

            Assert.AreEqual(5, SaveManager.GetSavedVersion("mem_slot_a"));
            Assert.IsTrue(_storage.Exists("slots/mem_slot_a/save_meta.json"));
        }

        [Test]
        public void DataFile_PerInstanceStorage_OverridesGlobalDefault()
        {
            var ownStorage = new InMemoryStorage();
            var file = new DataFile<Progress>("mem_progress.json", SerializationType.Json, new Progress { level = 9 }, ownStorage);

            file.SaveData();

            Assert.IsTrue(ownStorage.Exists("mem_progress.json"));
            Assert.IsFalse(_storage.Exists("mem_progress.json")); // global default untouched
        }

        [Test]
        public void ReactivePref_PerInstanceStorage_OverridesGlobalDefault()
        {
            var ownPrefs = new InMemoryPrefs();
            var pref = new ReactivePref<int>(0, "mem_pref_own", autoSave: true, storage: ownPrefs);

            pref.Value = 7;

            Assert.AreEqual(7, ownPrefs.GetInt("mem_pref_own", 0));
            Assert.IsFalse(_prefs.HasKey("mem_pref_own")); // global default untouched
        }

        [Test]
        public void ReactivePref_UsesCustomPrefsStorage()
        {
            var pref = new ReactivePref<int>(10, "mem_pref_int", autoSave: false);
            SaveManager.Register(pref);

            pref.Value = 25;
            SaveManager.SaveAll();

            Assert.AreEqual(25, _prefs.GetInt("mem_pref_int", 0));
            Assert.Greater(_prefs.SaveCalls, 0);

            pref.SilentChange(0);
            SaveManager.LoadAll();

            Assert.AreEqual(25, pref.Value);
        }
    }
}
