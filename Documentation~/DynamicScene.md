# DynamicScene

Namespace: `DataKeeper.DynamicScene`

Distance-based streaming of Addressable prefabs: content loads when a camera gets close and unloads (or returns to a pool) when it moves away. Designed for open worlds and large scenes split into independently loadable chunks.

> **Requires the Addressables package.** This entire module compiles only when `com.unity.addressables` is installed in the project (the `DATAKEEPER_ADDRESSABLES` define is set automatically via the assembly definition's version defines). Addressables is *not* a hard dependency of DataKeeper — install it via Package Manager if you want DynamicScene.

## Components

### `AddressableLoader`
Placed where a streamed prefab should appear (`Add Component > DataKeeper > Addressable > Addressable Loader`).

- `addressableAsset` — the `AssetReferenceGameObject` to stream.
- `loadDistance` / `unloadDistance` — hysteresis radii checked against `cameraList` (defaults to `Camera.main`).
- `checkInterval` / `checkDelay` — how often distance is polled.
- `useObjectPooling` — unloaded instances deactivate into a pool instead of being destroyed.
- `ForceLoad()` / `ForceUnload()` / `IsLoaded()` — manual control.

### `SubScene`
Optional parent-level optimizer (`DataKeeper > Addressable > Sub Scene`). With `checkChildren` enabled it computes a bounding sphere around all child loaders and only polls their distances while a camera is near the group — one check for the whole chunk instead of one per loader.

In the editor it also provides **Load Prefabs / Unload Prefabs** buttons to preview streamed content without entering play mode (previews are marked `[EDITOR_PREVIEW]` and never saved).

### `SubSceneManager`
Static coordinator used by the loaders — deduplicates loads of the same asset, reference-counts loaders per asset, pools instances, and releases Addressables handles when the last loader lets go. Useful members:

- `OnAllCurrentLoaded` — one-shot event fired when every pending load finishes.
- `IsAddressableLoaded(guid)`, `GetTotalInstanceCount()`, `GetLoadedAddressableCount()`.

## Addressable Converter Tool

`Tools`-window (also reachable from the SubScene inspector) that converts selected scene objects into Addressable prefabs and replaces them with configured `AddressableLoader`s — including batch conversion of children, key prefix/suffix rules, and duplicate-name handling.

## Typical setup

1. Group world chunks under empty roots.
2. Open the **Addressable Converter Tool**, select a root, convert its children (this creates the prefabs, Addressables entries, and loaders).
3. Add a `SubScene` to the root and enable `checkChildren`.
4. Play — content streams in and out around the camera.
