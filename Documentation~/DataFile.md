# DataFile

Namespace: `DataKeeper.Generic`

`DataFile<T>` persists a value of any serializable type to `Application.persistentDataPath`, with a choice of serializer and sync/async APIs.

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

- File paths are relative to `Application.persistentDataPath`; intermediate folders are created automatically (subfolders in the file name are fine, e.g. `"saves/slot1.json"`).
- Errors are caught and logged via `Debug.LogError` rather than thrown.
- The file name (including extension) is entirely up to you; the serializer does not infer anything from it.
