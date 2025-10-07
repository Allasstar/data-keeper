using UnityEngine;

namespace DataKeeper.Attributes
{
    public class PreviewAttribute : PropertyAttribute
    {
        public float PreviewSize { get; private set; }
        public float Padding { get; private set; }
        public bool AllowSceneObjects { get; private set; }

        public PreviewAttribute(float previewSize = 64f, float padding = 2f, bool allowSceneObjects = false)
        {
            PreviewSize = previewSize;
            Padding = padding;
            AllowSceneObjects = allowSceneObjects;
        }
    }
}