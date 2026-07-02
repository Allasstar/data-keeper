using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace DataKeeper.Generic
{
    [Serializable]
    public class DataFile<T> : IDataFile
    {
        [SerializeField] 
        private T data;
        
        public T Data { get => data; set => data = value; }

        public SaveScope Scope { get; } = SaveScope.Global;

        private readonly string _fileName;
        private string _key = null;
        private string _builtForSlot = null;
        private readonly SerializationType _serializationType;
        private IStorageProvider _storage;

        /// <summary>
        /// Storage backend of this file. Defaults to the global <see cref="DataKeeperStorage.Files"/>;
        /// assign (or pass to the constructor) a provider to override it per file.
        /// </summary>
        public IStorageProvider Storage
        {
            get => _storage ?? DataKeeperStorage.Files;
            set => _storage = value;
        }

        public DataFile(string fileName, SerializationType serializationType, IStorageProvider storage = null)
        {
            _fileName = fileName;
            _serializationType = serializationType;
            _storage = storage;
            data = default(T);
        }

        public DataFile(string fileName, SerializationType serializationType, T defaultValue, IStorageProvider storage = null) : this(fileName, serializationType, storage)
        {
            Data = defaultValue;
        }

        public DataFile(string fileName, SerializationType serializationType, SaveScope scope, IStorageProvider storage = null) : this(fileName, serializationType, storage)
        {
            Scope = scope;
        }

        public DataFile(string fileName, SerializationType serializationType, T defaultValue, SaveScope scope, IStorageProvider storage = null) : this(fileName, serializationType, defaultValue, storage)
        {
            Scope = scope;
        }

        private void BuildKey()
        {
            string slot = Scope == SaveScope.Slot ? DataKeeperStorage.CurrentSlot : string.Empty;

            if (string.IsNullOrEmpty(_key) || _builtForSlot != slot)
            {
                _builtForSlot = slot;
                _key = string.IsNullOrEmpty(slot)
                    ? _fileName
                    : $"{DataKeeperStorage.SlotsFolderName}/{slot}/{_fileName}";
            }
        }

        public bool IsFileExist()
        {
            BuildKey();
            return Storage.Exists(_key);
        }

        public void SaveData()
        {
            BuildKey();

            switch (_serializationType)
            {
                case SerializationType.Binary:
                    SaveDataBinary();
                    break;
                case SerializationType.Xml:
                    SaveDataXml();
                    break;
                case SerializationType.Json:
                    SaveDataJson();
                    break;
                default:
                    Debug.Log("Invalid serialization type.");
                    break;
            }
        }

        public void LoadData()
        {
            BuildKey();

            switch (_serializationType)
            {
                case SerializationType.Binary:
                    LoadDataBinary();
                    break;
                case SerializationType.Xml:
                    LoadDataXml();
                    break;
                case SerializationType.Json:
                    LoadDataJson();
                    break;
                default:
                    Debug.Log("Invalid serialization type.");
                    break;
            }
        }

        public async Task SaveDataAsync()
        {
            BuildKey();

            switch (_serializationType)
            {
                case SerializationType.Binary:
                    await SaveDataBinaryAsync();
                    break;
                case SerializationType.Xml:
                    await SaveDataXmlAsync();
                    break;
                case SerializationType.Json:
                    await SaveDataJsonAsync();
                    break;
                default:
                    Debug.Log("Invalid serialization type.");
                    break;
            }
        }

        public async Task LoadDataAsync()
        {
            BuildKey();

            switch (_serializationType)
            {
                case SerializationType.Binary:
                    await LoadDataBinaryAsync();
                    break;
                case SerializationType.Xml:
                    await LoadDataXmlAsync();
                    break;
                case SerializationType.Json:
                    await LoadDataJsonAsync();
                    break;
                default:
                    Debug.Log("Invalid serialization type.");
                    break;
            }
        }

        private byte[] SerializeBinary()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, Data);
                return stream.ToArray();
            }
        }

        private T DeserializeBinary(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        private string SerializeXml()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, Data);
                return writer.ToString();
            }
        }

        private T DeserializeXml(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        private void SaveDataBinary()
        {
            try
            {
                Storage.WriteBytes(_key, SerializeBinary());
                Debug.Log("Binary data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving binary data: " + ex.Message);
            }
        }

        private void LoadDataBinary()
        {
            try
            {
                byte[] bytes = Storage.ReadBytes(_key);
                if (bytes == null) throw new FileNotFoundException("Key not found in storage.", _key);

                Data = DeserializeBinary(bytes);
                Debug.Log("Binary data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading binary data: " + ex.Message);
            }
        }

        private void SaveDataXml()
        {
            try
            {
                Storage.WriteText(_key, SerializeXml());
                Debug.Log("XML data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving XML data: " + ex.Message + "Data: " + ex.Data);
            }
        }

        private void LoadDataXml()
        {
            try
            {
                string xml = Storage.ReadText(_key);
                if (xml == null) throw new FileNotFoundException("Key not found in storage.", _key);

                Data = DeserializeXml(xml);
                Debug.Log("XML data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading XML data: " + ex.Message);
            }
        }

        private void SaveDataJson()
        {
            try
            {
                Storage.WriteText(_key, JsonConvert.SerializeObject(Data, DataKeeperJson.Settings));
                Debug.Log("JSON data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving JSON data: " + ex.Message);
            }
        }

        private void LoadDataJson()
        {
            try
            {
                string jsonData = Storage.ReadText(_key);
                if (jsonData == null) throw new FileNotFoundException("Key not found in storage.", _key);

                Data = JsonConvert.DeserializeObject<T>(jsonData, DataKeeperJson.Settings);
                Debug.Log("JSON data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading JSON data: " + ex.Message);
            }
        }

        private async Task SaveDataBinaryAsync()
        {
            try
            {
                byte[] bytes = await Task.Run(SerializeBinary);
                await Storage.WriteBytesAsync(_key, bytes);
                Debug.Log("Binary data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving binary data: " + ex.Message);
            }
        }

        private async Task LoadDataBinaryAsync()
        {
            try
            {
                byte[] bytes = await Storage.ReadBytesAsync(_key);
                if (bytes == null) throw new FileNotFoundException("Key not found in storage.", _key);

                Data = await Task.Run(() => DeserializeBinary(bytes));
                Debug.Log("Binary data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading binary data: " + ex.Message);
            }
        }

        private async Task SaveDataXmlAsync()
        {
            try
            {
                string xml = await Task.Run(SerializeXml);
                await Storage.WriteTextAsync(_key, xml);
                Debug.Log("XML data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving XML data: " + ex.Message);
            }
        }

        private async Task LoadDataXmlAsync()
        {
            try
            {
                string xml = await Storage.ReadTextAsync(_key);
                if (xml == null) throw new FileNotFoundException("Key not found in storage.", _key);

                Data = await Task.Run(() => DeserializeXml(xml));
                Debug.Log("XML data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading XML data: " + ex.Message);
            }
        }

        private async Task SaveDataJsonAsync()
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(Data, DataKeeperJson.Settings);
                await Storage.WriteTextAsync(_key, jsonData);
                Debug.Log("JSON data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving JSON data: " + ex.Message);
            }
        }

        private async Task LoadDataJsonAsync()
        {
            try
            {
                string jsonData = await Storage.ReadTextAsync(_key);
                if (jsonData == null) throw new FileNotFoundException("Key not found in storage.", _key);

                Data = JsonConvert.DeserializeObject<T>(jsonData, DataKeeperJson.Settings);
                Debug.Log("JSON data loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading JSON data: " + ex.Message);
            }
        }
    }
    
    public enum SerializationType
    {
        Binary = 0,
        Xml = 1,
        Json = 2
    }

    public enum SaveScope
    {
        /// <summary>One shared file, unaffected by the active save slot (settings, unlocks).</summary>
        Global = 0,
        /// <summary>Stored per save slot under <c>slots/{slot}/</c> when <see cref="SaveManager.CurrentSlot"/> is set.</summary>
        Slot = 1
    }

    public interface IDataFile
    {
        public SaveScope Scope { get; }
        public bool IsFileExist();
        public void SaveData();
        public void LoadData();
        public Task SaveDataAsync();
        public Task LoadDataAsync();
    }
}