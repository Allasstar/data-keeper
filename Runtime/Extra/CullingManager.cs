using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.Extra
{
    // [AddComponentMenu("DataKeeper/Extra/Culling Manager")]
    public class CullingManager : MonoBehaviour
    {
        [System.Serializable]
        public class LayerCullingSetting
        {
            public LayerMask layer;
            public float cullingDistance = 100f;
            public bool enableCulling = true;
        }
        
        [Header("Culling Methods")]
        [SerializeField] private bool enableLayerCulling = true;
        [SerializeField] private bool enableFrustumCulling = false;
      
        [Header("Layer Culling Settings")]
        [SerializeField] private LayerCullingSetting[] layerCullingSettings = new LayerCullingSetting[0];
        [SerializeField] private float[] distances = new float[32];

        [Header("Layer Culling Settings")]
        [SerializeField] private float _distance = 100f;
        
        // Cached data
        private Camera[] allCameras;

        private void Awake()
        {
            ForceUpdate();
        }
        
        [Button]
        public void ForceUpdate()
        {
            allCameras = FindObjectsOfType<Camera>();
            
            foreach (var cam in allCameras)
            {
                if (enableFrustumCulling)
                {
                    if (cam.orthographic)
                    {
                        // cam.cullingMatrix = 
                        //     Matrix4x4.Ortho(-50, 50, -50, 50, 0.1f, 100f);
                    }
                    else
                    {
                        cam.cullingMatrix = 
                            Matrix4x4.Perspective(
                                cam.fieldOfView, 
                                    cam.aspect, 
                                    cam.nearClipPlane,
                                _distance);
                    }
                }
                else
                {
                    if (cam.orthographic)
                    {
                        // cam.cullingMatrix = 
                        //     Matrix4x4.Ortho(-50, 50, -50, 50, 0.1f, 100f);
                    }
                    else
                    {
                        cam.cullingMatrix = 
                            Matrix4x4.Perspective(
                                cam.fieldOfView, 
                                cam.aspect, 
                                cam.nearClipPlane,
                                cam.farClipPlane);
                    }
                }
                
                for (var i = 0; i < distances.Length; i++)
                {
                    distances[i] = 0;
                }

                if (enableLayerCulling)
                {
                    foreach (var layerCullingSetting in layerCullingSettings)
                    {
                        var index = Mathf.RoundToInt(Mathf.Log( layerCullingSetting.layer.value, 2));
                        distances[index] =
                            layerCullingSetting.enableCulling 
                                ? layerCullingSetting.cullingDistance
                                : 0;
                        
                    }
                }
               
                cam.layerCullDistances = distances;
            }
        }
    }
}