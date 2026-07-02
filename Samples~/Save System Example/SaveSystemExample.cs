using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;

namespace DataKeeper.Examples
{
    /// <summary>
    /// Complete save-system walkthrough in one script: data files, reactive prefs,
    /// save slots, versioning and swappable storage providers (local / in-memory / your own cloud).
    /// Drop it on a GameObject and use the inspector buttons.
    /// </summary>
    public class SaveSystemExample : MonoBehaviour
    {
        // =====================================================================
        // 1. DATA — what gets saved
        // =====================================================================

        // Any [Serializable] class works as file payload.
        [Serializable]
        public class PlayerData
        {
            public int level = 1;
            public int coins;
            public Vector3 position;
        }

        // DataFile<T> persists one value to one file. Pick a serializer per file:
        //   Json   — recommended default (Unity structs like Vector3 just work)
        //   Xml    — needs public parameterless ctor + public members
        //   Binary — compact but type-brittle; avoid for untrusted data
        //
        // SaveScope.Slot   -> file lives in slots/{slot}/ while a slot is active
        // SaveScope.Global -> file ignores slots (settings, unlocks, ...)
        public DataFile<PlayerData> Progress = new DataFile<PlayerData>("example_progress.json", SerializationType.Json, new PlayerData(), SaveScope.Slot);
        public DataFile<PlayerData> Settings = new DataFile<PlayerData>("example_settings.json", SerializationType.Json, new PlayerData());

        // ReactivePref<T> is a reactive key-value pref (int/float/string/bool/Vector/Color/...,
        // anything else falls back to JSON). autoSave: true writes on every Value change.
        public ReactivePref<float> MusicVolume = new ReactivePref<float>(1f, "example_music_volume", autoSave: false);

        // =====================================================================
        // 2. SETUP — choose a strategy, register, load
        // =====================================================================

        private void Start()
        {
            // --- Storage strategy -------------------------------------------
            // All files go through DataKeeperStorage.Files (IStorageProvider),
            // all prefs through DataKeeperStorage.Prefs (IPrefsStorage).
            // The lines below assign the DEFAULTS — skip them entirely to get the same result.

            DataKeeperStorage.Files = new LocalFileStorage();   // files under Application.persistentDataPath
            DataKeeperStorage.Prefs = new PlayerPrefsStorage(); // Unity PlayerPrefs

            // Any implementation can be plugged instead (see the bottom of this file):
            // DataKeeperStorage.Files = new ExampleInMemoryStorage(); // volatile, great for tests
            // DataKeeperStorage.Files = new MySteamCloudStorage();    // your Steam Remote Storage wrapper
            // DataKeeperStorage.Prefs = new MyCloudPrefs();

            // A single file/pref can also override the global default:
            // var cloudFile = new DataFile<PlayerData>("p.json", SerializationType.Json, new MySteamCloudStorage());
            // var localPref = new ReactivePref<int>(0, "fps_cap", autoSave: true, storage: new PlayerPrefsStorage());

            // --- Versioning & migrations (optional) -------------------------
            // Bump DataVersion when the save format changes and register one step per version.
            // Migrations run inside LoadAll(), oldest first, receiving the slot key prefix.
            SaveManager.DataVersion = 1;
            // SaveManager.RegisterMigration(2, keyPrefix =>
            // {
            //     var storage = DataKeeperStorage.Files;
            //     string old = $"{keyPrefix}/old_name.json";
            //     if (storage.Exists(old))
            //     {
            //         storage.WriteBytes($"{keyPrefix}/example_progress.json", storage.ReadBytes(old));
            //         storage.Delete(old);
            //     }
            // });

            // --- Register everything once -----------------------------------
            SaveManager.Register(Progress);
            SaveManager.Register(Settings);
            SaveManager.Register(MusicVolume);
            SaveManager.AutoSaveOnQuit = true;

            // Optional events, e.g. for systems without their own IDataFile:
            SaveManager.OnAfterLoad.AddListener(() => Debug.Log("[Save] OnAfterLoad fired"));

            // --- Load --------------------------------------------------------
            // Optional: activate a slot first; slot-scoped files then read slots/{slot}/.
            // SaveManager.SetSlot("slot_1");

            SaveManager.LoadAll();
            LogState();
        }

        // =====================================================================
        // 3. SAVE / LOAD — one call for everything registered
        // =====================================================================

        [Button]
        public void ChangeData()
        {
            Progress.Data.level++;
            Progress.Data.coins += 10;
            Progress.Data.position = UnityEngine.Random.insideUnitSphere * 5f;
            MusicVolume.Value = UnityEngine.Random.value;
            LogState();
        }

        [Button]
        public void SaveAll()
        {
            SaveManager.SaveAll();
            Debug.Log($"[Save] Saved via {DataKeeperStorage.Files.GetType().Name} (slot: '{SaveManager.CurrentSlot}')");
        }

        [Button]
        public void LoadAll()
        {
            SaveManager.LoadAll();
            LogState();
        }

        // Async variants await each file's SaveDataAsync/LoadDataAsync — required
        // for async-only cloud backends (their sync methods throw NotSupportedException).
        [Button]
        public async void SaveAllAsync()
        {
            await SaveManager.SaveAllAsync();
            Debug.Log("[Save] Async save done");
        }

