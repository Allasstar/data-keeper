using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using DataKeeper.Helpers;
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
        
        private readonly string _fileName;
        private string _filePath = null;
        private readonly SerializationType _serializationType;

        public DataFile(string fileName, SerializationType serializationType)
        {
            _fileName = fileName;
            _serializationType = serializationType;
            data = default(T);
        }

        public DataFile(string fileName, SerializationType serializationType, T defaultValue) : this(fileName, serializationType)
        {
            Data = defaultValue;
        }
        
        private void BuildFilePath()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = $"{Application.persistentDataPath}/{_fileName}";
                if (!FolderHelper.AllFoldersExist(_filePath))
                {
                    FolderHelper.CreateFolders(_filePath);
                }
            }
        }

        public bool IsFileExist()
        {
            BuildFilePath();
            return File.Exists(_filePath);
        }

        public void SaveData()
        {
            BuildFilePath();

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
            BuildFilePath();

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
            BuildFilePath();

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
            BuildFilePath();

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

        private void SaveDataBinary()
        {
            try
            {
                using (FileStream fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    binaryFormatter.Serialize(fileStream, Data);
                }
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
                using (FileStream fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    Data = (T)binaryFormatter.Deserialize(fileStream);
                }
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
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamWriter writer = new StreamWriter(_filePath))
                {
                    serializer.Serialize(writer, Data);
                }
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
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamReader reader = new StreamReader(_filePath))
                {
                    Data = (T)serializer.Deserialize(reader);
                }
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
                string jsonData = JsonConvert.SerializeObject(Data);
                using (StreamWriter writer = new StreamWriter(_filePath))
                {
                    writer.Write(jsonData);
                }
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
                using (StreamReader reader = new StreamReader(_filePath))
                {
                    string jsonData = reader.ReadToEnd();
                    Data = JsonConvert.DeserializeObject<T>(jsonData);
                }
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
                using (FileStream fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    await Task.Run(() => binaryFormatter.Serialize(fileStream, Data));
                }
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
                using (FileStream fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    Data = await Task.Run(() => (T)binaryFormatter.Deserialize(fileStream));
                }
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
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamWriter writer = new StreamWriter(_filePath))
                {
                    await Task.Run(() => serializer.Serialize(writer, Data));
                }
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
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamReader reader = new StreamReader(_filePath))
                {
                    Data = await Task.Run(() => (T)serializer.Deserialize(reader));
                }
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
                string jsonData = JsonConvert.SerializeObject(Data);
                using (StreamWriter writer = new StreamWriter(_filePath))
                {
                    await writer.WriteAsync(jsonData);
                }
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
                using (StreamReader reader = new StreamReader(_filePath))
                {
                    string jsonData = await reader.ReadToEndAsync();
                    Data = JsonConvert.DeserializeObject<T>(jsonData);
                }
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
    
    public interface IDataFile
    {
        public void SaveData();
        public void LoadData();
    }
}