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
| `ClampLayoutElement` | Bounds the size a `ContentSizeFitter` or a parent layout group will give an object |

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

### ClampLayoutElement

> Live example: **GameObject > UI > DataKeeper > Examples > Clamp Layout Element** builds two sections into
> the open scene: a maximum on self-sizing text, and a scroll view that hugs its content between a minimum
> and a maximum.

`LayoutElement` has a minimum and a preferred size but no maximum, so anything sized from its content - a
text bubble, a tooltip, a list that hugs its items - grows without a ceiling. This adds one, and carries the
minimum alongside it so both bounds live in the same component.

Four `Optional<float>` fields, all in pixels: `Min Width`, `Max Width`, `Min Height`, `Max Height`. A field
left unticked is not a bound and the component stays transparent on it.

It works by reporting a clamped size at a higher `layoutPriority` (2) than everything else on the object.
`LayoutUtility` resolves the highest priority first and only falls back to the largest value among equals, so
this is the only way to make a size *smaller* - a second `LayoutElement` can never do it. Both the
`ContentSizeFitter` and every layout group read through `LayoutUtility`, so both honour the bounds with no
further setup.

The minimum is applied after the maximum, so where the two contradict the minimum wins and content is never
crushed below the floor. The inspector flags the contradiction. Note the asymmetry with a separate
`LayoutElement` on the same object: its minimum is a *reported size* like any other and gets clamped by the
maximum here, while the minimum set here is a *bound* and is applied last.

**A maximum is a ceiling, not a source.** Something on the object still has to report a size for it to cap -
a `Text`, an `Image`, a nested layout group, or a plain `LayoutElement`. On an object that reports nothing
the cap has nothing to clamp; the inspector says so. A minimum has no such requirement: with nothing else to
read the resolved size is `0`, and the floor is the whole answer.

Cases where a bound cannot apply, all flagged in the inspector:

- the parent layout group has **Child Control Size** off for that axis - it then sizes children straight from
  their `RectTransform` and never queries a layout element;
- the parent layout group has **Child Force Expand** on for that axis - force expand raises the flexible size
  back to `1`, which overrides the *maximum* (a minimum is unaffected);
- the parent is a grid - every child gets the cell size.

A capped axis reports a flexible size of `0`, so a capped child stops at its preferred size instead of
absorbing a group's leftover space. A floor alone leaves the flexible size untouched. "Flexible, but only up
to N" is not expressible: a layout group asks for sizes once and distributes surplus in the same pass.

Bounds are absolute pixels. For a screen-relative ceiling, set the value from the root canvas rect yourself -
a fraction resolved against the *parent* rect would feed back into a parent that is itself sized by this
child, and collapse over successive rebuilds.

> Clamping `sizeDelta` afterwards (in `LateUpdate`, or from a driven-property override) looks equivalent and
> is not: the parent group has already positioned the siblings against the unclamped size, so the result is a
> hole in the layout. The bounds have to be visible at measurement time.

#### Size Source, and a scroll view that hugs its content

Everything above measures the object the component sits on. **Size Source** points it at another rect
instead: the reported size becomes that rect's size, clamped by the bounds here. The object still has to be
sized by something - a parent layout group or a `ContentSizeFitter` - it just no longer needs anything on
itself to measure.

The case this exists for is a list, a chat log or a dropdown-like panel that grows with its items and then
stops and scrolls:

```
Scroll View   Image + ScrollRect + ClampLayoutElement (Min/Max Height, Size Source -> Content)
  Viewport    RectMask2D, anchored stretch
    Content   VerticalLayoutGroup + ContentSizeFitter (Vertical = Preferred), anchored top-stretch
      items
```

The content is anchored and sizes itself, exactly as in a stock scroll view, so it is free to grow past the
view. `ScrollRect` reports `-1` for every `ILayoutElement` property, so without Size Source the view has no
size to bound.

Keep the Viewport. `ScrollRect` is itself an `ILayoutGroup`, so parenting the content straight to the view
makes Unity's self-controller warning fire on the content's `ContentSizeFitter` - *"Parent has a type of
layout group component"*. The warning only inspects the immediate parent, and a `RectMask2D` controls
nothing, so one intermediate object clears it.

**Do not put a layout group on the scroll view to measure the content instead.** On its main axis a layout
group sizes a child as `Mathf.Lerp(min, preferred, minMaxLerp)`, where `minMaxLerp` comes from the container
size - so the moment the maximum holds the view at its cap, the group squashes the content down to fit and
there is nothing left to scroll. Adding a `ContentSizeFitter` to the content forces it back and Unity warns
about the two controllers fighting over the same rect. Size Source measures without controlling, which is
the whole difference.

The inspector flags the ways this can be wired wrong: a source that is this object itself or one of its
ancestors (the size would feed back into itself), and a source that is a descendant while a layout group is
still present on this object.

#### Dropdowns

`Dropdown` / `TMP_Dropdown` do not use this chain at all — `Show()` writes the list size directly, so no
layout element is consulted. It sets the content height from the item count and then shrinks the template by
the leftover: **the template's authored height already is the maximum**, and shorter content hugs. If a
dropdown grows past the screen, either the template height is authored larger than the canvas, or a
`ContentSizeFitter` was added to the template — which replaces the built-in shrink and removes the ceiling
with it. Remove the fitter and set the template height.

For a screen-relative cap, set the template height before it is cloned (it is read at `Show()` time):

```csharp
private void ClampTemplate()
{
    float canvasHeight = _canvas.rootCanvas.GetComponent<RectTransform>().rect.height;
    Vector2 size = _template.sizeDelta;
    size.y = Mathf.Min(_maxHeight, canvasHeight * _maxScreenFraction);
    _template.sizeDelta = size;
}
```

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
