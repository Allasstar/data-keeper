using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DataKeeper.DynamicScene
{
    public static class SubSceneManager
    {
        // Track loaded addressables and their data
        private static Dictionary<string, AddressableData> loadedAddressables = new Dictionary<string, AddressableData>();
        
        // Track all loader instances
        private static HashSet<AddressableLoader> allLoaders = new HashSet<AddressableLoader>();
        
        // Track currently loading addressables to prevent double loads
        private static HashSet<string> currentlyLoading = new HashSet<string>();
        
        /// <summary>
        /// Auto unsubscribe on invoke.
        /// </summary>
        public static event Action OnAllCurrentLoaded;
        
        private class AddressableData
        {
            public AsyncOperationHandle<GameObject> loadHandle;
            public GameObject prefab;
            public List<GameObject> instances = new List<GameObject>();
            public List<GameObject> pooledInstances = new List<GameObject>();
            public HashSet<AddressableLoader> requestingLoaders = new HashSet<AddressableLoader>();
            public bool isLoaded = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Reset()
        {
            loadedAddressables.Clear();
            allLoaders.Clear();
            currentlyLoading.Clear();
            OnAllCurrentLoaded = null;
        }
        
        public static void RegisterLoader(AddressableLoader loader)
        {
            allLoaders.Add(loader);
        }
        
        public static void UnregisterLoader(AddressableLoader loader)
        {
            allLoaders.Remove(loader);
            
            // Clean up any requests from this loader
            if (loader.addressableAsset != null)
            {
                string guid = loader.addressableAsset.AssetGUID;
                if (loadedAddressables.ContainsKey(guid))
                {
                    var data = loadedAddressables[guid];
                    data.requestingLoaders.Remove(loader);
                    
                    // If no more loaders need this addressable, unload it
                    if (data.requestingLoaders.Count == 0)
                    {
                        UnloadAddressable(guid);
                    }
                }
            }
        }
        
        public static void RequestLoad(AddressableLoader loader)
        {
            if (loader.addressableAsset == null) return;
            
            string guid = loader.addressableAsset.AssetGUID;
            
            // Initialize data if needed
            if (!loadedAddressables.ContainsKey(guid))
            {
                loadedAddressables[guid] = new AddressableData();
            }
            
            var data = loadedAddressables[guid];
            data.requestingLoaders.Add(loader);
            
            // If already loaded, create instance immediately
            if (data.isLoaded && data.prefab != null)
            {
                CreateInstanceForLoader(loader, data);
                return;
            }
            
            // If currently loading, just wait
            if (currentlyLoading.Contains(guid))
            {
                return;
            }
            
            // Start loading
            LoadAddressable(guid, loader.addressableAsset);
        }
        
        public static void RequestUnload(AddressableLoader loader)
        {
            if (loader.addressableAsset == null) return;
            
            string guid = loader.addressableAsset.AssetGUID;
            
            if (!loadedAddressables.ContainsKey(guid)) return;
            
            var data = loadedAddressables[guid];
            data.requestingLoaders.Remove(loader);
            
            // Remove instances created for this loader
            RemoveInstancesForLoader(loader, data);
            
            // If no more loaders need this addressable, unload it
            if (data.requestingLoaders.Count == 0)
            {
                UnloadAddressable(guid);
            }
        }
        
        private static void LoadAddressable(string guid, AssetReferenceGameObject assetReference)
        {
            if (currentlyLoading.Contains(guid)) return;
            
            currentlyLoading.Add(guid);
            var data = loadedAddressables[guid];
            
            data.loadHandle = assetReference.LoadAssetAsync<GameObject>();
            data.loadHandle.Completed += (handle) => OnAddressableLoaded(guid, handle);
        }
        
        private static void OnAddressableLoaded(string guid, AsyncOperationHandle<GameObject> handle)
        {
            currentlyLoading.Remove(guid);
            
            if (!loadedAddressables.ContainsKey(guid)) return;
            
            var data = loadedAddressables[guid];
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                data.prefab = handle.Result;
                data.isLoaded = true;
                
                // Create instances for all requesting loaders
                foreach (var loader in data.requestingLoaders)
                {
                    CreateInstanceForLoader(loader, data);
                }
                
                // Check if all pending loads are complete
                CheckAllLoadingComplete();
            }
            else
            {
                Debug.LogError($"Failed to load addressable {guid}: {handle.OperationException}");
            }
        }
        
        private static void CreateInstanceForLoader(AddressableLoader loader, AddressableData data)
        {
            GameObject instance;
            
            // Use pooled instance if available and pooling is enabled
            if (loader.useObjectPooling && data.pooledInstances.Count > 0)
            {
                instance = data.pooledInstances[0];
                data.pooledInstances.RemoveAt(0);
                instance.SetActive(true);
                instance.transform.SetParent(loader.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Create new instance
                instance = UnityEngine.Object.Instantiate(data.prefab, loader.transform.position, loader.transform.rotation, loader.transform);
                instance.name = $"{data.prefab.name}_Instance";
            }
            
            data.instances.Add(instance);
            loader.SetLoadedInstance(instance);
            
            if (loader.debugLog)
            {
                Debug.Log($"Instance created for loader {loader.name}");
            }
        }
        
        private static void RemoveInstancesForLoader(AddressableLoader loader, AddressableData data)
        {
            GameObject loaderInstance = loader.GetLoadedInstance();
            if (loaderInstance == null) return;
            
            data.instances.Remove(loaderInstance);
            
            if (loader.useObjectPooling)
            {
                loaderInstance.SetActive(false);
                data.pooledInstances.Add(loaderInstance);
            }
            else
            {
                // Use Destroy instead of DestroyImmediate to avoid destruction timing issues
                if (loaderInstance != null)
                    UnityEngine.Object.Destroy(loaderInstance);
            }
            
            loader.SetLoadedInstance(null);
        }
        
        private static void UnloadAddressable(string guid)
        {
            if (!loadedAddressables.ContainsKey(guid)) return;
            
            var data = loadedAddressables[guid];
            
            // Destroy all instances - use Destroy instead of DestroyImmediate
            for (int i = data.instances.Count - 1; i >= 0; i--)
            {
                var instance = data.instances[i];
                if (instance != null)
                    UnityEngine.Object.Destroy(instance);
            }
            data.instances.Clear();
            
            for (int i = data.pooledInstances.Count - 1; i >= 0; i--)
            {
                var pooled = data.pooledInstances[i];
                if (pooled != null)
                    UnityEngine.Object.Destroy(pooled);
            }
            data.pooledInstances.Clear();
            
            // Release handle
            if (data.loadHandle.IsValid())
            {
                Addressables.Release(data.loadHandle);
            }
            
            loadedAddressables.Remove(guid);
        }
        
        private static void CheckAllLoadingComplete()
        {
            // Check if any addressables are still loading
            if (currentlyLoading.Count == 0)
            {
                OnAllCurrentLoaded?.Invoke();
                OnAllCurrentLoaded = null;
            }
        }
        
        public static bool IsAddressableLoaded(string guid)
        {
            return loadedAddressables.ContainsKey(guid) && loadedAddressables[guid].isLoaded;
        }
        
        public static int GetTotalInstanceCount()
        {
            int count = 0;
            foreach (var data in loadedAddressables.Values)
            {
                count += data.instances.Count;
            }
            return count;
        }
        
        public static int GetLoadedAddressableCount()
        {
            int count = 0;
            foreach (var data in loadedAddressables.Values)
            {
                if (data.isLoaded) count++;
            }
            return count;
        }
    }
}