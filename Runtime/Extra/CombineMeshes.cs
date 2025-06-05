using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using DataKeeper.Attributes;

namespace DataKeeper.Extra
{
    [AddComponentMenu("DataKeeper/Extra/Combine Meshes")]
    public class CombineMeshes : MonoBehaviour
    {
        [SerializeField] private bool _combineOnAwake = true;

        private bool IsCombined => _combinedMeshObject != null;
        private MeshFilter[] _originalMeshFilters;
        private GameObject _combinedMeshObject;

        private void Awake()
        {
            if (_combineOnAwake && !IsCombined)
            {
                CombineAllMeshes();
            }
        }

        private void CombineAllMeshes()
        {
            // Get all mesh filters in children, excluding self
            _originalMeshFilters = GetComponentsInChildren<MeshFilter>()
                .Where(mf => mf.gameObject != gameObject)
                .ToArray();

            if (_originalMeshFilters.Length == 0)
            {
                Debug.LogWarning("No meshes found to combine!");
                return;
            }

            // Get all unique materials
            Dictionary<Material, List<CombineInstance>> materialToCombines =
                new Dictionary<Material, List<CombineInstance>>();

            // Group meshes by material
            foreach (var mf in _originalMeshFilters)
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;

                // Handle submeshes and their materials
                for (int submesh = 0; submesh < mf.sharedMesh.subMeshCount; submesh++)
                {
                    Material material = mr.sharedMaterials[submesh];
                    if (!materialToCombines.ContainsKey(material))
                    {
                        materialToCombines[material] = new List<CombineInstance>();
                    }

                    CombineInstance ci = new CombineInstance
                    {
                        mesh = mf.sharedMesh,
                        subMeshIndex = submesh,
                        transform = mf.transform.localToWorldMatrix
                    };
                    materialToCombines[material].Add(ci);
                }
            }

            // Create the combined mesh object
            _combinedMeshObject = new GameObject("Combined Mesh");
            _combinedMeshObject.transform.SetParent(transform);
            _combinedMeshObject.transform.localPosition = Vector3.zero;
            _combinedMeshObject.transform.localRotation = Quaternion.identity;
            _combinedMeshObject.transform.localScale = Vector3.one;

            // Add mesh filter and renderer
            var finalMeshFilter = _combinedMeshObject.AddComponent<MeshFilter>();
            var finalMeshRenderer = _combinedMeshObject.AddComponent<MeshRenderer>();

            // Combine submeshes for each material
            Mesh combinedMesh = new Mesh();
            List<Material> materials = new List<Material>();
            List<CombineInstance> finalCombiners = new List<CombineInstance>();

            foreach (var kvp in materialToCombines)
            {
                Material material = kvp.Key;
                List<CombineInstance> combineInstances = kvp.Value;

                // Create a new mesh for this material
                Mesh submesh = new Mesh();
                submesh.CombineMeshes(combineInstances.ToArray(), true);

                // Add this submesh to the final combination
                CombineInstance ci = new CombineInstance
                {
                    mesh = submesh,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                };
                finalCombiners.Add(ci);
                materials.Add(material);
            }

            // Combine all submeshes
            combinedMesh.CombineMeshes(finalCombiners.ToArray(), false);
            combinedMesh.Optimize();
            combinedMesh.RecalculateBounds();
            combinedMesh.RecalculateNormals();

            // Assign the combined mesh and materials
            finalMeshFilter.sharedMesh = combinedMesh;
            finalMeshRenderer.sharedMaterials = materials.ToArray();

            // Disable original mesh renderers
            foreach (var mf in _originalMeshFilters)
            {
                if (mf.GetComponent<MeshRenderer>() != null)
                    mf.GetComponent<MeshRenderer>().enabled = false;
            }
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
#endif
    }
}