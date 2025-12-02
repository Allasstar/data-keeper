using UnityEngine;

namespace DataKeeper.Between
{
    public static class Tween
    {
        // Transform
        public static TransformPosition Move(Transform target)
        {
            return new TransformPosition(target);
        }
        
        public static TransformLocalPosition MoveLocal(Transform target)
        {
            return new TransformLocalPosition(target);
        }
        
        public static TransformScale Scale(Transform target)
        {
            return new TransformScale(target);
        }
        
        public static TransformEuler RotateEuler(Transform target)
        {
            return new TransformEuler(target);
        }
        
        public static TransformQuaternion RotateQuaternion(Transform target)
        {
            return new TransformQuaternion(target);
        }
        
        // RectTransform
        public static RectTransformPosition Move(RectTransform target)
        {
            return new RectTransformPosition(target);
        }
    }
    
    public enum LoopType
    {
        None = 0,
        Restart = 1, 
        PingPong = 2,
        Incremental = 3
    }
}