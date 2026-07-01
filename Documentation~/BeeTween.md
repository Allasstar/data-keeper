# BeeTween

Namespace: `DataKeeper.BeeTween`

BeeTween is a node-based tween/sequence system authored entirely in the inspector. A `BeeTweenPlayer` component holds a tree of `IBeeTweenNode`s (via `SerializeReference` type-picker dropdowns); nodes run as `Awaitable`s on Unity's async system, and every node input (target, end value, duration, ease) is a [ValueProvider](ValueProviders.md), so values can be constants, random ranges, blackboard reads, etc.

## Quick start (inspector)

1. `Add Component > DataKeeper > BeeTween > Bee Tween Player`.
2. Pick a **Root Node** from the dropdown — usually `SequenceNode` or `ParallelNode`.
3. Add child nodes (Move, Fade, Delay, …) and configure their providers.
4. Enable `runOnEnable`, call `Run()` from a UnityEvent, or drive it from code.

```csharp
player.Run();                 // fire and forget
await player.RunAsync();      // await completion
player.Stop();                // cancel (also happens on OnDisable)
```

`RestartOnEnd` / `RestartOnFail` (optional delays) loop the sequence automatically.

## Built-in nodes

| Category | Nodes |
| --- | --- |
| Composition | `SequenceNode` (one after another), `ParallelNode` (all at once), `ChainNode`, `LoopNode`, `InfinityLoopNode`, `FlipFlopNode` |
| Timing | `DelayNode`, `UpdateNode`, `DeltaUpdateNode` |
| Transform | `MoveNode`, `RotateNode`, `ScaleNode` |
| RectTransform | `AnchorPositionNode`, `SizeDeltaNode` |
| Image | `ColorNode`, `FadeNode` |
| Events | `UnityEventNode`, `SignalChannelNode` (fire a [Signal channel](Signals.md)) |
| Debug | `DebugLogNode`, `DebugPrintLogNode` |

## Easing

Ease inputs are `IEaseProvider`s: `EaseFuncProvider` (built-in `EaseType` functions) or `EaseCurveProvider` (an `AnimationCurve` you draw).

## Writing custom nodes

A node is a `[Serializable]` class implementing one async method:

```csharp
[Serializable]
public class ShakeNode : IBeeTweenNode
{
    [SerializeReference, SerializeReferenceSelector] public ITransformProvider Target;
    [SerializeReference, SerializeReferenceSelector] public IFloatProvider Duration;

    public async Awaitable ExecuteAsync(CancellationTokenSource cancellationToken)
    {
        var t = Target?.GetValue();
        if (t == null) return;
        float elapsed = 0f, duration = Duration.GetValue();
        var origin = t.localPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localPosition = origin + UnityEngine.Random.insideUnitSphere * 0.1f;
            await Awaitable.EndOfFrameAsync(cancellationToken.Token);
        }
        t.localPosition = origin;
    }
}
```

It appears automatically in the node-picker dropdown. Honor the cancellation token (pass it to `Awaitable` calls) so `Stop()` works.

## Notes

- Cancellation is cooperative — nodes that await `Awaitable.EndOfFrameAsync(token)` stop immediately on `Stop()`/disable.
- Nodes snap to their end value when their duration completes.
- Requires Unity 6 (`Awaitable`-based).
