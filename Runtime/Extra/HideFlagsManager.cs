using DataKeeper.Attributes;
using UnityEngine;

namespace DataKeeper.Extra
{
    [AddComponentMenu("DataKeeper/Extra/Hide Flags Manager"), DisallowMultipleComponent]
    public class HideFlagsManager : MonoBehaviour
    {
        [field: SerializeField] public HideFlagsTarget FlagsTarget { get; private set; } = HideFlagsTarget.Self;
        [field: SerializeField] public HideFlags Flags { get; private set; } = HideFlags.None;
        
        public HideFlagsManager SetHideFlagsTarget(HideFlagsTarget hideFlagsTarget)
        {
            FlagsTarget = hideFlagsTarget;
            return this;
        }
        
        public HideFlagsManager SetHideFlags(HideFlags hideFlags)
        {
            Flags = hideFlags;
            return this;
        }

        public HideFlagsManager Apply()
        {
            switch (FlagsTarget)
            {
                case HideFlagsTarget.Self:
                    ApplyToGameObject(gameObject, Flags);
                    break;
                
                case HideFlagsTarget.Children:
                    ApplyToChildren(gameObject, Flags);
                    break;
                
                case HideFlagsTarget.SelfAndChildren:
                    ApplyToChildren(gameObject, Flags);
                    ApplyToGameObject(gameObject, Flags);
                    break;
            }
            
            return this;
        }

        public HideFlagsManager Reset()
        {
            switch (FlagsTarget)
            {
                case HideFlagsTarget.Self:
                    ApplyToGameObject(gameObject, HideFlags.None);
                    break;
                
                case HideFlagsTarget.Children:
                    ApplyToChildren(gameObject, HideFlags.None);
                    break;
                
                case HideFlagsTarget.SelfAndChildren:
                    ApplyToChildren(gameObject, HideFlags.None);
                    ApplyToGameObject(gameObject, HideFlags.None);
                    break;
            }
            
            return this;
        }

        private void ApplyToGameObject(GameObject target, HideFlags hideFlags)
        {
            target.hideFlags = hideFlags;
        }

        private void ApplyToChildren(GameObject parent, HideFlags hideFlags)
        {
            Transform transform = parent.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                ApplyToGameObject(child.gameObject, hideFlags);
                
                // Recursively apply to all descendants
                if (child.childCount > 0)
                {
                    ApplyToChildren(child.gameObject, hideFlags);
                }
            }
        }

        [Button("Apply", 10)]
        private void ApplyHideFlags()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(GetAffectedObjects(), "Apply Hide Flags");
#endif
            Apply();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }
        
        [Button("Reset")]
        private void ResetHideFlags()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(GetAffectedObjects(), "Reset Hide Flags");
#endif
            Reset();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }

#if UNITY_EDITOR
        private Object[] GetAffectedObjects()
        {
            System.Collections.Generic.List<Object> objects = new System.Collections.Generic.List<Object>();
            
            switch (FlagsTarget)
            {
                case HideFlagsTarget.Self:
                    CollectObjectsFromGameObject(gameObject, objects);
                    break;
                
                case HideFlagsTarget.Children:
                    CollectObjectsFromChildren(gameObject, objects);
                    break;
                
                case HideFlagsTarget.SelfAndChildren:
                    CollectObjectsFromGameObject(gameObject, objects);
                    CollectObjectsFromChildren(gameObject, objects);
                    break;
            }
            
            return objects.ToArray();
        }

        private void CollectObjectsFromGameObject(GameObject target, System.Collections.Generic.List<Object> objects)
        {
            objects.Add(target);
            
            Component[] components = target.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component != null)
                {
                    objects.Add(component);
                }
            }
        }

        private void CollectObjectsFromChildren(GameObject parent, System.Collections.Generic.List<Object> objects)
        {
            Transform transform = parent.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                CollectObjectsFromGameObject(child.gameObject, objects);
                
                if (child.childCount > 0)
                {
                    CollectObjectsFromChildren(child.gameObject, objects);
                }
            }
        }
#endif
    }

    public enum HideFlagsTarget
    {
        Self = 0,
        Children = 1,
        SelfAndChildren = 2,
    }
}
