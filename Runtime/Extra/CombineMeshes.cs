using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DataKeeper.Attributes;
using DataKeeper.Generic;

namespace DataKeeper.Extra
{
    [AddComponentMenu("DataKeeper/Extra/Combine Meshes")]
    public class CombineMeshes : MonoBehaviour
    {
        [SerializeField] private Optional<CombineMeshesSettingsSO> OverrideSettings = new Optional<CombineMeshesSettingsSO>();
        
        [SerializeField] private bool _combineOnStart = true;
        [SerializeField] private bool _waitOneFrame = true;
        [SerializeField] private bool _includeInactive = false;
        [SerializeField] private bool _combineSubmeshes = true;
        [SerializeField] private bool _mergeSubmeshes = false;
        
        private bool CombineOnStart => OverrideSettings.Enabled ? OverrideSettings.Value.CombineOnStart : _combineOnStart;
        private bool WaitOneFrame => OverrideSettings.Enabled ? OverrideSettings.Value.WaitOneFrame : _waitOneFrame;
        private bool IncludeInactive => OverrideSettings.Enabled ? OverrideSettings.Value.IncludeInactive : _includeInactive;
        private bool CombineSubmeshes => OverrideSettings.Enabled ? OverrideSettings.Value.CombineSubmeshes : _combineSubmeshes;
        private bool MergeSubmeshes => OverrideSettings.Enabled ? OverrideSettings.Value.MergeSubmeshes : _mergeSubmeshes;

        private bool IsCombined => _combinedMeshObject != null;
        private MeshFilter[] _originalMeshFilters;
        private GameObject _combinedMeshObject;

        private IEnumerator Start()
        {
            if (CombineOnStart && !IsCombined)
            {
                if (WaitOneFrame)
                {
                    yield return null;
                    CombineAllMeshes();
                }
                else
                {
                    CombineAllMeshes();
                }
            }
            
            yield return null;
        }

        private void CombineAllMeshes()
        {
            // Get all MeshFilter components from children
            _originalMeshFilters = GetComponentsInChildren<MeshFilter>(IncludeInactive);
            
            if (_originalMeshFilters.Length == 0)
            {
                Debug.LogWarning("No MeshFilter components found in children.");
                return;
            }

            // Group meshes by material if not merging submeshes
            Dictionary<Material, List<CombineInstance>> materialGroups = new Dictionary<Material, List<CombineInstance>>();
            List<CombineInstance> allCombineInstances = new List<CombineInstance>();

            if (Application.isPlaying)
            {
                var isSkipCombine = false;
                foreach (MeshFilter meshFilter in _originalMeshFilters)
                {
                    if (!meshFilter.sharedMesh.isReadable)
                    {
                        isSkipCombine = true;
                        Debug.LogError($"Mesh '{meshFilter.sharedMesh.name}' on GameObject '{meshFilter.gameObject.name}' does not have read/write enabled. This may cause issues when combining meshes.", meshFilter.sharedMesh);
                    }
                }

                if (isSkipCombine)
                {
                    return;
                }
            }

            foreach (MeshFilter meshFilter in _originalMeshFilters)
            {
                if (meshFilter.sharedMesh == null) continue;
                if (meshFilter == GetComponent<MeshFilter>()) continue;

                // Skip self
                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;

                Transform meshTransform = meshFilter.transform;
                Matrix4x4 matrix = transform.worldToLocalMatrix * meshTransform.localToWorldMatrix;

                // Handle submeshes
                if (CombineSubmeshes && meshFilter.sharedMesh.subMeshCount > 1)
                {
                    for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                    {
                        CombineInstance combine = new CombineInstance
                        {
                            mesh = meshFilter.sharedMesh,
                            subMeshIndex = i,
                            transform = matrix
                        };

                        if (MergeSubmeshes)
                        {
                            allCombineInstances.Add(combine);
                        }
                        else
                        {
                            Material material = i < meshRenderer.sharedMaterials.Length ? 
                                              meshRenderer.sharedMaterials[i] : null;
                            
                            if (!materialGroups.ContainsKey(material))
                                materialGroups[material] = new List<CombineInstance>();
                            
                            materialGroups[material].Add(combine);
                        }
                    }
                }
                else
                {
                    CombineInstance combine = new CombineInstance
                    {
                        mesh = meshFilter.sharedMesh,
                        subMeshIndex = 0,
                        transform = matrix
                    };

                    if (MergeSubmeshes)
                    {
                        allCombineInstances.Add(combine);
                    }
                    else
                    {
                        Material material = meshRenderer.sharedMaterials.Length > 0 ? 
                                          meshRenderer.sharedMaterials[0] : null;
                        
                        if (!materialGroups.ContainsKey(material))
                            materialGroups[material] = new List<CombineInstance>();
                        
                        materialGroups[material].Add(combine);
                    }
                }
            }

            // Create the combined mesh object
            _combinedMeshObject = new GameObject("Combined Mesh");
            _combinedMeshObject.transform.SetParent(transform);
            _combinedMeshObject.transform.localPosition = Vector3.zero;
            _combinedMeshObject.transform.localRotation = Quaternion.identity;
            _combinedMeshObject.transform.localScale = Vector3.one;

            MeshFilter combinedMeshFilter = _combinedMeshObject.AddComponent<MeshFilter>();
            MeshRenderer combinedMeshRenderer = _combinedMeshObject.AddComponent<MeshRenderer>();

            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "Combined Mesh";

            if (MergeSubmeshes)
            {
                // Combine all into single submesh
                combinedMesh.CombineMeshes(allCombineInstances.ToArray(), true);
                
                // Use the first available material
                Material firstMaterial = null;
                foreach (var meshFilter in _originalMeshFilters)
                {
                    var renderer = meshFilter.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials[0] != null)
                    {
                        firstMaterial = renderer.sharedMaterials[0];
                        break;
                    }
                }
                combinedMeshRenderer.material = firstMaterial;
            }
            else
            {
                // Combine while preserving submeshes per material
                List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
                List<Material> materials = new List<Material>();

                foreach (var kvp in materialGroups)
                {
                    if (kvp.Value.Count > 0)
                    {
                        Mesh tempMesh = new Mesh();
                        tempMesh.CombineMeshes(kvp.Value.ToArray(), true);
                        
                        CombineInstance combineInstance = new CombineInstance
                        {
                            mesh = tempMesh,
                            subMeshIndex = 0,
                            transform = Matrix4x4.identity
                        };
                        
                        finalCombineInstances.Add(combineInstance);
                        materials.Add(kvp.Key);
                    }
                }

                combinedMesh.CombineMeshes(finalCombineInstances.ToArray(), false);
                combinedMeshRenderer.materials = materials.ToArray();

                // Clean up temporary meshes
                foreach (var instance in finalCombineInstances)
                {
                    if (Application.isPlaying)
                        Destroy(instance.mesh);
                    else
                        DestroyImmediate(instance.mesh);
                }
            }

