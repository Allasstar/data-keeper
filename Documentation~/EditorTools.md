# Editor Tools

Namespace: `DataKeeper.Editor` (editor assembly)

Editor windows and menu items shipped with the package. Most live under the **Tools** menu; `Tools > Windows > Tools` opens a hub window linking to the rest.

## Windows (`Tools > Windows > …`)

| Window | Purpose |
| --- | --- |
| PlayerPrefs | Browse, edit, and delete PlayerPrefs (including the package's `ReactivePref` values) |
| Game Tags Editor | Author the [GameTags](GameTags.md) tag tree, redirects, and code generation |
| Service Locator Inspector | Live view of everything registered in the [ServiceLocator](ServiceLocator.md) at runtime |
| FSM Debugger (Beta) | Inspect [FSM](FSM.md) current state and transition history at runtime |
| [Asset Commander](AssetCommander.md) | Two-panel project browser — folders and scenes side by side, analysis modes over an incremental index, and plan-then-confirm commands across both sides |
| Asset Reference Finder | Find where an asset is referenced across the project (also `Assets > Find References in Project`) |
| GUID Swapper | Swap asset GUID references (e.g. replace one sprite/material with another everywhere) |
| Asset Transfer | Move assets between projects/folders with their dependencies |
| Android Publisher Settings | Keystore/build settings helper for Android publishing |
| Color Tools | WCAG contrast checker, color-theory palette generator, and luminance-preserving (OKLCH) hue shifter |
| Image Manipulator | Batch image operations (resize, crop, format) on textures |
| [Mesh Tools](#mesh-tools-tools--windows--mesh-tools) | Move a mesh pivot to a transform, and scale/offset a UV channel |
| Material Shader Converter | Batch-convert materials between shaders (e.g. Built-in → URP) |
| Prefab Image Baker (Beta) | Render prefab previews to image assets |
| Script Execution Order | Edit script execution order in one list |
| Static Class Inspector (Beta) | Live-inspect static classes marked with `[StaticClassInspector]` |
| Table Editor (Beta) / List Table (Beta) | Spreadsheet-style editing of ScriptableObject collections |

## Menu items

| Menu | Action |
| --- | --- |
| `Tools > Snap > To Ground …` | Snap selection to ground by transform, collider, or mesh (hotkeys: `Ctrl+G`, `Home`, `End`, `PgDn`) |
| `Tools > Select UI` (`PgUp`) | Select the UI element under the mouse |
| `Tools > Find Missing Scripts in Scene` | Locate GameObjects with missing script references |
| `Tools > Materials GPU Instancing > Enable/Disable` | Toggle GPU instancing on selected materials |

## Mesh Tools (`Tools > Windows > Mesh Tools`)

Pick the object in **Mesh** at the top, then work in the **Pivot** or **UV** tab.

- **Pivot tab** — choose a **Mode**: *Transform* (pivot moves to the **New Pivot** transform) or *Bounds* (pivot moves to a point on the mesh bounds picked per axis: Left/Center/Right, Bottom/Center/Top, Back/Center/Front; default bottom-center). **Apply Pivot** moves the pivot there; the object, its children, and its colliders stay where they are in the scene. While this tab is open, the Scene view shows the mesh bounds and the target point.
- **UV tab — Apply UV** — remaps a UV channel as `uv * scale + offset` (same math as material Tiling/Offset). A negative scale flips.
- **UV preview** — the window draws the UV layout over the material's main texture (original in grey, edited in blue — both colors are adjustable and saved per user) and updates as you type. Mouse wheel zooms at the cursor, drag pans, double-click fits the view again. With **Preview in Scene** on and the UV tab open, the object shows the edited UVs live through a temporary mesh that is swapped back before scene saves, play mode, and Apply. Don't duplicate the object while previewing — the copy would keep the temporary mesh.

Imported (FBX) and built-in meshes are read-only, so the first edit saves a copy as `<name>_Pivot.asset` / `<name>_UV.asset` next to the source (or in `Assets/`) and assigns it. Meshes that are already `.asset` files are edited in place, which affects every object using them. Both operations support Undo.

## Main toolbar buttons

**Save Project** (saves dirty scenes and assets) and **Reload Domain** buttons dock on the left side of the main toolbar. Show, hide, or move them via the toolbar's right-click menu under `Data Keeper`. The same actions are in the Tools window's Shortcuts section.

## Hierarchy & inspector enhancements

- **Enhanced hierarchy icons** — shows component icons for common Unity and DataKeeper components in the Hierarchy window. Toggle and configure under `Edit > Preferences > Data Keeper`.
- **Custom drawers** — for the package's [attributes](Attributes.md), `Reactive`/`ReactivePref`, `Optional<T>`, `DataFile`, `SelectableColorPalette`, and GameTag pickers.

## Preferences

`Edit > Preferences > Data Keeper` hosts package-wide editor settings (hierarchy icon style, feature toggles). Values persist via `ReactiveEditorPref`.