        [Button]
        public async void LoadAllAsync()
        {
            await SaveManager.LoadAllAsync();
            LogState();
        }

        // =====================================================================
        // 4. SLOTS — independent save games
        // =====================================================================

        [SerializeField] private string slotName = "slot_1";

        [Button]
        public void SwitchSlot()
        {
            SaveManager.SaveAll();              // flush the old slot
            SaveManager.SetSlot(slotName);      // switch (fires OnSlotChanged; no IO by itself)
            SaveManager.LoadAll();              // read the new one
            LogState();
        }

        [Button]
        public void ClearSlot() => SaveManager.SetSlot(string.Empty);

        [Button]
        public void DeleteSlot() => SaveManager.DeleteSlot(slotName);

        [Button]
        public void LogSlots()
        {
            Debug.Log($"[Save] Slots: [{string.Join(", ", SaveManager.GetSlots())}], " +
                      $"current: '{SaveManager.CurrentSlot}', saved version: {SaveManager.GetSavedVersion()}");
        }

        // =====================================================================
        // 5. SWITCHING PROVIDERS AT RUNTIME
        // =====================================================================
        // Providers are independent stores: switching does NOT migrate data between them.

        [Button]
        public void UseLocalStorage()
        {
            DataKeeperStorage.Files = new LocalFileStorage();
            DataKeeperStorage.Prefs = new PlayerPrefsStorage();
            Debug.Log($"[Save] Local storage ({Application.persistentDataPath})");
        }

        [Button]
        public void UseInMemoryStorage()
        {
            DataKeeperStorage.Files = new ExampleInMemoryStorage();
            DataKeeperStorage.Prefs = new ExampleInMemoryPrefs();
            Debug.Log("[Save] In-memory storage (starts empty, lost on exit)");
        }

        [Button]
        public void DumpStorage()
        {
            if (DataKeeperStorage.Files is ExampleInMemoryStorage mem)
            {
                Debug.Log($"[Save] InMemory keys ({mem.Data.Count}): {string.Join(", ", mem.Data.Keys)}");
            }
            else
            {
                Debug.Log($"[Save] {DataKeeperStorage.Files.GetType().Name}: files under {Application.persistentDataPath}");
            }
        }

        private void LogState()
        {
            Debug.Log($"[Save] level {Progress.Data.level}, coins {Progress.Data.coins}, " +
                      $"volume {MusicVolume.Value:F2} (slot: '{SaveManager.CurrentSlot}')");
        }
    }

    // =====================================================================
    // 6. WRITING YOUR OWN PROVIDER
    // =====================================================================
    // A cloud provider (Steam Remote Storage, Unity Cloud Save, ...) has exactly
    // this shape — replace the dictionary with the backend's read/write/list/delete.
    // Contract: keys are relative ("slots/slot_1/file.json"), ReadBytes returns null
    // for missing keys, WriteBytes creates missing "folders" itself.

    public class ExampleInMemoryStorage : IStorageProvider
    {
        public readonly Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();

        public bool Exists(string key) => Data.ContainsKey(key);
        public byte[] ReadBytes(string key) => Data.TryGetValue(key, out byte[] bytes) ? bytes : null;
        public void WriteBytes(string key, byte[] data) => Data[key] = data;
        public void Delete(string key) => Data.Remove(key);

        // Flat namespace: "directories" are derived from key prefixes.
        public string[] ListDirectories(string prefix)
        {
            var names = new HashSet<string>();
            string start = prefix + "/";
            foreach (string key in Data.Keys)
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
            foreach (string key in Data.Keys)
            {
                if (key.StartsWith(prefix + "/")) toRemove.Add(key);
            }
            foreach (string key in toRemove) Data.Remove(key);
        }

        // This backend is synchronous, so async members just wrap the sync ones.
        // An async-only backend (Unity Cloud Save) does the opposite: implement these
        // with await and throw NotSupportedException from the sync members.
        public Task<byte[]> ReadBytesAsync(string key) => Task.FromResult(ReadBytes(key));

        public Task WriteBytesAsync(string key, byte[] data)
        {
            WriteBytes(key, data);
            return Task.CompletedTask;
        }
    }

    // Key-value counterpart for ReactivePref<T>; mirrors the PlayerPrefs API.
    public class ExampleInMemoryPrefs : IPrefsStorage
    {
        public readonly Dictionary<string, object> Data = new Dictionary<string, object>();

        public int GetInt(string key, int defaultValue) => Data.TryGetValue(key, out object v) ? (int)v : defaultValue;
        public void SetInt(string key, int value) => Data[key] = value;
        public float GetFloat(string key, float defaultValue) => Data.TryGetValue(key, out object v) ? (float)v : defaultValue;
        public void SetFloat(string key, float value) => Data[key] = value;
        public string GetString(string key, string defaultValue) => Data.TryGetValue(key, out object v) ? (string)v : defaultValue;
        public void SetString(string key, string value) => Data[key] = value;
        public bool HasKey(string key) => Data.ContainsKey(key);
        public void DeleteKey(string key) => Data.Remove(key);
        public void Save() { } // flush point (PlayerPrefs.Save equivalent) — nothing to do in memory
    }
}
