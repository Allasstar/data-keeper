using DataKeeper.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DataKeeper.Extra
{
    [AddComponentMenu("DataKeeper/Extra/Hide Flags Menu"), DisallowMultipleComponent]
    public class HideFlagsMenu : MonoBehaviour
    {
        [field: SerializeField] public HideFlagsTarget HideFlagsTarget { get; private set; } = HideFlagsTarget.Self;
        [field: SerializeField] public HideFlags HideFlags { get; private set; } = HideFlags.None;
        
        public HideFlagsMenu SetHideFlagsTarget(HideFlagsTarget hideFlagsTarget)
        {
            HideFlagsTarget = hideFlagsTarget;
            return this;
        }
        
        public HideFlagsMenu SetHideFlags(HideFlags hideFlags)
        {
            HideFlags = hideFlags;
            return this;
        }

        public void Apply()
        {
            switch (HideFlagsTarget)
            {
                case HideFlagsTarget.Self:
                    ApplyToGameObject(gameObject);
                    break;
                
                case HideFlagsTarget.Children:
                    ApplyToChildren(gameObject);
                    break;
                
                case HideFlagsTarget.SelfAndChildren:
                    ApplyToChildren(gameObject);
                    ApplyToGameObject(gameObject);
                    break;
            }
        }

        private void ApplyToGameObject(GameObject target)
        {
            target.hideFlags = HideFlags;
        }

        private void ApplyToChildren(GameObject parent)
        {
            Transform transform = parent.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                ApplyToGameObject(child.gameObject);
                
                // Recursively apply to all descendants
                if (child.childCount > 0)
                {
                    ApplyToChildren(child.gameObject);
                }
            }
        }

        [Button("Apply", 10)]
        private void ApplyHideFlags()
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(GetAffectedObjects(), "Apply Hide Flags");
#endif
            Apply();
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

#if UNITY_EDITOR
        private Object[] GetAffectedObjects()
        {
            System.Collections.Generic.List<Object> objects = new System.Collections.Generic.List<Object>();
            
            switch (HideFlagsTarget)
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
