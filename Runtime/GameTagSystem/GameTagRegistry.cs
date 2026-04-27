using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using DataKeeper.Base;
using UnityEngine;

namespace DataKeeper.GameTagSystem
{
    [CreateAssetMenu(menuName = "DataKeeper/GameTagRegistry", fileName = "GameTagRegistry", order = 1000)]
    public class GameTagRegistry : SO
    {
        private const string DEFAULT_REGISTRY_NAME = "GameTagRegistry";

        private static GameTagRegistry _default;
        private static readonly Queue<string> _registrationQueue = new();

        [SerializeField] private List<string> _tags = new();

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
                    }
                }
                
                return _default;
            }
        }

        public IReadOnlyList<string> Tags => _tags;

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
                    registry.Add(tag);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(registry);
#endif
                }
                else
                {
                    if (!_registrationQueue.Contains(tag))
                        _registrationQueue.Enqueue(tag);
                }
            }
            catch (UnityException)
            {
                if (!_registrationQueue.Contains(tag))
                    _registrationQueue.Enqueue(tag);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameTagRegistry] Unexpected error during registration of '{tag}': {e.Message}");
            }
        }

        public bool IsExist(GameTag tag) => _tags.Contains(tag.Value) || _tags.Any(t => t.StartsWith(tag.Value + GameTag.SEPARATOR));
        public bool IsExist(string tag) => _tags.Contains(tag) || _tags.Any(t => t.StartsWith(tag + GameTag.SEPARATOR));
        
        public IEnumerable<string> GetChildren(string parent) => _tags.Where(t => t.StartsWith(parent + GameTag.SEPARATOR));

        public override void Initialize()
        {
            Refresh();
        }

        [Button]
        private void Refresh()
        {
            _default = this;
            Prune();
            ProcessQueue(this);
        }

        // Removes any tag that is a prefix of another tag in the list.
        private void Prune()
        {
            // Trim trailing separators from all existing tags
            _tags = _tags.Distinct().ToList(); // Remove duplicates before trimming

            for (int i = 0; i < _tags.Count; i++)
                _tags[i] = _tags[i].TrimEnd(GameTag.SEPARATOR[0]);

            // Remove empty entries that may result from trimming
            _tags.RemoveAll(string.IsNullOrEmpty);

            // Remove any tag that is a redundant prefix of another
            _tags.RemoveAll(t => _tags.Any(other => other != t && other.StartsWith(t + GameTag.SEPARATOR)));
        }

        public void Add(string newTag)
        {
            if (string.IsNullOrEmpty(newTag)) return;

            // Trim trailing separators (e.g. "Damage/" -> "Damage")
            newTag = newTag.TrimEnd(GameTag.SEPARATOR[0]);

            if (string.IsNullOrEmpty(newTag)) return;

            // A more-specific tag already exists — newTag is a redundant prefix, skip it
            if (_tags.Any(t => t.StartsWith(newTag + GameTag.SEPARATOR)))
                return;

            // newTag is more specific — remove any existing prefix tags it supersedes
            _tags.RemoveAll(t => newTag.StartsWith(t + GameTag.SEPARATOR));

            if (!_tags.Contains(newTag))
                _tags.Add(newTag);
        }
    }
}