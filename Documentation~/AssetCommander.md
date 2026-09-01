# Asset Commander

`Tools > Windows > Asset Commander` — editor only.

A two-panel, Total-Commander-style browser for the project. Each side holds a **folder** or a
**scene**, renders its contents as a tree or a list, filters them through an **analysis mode**,
and applies **commands** to the selection — including across the two sides.

## Sides

A side points at a folder or a scene; drop either onto the side's object field, or click a
breadcrumb segment to go up.

A scene side does **not** need the scene to be open. An already-open scene is used live; a closed
one is loaded with `EditorSceneManager.OpenPreviewScene`, the same machinery Prefab Mode runs on,
so browsing a scene never disturbs which scenes you have open. A preview-backed side is
**read-only** — any command that would mutate it asks to open the scene first, and because
promoting closes the preview and reopens the file, the selection is rebuilt and the command has
to be re-issued.

The `Components` chip folds a scene GameObject's components into the tree. `Tree` / `List` switch
the view; the list is sortable by name, type, size and modified date.

## Analysis modes

Every mode is a lookup against a persistent, incrementally maintained index of `Assets/` plus
local and embedded packages — built on worker threads, cached under `Library/`, and kept fresh by
an `AssetPostprocessor`. No mode re-scans the disk.

| Mode | Answers |
| --- | --- |
| Search | Filter this side by name. `t:mat` matches type or extension, `*` and `?` glob, several terms all have to match |
| Broken Refs | Assets and objects referencing something that no longer exists |
| Missing Scripts | Objects whose script component no longer resolves to a class |
| Cross-Side | What this side references on the other — or, `Reversed`, what the other references here |
| Unused | Assets no build scene, `Resources` folder or other asset can reach |
| Duplicates | Assets with byte-identical content, grouped |

The `Reversed` toggle belongs to Cross-Side: it appears next to that chip while the mode is
selected, and only on a side that can answer the reversed question. It is hidden otherwise rather
than shown greyed out.

A mode chip that does not apply to the side's kind is dimmed rather than disabled, so hovering it
still explains what it does and why it cannot answer here. `Unused` cannot see
`Resources.Load(string)` and nothing can see reflection — the mode says so above its results.

## Commands

Every command answers with a **plan, not an action**: it resolves each destination and each
name collision up front, shows the whole list in a confirm dialog, and then does exactly what the
dialog showed.

| Command | Notes |
| --- | --- |
| Rename | One name or a batch pattern — `{name}`, `{n}`, `{n:000}` |
| Copy | New GUIDs; existing references keep pointing at the originals |
| Move | Keeps the GUID, so references survive |
| New Folder | Created inside the active side's root |
| Delete | Assets go to the OS trash; rows carry an inbound-reference count |
| Duplicate | In place, named by Unity's own `GenerateUniqueAssetPath` |
| Swap | Exchanges two assets' GUIDs, redirecting every reference. Requires Force Text serialization, and is **not** covered by Undo — the way back is to swap again |
| Prefab | Scene objects → folder side saves prefabs and connects the originals; prefabs → scene side instantiates them |

### Drag and drop

Dragging a selection from one panel to the other runs the same commands through the same confirm
dialog — a drop is never a second implementation. What the drop resolves to depends on the two
sides: folder → folder and scene → scene is a **Move** (hold **Ctrl** for a **Copy**), scene
objects → folder saves prefabs, and prefabs → scene instantiates them. Dropping onto a folder row
targets that folder; dropping onto a GameObject row parents the new instances under it; dropping
on the panel background targets the side's root. A drop the sides cannot make sense of shows the
rejected cursor and does nothing.

## Keyboard

**No command is bound to a key.** Function keys and `Ctrl+D` collide with the editor's own global
shortcuts, and a destructive command reached by a stray keystroke is the one mistake this window
must not make — every command is a button, a context-menu entry, or a drop.

What is bound is navigation, and only inside the panel that has focus:

| Key | Action |
| --- | --- |
| Arrows, Page Up/Down, Home/End | Move through the rows (the collection view's own) |
| Left / Right | Collapse / expand, in Tree view |
| Enter | Descend into a folder, or open the asset |
| Backspace | Leave the current folder |
| `Ctrl+A` | Select everything visible |
| `Ctrl+F` | Focus the search box |
| `Esc` | Clear the filter, then the selection |

## Toolbar

`Sync` walks side B to the same folder as side A while you navigate (folders only; B can still be
moved on its own, and is simply overwritten the next time A moves). `Swap Sides` exchanges what
the two sides point at. `Rebuild Index` re-scans every asset from disk, ignoring the cache.

## Limits

- Scene sides are unavailable in Play Mode — a preview scene cannot survive the reload, and the
  live one belongs to the player.
- `Swap` needs `Edit > Project Settings > Editor > Asset Serialization` set to **Force Text**; in
  a binary project the window says so and the command stays disabled.
- Deleting is undone from the OS trash, not with `Ctrl+Z`. Scene edits are normal `Undo` entries.
