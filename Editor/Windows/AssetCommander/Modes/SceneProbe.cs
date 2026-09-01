using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // What the index cannot answer. A scene side's objects are live GameObjects, not records,
    // so its modes read them through SerializedObject — the same walk the Inspector does, and
    // the only way to see a reference that no longer resolves.
    public static class SceneProbe
    {
        // Hard ceiling on how much of a field list ends up in one badge; the rest is a count.
        private const int MaxNamedFields = 2;

        public static List<GameObject> AllObjects(Scene scene)
        {
            var result = new List<GameObject>();
            if (!scene.IsValid() || !scene.isLoaded) return result;

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);

            foreach (var root in roots) Collect(root.transform, result);

            return result;
        }

        private static void Collect(Transform transform, List<GameObject> into)
        {
            into.Add(transform.gameObject);

            for (int i = 0; i < transform.childCount; i++) Collect(transform.GetChild(i), into);
        }

        // The chain above the object, which is what a flat result list has lost. The object's
        // own name is already the row's title, so it is not repeated here.
        public static string HierarchyPath(GameObject gameObject)
        {
            var parent = gameObject == null ? null : gameObject.transform.parent;
            if (parent == null) return "Scene root";

            var path = parent.name;
            for (var next = parent.parent; next != null; next = next.parent) path = next.name + "/" + path;

            return path;
        }

        public static int MissingScriptCount(GameObject gameObject) =>
            gameObject == null ? 0 : GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        // A reference Unity could not resolve reads as null while still carrying the instance id
        // it was serialized with — that gap is the whole test, and there is no other way to tell
        // a broken reference from a field the user deliberately left empty.
        public static string DescribeMissingReferences(GameObject gameObject)
        {
            if (gameObject == null) return null;

            var named = new List<string>(MaxNamedFields);
            int total = 0;
            int count = gameObject.GetComponentCount();

            for (int i = 0; i < count; i++)
            {
                var component = gameObject.GetComponentAtIndex(i);
                if (component == null) continue;

                var type = component.GetType().Name;

                using var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue != null) continue;
#if UNITY_6000_5_OR_NEWER
                    if (property.objectReferenceEntityIdValue == EntityId.None) continue;
#else
                    if (property.objectReferenceInstanceIDValue == 0) continue;
#endif

                    total++;
                    if (named.Count < MaxNamedFields) named.Add(type + "." + property.displayName);
                }
            }

            if (total == 0) return null;

            var text = string.Join(", ", named);
            return total > named.Count ? text + " +" + (total - named.Count) : text;
        }

        // Guids of the project assets this object's own components point at. m_Script is skipped
        // for the same reason the reference badge skips it: every MonoBehaviour references its
        // own MonoScript, so counting it would match any side that holds a script.
        public static void CollectAssetGuids(GameObject gameObject, HashSet<string> into)
        {
            if (gameObject == null) return;

            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab != null) Add(into, prefab);

            int count = gameObject.GetComponentCount();
            for (int i = 0; i < count; i++)
            {
                var component = gameObject.GetComponentAtIndex(i);
                if (component == null) continue;

                using var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.propertyPath == "m_Script") continue;

                    var reference = property.objectReferenceValue;
                    if (reference == null || !EditorUtility.IsPersistent(reference)) continue;

                    Add(into, reference);
                }
            }
        }

        private static void Add(HashSet<string> into, Object reference)
        {
            // The local file id is what distinguishes sub-assets inside one file; the side sets
            // are keyed by asset, so only the guid matters here.
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out var guid, out long _))
                into.Add(guid);
        }
    }
}
