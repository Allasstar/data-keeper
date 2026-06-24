using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DataKeeper.Editor.Utility
{
    /// <summary>
    /// Resolves and caches the custom Editor icon assigned to a type's MonoScript.
    ///
    /// Design goals (see drawer history):
    /// - Synchronous <see cref="GetIcon"/> never touches AssetDatabase search. It returns the
    ///   shared generic script icon immediately and queues the real lookup, so building a
    ///   dropdown of thousands of types stays at ~1-2s instead of minutes.
    /// - The expensive Type -> MonoScript resolution runs incrementally on the main thread
    ///   (AssetDatabase is main-thread only) a few items per editor tick, so it never freezes.
    /// - Resolved "Type -> MonoScript GUID" is persisted in the Library folder, so later sessions
    ///   skip the search entirely. Only types that actually HAVE a custom icon keep a Texture
    ///   entry in memory; every plain script shares the single generic icon (no per-type dict bloat).
    ///
    /// Reusable as a general building block: call <see cref="GetIcon"/> from any editor UI.
    /// </summary>
    [FilePath("Library/DataKeeper/ScriptIconCache.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ScriptIconCache : ScriptableSingleton<ScriptIconCache>, ISerializationCallbackReceiver
    {
        private const string NO_CUSTOM_ICON = ""; // sentinel: resolved, type has no custom script icon
        private const int RESOLVES_PER_TICK = 8;

        // Persisted as parallel lists (Unity can't serialize Dictionary directly).
        [SerializeField] private List<string> _typeKeys = new List<string>();
        [SerializeField] private List<string> _guids = new List<string>();

        // type AssemblyQualifiedName -> MonoScript GUID ("" = resolved, no custom icon)
        private Dictionary<string, string> _guidByType = new Dictionary<string, string>();

        // Only types that actually have a custom icon. Generic-icon types are NOT stored here.
        private readonly Dictionary<Type, Texture2D> _customIcons = new Dictionary<Type, Texture2D>();

        private readonly Queue<Type> _queue = new Queue<Type>();
        private readonly HashSet<Type> _queued = new HashSet<Type>();
        private bool _ticking;

        private static Texture2D s_Generic;
        public static Texture2D GenericIcon =>
            s_Generic != null ? s_Generic : (s_Generic = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D);

        /// <summary>
        /// Returns the type's custom script icon if known, otherwise the generic icon now and
        /// resolves the real one in the background (call again / repaint to pick it up).
        /// </summary>
        public static Texture2D GetIcon(Type type) => instance.Resolve(type);

        private Texture2D Resolve(Type type)
        {
            if (type == null) return null;

            if (_customIcons.TryGetValue(type, out Texture2D cached) && cached != null)
                return cached;

            string aqn = type.AssemblyQualifiedName;
            if (aqn != null && _guidByType.TryGetValue(aqn, out string guid))
            {
                if (string.IsNullOrEmpty(guid))
                    return GenericIcon; // known: this type has no custom icon

                Texture2D tex = IconFromGuid(guid);
                if (tex != null)
                {
                    _customIcons[type] = tex;
                    return tex;
                }

                _guidByType.Remove(aqn); // stale guid (asset moved/removed) -> re-resolve
            }

            Enqueue(type);
            return GenericIcon;
        }

        private void Enqueue(Type type)
        {
            if (!_queued.Add(type)) return;
            _queue.Enqueue(type);

            if (!_ticking)
            {
                _ticking = true;
                EditorApplication.update += Tick;
            }
        }

        private void Tick()
        {
            bool changed = false;
            int processed = 0;

            while (processed++ < RESOLVES_PER_TICK && _queue.Count > 0)
            {
                Type type = _queue.Dequeue();
                _queued.Remove(type);
                changed |= ResolveNow(type);
            }

            if (changed)
                InternalEditorUtility.RepaintAllViews(); // stream icons into open inspectors

            if (_queue.Count == 0)
            {
                _ticking = false;
                EditorApplication.update -= Tick;
                Save(true);
            }
        }

        // The one slow path (AssetDatabase search), run only for queued candidate types,
        // a few per tick. Persists the result so it never runs again for this type.
        private bool ResolveNow(Type type)
        {
            string aqn = type.AssemblyQualifiedName;
            if (aqn == null) return false;

            string guid = NO_CUSTOM_ICON;
            Texture2D icon = null;

            MonoScript script = FindMonoScript(type);
            if (script != null)
            {
                icon = EditorGUIUtility.GetIconForObject(script) as Texture2D;
                if (icon != null)
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
            }

            _guidByType[aqn] = guid;
            if (icon != null) _customIcons[type] = icon;
            return icon != null;
        }

        private static Texture2D IconFromGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;

            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            return script != null ? EditorGUIUtility.GetIconForObject(script) as Texture2D : null;
        }

        private static MonoScript FindMonoScript(Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type) return script;
            }
            return null;
        }

        /// <summary>Forget a type so its icon is re-resolved on next request.</summary>
        private void Invalidate(Type type)
        {
            if (type == null) return;
            _customIcons.Remove(type);
            string aqn = type.AssemblyQualifiedName;
            if (aqn != null) _guidByType.Remove(aqn);
        }

        public void OnBeforeSerialize()
        {
            _typeKeys.Clear();
            _guids.Clear();
            foreach (KeyValuePair<string, string> kv in _guidByType)
            {
                _typeKeys.Add(kv.Key);
                _guids.Add(kv.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            _guidByType = new Dictionary<string, string>(_typeKeys.Count);
            for (int i = 0; i < _typeKeys.Count && i < _guids.Count; i++)
                _guidByType[_typeKeys[i]] = _guids[i];
        }

        // Keep icons correct within a session: when a script is re-imported (e.g. its custom
        // icon changed), drop the cached entry so it re-resolves on next use.
        private class IconChangeWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                bool any = false;
                foreach (string path in imported)
                {
                    if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;

                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    Type type = script != null ? script.GetClass() : null;
                    if (type == null) continue;

                    instance.Invalidate(type);
                    any = true;
                }

                if (any)
                    InternalEditorUtility.RepaintAllViews();
            }
        }
    }
}
