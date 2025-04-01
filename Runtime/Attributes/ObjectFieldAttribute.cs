using UnityEngine;

namespace DataKeeper.Attributes
{
    public class ObjectFieldAttribute : PropertyAttribute
    {
        public bool AllowSceneObjects { get; set; }

        public ObjectFieldAttribute(bool allowSceneObjects = false)
        {
            AllowSceneObjects = allowSceneObjects;
        }
    }
}