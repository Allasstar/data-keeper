using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.MenuItems
{
    public class SnapToGroundEditor : UnityEditor.Editor
    {
        private const string UNDO_GROUP_NAME = "Snap to Ground";

        [MenuItem("Tools/Snap to Ground (Auto) %g")]
        private static void SnapToGroundAuto()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectToGround(gameObject);
            }
        }

        [MenuItem("Tools/Snap to Ground (Collider) _home")]
        private static void SnapToGroundCollider()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectWithCollider(gameObject);
            }
        }

        [MenuItem("Tools/Snap to Ground (Mesh) _end")]
        private static void SnapToGroundMesh()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectWithMesh(gameObject);
            }
        }

        [MenuItem("Tools/Snap to Ground (Transform) _pgdn")]
        private static void SnapToGroundTransform()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectWithTransform(gameObject);
            }
        }

        private static void SnapObjectToGround(GameObject gameObject)
        {
            // Try collider first
            if (TryGetColliderLowestPoint(gameObject, out Vector3 lowestPoint))
            {
                PerformSnap(gameObject, lowestPoint);
                return;
            }

            // Try mesh second
            if (TryGetMeshLowestPoint(gameObject, out lowestPoint))
            {
                PerformSnap(gameObject, lowestPoint);
                return;
            }

            // Fall back to transform
            SnapObjectWithTransform(gameObject);
        }

        private static void SnapObjectWithCollider(GameObject gameObject)
        {
            if (TryGetColliderLowestPoint(gameObject, out Vector3 lowestPoint))
            {
                PerformSnap(gameObject, lowestPoint);
            }
            else
            {
                Debug.LogWarning($"GameObject {gameObject.name} does not have a collider.", gameObject);
            }
        }

        private static void SnapObjectWithMesh(GameObject gameObject)
        {
            if (TryGetMeshLowestPoint(gameObject, out Vector3 lowestPoint))
            {
                PerformSnap(gameObject, lowestPoint);
            }
            else
            {
                Debug.LogWarning($"GameObject {gameObject.name} does not have a mesh.", gameObject);
            }
        }

        private static void SnapObjectWithTransform(GameObject gameObject)
        {
            Vector3 position = gameObject.transform.position;
            PerformSnap(gameObject, position);
        }

        private static bool TryGetColliderLowestPoint(GameObject gameObject, out Vector3 lowestPoint)
        {
            lowestPoint = Vector3.zero;
            Collider collider = gameObject.GetComponent<Collider>();
            if (!collider) return false;

            lowestPoint = collider.bounds.min;
            return true;
        }

        private static bool TryGetMeshLowestPoint(GameObject gameObject, out Vector3 lowestPoint)
        {
            lowestPoint = Vector3.zero;
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (!meshFilter || !meshFilter.sharedMesh) return false;

            Bounds bounds = meshFilter.sharedMesh.bounds;
            lowestPoint = gameObject.transform.TransformPoint(bounds.min);
            return true;
        }

        private static void PerformSnap(GameObject gameObject, Vector3 lowestPoint)
        {
            RaycastHit hit;
            if (Physics.Raycast(lowestPoint + Vector3.up * 0.1f, Vector3.down, out hit))
            {
                Undo.RecordObject(gameObject.transform, UNDO_GROUP_NAME);
                float distanceToMoveDown = Vector3.Distance(lowestPoint, hit.point);
                gameObject.transform.position -= Vector3.up * distanceToMoveDown;
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            else
            {
                Debug.LogWarning($"Could not find ground below {gameObject.name}.", gameObject);
            }
        }
    }
}