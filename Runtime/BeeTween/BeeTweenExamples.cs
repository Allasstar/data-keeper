using UnityEngine;
using UnityEngine.UI;
using DataKeeper.BeeTween;

namespace DataKeeper.Examples
{
    /// <summary>
    /// Example usage of the BeeTween tween system
    /// </summary>
    public class BeeTweenExamples : MonoBehaviour
    {
        [SerializeField] private BeeTweenPlayer tweenPlayer;
        [SerializeField] private Transform targetGameObject;
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform targetRectTransform;

        // Example 1: Simple move animation
        public void Example_SimpleMove()
        {
            var sequence = BeeTweenBuilder
                .CreateGameObjectSequence(targetGameObject)
                .Move(new Vector3(5, 0, 0), duration: 2f)
                .Build();

            tweenPlayer.Context = sequence;
        }

        // Example 2: Complex sequence - Move, rotate, scale
        public void Example_ComplexSequence()
        {
            var sequence = BeeTweenBuilder
                .CreateGameObjectSequence(targetGameObject)
                .Move(new Vector3(5, 0, 0), duration: 2f)
                .Delay(0.5f)
                .Scale(Vector3.one * 1.5f, duration: 1f)
                .Delay(0.3f)
                .Rotate(Quaternion.Euler(0, 360, 0), duration: 2f)
                .Build();

            tweenPlayer.Context = sequence;
        }

        // Example 3: UI Image fade in/out
        public void Example_ImageFade()
        {
            var sequence = BeeTweenBuilder
                .CreateImageSequence(targetImage)
                .Fade(targetAlpha: 0, duration: 1f)  // Fade out
                .Delay(0.5f)
                .Fade(targetAlpha: 1, duration: 1f)  // Fade in
                .Build();

            tweenPlayer.Context = sequence;
        }

        // Example 4: UI Image color change
        public void Example_ImageColor()
        {
            var sequence = BeeTweenBuilder
                .CreateImageSequence(targetImage)
                .Color(Color.red, duration: 1f)
                .Delay(0.5f)
                .Color(Color.blue, duration: 1f)
                .Delay(0.5f)
                .Color(Color.white, duration: 1f)
                .Build();

            tweenPlayer.Context = sequence;
        }

        // Example 5: RectTransform animation (UI movement)
        public void Example_UIMovement()
        {
            var sequence = BeeTweenBuilder
                .CreateRectTransformSequence(targetRectTransform)
                .AnchorPosition(new Vector2(100, 100), duration: 1f)
                .Delay(0.5f)
                .AnchorPosition(new Vector2(-100, -100), duration: 1f)
                .Build();

            tweenPlayer.Context = sequence;
        }

        // Example 6: Using easing curves for smooth animation
        public void Example_WithEasing()
        {
            // Create a custom easing curve
            var easeInOutCurve = AnimationCurve.Linear(0, 0, 1, 1);

            var sequence = BeeTweenBuilder
                .CreateGameObjectSequence(targetGameObject)
                .Move(new Vector3(10, 0, 0), duration: 2f)
                .Build();

            // Access the node and set easing manually if needed
            tweenPlayer.Context = sequence;
        }

        // Example 7: Manual context creation without builder
        public void Example_ManualContext()
        {
            var moveNode = new MoveNode
            {
                TargetPosition = new Vector3(5, 5, 0),
                Duration = 2f,
                Ease = new EaseValueProvider()
            };

            var scaleNode = new ScaleNode
            {
                TargetScale = Vector3.one * 2f,
                Duration = 1f,
                EaseCurve = AnimationCurve.Linear(0, 0, 1, 1)
            };

            var sequence = new SequenceNode(moveNode, scaleNode);
            var context = new TransformContext(targetGameObject, sequence);

            tweenPlayer.Context = context;
        }

        // Example 8: Parallel animations (simultaneous)
        public void Example_ParallelAnimations()
        {
            var moveNode = new MoveNode
            {
                TargetPosition = new Vector3(5, 0, 0),
                Duration = 2f
            };

            var scaleNode = new ScaleNode
            {
                TargetScale = Vector3.one * 1.5f,
                Duration = 2f
            };

            var rotateNode = new RotateNode
            {
                TargetRotation = Quaternion.Euler(0, 360, 0),
                Duration = 2f
            };

            var parallel = new ParallelNode(moveNode, scaleNode, rotateNode);
            var context = new TransformContext(targetGameObject, parallel);

            tweenPlayer.Context = context;
        }

        // Example 9: Quick animation with extension methods
        public void Example_QuickAnimation()
        {
            targetGameObject.AnimatePosition(new Vector3(5, 0, 0), duration: 1f, player: tweenPlayer);
            // Or for images:
            // targetImage.AnimateFade(targetAlpha: 0, duration: 1f, player: tweenPlayer);
        }

        // Example 10: Chained animations with delays
        public void Example_ChainedAnimations()
        {
            var sequence = BeeTweenBuilder
                .CreateGameObjectSequence(targetGameObject)
                // First sequence
                .Move(new Vector3(5, 0, 0), duration: 1f)
                .Scale(Vector3.one * 1.5f, duration: 1f)
                // Pause
                .Delay(0.5f)
                // Second sequence
                .Move(Vector3.zero, duration: 1f)
                .Scale(Vector3.one, duration: 1f)
                .Build();

            tweenPlayer.Context = sequence;
        }

        // Example 11: Combined GameObject and UI animation
        public void Example_CombinedAnimations()
        {
            // This would require manual context setup:
            var goSequence = new SequenceNode(
                new MoveNode { TargetPosition = new Vector3(5, 0, 0), Duration = 1f },
                new ScaleNode { TargetScale = Vector3.one * 1.5f, Duration = 1f }
            );

            var context = new TransformContext(targetGameObject, goSequence);
            tweenPlayer.Context = context;

            // For UI, create a separate tweenPlayer and animate separately
        }

        // Example 12: Creating a custom animation loop
        public async void Example_AnimationLoop()
        {
            for (int i = 0; i < 3; i++)
            {
                var sequence = BeeTweenBuilder
                    .CreateGameObjectSequence(targetGameObject)
                    .Move(Vector3.right * 5, duration: 1f)
                    .Move(Vector3.zero, duration: 1f)
                    .Build();

                tweenPlayer.Context = sequence;
                await Awaitable.WaitForSecondsAsync(2.1f); // Wait for animation to complete
            }
        }
    }
}

