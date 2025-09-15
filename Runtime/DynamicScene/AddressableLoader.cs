using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DataKeeper.DynamicScene
{
    [AddComponentMenu("DataKeeper/Addressable/Addressable Loader"), DisallowMultipleComponent, DefaultExecutionOrder(-10)]
    [SelectionBase]
    public class AddressableLoader : MonoBehaviour
    {
        [Header("Addressable Reference")]
        public AssetReferenceGameObject addressableAsset;
    
        [Header("Settings")]
        public bool debugLog = false;
        public bool drawGizmo = false;
        public bool useObjectPooling = true;
        public float loadDistance = 1100f;
        public float unloadDistance = 1150f;
        
        public float checkInterval = 1f;
        public float checkDelay = 0f;
        public List<Camera> cameraList = new List<Camera>();
    
        private GameObject loadedInstance;
        private bool isLoaded = false;
        private bool updateFromSubScene = false;
        private float loadDistanceSquared;
        private float unloadDistanceSquared;

        private void Awake()
        {
            loadDistanceSquared = loadDistance * loadDistance;
            unloadDistanceSquared = unloadDistance * unloadDistance;
            
            SubSceneManager.RegisterLoader(this);
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
                Debug.LogError("No camera found for AddressableLoader!");
            }
        }
    
        private void OnDestroy()
        {
            SubSceneManager.UnregisterLoader(this);
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
                Debug.Log($"Requesting load for addressable at {transform.name}");
            }
        
            SubSceneManager.RequestLoad(this);
        }
    
        private void UnloadAddressable()
        {
            if (!isLoaded) return;
            
            if (debugLog)
            {
                Debug.Log($"Requesting unload for addressable at {transform.name}");
            }
        
            SubSceneManager.RequestUnload(this);
            isLoaded = false;
        }
    
        // Called by SubSceneManager when instance is ready
        public void SetLoadedInstance(GameObject instance)
        {
            loadedInstance = instance;
            isLoaded = instance != null;
        }
        
        public GameObject GetLoadedInstance()
        {
            return loadedInstance;
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