using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DataKeeper.Editor.MeshTools
{
    public static class MeshEditUtility
    {
        public const int UVChannelCount = 8;

        public static void SetPivot(MeshFilter filter, Vector3 worldPivot)
        {
            var target = filter.transform;
            var localPivot = target.InverseTransformPoint(worldPivot);
            if (localPivot.sqrMagnitude < 1e-12f)
            {
                Debug.Log("Pivot already at target position.");
                return;
            }

            Undo.SetCurrentGroupName("Set Mesh Pivot");
            var undoGroup = Undo.GetCurrentGroup();

            var mesh = GetWritableMesh(filter, "Pivot");
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] -= localPivot;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            ShiftColliders(target.gameObject, mesh, localPivot);

            // Children follow the parent's move, so their world poses are restored to keep the scene unchanged.
            var childCount = target.childCount;
            var childPositions = new Vector3[childCount];
            for (int i = 0; i < childCount; i++)
            {
                var child = target.GetChild(i);
                Undo.RecordObject(child, "Set Mesh Pivot");
                childPositions[i] = child.position;
            }

            Undo.RecordObject(target, "Set Mesh Pivot");
            target.position = worldPivot;

            for (int i = 0; i < childCount; i++)
                target.GetChild(i).position = childPositions[i];

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"Pivot of '{mesh.name}' moved to {worldPivot}.");
        }

        public static void TransformUV(MeshFilter filter, int channel, Vector2 scale, Vector2 offset)
        {
            if (!filter.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord0 + channel))
            {
                Debug.LogWarning($"Mesh '{filter.sharedMesh.name}' has no UV{channel}.");
                return;
            }

            Undo.SetCurrentGroupName("Transform Mesh UV");
            var undoGroup = Undo.GetCurrentGroup();

            var mesh = GetWritableMesh(filter, "UV");
            var uvs = new List<Vector2>(mesh.vertexCount);
            mesh.GetUVs(channel, uvs);
            for (int i = 0; i < uvs.Count; i++)
                uvs[i] = Vector2.Scale(uvs[i], scale) + offset;
            mesh.SetUVs(channel, uvs);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"UV{channel} of '{mesh.name}' scaled by {scale}, offset by {offset}.");
        }

        // Imported (FBX) and built-in meshes are read-only, so they get copied into a new .asset;
        // a mesh that is already an .asset is edited in place, which also affects every other user of it.
        private static Mesh GetWritableMesh(MeshFilter filter, string suffix)
        {
            var source = filter.sharedMesh;
            var sourcePath = AssetDatabase.GetAssetPath(source);

            if (sourcePath.EndsWith(".asset"))
            {
                Undo.RecordObject(source, "Edit Mesh");
                return source;
            }

            var copy = Object.Instantiate(source);
            copy.name = $"{source.name}_{suffix}";

            var folder = sourcePath.StartsWith("Assets/") ? Path.GetDirectoryName(sourcePath) : "Assets";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{copy.name}.asset");
            AssetDatabase.CreateAsset(copy, path);
            Debug.Log($"Created mesh asset '{path}' from '{source.name}'.", copy);

            Undo.RecordObject(filter, "Edit Mesh");
            filter.sharedMesh = copy;

            foreach (var meshCollider in filter.GetComponents<MeshCollider>())
            {
                if (meshCollider.sharedMesh != source)
                    continue;
                Undo.RecordObject(meshCollider, "Edit Mesh");
                meshCollider.sharedMesh = copy;
            }

            return copy;
        }

        private static void ShiftColliders(GameObject gameObject, Mesh mesh, Vector3 localOffset)
        {
            foreach (var collider in gameObject.GetComponents<Collider>())
            {
                Undo.RecordObject(collider, "Set Mesh Pivot");
                switch (collider)
                {
                    case BoxCollider box: box.center -= localOffset; break;
                    case SphereCollider sphere: sphere.center -= localOffset; break;
                    case CapsuleCollider capsule: capsule.center -= localOffset; break;
                    case MeshCollider meshCollider when meshCollider.sharedMesh == mesh:
                        // Reassigning forces the collider to rebuild from the moved vertices.
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = mesh;
                        break;
                }
            }
        }
    }
}
