using System.Collections.Generic;
using System.Linq;
using DataKeeper.Base;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    public class GameTagRegistry : SO
    {
        private const string DEFAULT_REGISTRY_NAME = "GameTagRegistry";
        
        private static GameTagRegistry _default;
        private static readonly Queue<string> _registrationQueue = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Preload()
        {
            _ = Default;
        }

        private static void ProcessQueue(GameTagRegistry registry)
        {
            if (registry != null)
            {
                while (_registrationQueue.Count > 0)
                {
                    registry.Add(_registrationQueue.Dequeue());
                }
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(registry);
#endif
            }
        }

        public static void RegisterTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            try
            {
                var registry = Default;
                if (registry != null)
                {
                    if (registry.IsExist(tag)) return;
                    registry.Add(tag);
                    Debug.Log($"[GameTagRegistry] 1> '{tag}' register tag.");
                    
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(registry);
#endif
                }
                else
                {
                    if (!_registrationQueue.Contains(tag))
                    {
                        Debug.Log($"[GameTagRegistry] 2> '{tag}' attempting to register tag.");
                        _registrationQueue.Enqueue(tag);
                    }
                }
            }
            catch (UnityException)
            {
                if (!_registrationQueue.Contains(tag))
                {
                    Debug.Log($"[GameTagRegistry] 3> '{tag}'attempting to register tag.");
                    _registrationQueue.Enqueue(tag);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameTagRegistry] Unexpected error during registration of '{tag}': {e.Message}");
            }
        }

        public static GameTagRegistry Default 
        {
            get
            {
                if (_default == null)
                {
                    try
                    {
                        _default = Resources.Load<GameTagRegistry>(DEFAULT_REGISTRY_NAME);
                    }
                    catch (UnityException e)
                    {
                        Debug.LogWarning($"[GameTagRegistry] Failed to load default registry from Resources: {e.Message}");
                    }
                }
                
                if (_default == null)
                {
                    try
                    {
                        _default = CreateInstance<GameTagRegistry>();
                    }
                    catch (UnityException e)
                    {
                        Debug.LogWarning($"[GameTagRegistry] Failed to create instance of registry: {e.Message}");
                        return null;
                    }
                    
                    const string path = "Assets/Resources";

#if UNITY_EDITOR
                    if (!UnityEditor.AssetDatabase.IsValidFolder(path))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    
                    UnityEditor.AssetDatabase.CreateAsset(_default, $"{path}/{GameTagRegistry.DEFAULT_REGISTRY_NAME}.asset");
                    UnityEditor.AssetDatabase.SaveAssets();
#endif
                }
                
                return _default;
            }
        }
        
        [SerializeField] private List<string> _tags = new();
        public IReadOnlyList<string> Tags => _tags;

        public bool IsExist(GameTag tag) => _tags.Contains(tag.Value);
        public bool IsExist(string tag) => _tags.Contains(tag);
        public IEnumerable<string> GetChildren(string parent) => _tags.Where(t => t.StartsWith(parent + GameTag.SEPARATOR));

        public override void Initialize()
        {
            Refresh();
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            Refresh();
        }
#endif

        private void Refresh()
        {
            _default = this;
            ProcessQueue(this);
        }
        
        public void Add(string newTag)
        {
            if(IsExist(newTag)) return;
            _tags.Add(newTag);
        }
    }
}