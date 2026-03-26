using System;
using System.Threading;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.BeeTween
{
    /// <summary>
    /// Main player component that executes tween sequences
    /// </summary>
    public class BeeTweenPlayer : MonoBehaviour
    {
        public bool runOnEnable = false;
        public bool waitContext = false;
        [field: SerializeField] public Optional<float> RestartOnFail { get; private set; } = new Optional<float>(1, false);
        
        [SerializeReference, SerializeReferenceSelector]
        public IBeeTweenContext Context;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            if (!runOnEnable) return;
            Play();
        }
        
        public void Play() => _ = RunAsync();
        public void Stop() => _cts?.Cancel();

        public async Awaitable RunAsync()
        {
            _cts = new CancellationTokenSource();

            while (waitContext && (Context == null || !Context.IsValid()))
            {
                await Awaitable.EndOfFrameAsync(_cts.Token);
            }
            
            if (Context?.RootNode == null) return;

            try
            {
                await Context.RootNode.ExecuteAsync(Context, _cts);
            }
            catch (OperationCanceledException)
            {
                /* expected on disable */
            }
            catch (Exception e)
            {
                Debug.LogError(e, this);

                if (RestartOnFail.Enabled)
                {
                    await Awaitable.WaitForSecondsAsync(RestartOnFail.Value, _cts.Token);
                    _ = RunAsync();
                }
            }
        }

        private void OnDisable()
        {
            Stop();
        }
    }
    
    /// <summary>
    /// Base interface for all tween contexts
    /// </summary>
    public interface IBeeTweenContext
    {
        object Target { get; }
        IBeeTweenNode RootNode { get; }
        bool IsValid();
    }
    
    /// <summary>
    /// Generic context interface for type-safe operations
    /// </summary>
    public interface IBeeTweenContext<T> : IBeeTweenContext where T : class
    {
        new T Target { get; }
    }
    
    /// <summary>
    /// Base interface for all tween nodes
    /// </summary>
    public interface IBeeTweenNode
    {
        Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken);
    }
    
    /// <summary>
    /// Generic node interface for type-safe node operations
    /// </summary>
    public interface IBeeTweenNode<T> : IBeeTweenNode where T : class
    {
        new Awaitable ExecuteAsync(IBeeTweenContext<T> context, CancellationTokenSource cancellationToken);
    }

    // ============== Transform CONTEXT ==============
    
    /// <summary>
    /// Context for controlling GameObjects
    /// </summary>
    [Serializable]
    public class TransformContext : IBeeTweenContext<Transform>
    {
        [SerializeField] private Transform target;
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode rootNode;

        public Transform Target => target;
        public IBeeTweenNode RootNode => rootNode;
        object IBeeTweenContext.Target => target;

        public TransformContext() { }
        
        public TransformContext(Transform target, IBeeTweenNode rootNode)
        {
            this.target = target;
            this.rootNode = rootNode;
        }

        public bool IsValid() => target != null && rootNode != null;
    }

    // ============== IMAGE CONTEXT ==============
    
    /// <summary>
    /// Context for controlling UI Images
    /// </summary>
    [Serializable]
    public class ImageContext : IBeeTweenContext<Image>
    {
        [SerializeField] private Image target;
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode rootNode;

        public Image Target => target;
        public IBeeTweenNode RootNode => rootNode;
        object IBeeTweenContext.Target => target;

        public ImageContext() { }
        
        public ImageContext(Image target, IBeeTweenNode rootNode)
        {
            this.target = target;
            this.rootNode = rootNode;
        }

        public bool IsValid() => target != null && rootNode != null;
    }

    // ============== RECTRANSFORM CONTEXT ==============
    
    /// <summary>
    /// Context for controlling RectTransforms (UI elements)
    /// </summary>
    [Serializable]
    public class RectTransformContext : IBeeTweenContext<RectTransform>
    {
        [SerializeField] private RectTransform target;
        [SerializeReference, SerializeReferenceSelector] private IBeeTweenNode rootNode;

        public RectTransform Target => target;
        public IBeeTweenNode RootNode => rootNode;
        object IBeeTweenContext.Target => target;

        public RectTransformContext() { }
        
        public RectTransformContext(RectTransform target, IBeeTweenNode rootNode)
        {
            this.target = target;
            this.rootNode = rootNode;
        }

        public bool IsValid() => target != null && rootNode != null;
    }

    // ============== GAMEOBJECT NODES ==============

    /// <summary>
    /// Sequence node - executes multiple nodes in sequence
    /// </summary>
    [Serializable]
    public class SequenceNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode[] Nodes;

        public SequenceNode() => Nodes = Array.Empty<IBeeTweenNode>();
        
        public SequenceNode(params IBeeTweenNode[] nodes) => Nodes = nodes;

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            foreach (var node in Nodes)
            {
                await node.ExecuteAsync(context, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Parallel node - executes multiple nodes in parallel
    /// </summary>
    [Serializable]
    public class ParallelNode : IBeeTweenNode
    {
        [SerializeReference, SerializeReferenceSelector] public IBeeTweenNode[] Nodes;

        public ParallelNode() => Nodes = Array.Empty<IBeeTweenNode>();
        
        public ParallelNode(params IBeeTweenNode[] nodes) => Nodes = nodes;

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            // Execute all nodes concurrently
            var tasks = new System.Collections.Generic.List<Awaitable>();
            foreach (var node in Nodes)
            {
                tasks.Add(node.ExecuteAsync(context, cancellationToken));
            }
            
            // Wait for all tasks to complete
            foreach (var task in tasks)
            {
                await task;
            }
        }
    }

    /// <summary>
    /// Move node - moves a GameObject from current position to target
    /// </summary>
    [Serializable]
    public class MoveNode : IBeeTweenNode
    {
        public Vector3 TargetPosition;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<GameObject> goContext || goContext.Target == null) return;
            
            var startPosition = goContext.Target.transform.position;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                goContext.Target.transform.position = Vector3.Lerp(startPosition, TargetPosition, easeT);
            }

            goContext.Target.transform.position = TargetPosition;
        }
    }

    /// <summary>
    /// Rotate node - rotates a GameObject
    /// </summary>
    [Serializable]
    public class RotateNode : IBeeTweenNode
    {
        public Quaternion TargetRotation;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<GameObject> goContext || goContext.Target == null) return;

            var startRotation = goContext.Target.transform.rotation;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                goContext.Target.transform.rotation = Quaternion.Lerp(startRotation, TargetRotation, easeT);
            }

            goContext.Target.transform.rotation = TargetRotation;
        }
    }

    /// <summary>
    /// Scale node - scales a GameObject
    /// </summary>
    [Serializable]
    public class ScaleNode : IBeeTweenNode
    {
        public Vector3 TargetScale;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            if (context is not IBeeTweenContext<GameObject> goContext || goContext.Target == null) return;

            var startScale = goContext.Target.transform.localScale;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                goContext.Target.transform.localScale = Vector3.Lerp(startScale, TargetScale, easeT);
            }

            goContext.Target.transform.localScale = TargetScale;
        }
    }

    /// <summary>
    /// Delay node - waits for a specified duration
    /// </summary>
    [Serializable]
    public class DelayNode : IBeeTweenNode
    {
        public float Duration;

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            await Awaitable.WaitForSecondsAsync(Duration, cancellationToken.Token);
        }
    }

    // ============== IMAGE NODES ==============

    /// <summary>
    /// Fade node - fades an Image's alpha
    /// </summary>
    [Serializable]
    public class FadeNode : IBeeTweenNode
    {
        public float TargetAlpha;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            Image image = null;
            
            if (context is IBeeTweenContext<Image> imageContext)
                image = imageContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                image = goContext.Target.GetComponent<Image>();

            if (image == null) return;

            var startAlpha = image.color.a;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                var color = image.color;
                color.a = Mathf.Lerp(startAlpha, TargetAlpha, easeT);
                image.color = color;
            }

            var finalColor = image.color;
            finalColor.a = TargetAlpha;
            image.color = finalColor;
        }
    }

    /// <summary>
    /// Color tween node - tweens an Image's color
    /// </summary>
    [Serializable]
    public class ColorNode : IBeeTweenNode
    {
        public Color TargetColor;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            Image image = null;
            
            if (context is IBeeTweenContext<Image> imageContext)
                image = imageContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                image = goContext.Target.GetComponent<Image>();

            if (image == null) return;

            var startColor = image.color;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                image.color = Color.Lerp(startColor, TargetColor, easeT);
            }

            image.color = TargetColor;
        }
    }

    // ============== RECTTRANSFORM NODES ==============

    /// <summary>
    /// Anchor position tween node - tweens a RectTransform's anchoredPosition
    /// </summary>
    [Serializable]
    public class AnchorPositionNode : IBeeTweenNode
    {
        public Vector2 TargetPosition;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            RectTransform rectTransform = null;

            if (context is IBeeTweenContext<RectTransform> rtContext)
                rectTransform = rtContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                rectTransform = goContext.Target.GetComponent<RectTransform>();

            if (rectTransform == null) return;

            var startPosition = rectTransform.anchoredPosition;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, TargetPosition, easeT);
            }

            rectTransform.anchoredPosition = TargetPosition;
        }
    }

    /// <summary>
    /// Size delta tween node - tweens a RectTransform's sizeDelta
    /// </summary>
    [Serializable]
    public class SizeDeltaNode : IBeeTweenNode
    {
        public Vector2 TargetSize;
        public float Duration;
        public AnimationCurve EaseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public async Awaitable ExecuteAsync(IBeeTweenContext context, CancellationTokenSource cancellationToken)
        {
            RectTransform rectTransform = null;

            if (context is IBeeTweenContext<RectTransform> rtContext)
                rectTransform = rtContext.Target;
            else if (context is IBeeTweenContext<GameObject> goContext && goContext.Target != null)
                rectTransform = goContext.Target.GetComponent<RectTransform>();

            if (rectTransform == null) return;

            var startSize = rectTransform.sizeDelta;
            var elapsedTime = 0f;

            while (elapsedTime < Duration)
            {
                await Awaitable.EndOfFrameAsync(cancellationToken.Token);
                elapsedTime += Time.deltaTime;
                
                var t = Mathf.Clamp01(elapsedTime / Duration);
                var easeT = EaseCurve.Evaluate(t);
                rectTransform.sizeDelta = Vector2.Lerp(startSize, TargetSize, easeT);
            }

            rectTransform.sizeDelta = TargetSize;
        }
    }
}
