using UnityEngine;

namespace DataKeeper.Attributes
{
    public class SpritePreviewAttribute : PropertyAttribute
    {
        public float height;

        public SpritePreviewAttribute(float height = 50)
        {
            this.height = height;
        }
    }
}