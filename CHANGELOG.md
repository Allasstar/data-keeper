# Changelog

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
