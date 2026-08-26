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
| `AutoGridLayoutGroup` | Grid that derives its cell size from the container (aspect-ratio or fill cells) |
| `WrapLayoutGroup` | Flows children onto new rows/columns when out of space |

### AutoGridLayoutGroup

Unlike Unity's `GridLayoutGroup` the cell size is not authored — it is computed from the container size, the padding/spacing and the constraint:

| Constraint | Fixed | Derived from child count |
| --- | --- | --- |
| `FixedColumnCount` | columns | rows |
| `FixedRowCount` | rows | columns |

| Cell Size Mode | Cell size |
| --- | --- |
| `AspectRatio` | the constrained axis fills the container, the other follows `aspectRatio` (width / height) |
| `Fill` | both axes fill the container, cells stretch to the grid |

`startCorner`, `startAxis`, `childAlignment`, `padding` and `spacing` behave like on Unity's grid. The axis that is *not* driven by the container reports its total size as the preferred layout size, so a `ContentSizeFitter` on that axis works (e.g. `FixedColumnCount` + vertical fitter = a grid that grows downwards).

> `AspectRatioGridLayoutGroup` was merged into this component: `FixedRows` → `FixedRowCount`, `FixedColumns` → `FixedColumnCount`, same `aspectRatio`.

### WrapLayoutGroup

Flows children along `mainAxis` and wraps to a new line when the line is full.

Children keep their own size — no `LayoutElement` needed. The group reads each child's rect, packs the line, and writes back only the position, so a child's width/height stay its own and stay editable in the inspector. A child with no usable rect falls back to its preferred size so it still lands somewhere sensible.

Children anchored to stretch are re-anchored to a corner on the first layout, keeping the size they had — every layout group collapses its children onto a single anchor point, and without that step a stretched child would drop to zero.

`childAlignment` positions each line along the main axis and each child inside its line on the cross axis. `childForceExpandWidth` / `childForceExpandHeight` spread a line's leftover space over its children's slots — each child keeps its size and is aligned inside the slot it got. Only the one matching `mainAxis` applies (`...Width` for a horizontal flow, `...Height` for a vertical one). The cross axis reports the flowed content size, so a `ContentSizeFitter` on it fits the wrapped content.

A child that sizes itself with a `ContentSizeFitter` settles a frame later, since a self-sizing child is applied after its parent group has measured it.

## Utility

| Component | Description |
| --- | --- |
| `SafeAreaUI` | Fits a `RectTransform` to the device safe area (notches, rounded corners) |
| `DragHandle` | Makes a window/panel draggable |
| `ResizeHandle` | Makes a panel resizable by dragging the handle |
| `ApplyPreset` | Applies configured presets to target components at runtime |
| `TMP_LinkHandler` | Click handler for `<link>` tags inside TextMeshPro text |

## Reactive UI bindings

Extension methods (in `DataKeeper.UI`) that bind an `IReactive<T>` source — [`Reactive<T>`](Reactive.md) or `ReactivePref<T>` — to uGUI elements. The current value is pushed on bind, and bindings release automatically when the target is destroyed (or dispose the returned `UIBinding` manually).

```csharp
using DataKeeper.UI;

health.BindTo(healthLabel, v => $"HP {v}");   // TMP_Text, custom format
health.BindToFill(healthBarImage);            // Image.fillAmount
musicVolume.BindTo(volumeSlider);             // two-way Slider
muted.BindTo(muteToggle);                     // two-way Toggle or ToggleUI
playerName.BindTo(nameInput);                 // two-way TMP_InputField
isDead.BindToActive(gameOverPanel);           // GameObject.SetActive
canBuy.BindToInteractable(buyButton);         // Selectable.interactable
fade.BindToAlpha(canvasGroup);                // CanvasGroup.alpha
tint.BindToColor(portrait);                   // Graphic.color
score.Bind(owner, v => minimap.SetScore(v));  // custom apply, tied to owner's lifetime
```

- Two-way bindings (`Slider`, `Toggle`, `TMP_InputField`) update the UI with `Set…WithoutNotify` and write UI edits back through `UniqueValue`, so there are no feedback loops and no redundant notifications.
- The int `Slider` overload rounds on write-back — pair it with `wholeNumbers`.
- Pass `twoWay: false` for display-only sliders/toggles/inputs.

## Samples

The **Overlay UI** package sample (Package Manager > DataKeeper > Samples) contains an `OverlayCanvas` prefab with spinner/overlay patterns built from these components; **UI Mask Materials** contains hole/target mask materials; **IconKeeper Font** is a TMP icon font (see the bundled RTF helper for glyph codes).
