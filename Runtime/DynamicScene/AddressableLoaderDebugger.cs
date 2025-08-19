#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.DynamicScene
{
    public class AddressableLoaderDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool showDebugInfo = true;
        public bool showPerformanceStats = true;
        public KeyCode toggleKey = KeyCode.F1;
    
        private Camera mainCamera;
        private List<AddressableLoader> allManagers = new List<AddressableLoader>();
        private bool debugVisible = false;
    
        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        
            // Find all LOD managers
            RefreshManagerList();
        }
    
        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                debugVisible = !debugVisible;
            }
        
            // Refresh manager list periodically
            if (Time.frameCount % 60 == 0) // Every 60 frames
            {
                RefreshManagerList();
            }
        }
    
        private void RefreshManagerList()
        {
            allManagers.Clear();
            allManagers.AddRange(FindObjectsOfType<AddressableLoader>());
        }
    
        private void OnGUI()
        {
            if (!debugVisible || !showDebugInfo) return;
        
            GUI.Box(new Rect(10, 10, 300, 200 + allManagers.Count * 20), "Addressable LOD Debug");
        
            int yOffset = 30;
        
            if (mainCamera != null)
            {
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Camera Position: {mainCamera.transform.position}");
                yOffset += 20;
            }
        
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Total LOD Managers: {allManagers.Count}");
            yOffset += 20;
        
            int loadedCount = 0;
            int totalInstances = 0;
        
            foreach (var manager in allManagers)
            {
                if (manager == null) continue;
            
                bool isLoaded = manager.IsLoaded();
                int instances = manager.GetLoadedInstanceCount();
            
                if (isLoaded) loadedCount++;
                totalInstances += instances;
            
                float distance = mainCamera != null ? 
                    Vector3.Distance(manager.transform.position, mainCamera.transform.position) : 0f;
            
                string status = isLoaded ? "LOADED" : "UNLOADED";
                string color = isLoaded ? "green" : "red";
            
                GUI.Label(new Rect(20, yOffset, 280, 20), 
                    $"<color={color}>{manager.name}: {status} ({instances}) - {distance:F0}m</color>");
                yOffset += 20;
            }
        
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Loaded: {loadedCount}/{allManagers.Count}");
            yOffset += 20;
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Total Instances: {totalInstances}");
        
            GUI.Label(new Rect(20, yOffset + 30, 280, 20), $"Press {toggleKey} to toggle debug");
        }
    }
}
#endif