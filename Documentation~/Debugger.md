# Debugger

Namespace: `DataKeeper.Debugger`

Runtime debug helpers: contextual logging, 3D primitive drawing, and on-screen print. In release player builds the drawing/logging helpers compile to no-ops where marked, so calls can stay in shipped code.

## DebugLog

`Debug.Log`-style logging that automatically prefixes the calling file name (via `[CallerFilePath]`), making log sources obvious:

```csharp
DebugLog.Log("spawned");           // [Spawner] spawned
DebugLog.Error("missing config", this);
```

## DebugDraw

Draws debug geometry in the Game/Scene view with a duration, similar to `Debug.DrawLine` but with many more shapes:

```csharp
DebugDraw.Sphere(hit.point, 0.25f, Color.red, duration: 2f);
DebugDraw.Arrow(transform.position, Color.cyan);
DebugDraw.Capsule(a, b, 0.5f, Color.yellow);
```

Available shapes: `Line`, `Ray`, `Cross`, `Point`, `Circle`, `Square`, `Triangle`, `Sphere`, `Capsule` (two variants), `Cylinder`, `Pyramid`, `Cube` (two variants), `Bounds`, `Arrow`.

## DebugPrint

On-screen text output (`DebugPrint`, styled via `DebugPrintStyle`, managed by `DebugPrintSystem`) for values you want to watch during play without opening the console.

## Related

- `AllocCounter` (`DataKeeper.Extra`) — measures GC allocations around a code block; useful when verifying zero-GC paths.
- `DebugLogNode` / `DebugPrintLogNode` — [BeeTween](BeeTween.md) nodes for logging inside sequences.
