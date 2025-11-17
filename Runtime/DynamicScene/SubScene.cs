using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using DataKeeper.Extra;
using DataKeeper.Helpers;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.DynamicScene
{
    [AddComponentMenu("DataKeeper/Addressable/Sub Scene"), DisallowMultipleComponent, DefaultExecutionOrder(-20)]
    public class SubScene : MonoBehaviour
    {
        [Header("Settings")]
        public bool checkChildren = false;
        public float checkInterval = 1f;
        public float checkDelay = 0f;
        public List<Camera> cameraList = new List<Camera>();

        [Header("Runtime")]
        [SerializeField] private Vector3 _center;
        [SerializeField] private float _radius;
        [SerializeField] private bool _wasInRadius;
        [SerializeField] private AddressableLoader[] _children;

        private void Awake()
        {
            _children = GetComponentsInChildren<AddressableLoader>();
            
            foreach (var child in _children)
            {
                child.SetUpdateFromSubScene(checkChildren);
            }
        }
        
        private void Start()
        {
            if(!checkChildren) return;

            InitializeCameraList();
            InitializeCheckRadius();
        }

        private void OnEnable()
        {
            if(!checkChildren) return;
            
            InvokeRepeating(nameof(CheckDistance), checkDelay, checkInterval);
        }

        private void OnDisable()
        {
            if(!checkChildren) return;
            
            CancelInvoke(nameof(CheckDistance));
        }

        private void CheckDistance()
        {
            var inRadius = IsInRadius();

            if (inRadius || _wasInRadius)
            {
                foreach (var child in _children)
                {
                    child.CheckDistance();
                }
                
                _wasInRadius = inRadius;
            }
        }

        private bool IsInRadius()
        {
            if (cameraList.Count == 0) return false;
            return cameraList.Any(a => Vector3.Distance(a.transform.position, _center) <= _radius);
        }

        private void InitializeCheckRadius()
        {
            _center = Vector3.zero;

            foreach (var child in _children)
            {
                _center += child.transform.position;
            }
            
            _center /= _children.Length;
            var maxDistance = _children.Max(m => Vector3.Distance(_center, m.transform.position));
            _radius = _children.Max(m => m.unloadDistance) * 1.2f + maxDistance;
            _wasInRadius = false;
        }
        
        private void InitializeCameraList()
        {
            if (cameraList.Count == 0)
            {
                if (Camera.main != null)
                {
                    cameraList.Add(Camera.main);
                }
            }
 
            if (cameraList.Count == 0)
            {
                Debug.LogError("No camera found for SubScene!");
                return;
            }
            
            foreach (var child in _children)
            {
                child.cameraList = cameraList;
            }
        }

#if UNITY_EDITOR
        
         [Header("Editor Only\nHelps to preview addressable prefabs"), 
         SerializeField, TextArea(3, 10)] 
        private string _description;
        
        [SerializeField, ReadOnlyInspector] 
        private List<GameObject> instantiatedPrefabs = new List<GameObject>();
        
        [SerializeField] private bool _showGizmo = false;

        private void OnDrawGizmosSelected()
        {
            if(!_showGizmo) return;
            if(!Application.isPlaying) return;
            
            Gizmos.color = Colors.GreenFaint;
            Gizmos.DrawSphere(_center, _radius);
        }

        [Button("Open Addressable Converter Tool", 10, ButtonEnabledState.InEditMode, "Editor Window")]
        private void OpenAddressableConverterTool()
        {
            AddressableConverterTool.ShowWindow();
        }
    
        [Button("Load Prefabs", 20, ButtonEnabledState.InEditMode, "Preview")]
        private void FindAndInstantiatePrefabs()
        {
            if (Application.isPlaying) return;
            
            // Clear any existing references to destroyed objects
            instantiatedPrefabs.RemoveAll(obj => obj == null);
        
            // Find all AddressableLoader components in children
            AddressableLoader[] lodManagers = GetComponentsInChildren<AddressableLoader>();
        
            if (lodManagers.Length == 0)
            {
                Debug.LogWarning("No AddressableLoader components found in children!");
                return;
            }
        
            Debug.Log($"Found {lodManagers.Length} AddressableLoader components. Starting instantiation...");
        
            foreach (AddressableLoader lodManager in lodManagers)
            {
                if (lodManager.addressableAsset != null)
                {
                    InstantiatePrefabForManager(lodManager);
                }
                else
                {
                    Debug.LogWarning($"AddressableLoader on {lodManager.name} has no addressable asset assigned!");
                }
            }
        
            // Mark the scene as dirty to indicate unsaved changes
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            
            Debug.Log($"Instantiation complete. Created {instantiatedPrefabs.Count} preview objects.");
        }
    
        private void InstantiatePrefabForManager(AddressableLoader lodManager)
        {
            // Load the addressable asset synchronously for editor use
            var loadHandle = lodManager.addressableAsset.LoadAssetAsync<GameObject>();
            var prefab = loadHandle.WaitForCompletion();
        
            if (prefab != null)
            {
                // Instantiate as child of the AddressableLoader
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.transform.SetParent(lodManager.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                
                instance.AddComponent<HideFlagsManager>()
                    .SetHideFlagsTarget(HideFlagsTarget.Self)
                    .SetHideFlags(HideFlags.DontSave | HideFlags.NotEditable)
                    .SetApplyOnValidate(true)
                    .Apply();
            
                // Add a tag to identify this as an editor preview
                instance.name = $"[EDITOR_PREVIEW] {prefab.name}";
            
                // Keep track of instantiated objects
                instantiatedPrefabs.Add(instance);
            
                Debug.Log($"Instantiated preview of {prefab.name} for {lodManager.name}");
            }
            else
            {
                Debug.LogError($"Failed to load addressable asset for {lodManager.name}");
            }
        
            // Release the handle
            if (loadHandle.IsValid())
            {
                UnityEngine.AddressableAssets.Addressables.Release(loadHandle);
            }
        }
    
        [Button("Unload Prefabs", 2, ButtonEnabledState.InEditMode)]
        private void RemovePrefabInstances()
        {
            if (Application.isPlaying) return;
            
            if (instantiatedPrefabs.Count == 0)
            {
                Debug.Log("No instantiated prefabs to remove.");
                return;
            }
        
            int removedCount = 0;
        
            // Remove all tracked instances
            for (int i = instantiatedPrefabs.Count - 1; i >= 0; i--)
            {
                if (instantiatedPrefabs[i] != null)
                {
                    Debug.Log($"Removing preview object: {instantiatedPrefabs[i].name}");
                    DestroyImmediate(instantiatedPrefabs[i]);
                    removedCount++;
                }
                instantiatedPrefabs.RemoveAt(i);
            }
        
            // Also find and remove any objects with the editor preview name pattern
            // This handles cases where the tracking list might be out of sync
            AddressableLoader[] lodManagers = GetComponentsInChildren<AddressableLoader>();
            foreach (AddressableLoader lodManager in lodManagers)
            {
                Transform[] children = lodManager.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    if (child != lodManager.transform && child.name.StartsWith("[EDITOR_PREVIEW]"))
                    {
                        Debug.Log($"Removing orphaned preview object: {child.name}");
                        DestroyImmediate(child.gameObject);
                        removedCount++;
                    }
                }
            }
        
            // Mark the scene as dirty to indicate unsaved changes
            if (removedCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
            
            Debug.Log($"Removed {removedCount} preview objects.");
        }
    
        // Clean up on destroy
        private void OnDestroy()
        {
            RemovePrefabInstances();
        }
    
        // Validation method to show info in inspector
        private void OnValidate()
        {
            if (Application.isPlaying) return;
        
            // Count current preview objects
            int previewCount = 0;
            AddressableLoader[] lodManagers = GetComponentsInChildren<AddressableLoader>();
            foreach (AddressableLoader lodManager in lodManagers)
            {
                Transform[] children = lodManager.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    if (child != lodManager.transform && child.name.StartsWith("[EDITOR_PREVIEW]"))
                    {
                        previewCount++;
                    }
                }
            }
        
            if (previewCount != instantiatedPrefabs.Count)
            {
                // Sync the tracking list
                instantiatedPrefabs.RemoveAll(obj => obj == null);
            }
        }
#endif
    }
}
