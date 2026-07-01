# UI Components

Namespace: `DataKeeper.UI`

uGUI components extending Unity's built-in UI. All are available under `Add Component > DataKeeper > UI`.

## Selectables

| Component | Description |
| --- | --- |
| `SelectableUI` | Extended `Selectable` base with the package's color-palette support (`SelectableColorPalette`) and custom editor |
| `ButtonUI` | Button built on `SelectableUI` (`IPointerClickHandler`, `ISubmitHandler`) |
| `ToggleUI` | Toggle built on `SelectableUI`, with per-state sprite, color, font-style, and text swaps |
| `TabsUI` | Tab group coordinating a set of toggles/panels |

## Layout

| Component | Description |
| --- | --- |
| `AutoGridLayoutGroup` | Grid that computes cell size/count from available space |
| `AspectRatioGridLayoutGroup` | Grid whose cells keep a fixed aspect ratio |
| `WrapLayoutGroup` | Flows children onto new rows/columns when out of space |

## Utility

| Component | Description |
| --- | --- |
| `SafeAreaUI` | Fits a `RectTransform` to the device safe area (notches, rounded corners) |
| `DragHandle` | Makes a window/panel draggable |
| `ResizeHandle` | Makes a panel resizable by dragging the handle |
| `ApplyPreset` | Applies configured presets to target components at runtime |
| `TMP_LinkHandler` | Click handler for `<link>` tags inside TextMeshPro text |

## Samples

The **Overlay UI** package sample (Package Manager > DataKeeper > Samples) contains an `OverlayCanvas` prefab with spinner/overlay patterns built from these components; **UI Mask Materials** contains hole/target mask materials; **IconKeeper Font** is a TMP icon font (see the bundled RTF helper for glyph codes).
