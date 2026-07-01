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

## Reactive UI bindings

Extension methods (in `DataKeeper.UI`) that bind an `IReactive<T>` source — [`Reactive<T>`](Reactive.md) or `ReactivePref<T>` — to uGUI elements. The current value is pushed on bind, and bindings release automatically when the target is destroyed (or dispose the returned `UIBinding` manually).

```csharp
using DataKeeper.UI;

health.BindTo(healthLabel, v => $"HP {v}");   // TMP_Text, custom format
health.BindToFill(healthBarImage);            // Image.fillAmount
musicVolume.BindTo(volumeSlider);             // two-way Slider
muted.BindTo(muteToggle);                     // two-way Toggle
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