            combinedMeshFilter.mesh = combinedMesh;

            // Disable original mesh renderers
            foreach (MeshFilter meshFilter in _originalMeshFilters)
            {
                if (meshFilter != GetComponent<MeshFilter>())
                {
                    MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }

        [ContextMenu("Separate Meshes")]
        public void SeparateMeshes()
        {
            if (!IsCombined) return;

            // Re-enable original mesh renderers
            if (_originalMeshFilters != null)
            {
                foreach (MeshFilter meshFilter in _originalMeshFilters)
                {
                    if (meshFilter != null && meshFilter != GetComponent<MeshFilter>())
                    {
                        MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
                        if (renderer != null)
                            renderer.enabled = true;
                    }
                }
            }

            // Destroy combined mesh object
            if (_combinedMeshObject != null)
            {
                if (Application.isPlaying)
                    Destroy(_combinedMeshObject);
                else
                    DestroyImmediate(_combinedMeshObject);
                
                _combinedMeshObject = null;
            }
        }

        [ContextMenu("Force Combine")]
        public void ForceCombine()
        {
            if (IsCombined)
                SeparateMeshes();
            
            CombineAllMeshes();
        }

#if UNITY_EDITOR
        [Button("Bake Combined Mesh")]
        private void BakeCombinedMesh()
        {
            if (!IsCombined)
            {
                CombineAllMeshes();
            }

            if (_combinedMeshObject != null)
            {
                MeshFilter meshFilter = _combinedMeshObject.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                        "Save Combined Mesh",
                        "CombinedMesh",
                        "asset",
                        "Please enter a file name to save the combined mesh."
                    );

                    if (!string.IsNullOrEmpty(path))
                    {
                        UnityEditor.AssetDatabase.CreateAsset(meshFilter.sharedMesh, path);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"Combined mesh saved to: {path}");
                    }
                }
            }
        }

        [Button("Toggle Combined/Separated")]
        private void ToggleCombined()
        {
            if (IsCombined)
                SeparateMeshes();
            else
                CombineAllMeshes();
        }
#endif
    }
}