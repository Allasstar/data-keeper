﻿using UnityEngine;

namespace DataKeeper.Between
{
    public static class Tween
    {
        public static MoveTransform<Transform> Move(Transform target)
        {
            return new MoveTransform<Transform>(target);
        }

        public static MoveTransform<RectTransform> Move(RectTransform target)
        {
            return new MoveTransform<RectTransform>(target);
        }
    }
    
    public enum LoopType
    {
        None = 0,
        Restart = 1, 
        PingPong = 2,
        Incremental = 3
    }
    
    public enum EaseType
    {
        Linear = 0,
        EaseInQuad = 1,
        EaseOutQuad = 2,
        EaseInOutQuad = 3,
        EaseInCubic = 4,
        EaseOutCubic = 5,
        EaseInOutCubic = 6,
        EaseInQuart = 7,
        EaseOutQuart = 8,
        EaseInOutQuart = 9,
        EaseInQuint = 10,
        EaseOutQuint = 11,
        EaseInOutQuint = 12,
        EaseInSine = 13,
        EaseOutSine = 14,
        EaseInOutSine = 15,
        EaseInExpo = 16,
        EaseOutExpo = 17,
        EaseInOutExpo = 18,
        EaseInCirc = 19,
        EaseOutCirc = 20,
        EaseInOutCirc = 21,
        EaseInBounce = 22,
        EaseOutBounce = 23,
        EaseInOutBounce = 24,
        EaseInBack = 25,
        EaseOutBack = 26,
        EaseInOutBack = 27,
        EaseInElastic = 28,
        EaseOutElastic = 29,
        EaseInOutElastic = 30
    }
}