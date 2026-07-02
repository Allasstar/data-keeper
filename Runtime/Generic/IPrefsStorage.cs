using UnityEngine;

namespace DataKeeper.Generic
{
    /// <summary>
    /// Key-value backend for <see cref="ReactivePref{T}"/>. Assign a custom implementation to
    /// <see cref="DataKeeperStorage.Prefs"/> (global default) or to an individual
    /// <see cref="ReactivePref{T}.Storage"/> to store prefs somewhere other than Unity PlayerPrefs
    /// (e.g. a cloud save service, alongside a custom <see cref="IStorageProvider"/>).
    /// Default is <see cref="PlayerPrefsStorage"/>.
    /// </summary>
    public interface IPrefsStorage
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        float GetFloat(string key, float defaultValue);
        void SetFloat(string key, float value);
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
        bool HasKey(string key);
        void DeleteKey(string key);

        /// <summary>Flushes pending writes to the backend (<see cref="PlayerPrefs.Save"/> equivalent).</summary>
        void Save();
    }

    /// <summary>Default <see cref="IPrefsStorage"/> backed by Unity <see cref="PlayerPrefs"/>.</summary>
    public sealed class PlayerPrefsStorage : IPrefsStorage
    {
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public float GetFloat(string key, float defaultValue) => PlayerPrefs.GetFloat(key, defaultValue);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}
