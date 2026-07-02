using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataKeeper.Signals;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    /// <summary>
    /// Central save pipeline: registers <see cref="IDataFile"/>s and <see cref="IReactivePref"/>s,
    /// saves/loads them together, manages save slots (<c>slots/{slot}/</c> for <see cref="SaveScope.Slot"/> files),
    /// and versions save data with ordered migrations.
    /// </summary>
    public static class SaveManager
    {
        public const string SlotsFolderName = DataKeeperStorage.SlotsFolderName;
        private const string MetaFileName = "save_meta.json";

        /// <summary>Current version of the save format. Bump it together with <see cref="RegisterMigration"/>.</summary>
        public static int DataVersion { get; set; } = 1;

        /// <summary>Active save slot (stored in <see cref="DataKeeperStorage"/>). Empty = no slot (files live at the storage root).</summary>
        public static string CurrentSlot
        {
            get => DataKeeperStorage.CurrentSlot;
            private set => DataKeeperStorage.CurrentSlot = value;
        }

        /// <summary>When enabled, <see cref="SaveAll"/> is called on <c>Application.quitting</c>.</summary>
        public static bool AutoSaveOnQuit { get; set; } = false;

        public static readonly Signal OnBeforeSave = new Signal();
        public static readonly Signal OnAfterSave = new Signal();
        public static readonly Signal OnBeforeLoad = new Signal();
        public static readonly Signal OnAfterLoad = new Signal();
        public static readonly Signal<string> OnSlotChanged = new Signal<string>();

        private static readonly List<IDataFile> _files = new List<IDataFile>();
        private static readonly List<IReactivePref> _prefs = new List<IReactivePref>();
        private static readonly SortedList<int, Action<string>> _migrations = new SortedList<int, Action<string>>();

        [Serializable]
        private class SaveMeta
        {
            public int version;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _files.Clear();
            _prefs.Clear();

            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            if (AutoSaveOnQuit) SaveAll();
        }

        // --- Registration ---

        public static void Register(IDataFile file)
        {
            if (file != null && !_files.Contains(file)) _files.Add(file);
        }

        public static void Unregister(IDataFile file) => _files.Remove(file);

        public static void Register(IReactivePref pref)
        {
            if (pref != null && !_prefs.Contains(pref)) _prefs.Add(pref);
        }

        public static void Unregister(IReactivePref pref) => _prefs.Remove(pref);

        public static void UnregisterAll()
        {
            _files.Clear();
            _prefs.Clear();
        }

        // --- Slots ---

        /// <summary>Switches the active slot. Does not save or load — call <see cref="SaveAll"/> before and <see cref="LoadAll"/> after as needed.</summary>
        public static void SetSlot(string slot)
        {
            slot ??= string.Empty;
            if (slot == CurrentSlot) return;

            CurrentSlot = slot;
            OnSlotChanged.Invoke(slot);
        }

        public static bool SlotExists(string slot) =>
            !string.IsNullOrEmpty(slot) && Array.IndexOf(GetSlots(), slot) >= 0;

        public static string[] GetSlots() => DataKeeperStorage.Files.ListDirectories(SlotsFolderName);

        public static void DeleteSlot(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return;

            DataKeeperStorage.Files.DeleteAll(SlotKeyPrefix(slot));
        }

        /// <summary>Key prefix of a slot ("slots/{slot}"), empty when no slot.</summary>
        private static string SlotKeyPrefix(string slot) => string.IsNullOrEmpty(slot)
            ? string.Empty
            : $"{SlotsFolderName}/{slot}";

        private static string MetaKey(string slot) => string.IsNullOrEmpty(slot)
            ? MetaFileName
            : $"{SlotKeyPrefix(slot)}/{MetaFileName}";

        // --- Versioning & migration ---

        /// <summary>
        /// Registers a migration that upgrades save data *to* <paramref name="toVersion"/>.
        /// On <see cref="LoadAll"/>, migrations with saved version &lt; toVersion &lt;= <see cref="DataVersion"/>
        /// run in ascending order, receiving the active slot's key prefix ("slots/{slot}", empty when no slot) —
        /// read/write keys through <see cref="DataKeeperStorage.Files"/>. Register before the first load.
        /// </summary>
        public static void RegisterMigration(int toVersion, Action<string> migration)
        {
            _migrations[toVersion] = migration;
        }

        public static void ClearMigrations() => _migrations.Clear();

        /// <summary>Version stamped in the slot's meta file; 0 when no meta exists (pre-versioning or fresh).</summary>
        public static int GetSavedVersion(string slot = null)
        {
            try
            {
                string json = DataKeeperStorage.Files.ReadText(MetaKey(slot ?? CurrentSlot));
                if (json == null) return 0;

                var meta = JsonConvert.DeserializeObject<SaveMeta>(json, DataKeeperJson.Settings);
                return meta?.version ?? 0;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading save meta: " + ex.Message);
                return 0;
            }
        }

        private static void RunMigrations()
        {
            if (_migrations.Count == 0) return;

            bool hasMeta = DataKeeperStorage.Files.Exists(MetaKey(CurrentSlot));
            int saved = hasMeta ? GetSavedVersion(CurrentSlot) : 0;
            if (saved >= DataVersion) return;

            // Fresh install (no meta, no data in storage) — nothing to migrate.
            if (!hasMeta && !AnyRegisteredFileExists()) return;

            string keyPrefix = SlotKeyPrefix(CurrentSlot);
            for (int i = 0; i < _migrations.Count; i++)
            {
                int toVersion = _migrations.Keys[i];
                if (toVersion > saved && toVersion <= DataVersion)
                {
                    _migrations.Values[i].Invoke(keyPrefix);
                }
            }

            WriteMeta();
        }

        private static bool AnyRegisteredFileExists()
        {
            for (int i = 0; i < _files.Count; i++)
            {
                if (_files[i].IsFileExist()) return true;
            }
            return false;
        }

        private static void WriteMeta()
        {
            try
            {
                DataKeeperStorage.Files.WriteText(MetaKey(CurrentSlot), JsonConvert.SerializeObject(new SaveMeta { version = DataVersion }, DataKeeperJson.Settings));
            }
            catch (Exception ex)
            {
                Debug.LogError("Error writing save meta: " + ex.Message);
            }
        }

        // --- Save / Load ---

        public static void SaveAll()
        {
            OnBeforeSave.Invoke();

            for (int i = 0; i < _files.Count; i++)
            {
                _files[i].SaveData();
            }
            for (int i = 0; i < _prefs.Count; i++)
            {
                _prefs[i].Save();
            }

            WriteMeta();
            OnAfterSave.Invoke();
        }

        public static void LoadAll()
        {
            OnBeforeLoad.Invoke();
            RunMigrations();

            for (int i = 0; i < _files.Count; i++)
            {
                if (_files[i].IsFileExist()) _files[i].LoadData();
            }
            for (int i = 0; i < _prefs.Count; i++)
            {
                _prefs[i].Load();
            }

            OnAfterLoad.Invoke();
        }

        public static async Task SaveAllAsync()
        {
            OnBeforeSave.Invoke();

            for (int i = 0; i < _files.Count; i++)
            {
                await _files[i].SaveDataAsync();
            }
            for (int i = 0; i < _prefs.Count; i++)
            {
                _prefs[i].Save();
            }

            WriteMeta();
            OnAfterSave.Invoke();
        }

        public static async Task LoadAllAsync()
        {
            OnBeforeLoad.Invoke();
            RunMigrations();

            for (int i = 0; i < _files.Count; i++)
            {
                if (_files[i].IsFileExist()) await _files[i].LoadDataAsync();
            }
            for (int i = 0; i < _prefs.Count; i++)
            {
                _prefs[i].Load();
            }

            OnAfterLoad.Invoke();
        }
    }
}
