using UnityEngine;

namespace DataKeeper.Generic
{
    /// <summary>
    /// Global storage defaults for the persistence primitives — the counterpart of <see cref="DataKeeperJson"/>.
    /// <see cref="DataFile{T}"/> and <see cref="ReactivePref{T}"/> fall back to these when no per-instance
    /// provider is given; <see cref="SaveManager"/> uses <see cref="Files"/> for slots and version meta.
    /// Swap the providers here (before the first save/load) to redirect everything to a cloud backend.
    /// </summary>
    public static class DataKeeperStorage
    {
        public const string SlotsFolderName = "slots";

        /// <summary>File/blob backend. Default: <see cref="LocalFileStorage"/> (persistentDataPath).</summary>
        public static IStorageProvider Files { get; set; } = new LocalFileStorage();

        /// <summary>Key-value backend for prefs. Default: <see cref="PlayerPrefsStorage"/> (Unity PlayerPrefs).</summary>
        public static IPrefsStorage Prefs { get; set; } = new PlayerPrefsStorage();

        /// <summary>
        /// Active save slot used by slot-scoped <see cref="DataFile{T}"/>s; empty = no slot.
        /// Change it via <see cref="SaveManager.SetSlot"/> so <see cref="SaveManager.OnSlotChanged"/> fires.
        /// </summary>
        public static string CurrentSlot { get; set; } = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Files = new LocalFileStorage();
            Prefs = new PlayerPrefsStorage();
            CurrentSlot = string.Empty;
        }
    }
}
