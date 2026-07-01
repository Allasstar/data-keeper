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
| Asset Reference Finder | Find where an asset is referenced across the project (also `Assets > Find References in Project`) |
| GUID Swapper | Swap asset GUID references (e.g. replace one sprite/material with another everywhere) |
| Asset Transfer | Move assets between projects/folders with their dependencies |
| Android Publisher Settings | Keystore/build settings helper for Android publishing |
| Color Contrast Checker | WCAG-style contrast checking for UI color pairs |
| Image Manipulator | Batch image operations (resize, crop, format) on textures |
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

## Hierarchy & inspector enhancements

- **Enhanced hierarchy icons** — shows component icons for common Unity and DataKeeper components in the Hierarchy window. Toggle and configure under `Edit > Preferences > Data Keeper`.
- **Custom drawers** — for the package's [attributes](Attributes.md), `Reactive`/`ReactivePref`, `Optional<T>`, `DataFile`, `SelectableColorPalette`, and GameTag pickers.

## Preferences

`Edit > Preferences > Data Keeper` hosts package-wide editor settings (hierarchy icon style, feature toggles). Values persist via `ReactiveEditorPref`.
