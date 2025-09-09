using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DataKeeper.Editor.MenuItems
{
    public class SnapToGroundEditor : UnityEditor.Editor
    {
        /*
            % = Ctrl (Cmd on Mac)
            # = Shift
            & = Alt
            _ = No modifier (just the key)
         */
        
        private const string UNDO_GROUP_NAME = "Snap to Ground";
        private const float RAYCAST_MAX_DISTANCE = 1000f;
        
        // Static field to track mouse snap mode
        private static bool isMouseSnapModeActive = false;

        [MenuItem("Tools/Snap/To Ground (Auto) %g", priority = 4)]
        private static void SnapToGroundAuto()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectToGround(gameObject);
            }
        }

        [MenuItem("Tools/Snap/To Ground (Collider) _home", priority = 5)]
        private static void SnapToGroundCollider()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectWithCollider(gameObject);
            }
        }

        [MenuItem("Tools/Snap/To Ground (Mesh) _end", priority = 6)]
        private static void SnapToGroundMesh()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectWithMesh(gameObject);
            }
        }

        [MenuItem("Tools/Snap/To Ground (Transform) _pgdn", priority = 7)]
        private static void SnapToGroundTransform()
        {
            Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SnapObjectWithTransform(gameObject);
            }
        }

        [MenuItem("Tools/Snap/To Ground From Mouse %&g", priority = 1)]
        private static void SnapToGroundFromMouse()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("No objects selected for snapping.");
                return;
            }

            Vector2 mousePos = Event.current?.mousePosition ?? Vector2.zero;
            mousePos.y -= 50; // Adjust for GUI space
            
            if (mousePos == Vector2.zero && SceneView.lastActiveSceneView != null)
            {
                Debug.Log("mousePos == Vector2.zero");
                
                mousePos = new Vector2(
                    SceneView.lastActiveSceneView.position.width * 0.5f, 
                    SceneView.lastActiveSceneView.position.height * 0.5f);
            }

            PerformMouseSnap(mousePos);
        }

        private static void PerformMouseSnap(Vector2 mousePosition)
        {
            if (Selection.gameObjects.Length == 0) return;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            // Get all selected objects and their children's colliders to ignore
            List<Collider> collidersToIgnore = GetAllCollidersInHierarchy(Selection.gameObjects);
            
            // Temporarily disable colliders
            SetCollidersEnabled(collidersToIgnore, false);

            try
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, RAYCAST_MAX_DISTANCE * 10))
                {
                    Undo.SetCurrentGroupName(UNDO_GROUP_NAME);
                    
                    foreach (GameObject gameObject in Selection.gameObjects)
                    {
                        Undo.RecordObject(gameObject.transform, UNDO_GROUP_NAME);
                        
                        // Move object to hit point, but adjust for object bounds
                        if (TryGetObjectLowestPoint(gameObject, out Vector3 lowestPoint))
                        {
                            float offsetY = gameObject.transform.position.y - lowestPoint.y;
                            gameObject.transform.position = new Vector3(hit.point.x, hit.point.y + offsetY, hit.point.z);
                        }
                        else
                        {
                            gameObject.transform.position = hit.point;
                        }
                    }
                    
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }
                else
                {
                    Debug.LogWarning("Could not find ground at mouse position.");
                }
            }
            finally
            {
                // Re-enable colliders
                SetCollidersEnabled(collidersToIgnore, true);
            }
        }
        
        private static void SnapObjectToGround(GameObject gameObject)
        {
            // Try collider first
            if (TryGetColliderLowestPoint(gameObject, out Vector3 lowestPoint))
            {
                PerformSnapWithColliderIgnoring(gameObject, lowestPoint);
                return;
            }

            // Try mesh second
            if (TryGetMeshLowestPoint(gameObject, out lowestPoint))
            {
                PerformSnapWithColliderIgnoring(gameObject, lowestPoint);
                return;
            }

            // Fall back to transform
            SnapObjectWithTransform(gameObject);
        }

        private static void SnapObjectWithCollider(GameObject gameObject)
        {
            if (TryGetColliderLowestPoint(gameObject, out Vector3 lowestPoint))
            {
                PerformSnapWithColliderIgnoring(gameObject, lowestPoint);
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
                PerformSnapWithColliderIgnoring(gameObject, lowestPoint);
            }
            else
            {
                Debug.LogWarning($"GameObject {gameObject.name} does not have a mesh.", gameObject);
            }
        }

        private static void SnapObjectWithTransform(GameObject gameObject)
        {
            Vector3 position = gameObject.transform.position;
            PerformSnapWithColliderIgnoring(gameObject, position);
        }

        private static bool TryGetColliderLowestPoint(GameObject gameObject, out Vector3 lowestPoint)
        {
            lowestPoint = Vector3.zero;
            
            // Get all colliders in the hierarchy
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) return false;

            // Find the lowest point among all colliders
            float lowestY = float.MaxValue;
            bool foundAny = false;

            foreach (Collider collider in colliders)
            {
                if (collider.bounds.min.y < lowestY)
                {
                    lowestY = collider.bounds.min.y;
                    lowestPoint = new Vector3(collider.bounds.center.x, collider.bounds.min.y, collider.bounds.center.z);
                    foundAny = true;
                }
            }

            return foundAny;
        }

        private static bool TryGetMeshLowestPoint(GameObject gameObject, out Vector3 lowestPoint)
        {
            lowestPoint = Vector3.zero;
            
            // Get all mesh renderers in the hierarchy
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length == 0) return false;

            // Find the lowest point among all mesh renderers
            float lowestY = float.MaxValue;
            bool foundAny = false;

            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (renderer.bounds.min.y < lowestY)
                {
                    lowestY = renderer.bounds.min.y;
                    lowestPoint = new Vector3(renderer.bounds.center.x, renderer.bounds.min.y, renderer.bounds.center.z);
                    foundAny = true;
                }
            }

            return foundAny;
        }

        private static bool TryGetObjectLowestPoint(GameObject gameObject, out Vector3 lowestPoint)
        {
            // Try collider first
            if (TryGetColliderLowestPoint(gameObject, out lowestPoint))
            {
                return true;
            }

            // Try mesh second
            if (TryGetMeshLowestPoint(gameObject, out lowestPoint))
            {
                return true;
            }

            // Fall back to transform
            lowestPoint = gameObject.transform.position;
            return true;
        }

        private static void PerformSnapWithColliderIgnoring(GameObject gameObject, Vector3 lowestPoint)
        {
            // Get all colliders that should be ignored (selected objects and their children)
            List<Collider> collidersToIgnore = GetAllCollidersInHierarchy(new GameObject[] { gameObject });
            
            // Temporarily disable colliders
            SetCollidersEnabled(collidersToIgnore, false);

            try
            {
                // Calculate raycast start position (highest bound point)
                Vector3 raycastStart = GetHighestBoundPoint(gameObject);
                
                RaycastHit hit;
                if (Physics.Raycast(raycastStart, Vector3.down, out hit, RAYCAST_MAX_DISTANCE))
                {
                    Undo.RecordObject(gameObject.transform, UNDO_GROUP_NAME);
                    
                    // Calculate how much to move the object so its lowest point touches the ground
                    float distanceToMove = lowestPoint.y - hit.point.y;
                    gameObject.transform.position -= Vector3.up * distanceToMove;
                    
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }
                else
                {
                    Debug.LogWarning($"Could not find ground below {gameObject.name}.", gameObject);
                }
            }
            finally
            {
                // Re-enable colliders
                SetCollidersEnabled(collidersToIgnore, true);
            }
        }

        private static Vector3 GetHighestBoundPoint(GameObject gameObject)
        {
            Vector3 center = gameObject.transform.position;
            float highestY = gameObject.transform.position.y;

            // Check colliders
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider.bounds.max.y > highestY)
                {
                    highestY = collider.bounds.max.y;
                    center = new Vector3(
                        collider.bounds.center.x, 
                        collider.bounds.max.y, 
                        collider.bounds.center.z);
                }
            }

            // Check mesh renderers if no colliders found
            if (colliders.Length == 0)
            {
                MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in meshRenderers)
                {
                    if (renderer.bounds.max.y > highestY)
                    {
                        highestY = renderer.bounds.max.y;
                        center = new Vector3(
                            renderer.bounds.center.x, 
                            renderer.bounds.max.y, 
                            renderer.bounds.center.z);
                    }
                }
            }
            
            return center;
        }

        private static List<Collider> GetAllCollidersInHierarchy(GameObject[] gameObjects)
        {
            List<Collider> allColliders = new List<Collider>();
            
            foreach (GameObject gameObject in gameObjects)
            {
                Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
                allColliders.AddRange(colliders.Where(w => w.enabled));
            }
            
            return allColliders;
        }

        private static void SetCollidersEnabled(List<Collider> colliders, bool enabled)
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
        }
    }
}