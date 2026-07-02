# DataFile

Namespace: `DataKeeper.Generic`

`DataFile<T>` persists a value of any serializable type through an `IStorageProvider` (by default the global `DataKeeperStorage.Files` — `LocalFileStorage`, i.e. files under `Application.persistentDataPath`), with a choice of serializer and sync/async APIs. Swap `DataKeeperStorage.Files` for a custom provider to redirect all files to a cloud backend, or pass a provider to the constructor to override it per file — see [SaveManager — Custom storage](SaveManager.md#custom-storage-cloud-saves).

## Quick start

```csharp
[Serializable]
public class SaveGame
{
    public int level;
    public List<string> unlocked = new();
}

var save = new DataFile<SaveGame>("saves/game.json", SerializationType.Json, new SaveGame());

if (save.IsFileExist())
    save.LoadData();

save.Data.level++;
save.SaveData();

// Async variants
await save.SaveDataAsync();
await save.LoadDataAsync();
```

## API

| Member | Description |
| --- | --- |
| `Data` | The wrapped value; assign it then `SaveData()` |
| `SaveData()` / `LoadData()` | Synchronous save/load |
| `SaveDataAsync()` / `LoadDataAsync()` | `Task`-based variants |
| `IsFileExist()` | Whether the backing file exists on disk |
| `Scope` | `SaveScope.Global` (default) or `SaveScope.Slot` — set via constructor |

## Save scope & slots

Pass a `SaveScope` to the constructor to make the file participate in [SaveManager](SaveManager.md) save slots:

```csharp
// Lives in persistentDataPath/slots/{slot}/ when a slot is active
var progress = new DataFile<Progress>("progress.json", SerializationType.Json, new Progress(), SaveScope.Slot);

// Always at persistentDataPath root, regardless of slot
var settings = new DataFile<Settings>("settings.json", SerializationType.Json, new Settings());
```

With no active slot (`SaveManager.CurrentSlot` empty), slot-scoped files fall back to the root folder. Register files with `SaveManager.Register(...)` to save/load them as a batch with versioning and migrations.

## Serialization types

| `SerializationType` | Backend | Notes |
| --- | --- | --- |
| `Json` | Newtonsoft Json.NET | Recommended default; human-readable, version-tolerant |
| `Xml` | `XmlSerializer` | Requires public parameterless constructor and public members |
| `Binary` | `BinaryFormatter` | Compact but type-brittle; avoid for untrusted data — `BinaryFormatter` is deprecated by .NET for security reasons |

## Unity types in JSON

JSON serialization goes through `DataKeeperJson.Settings` — shared Json.NET settings that serialize UnityEngine structs (`Vector2/3/4`, `Quaternion`, `Color`, `Rect`, `Bounds`, `Vector2Int/3Int`, `Matrix4x4`, …) **by fields only**. Without this, Json.NET would write read-only properties too (`Vector3.normalized`, `Quaternion.eulerAngles`, …), bloating files and risking self-reference loops. So Unity types inside your save data just work:

```csharp
[Serializable]
public class SaveGame
{
    public Vector3 position;      // {"x":…,"y":…,"z":…}
    public Quaternion rotation;
    public Color tint;
}
```

To customize (e.g. add your own `JsonConverter`s), mutate or replace `DataKeeperJson.Settings` before the first save/load. The same settings are used by [`JsonData<T>`](GenericUtilities.md) and `ReactivePref<T>`'s JSON fallback.

## Notes

- File names are relative keys resolved by the file's storage provider (for `LocalFileStorage`: relative to `Application.persistentDataPath`); intermediate folders are created automatically (subfolders in the file name are fine, e.g. `"saves/slot1.json"`).
- Errors are caught and logged via `Debug.LogError` rather than thrown.
- The file name (including extension) is entirely up to you; the serializer does not infer anything from it.
