using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DataKeeper.DynamicScene
{
    [AddComponentMenu("DataKeeper/Addressable/Addressable Loader")]
    [SelectionBase]
    public class AddressableLoader : MonoBehaviour
    {
        [Header("Addressable Reference")]
        public AssetReferenceGameObject addressableAsset;
    
        [Header("Settings")]
        public float loadDistance = 1000f;
        public float unloadDistance = 1200f; // Slightly larger to prevent flickering
        public float checkInterval = 1f; // How often to check distances
        public float checkDelay = 0f; // How often to check distances
    
        [Header("Instance Settings")]
        public int maxInstances = 10;
        public bool useObjectPooling = true;
    
        private Camera mainCamera;
        private List<GameObject> loadedInstances = new List<GameObject>();
        private List<GameObject> pooledInstances = new List<GameObject>();
        private AsyncOperationHandle<GameObject> loadHandle;
        private bool isLoaded = false;
    
        // Static reference for multiple instances support
        private static Dictionary<string, List<AddressableLoader>> allManagers = new Dictionary<string, List<AddressableLoader>>();
    
        private void Awake()
        {
            // Register this manager
            string key = addressableAsset != null ? addressableAsset.AssetGUID : transform.position.ToString();
            if (!allManagers.ContainsKey(key))
            {
                allManagers[key] = new List<AddressableLoader>();
            }
            allManagers[key].Add(this);
        }
    
        private void Start()
        {
            // Find main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        
            if (mainCamera == null)
            {
                Debug.LogError("No camera found for AddressableLODManager!");
                return;
            }
        }
    
        private void OnDestroy()
        {
            // Unregister this manager
            string key = addressableAsset != null ? addressableAsset.AssetGUID : transform.position.ToString();
            if (allManagers.ContainsKey(key))
            {
                allManagers[key].Remove(this);
                if (allManagers[key].Count == 0)
                {
                    allManagers.Remove(key);
                }
            }
        
            UnloadAddressable();
        }

        private void OnEnable()
        {
            InvokeRepeating(nameof(CheckDistance), checkDelay, checkInterval);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(CheckDistance));
        }

        private void CheckDistance()
        {
            if (mainCamera == null) return;
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
            
            if (!isLoaded && distance <= loadDistance)
            {
                LoadAddressable();
            }
            else if (isLoaded && distance > unloadDistance)
            {
                UnloadAddressable();
            }
        }
    
        private void LoadAddressable()
        {
            if (isLoaded || addressableAsset == null) return;
        
            Debug.Log($"Loading addressable at {transform.name}");
        
            if (useObjectPooling && pooledInstances.Count > 0)
            {
                // Use pooled instance
                GameObject pooledObj = pooledInstances[0];
                pooledInstances.RemoveAt(0);
                loadedInstances.Add(pooledObj);
                pooledObj.SetActive(true);
                isLoaded = true;
            }
            else
            {
                // Load from addressables
                loadHandle = addressableAsset.LoadAssetAsync<GameObject>();
                loadHandle.Completed += OnAddressableLoaded;
            }
        }
    
        private void OnAddressableLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // Create instances up to maxInstances
                for (int i = 0; i < maxInstances && loadedInstances.Count < maxInstances; i++)
                {
                    GameObject instance = Instantiate(handle.Result, transform.position, transform.rotation, transform);
                    instance.name = $"{handle.Result.name}_Instance_{i}";
                    loadedInstances.Add(instance);
                }
            
                isLoaded = true;
                Debug.Log($"Addressable loaded successfully with {loadedInstances.Count} instances");
            }
            else
            {
                Debug.LogError($"Failed to load addressable: {handle.OperationException}");
            }
        }
    
        private void UnloadAddressable()
        {
            if (!isLoaded) return;
        
            Debug.Log($"Unloading addressable at {transform.name}");
        
            if (useObjectPooling)
            {
                // Move to pool instead of destroying
                foreach (GameObject instance in loadedInstances)
                {
                    if (instance != null)
                    {
                        instance.SetActive(false);
                        pooledInstances.Add(instance);
                    }
                }
            }
            else
            {
                // Destroy instances
                foreach (GameObject instance in loadedInstances)
                {
                    if (instance != null)
                    {
                        DestroyImmediate(instance);
                    }
                }
            }
        
            loadedInstances.Clear();
            isLoaded = false;
        
            // Release handle if valid
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
        }
    
        // Public methods for manual control
        public void ForceLoad()
        {
            LoadAddressable();
        }
    
        public void ForceUnload()
        {
            UnloadAddressable();
        }
    
        public bool IsLoaded()
        {
            return isLoaded;
        }
    
        public int GetLoadedInstanceCount()
        {
            return loadedInstances.Count;
        }
    }
}
