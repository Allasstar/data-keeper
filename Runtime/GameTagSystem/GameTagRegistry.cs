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
        public static GameTagRegistry Default 
        {
            get
            {
                if (_default == null)
                {
                    _default = Resources.Load<GameTagRegistry>(DEFAULT_REGISTRY_NAME);
                }
                
                if (_default == null)
                {
                    _default = CreateInstance<GameTagRegistry>();
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
            _default = this;
        }
        
        public void Add(string newTag)
        {
            if(IsExist(newTag)) return;
            _tags.Add(newTag);
        }
    }
}