using System;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Fluent API helper for building tween sequences
    /// </summary>
    public static class BeeTweenBuilder
    {
        /// <summary>
        /// Start building a sequence for a GameObject
        /// </summary>
        public static BeeTweenSequenceBuilder<Transform> CreateGameObjectSequence(Transform target)
        {
            return new BeeTweenSequenceBuilder<Transform>(target);
        }

        /// <summary>
        /// Start building a sequence for an Image
        /// </summary>
        public static BeeTweenSequenceBuilder<Image> CreateImageSequence(Image target)
        {
            return new BeeTweenSequenceBuilder<Image>(target);
        }

        /// <summary>
        /// Start building a sequence for a RectTransform
        /// </summary>
        public static BeeTweenSequenceBuilder<RectTransform> CreateRectTransformSequence(RectTransform target)
        {
            return new BeeTweenSequenceBuilder<RectTransform>(target);
        }
    }

    /// <summary>
    /// Fluent builder for creating tween sequences
    /// </summary>
    public class BeeTweenSequenceBuilder<T> where T : class
    {
        private readonly T _target;
        private readonly System.Collections.Generic.List<IBeeTweenNode> _nodes = new();

        public BeeTweenSequenceBuilder(T target)
        {
            _target = target;
        }

        /// <summary>
        /// Add a delay to the sequence
        /// </summary>
        public BeeTweenSequenceBuilder<T> Delay(float duration)
        {
            _nodes.Add(new DelayNode { Duration = duration });
            return this;
        }

        /// <summary>
        /// Add a move animation (GameObject only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> Move(Vector3 targetPosition, float duration, AnimationCurve easing = null)
        {
            if (_target is not GameObject)
                throw new InvalidOperationException("Move is only available for GameObject targets");

            _nodes.Add(new MoveNode
            {
                TargetPosition = targetPosition,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Add a rotation animation (GameObject only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> Rotate(Quaternion targetRotation, float duration, AnimationCurve easing = null)
        {
            if (_target is not GameObject)
                throw new InvalidOperationException("Rotate is only available for GameObject targets");

            _nodes.Add(new RotateNode
            {
                TargetRotation = targetRotation,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Add a scale animation (GameObject only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> Scale(Vector3 targetScale, float duration, AnimationCurve easing = null)
        {
            if (_target is not GameObject)
                throw new InvalidOperationException("Scale is only available for GameObject targets");

            _nodes.Add(new ScaleNode
            {
                TargetScale = targetScale,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Add a fade animation (Image only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> Fade(float targetAlpha, float duration, AnimationCurve easing = null)
        {
            if (_target is not Image)
                throw new InvalidOperationException("Fade is only available for Image targets");

            _nodes.Add(new FadeNode
            {
                TargetAlpha = targetAlpha,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Add a color animation (Image only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> Color(Color targetColor, float duration, AnimationCurve easing = null)
        {
            if (_target is not Image)
                throw new InvalidOperationException("Color is only available for Image targets");

            _nodes.Add(new ColorNode
            {
                TargetColor = targetColor,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Add an anchor position animation (RectTransform only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> AnchorPosition(Vector2 targetPosition, float duration, AnimationCurve easing = null)
        {
            if (_target is not RectTransform)
                throw new InvalidOperationException("AnchorPosition is only available for RectTransform targets");

            _nodes.Add(new AnchorPositionNode
            {
                TargetPosition = targetPosition,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Add a size delta animation (RectTransform only)
        /// </summary>
        public BeeTweenSequenceBuilder<T> SizeDelta(Vector2 targetSize, float duration, AnimationCurve easing = null)
        {
            if (_target is not RectTransform)
                throw new InvalidOperationException("SizeDelta is only available for RectTransform targets");

            _nodes.Add(new SizeDeltaNode
            {
                TargetSize = targetSize,
                Duration = duration,
                EaseCurve = easing ?? AnimationCurve.Linear(0, 0, 1, 1)
            });
            return this;
        }

        /// <summary>
        /// Build the final sequence and context
        /// </summary>
        public IBeeTweenContext Build()
        {
            if (_nodes.Count == 0)
                throw new InvalidOperationException("Cannot build an empty sequence");

            var rootNode = _nodes.Count == 1 ? _nodes[0] : new SequenceNode(_nodes.ToArray());

            return _target switch
            {
                RectTransform rt => new RectTransformContext(rt, rootNode),
                Transform tr => new TransformContext(tr, rootNode),
                Image img => new ImageContext(img, rootNode),
                _ => throw new InvalidOperationException($"Unsupported target type: {_target.GetType().Name}")
            };
        }
    }

    /// <summary>
    /// Extension methods for common tween operations
    /// </summary>
    public static class BeeTweenExtensions
    {
        /// <summary>
        /// Animate a GameObject's position
        /// </summary>
        public static void AnimatePosition(this Transform target, Vector3 targetPosition, float duration, BeeTweenPlayer player)
        {
            var context = new TransformContext(target, new MoveNode { TargetPosition = targetPosition, Duration = duration });
            player.Context = context;
        }

        /// <summary>
        /// Animate an Image's fade
        /// </summary>
        public static void AnimateFade(this Image target, float targetAlpha, float duration, BeeTweenPlayer player)
        {
            var context = new ImageContext(target, new FadeNode { TargetAlpha = targetAlpha, Duration = duration });
            player.Context = context;
        }

        /// <summary>
        /// Animate a RectTransform's position
        /// </summary>
        public static void AnimateAnchorPosition(this RectTransform target, Vector2 targetPosition, float duration, BeeTweenPlayer player)
        {
            var context = new RectTransformContext(target, new AnchorPositionNode { TargetPosition = targetPosition, Duration = duration });
            player.Context = context;
        }
    }
}

