# Changelog

## [Unreleased]

### Changed
- `AutoGridLayoutGroup` — now the single auto-sizing grid. Added a `cellSizeMode` (`AspectRatio` / `Fill`) plus `cellSize` / `columns` / `rows` read-only accessors.
- `AutoGridLayoutGroup` — cell placement moved out of `CalculateLayoutInput*` into `SetLayout*`, so cells are sized against the final container size instead of the previous frame's. The driven axis no longer reports the container size as its *minimum* (that fed back into parent layouts and `ContentSizeFitter`), and `startCorner` / `startAxis` / `childAlignment` now apply in every mode.
- `WrapLayoutGroup` — children now keep their own size and are only positioned; sizes were taken from `LayoutUtility.GetPreferredWidth/Height`, which is `0` for any child without an `ILayoutElement`, so plain children collapsed to nothing. A child's preferred size is now only a fallback for a child with no usable rect.
- `WrapLayoutGroup` — cross-axis sizes are measured in `CalculateLayoutInputVertical` instead of during the wrap, the phase where children have already been measured against the width the group assigned them. Line heights of self-sizing children (wrapping text) are now correct on the same frame instead of trailing one behind.
- `WrapLayoutGroup` — children anchored to stretch are re-anchored to a corner keeping their current size. Every layout group collapses its children onto a single anchor point, which used to drop such a child to `sizeDelta` (usually zero).
- `WrapLayoutGroup` — wrapping now runs against the final container size (was one frame behind), the line break no longer counts padding twice, an oversized child no longer leaves an empty line, and `childForceExpandWidth` / `childForceExpandHeight` are implemented (they were serialized but unused) — they spread a line's leftover space over the children's slots, the one matching `mainAxis` applying. `childAlignment` aligns lines on the main axis and children inside their line on the cross axis. Cross-axis preferred size is reported from the flow, so a `ContentSizeFitter` fits wrapped content.

### Added
- `MaxSizeLayoutElement` — the maximum size `LayoutElement` never had. Reports a clamped size at `layoutPriority` 2, which is the only way to make a resolved layout size *smaller* (`LayoutUtility` takes the largest value among equal priorities), so both `ContentSizeFitter` and every layout group honour it unchanged. Per-axis `Absolute` or `CanvasFraction` cap; an uncapped axis reports `-1` and stays transparent. The inspector flags the setups where a cap cannot apply — nothing on the object reporting a size, or a parent group with Child Control Size off / Child Force Expand on for that axis.
- `AutoGridLayoutGroup` — inspector warns when a `ContentSizeFitter` constrains an axis the cell size is derived from, the setup that leaves the grid stuck at its current (possibly zero) size.

### Removed
- **Breaking**: `AspectRatioGridLayoutGroup` — merged into `AutoGridLayoutGroup`. Replace the component and map `LayoutType.FixedRows` → `Constraint.FixedRowCount`, `LayoutType.FixedColumns` → `Constraint.FixedColumnCount`, `fixedCount` → `constraintCount`; `aspectRatio` and `spacing` keep their meaning.

## [0.90.0] - 2026-07-02

### Added
- `IStorageProvider` — pluggable, key-based storage backend for the whole save pipeline (`DataFile<T>` files plus `SaveManager` slots, version meta, migrations). Default: `LocalFileStorage` (files under `persistentDataPath`, same layout as before).
- `IPrefsStorage` — pluggable key-value backend for `ReactivePref<T>`. Default: `PlayerPrefsStorage` (Unity PlayerPrefs, same behavior as before).
- `DataKeeperStorage` — static holder of the global storage defaults (`Files`, `Prefs`) and the active slot. Swap the providers there to store saves in the cloud (Steam Remote Storage, Unity Cloud Save, ...). `DataFile<T>` and `ReactivePref<T>` stay independent of `SaveManager` and can also take a per-instance provider via constructor or their `Storage` property.
- `StorageProviderExtensions` — UTF-8 text helpers (`ReadText`/`WriteText` + async) on top of `IStorageProvider`.
- "Save System Example" sample — single-script walkthrough of the save pipeline (files, prefs, slots, versioning, provider swapping, custom provider template).

### Changed
- **Breaking**: `SaveManager.RegisterMigration` callbacks now receive the slot's *key prefix* (`"slots/{slot}"`, empty when no slot) instead of an absolute folder path. Read/write save data through `DataKeeperStorage.Files` inside migrations.
- `DataFile<T>` no longer creates folders eagerly when resolving its path; the storage provider creates them on write.
- `SaveManager.SlotExists(slot)` now returns `false` for an empty slot name (previously `true`, since it checked `persistentDataPath` itself).
- `SaveManager.CurrentSlot` is now stored in `DataKeeperStorage.CurrentSlot` (the `SaveManager` property delegates to it).
