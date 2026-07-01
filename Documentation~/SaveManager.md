# SaveManager

Namespace: `DataKeeper.Generic`

A static save pipeline that ties the package's persistence pieces together: register your [`DataFile`](DataFile.md)s and `ReactivePref`s once, then save/load everything with one call — with **save slots**, **data versioning + migrations**, and before/after events.

## Quick start

```csharp
[Serializable]
public class Progress
{
    public int level;
    public Vector3 checkpoint;
}

// Per-slot data (lives in slots/{slot}/ once a slot is active)
private DataFile<Progress> _progress = new("progress.json", SerializationType.Json, new Progress(), SaveScope.Slot);

// Global data (settings etc.) — unaffected by slot switches
private DataFile<Settings> _settings = new("settings.json", SerializationType.Json, new Settings());

private void Awake()
{
    SaveManager.DataVersion = 1;
    SaveManager.Register(_progress);
    SaveManager.Register(_settings);
    SaveManager.AutoSaveOnQuit = true;

    SaveManager.SetSlot("slot_1");
    SaveManager.LoadAll();
}

public void OnCheckpoint()
{
    _progress.Data.level++;
    SaveManager.SaveAll();          // or await SaveManager.SaveAllAsync();
}
```

## Save slots

A slot is a subfolder under `persistentDataPath/slots/`. Files created with `SaveScope.Slot` resolve their path against the active slot; `SaveScope.Global` files (the default) ignore slots entirely.

| Member | Description |
| --- | --- |
| `SetSlot(slot)` | Switch the active slot (fires `OnSlotChanged`). Does **not** save/load — call `SaveAll()` before and `LoadAll()` after as needed |
| `CurrentSlot` | Active slot name; empty = no slot (slot-scoped files fall back to the root folder) |
| `GetSlots()` / `SlotExists(slot)` | Enumerate / check existing slot folders |
| `DeleteSlot(slot)` | Delete a slot folder and everything in it |

```csharp
// Typical slot switch
SaveManager.SaveAll();          // flush the old slot
SaveManager.SetSlot("slot_2");
SaveManager.LoadAll();          // read the new one
```

## Versioning & migrations

Each `SaveAll()` stamps a `save_meta.json` in the active slot with `SaveManager.DataVersion`. When the format changes, bump `DataVersion` and register a migration for each step; on `LoadAll()`, pending migrations run in ascending order **before** files are read, receiving the slot's folder path:

```csharp
SaveManager.DataVersion = 2;
SaveManager.RegisterMigration(2, folder =>
{
    // v1 -> v2: rename a save file
    string old = $"{folder}/save.json";
    if (File.Exists(old)) File.Move(old, $"{folder}/progress.json");
});
```

- A missing meta file counts as version 0 (pre-versioning data) — but if no registered file exists on disk either, it's a fresh install and migrations are skipped.
- Migrations run once: after they complete, the meta is re-stamped with the current `DataVersion`.
- `GetSavedVersion(slot)` reads the stamped version without loading.

## Events

`OnBeforeSave`, `OnAfterSave`, `OnBeforeLoad`, `OnAfterLoad` ([Signals](Signals.md)) let systems without an `IDataFile` participate. Example — persisting a [PitySystem](Pity.md):

```csharp
var pityFile = new DataFile<string>("pity.json", SerializationType.Json, SaveScope.Slot);
SaveManager.Register(pityFile);
SaveManager.OnBeforeSave.AddListener(() => pityFile.Data = pity.SaveState());
SaveManager.OnAfterLoad.AddListener(() => pity.LoadState(pityFile.Data));
```

## Notes

- `LoadAll()` skips files whose backing file doesn't exist yet, leaving their default `Data` intact.
- Registered `ReactivePref`s are saved/loaded with the batch, but PlayerPrefs is inherently global — prefs are never slot-scoped.
- `SaveAllAsync()` / `LoadAllAsync()` await each file's async variant sequentially.
- Registration is a plain static list; register from `Awake`/`RuntimeInitializeOnLoad`. State is reset on play-mode entry (domain-reload-safe), and `UnregisterAll()` / `ClearMigrations()` are available for manual control.
