using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DataKeeper.DynamicScene
{
    [AddComponentMenu("DataKeeper/Addressable/Addressable Loader"), DisallowMultipleComponent, DefaultExecutionOrder(10)]
    [SelectionBase]
    public class AddressableLoader : MonoBehaviour
    {
        [Header("Addressable Reference")]
        public AssetReferenceGameObject addressableAsset;
    
        [Header("Settings")]
        public bool debugLog = false;
        public bool drawGizmo = false;
        public float loadDistance = 1100f;
        public float unloadDistance = 1150f;
        
        public float checkInterval = 1f; // How often to check distances
        public float checkDelay = 0f; // How often to check distances
        public List<Camera> cameraList = new List<Camera>();
    
        [Header("Instance Settings")]
        public int maxInstances = 10;
        public bool useObjectPooling = true;
    
        private List<GameObject> loadedInstances = new List<GameObject>();
        private List<GameObject> pooledInstances = new List<GameObject>();
        private AsyncOperationHandle<GameObject> loadHandle;
        private bool isLoaded = false;
        private bool updateFromSubScene = false;
        private float loadDistanceSquared;
        private float unloadDistanceSquared;
    
        // Static reference for multiple instances support
        private static Dictionary<string, List<AddressableLoader>> allManagers = new Dictionary<string, List<AddressableLoader>>();
    
        private void Awake()
        {
            loadDistanceSquared = loadDistance * loadDistance;
            unloadDistanceSquared = unloadDistance * unloadDistance;
            
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
            if(updateFromSubScene) return;

            if (cameraList.Count == 0)
            {
                if (Camera.main != null)
                {
                    cameraList.Add(Camera.main);
                }
            }
 
            if (cameraList.Count == 0)
            {
                Debug.LogError("No camera found for AddressableLODManager!");
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
            if(updateFromSubScene) return;
            
            InvokeRepeating(nameof(CheckDistance), checkDelay, checkInterval);
        }

        private void OnDisable()
        {
            if(updateFromSubScene) return;
            
            CancelInvoke(nameof(CheckDistance));
        }

        public void CheckDistance()
        {
            if (cameraList.Count == 0) return;
    
            Vector3 position = transform.position;
            float minDistanceSquared = float.MaxValue;
    
            for (int i = 0; i < cameraList.Count; i++)
            {
                if (cameraList[i] == null) continue;
        
                Vector3 cameraPos = cameraList[i].transform.position;
                float distanceSquared = (position.x - cameraPos.x) * (position.x - cameraPos.x) +
                                        (position.y - cameraPos.y) * (position.y - cameraPos.y) +
                                        (position.z - cameraPos.z) * (position.z - cameraPos.z);
        
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                }
            }
    
            if (!isLoaded && minDistanceSquared <= loadDistanceSquared)
            {
                LoadAddressable();
            }
            else if (isLoaded && minDistanceSquared > unloadDistanceSquared)
            {
                UnloadAddressable();
            }
        }
    
        private void LoadAddressable()
        {
            if (isLoaded || addressableAsset == null) return;

            if (debugLog)
            {
                Debug.Log($"Loading addressable at {transform.name}");
            }
        
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
                if (debugLog)
                {
                    Debug.Log($"Addressable loaded successfully with {loadedInstances.Count} instances");
                }
            }
            else
            {
                Debug.LogError($"Failed to load addressable: {handle.OperationException}");
            }
        }
    
        private void UnloadAddressable()
        {
            if (!isLoaded) return;
            if (debugLog)
            {
                Debug.Log($"Unloading addressable at {transform.name}");
            }
        
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

        public void SetUpdateFromSubScene(bool isUpdateFromSubScene)
        {
            updateFromSubScene = isUpdateFromSubScene;
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(transform.position, Vector3.one);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, loadDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, unloadDistance);
        }
#endif
    }
}
