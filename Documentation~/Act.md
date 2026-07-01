# Act

Namespace: `DataKeeper.ActCore`

`Act` is a static coroutine and timing utility. On first use it spawns a hidden, scene-persistent `[ActEngine]` GameObject that hosts coroutines and forwards application/scene events — so any plain C# class can run coroutines, delays, and interpolations without owning a `MonoBehaviour`.

## Coroutines from anywhere

```csharp
Coroutine routine = Act.StartCoroutine(MyRoutine());
Act.StopCoroutine(routine);
Act.StopAllCoroutine();
```

## Timers and interpolation

All methods return the underlying `Coroutine` so they can be stopped.

| Method | What it does |
| --- | --- |
| `DelayedCall(time, callback)` | Invoke after `time` seconds (`0` = next frame, `< 0` = immediately) |
| `WaitWhile(cond, callback)` / `WaitUntil(cond, callback)` | Invoke when the condition flips |
| `One(duration, value, onComplete)` | Drives `value` with the normalized 0→1 progress |
| `Float(from, to, duration, value, onComplete)` | Interpolates a float; overload takes an `EaseType` |
| `Int(from, to, duration, value, onComplete)` | Interpolates an int |
| `Delta(duration, delta, onComplete)` | Reports per-frame `deltaTime` for the duration |
| `DeltaValue(value, duration, deltaOfValue, onComplete)` | Distributes `value` across the duration as per-frame deltas |
| `Timer(duration, value, onComplete)` | Counts elapsed seconds |
| `Period(from, to, duration, period, value, callback, onComplete)` | Interpolation plus a periodic tick callback |
| `OneSecondUpdate(callback)` | Invokes the callback once per second, forever |

## Application & scene events

Subscribe without writing a `MonoBehaviour`:

```csharp
Act.OnApplicationQuitEvent.AddListener(SaveGame);
Act.OnApplicationPauseEvent.AddListener(paused => { /* ... */ });
Act.OnApplicationFocusEvent.AddListener(focused => { /* ... */ });
Act.OnSceneLoadedEvent.AddListener((scene, mode) => { /* ... */ });
Act.OnSceneUnloadedEvent.AddListener(scene => { /* ... */ });
Act.OnUpdateEvent.AddListener(() => { /* every frame */ });
```

## ActChain

A pooled, fluent sequencer. Chains are `CustomYieldInstruction`s, so a coroutine can `yield return` one.

```csharp
Act.StartActChain()
    .Call(() => Debug.Log("start"))
    .Wait(0.5f)
    .Float(0f, 1f, 1f, v => canvasGroup.alpha = v)
    .WaitUntil(() => Input.anyKeyDown)
    .Parallel(RoutineA(), RoutineB())   // both run to completion
    .Sequential(RoutineC(), RoutineD()) // one after another
    .Call(() => Debug.Log("done"));
```

Steps execute in order; each step starts when the previous one completes. Chain and step objects are pooled, so building chains repeatedly does not allocate garbage in steady state.

## Notes

- `ActEnumerator` exposes the raw `IEnumerator` builders (`ActEnumerator.Float(...)`, etc.) if you want to run them on your own `MonoBehaviour`.
- The engine object is created lazily and survives scene loads (`DontDestroyOnLoad`); state resets on domain reload via `RuntimeInitializeOnLoadMethod`.
