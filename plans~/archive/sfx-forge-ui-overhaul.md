# SFX Forge UI/UX Overhaul
Status: Shipped 2026-10-05 in `3485b3d`

Reorganise the SFX Forge editor window around Vital's layout ideas: every module has the same layout, each layer shows its own sound, `< value >` steppers replace dropdowns, the window is split into pages, and modulation is done by dragging onto knobs. Editor UI only; DSP and the recipe format stay as they are.

Written 2026-10-05 against `main` @ `2627a11`. Local ids are `FUI-n`.

## Progress
| # | Phase | Status | Verified |
|---|-------|--------|----------|
| 1 | Stepper control, layer strip rebuild, per-layer mini display | Complete | 2026-10-05 |
| 2 | Page tabs and top bar declutter | Complete | 2026-10-05 |
| 3 | FX modules with visualisations | Complete | 2026-10-05 |
| 4 | Drag-to-modulate with knob arcs | Complete | 2026-10-05 |

**Now:** — closed. Follow-up work: `plans~/sfx-forge-sound-features.md` (FUI-O2).

## Context
- **Specs:** this repo has no `specs/` folder, so the design is described below (FUI-D1).
- **Engine:** Unity 6000.6.0f1, UI Toolkit editor window. Assemblies: `DataKeeper.Forge.Core` (`Runtime/Forge` minus `Player/`), `DataKeeper.Forge.Editor` (`Editor/Forge`), `DataKeeper.Forge.Tests` (`Tests/Editor/Forge`).
- **Surface:** `Editor/Forge/SfxForgeWindow.cs` + `SfxForgeWindow.uss`, elements in `Editor/Forge/UI/`, help text in `Editor/Forge/ForgeHelp.cs`, docs in `Documentation~/SfxForge.md`. Opened from Tools > Windows > SFX Forge.
- **Reference:** Vital's oscillator section. Each row has a side tab with an on/off light, a small cluster of controls, a big display of the sound with a `< name >` stepper above it, and parameter boxes with a pill title on the top edge. One accent colour; off means grey.
- **Design (carried here, no spec):**
  - *Stepper:* `◀ value ▶`. Arrows wrap around. Clicking the value opens the full list. Right-click locks, as the dropdowns do today. No mouse-wheel stepping (FUI-D4).
  - *Layer strip:*
    - A side tab on the left: the enable light, the layer number, and a band colour.
    - A header: name, band pill, M/S and a `…` menu.
    - A display zone: a source-type stepper plus a variant stepper (wave / noise colour / bank / clip), then the layer's pictures.
    - Titled boxes for **VOICE** (Pitch, Offset, Decay), **AMP** (Level, Pan), **FILTER** (type stepper, Cutoff, Reso) and **SOURCE** (the type's own knobs; hidden for Oscillator and Noise).
    - The boxes wrap under the display when the centre column is narrow, and sit in one row when it is wide (after phase 2).
  - *Mini display:* the layer's own render, before mixing and FX (FUI-D3). Two pictures (FUI-D7):
    - A scope with two cycles at the loudest point, so the wave shape is visible.
    - The envelope over the whole sound, scaled to its own peak, with the playhead.
    - Both are flat when the layer is silent.
  - *Compact mode:* a toggle in the Layers header hides the boxes on every strip, leaving the side tab and display zone (FUI-D5).
- **Success criteria:**
  - *Measurable:* all four Forge assemblies compile, and the EditMode suite is green, including the new layer-output test.
  - *Feel:* you can tell what each layer is from its picture without reading anything. Flipping a waveform with ▶ is one click, and you hear it through autoplay. Every control has the same layout in every strip, so the eye knows where to look.
- **Non-goals:**
  - Any DSP, recipe-format or preset-format change (the renderer only gains read accessors).
  - New sound features.
  - Runtime player UI.
  - Theming or light-skin support.

## For Future Agents
The plan file is the source of truth; the conversation is not.

**Resuming:** read **Progress** and **Now**, then only the current phase. `grep -n "^## \|^\*\*Now:"` gives you the map; jump straight into the one phase.

**Working:** tick `- [x]` items as they are done. When a phase completes:
1. Set its **Progress** row and update **Now**.
2. Run the **Verification Plan** and record the outcome in **Verification Results**.
3. Write the **Phase Summary**.

Never rewrite a completed phase summary; append a correction. Log decisions and open questions in **Decisions & Open Questions**.

Compile check without focusing Unity: see the memory note "forge-compile-check" (reuse Unity's Bee `.rsp` files with the bundled Roslyn). The user runs EditMode tests in Unity themselves.

## Phase 1: Stepper control, layer strip rebuild, per-layer mini display
Status: Complete

- [x] `SfxRenderer`: add read accessors `LayerCount`, `IsLayerAudible(i)`, `LayerFrameRange(i, out start, out end)`, `LayerOutput(i)` (a slice of the existing per-layer buffer). Rendering itself is unchanged.
- [x] Test: with FX off, the audible layers' outputs add up to `Output`; a muted layer reports not audible.
- [x] `WaveformElement`:
  - Add a `SetSamples` overload that takes a frame range and treats samples outside it as silence, because the per-layer buffer is only written inside the voice range.
  - Add a `Normalize` option.
- [x] `StepperElement` (`Editor/Forge/UI/StepperElement.cs`): wraps a real `EnumField` (styled as a centred pill), so binding and the dropdown list come for free (FUI-D2). ◀ ▶ step through the values with wrap-around.
- [x] `ParamBoxElement` (`Editor/Forge/UI/ParamBoxElement.cs`): a box with a pill title on its top edge and a row of content. Reused by phase 3.
- [x] Rebuild `LayerStripElement` to the layout in Context:
  - Side tab with enable light, number and band colour.
  - Header: name, band, M/S, and a `…` menu with Duplicate, Remove, Lock Layer, Lock Curves and Clear Parameter Locks. The `L` toggle and the `Dup` and `×` buttons move into that menu.
  - Steppers for source, wave, noise colour, bank, filter and interpolation.
  - The VOICE / AMP / FILTER / SOURCE boxes.
  - Keep every existing lock path working: right-click a knob, right-click a stepper, the header menu.
- [x] `ScopeElement` (`Editor/Forge/UI/ScopeElement.cs`): a line trace of two cycles from the layer's loudest point, aligned to a rising zero crossing and clamped to 2–40 ms (FUI-D7).
- [x] Mini display: `SfxForgeWindow.RenderNow` sends each strip its layer slice. `UpdatePlayhead` sends the playhead. `RebuildStrips` refills the displays from the last render.
- [x] Add a Compact toggle in the Layers header that collapses every strip's boxes. Store it in a window field with `[SerializeField]`.
- [x] USS:
  - Styles for the stepper, param box, side tab and display zone.
  - Remove the dead `forge-strip__*` rules (`__flag--lock`, `__action`, `__dropdown`).
  - Keep the existing colour tokens.
- [x] Update the `ForgeHelp.Layers` text and the layer section of `Documentation~/SfxForge.md` to the new controls.

### Affected Assets
- Code: `Runtime/Forge/Render/SfxRenderer.cs`, `Editor/Forge/UI/WaveformElement.cs`, `Editor/Forge/UI/LayerStripElement.cs`, new `Editor/Forge/UI/StepperElement.cs`, `ParamBoxElement.cs` and `ScopeElement.cs` (+ `.meta`), `Editor/Forge/SfxForgeWindow.cs`, `Editor/Forge/ForgeHelp.cs`, new `Tests/Editor/Forge/LayerOutputTests.cs` (+ `.meta`).
- Styles: `Editor/Forge/SfxForgeWindow.uss`.
- Docs: `Documentation~/SfxForge.md`.
- No scenes, prefabs or ScriptableObjects. Recipes and presets are untouched.
- Revert path: `git checkout 2627a11 -- Editor/Forge Runtime/Forge/Render/SfxRenderer.cs`.

### Verification Plan
**Agent-runnable:**
- Compile Core → Editor → Tests with Unity's Roslyn (see the "forge-compile-check" memory note). Expected: 0 errors.

**Playtest (human, repeatable):**
- Run the EditMode tests (Test Runner > EditMode > `DataKeeper.Forge.Tests`). Expected: all green, including the new layer-output test.
- Open SFX Forge and load or create a recipe with 3+ layers (Impact category, Randomize).
  - Every strip shows its own pictures. In the left scope, a noise layer looks jagged, a sine layer is a smooth wave and a saw has a ramp shape. In the right envelope, different offsets and decays sit at different places in time.
  - Press Play: a playhead moves across every mini display together with the main waveform.
- On an oscillator layer, click ▶ on the wave stepper four times: Sine → Saw → Square → Triangle → Sine. Each click is heard (autoplay on), and Ctrl+Z steps back one value at a time.
- Click the wave name: the full list opens and picking an entry works.
- Change source to FM: the wave stepper hides and the SOURCE box shows Ratio, Index and Index Env. Change to Sample: the clip field appears in the variant slot.
- Right-click the wave stepper > Lock → its text turns lock-red. Randomize → that wave stays the same. Unlock clears it.
- Mute a layer → the strip dims and its display goes flat. Solo another → only the soloed layer has a waveform.
- `…` > Duplicate adds a copy below. `…` > Remove deletes it. `…` > Lock Layer turns the side tab lock-red.
- Toggle Compact: the boxes hide and each strip is a single short row. Toggle it back: everything returns.
- Resize the window to its 900px minimum: the boxes wrap under the display, nothing is clipped, and there is no horizontal scroll.

### Verification Results
- 2026-10-05, agent: Core, Runtime, Editor and Tests compiled with Unity 6000.6's Roslyn using Unity's Bee `.rsp` files. 0 errors. Unity had already imported the new files (their `.meta` files were generated).
- 2026-10-05, user: EditMode suite green, including `LayerOutputTests`.
- 2026-10-05, user screenshot (Impact recipe, 3 layers): the scope pictures, steppers and band edges work. Two layout bugs:
  - Thump's FILTER box was clipped at the bottom: a 100%-wide stepper in a wrapping row, plus the title pill's negative margin, made UI Toolkit measure the box too short.
  - Body's FILTER box wrapped onto its own line while Thump's didn't, because each box wrapped on its own.
  - Fixed: boxes now wrap as one `controls` group; FILTER is a stacked box (stepper row over a knob row); the title pill is absolutely positioned. Recompiled, 0 errors.
- 2026-10-05, user screenshots at a wide and a narrow window width: no clipping. Wide: every strip is one row and the boxes line up across strips. Narrow: the boxes drop below the pictures together, in the same layout on every strip. Pass. FM/Granular SOURCE box not shown in the screenshots.

### Phase Summary
- **Renderer:** `SfxRenderer` exposes each layer's pre-mix audio (`LayerCount`, `IsLayerAudible`, `LayerFrameRange`, `LayerOutput`) from the buffer it already renders into, so the strip pictures cost no extra rendering. `LayerOutputTests` covers this.
- **New elements** in `Editor/Forge/UI/`:
  - `StepperElement` wraps an `EnumField` (FUI-D2).
  - `ParamBoxElement` has a pill title in an absolutely positioned header; add the `--stacked` class for column content.
  - `ScopeElement` draws a two-cycle trace (FUI-D7).
- **`WaveformElement`** gained a frame-range `SetSamples` overload and `Normalize`.
- **`LayerStripElement` was rebuilt:**
  - Side tab, header with a `…` menu (shared item list with the right-click menu), and a display zone with steppers and the two pictures.
  - A `controls` group holding the VOICE / AMP / FILTER / SOURCE boxes. The group wraps as a unit, and FILTER is stacked.
- **Window:** pushes `ShowRender` and the playhead to the strips, and has the Compact toggle (`_compactStrips`).
- **Help and docs:** `ForgeHelp.Layers` and `Documentation~/SfxForge.md` are updated.
- **Layout lesson for later phases:** avoid percentage widths inside wrapping rows and negative margins in UI Toolkit, because both make Yoga measure boxes short and the strip clips them.
- **Assets:** no scene, prefab or ScriptableObject changes. Recipes and presets are untouched. Nothing is committed yet.

## Phase 2: Page tabs and top bar declutter
Status: Complete
Depends on: Phase 1

- [x] Make the param boxes in a strip equal height, with their content centred vertically. FILTER is stacked and currently taller than VOICE and AMP (seen in the phase 1 screenshots).
  - `.forge-strip__controls` now stretches its items (`align-items: stretch`), and `.forge-box` centres its content (`justify-content: center`).
- [x] Add page tabs under the top bar: **Sound** (waveform, curves, layers), **FX**, **Mod** (macros, LFO, routes), **Export**. Remember the last tab in a window field.
  - The tabs sit at the top of the centre column, because the left column stays the same on every page (FUI-D11). The last tab is stored in `[SerializeField] Page _page`.
- [x] Remove the right column; move its content onto the FX and Mod pages. The randomizer controls (harmony, variation, physics, candidates) move to the left column under Variations.
  - FX page: the effect sections wrap in a grid, 230px each. Phase 3 rebuilds them.
  - Mod page: MACROS and LFO `ParamBoxElement`s (the LFO shape is now a stepper), then a Routes header with the count, `+ Route` and `Defaults`, then the route list (max 560px). Routes are no longer in a foldout.
- [x] Move Export from the left-column foldout to its own page. The foldout and `_exportExpanded` / `_routesExpanded` are gone.
- [x] Top bar, left to right:
  - Recipe field + New.
  - Centred `◀ preset ▶` + save icon.
  - Dice (Randomize) + Mutate.
  - Undo/redo icons.
  - Transport as icons (▶ ■ autoplay), then meter and volume.
  - The volume slider was merged into the meter at the user's request (FUI-D14).
  - Icons are drawn by the new `IconElement` (FUI-D9). The preset browser reuses the stepper pill style with `< >` arrows (FUI-D12). Centred means between the left and right groups, using two spacers.
- [x] Category becomes a stepper next to the preset name, replacing the 8 left-column buttons. It is unbound: `StepperElement.Changed` → `SelectCategory`, and `RefreshSelectors` pushes the value back with `SetValueWithoutNotify`. `ForgeHelp.Category` is folded into a `Category` item in `ForgeHelp.TopBar`.
- [x] Replace the permanent hint labels with a hover hint in the status bar (control name + one line of help).
  - New `ForgeHints` registry. The help items are the single source of the text (`HelpTopic.TextOf`, FUI-D13).
  - The window listens to `PointerOverEvent` on the root and walks up from the target.
  - The curve-hint and right-column hint labels are removed. The old right-column tip is now the idle status text.
- [x] Check that the layer strips now sit in one row at the 900px minimum.
  - Arithmetic: 617px of strip body. Display basis is 176 (was 240) + 4 margin. VOICE 168 + AMP 116 + FILTER 116 = 580 ≤ 617, so the strip should be one row.
  - The first pass used 200, which gave 604: only about 2px of headroom. The screenshot at about 830px confirmed how thin that was.
  - An FM/Wavetable/Sample/Granular SOURCE box (+60–170px) will still wrap at 900. That is expected.
  - Needs the user's screenshot.

### Affected Assets
- Code:
  - `Editor/Forge/SfxForgeWindow.cs`, `Editor/Forge/ForgeHelp.cs`, `Editor/Forge/UI/StepperElement.cs` (`Changed`, `SetValueWithoutNotify`) and `Editor/Forge/UI/LayerStripElement.cs` (hints; tooltips removed).
  - New `Editor/Forge/ForgeHints.cs` and `Editor/Forge/UI/IconElement.cs` (+ `.meta`, which Unity generated).
- Styles: `Editor/Forge/SfxForgeWindow.uss`.
  - Removed: `forge-right*`, `forge-hint`, `forge-curve-hint`, `forge-category-list`, `forge-preset-name`, the `forge-export` foldout rules, `forge-help-button--corner`, `forge-macros`, `forge-mod-panel`, `forge-lfo`, `forge-routes__buttons`, `forge-fx-panel`, `forge-fx-header` and `forge-volume > Label`.
  - Added: pages, status bar, icons, category/preset steppers, mod page, FX grid and export page.
- Docs: `Documentation~/SfxForge.md` (Window table, Randomizer, Export, Presets).
- Window state: the serialized `_routesExpanded` / `_exportExpanded` were removed and `_page` was added. An open window just takes the defaults. No recipe, preset, scene or prefab changes.
- Revert path: phases 1 and 2 are both uncommitted on top of `2627a11` and share files, so there is no phase-2-only revert. `git checkout 2627a11 -- Editor/Forge` drops both. Commit phase 1 first if a clean split is wanted.

### Verification Plan
**Agent-runnable:**
- Compile Core → Editor with Unity's Roslyn (the "forge-compile-check" memory note; driver in the session scratchpad, `compile.py`). Expected: 0 errors. Tests and Runtime don't reference the Editor assembly.
- Every `Hint(...)` / `ForgeHints.Set(...)` item name must exist in a `ForgeHelp` topic. A regex scan over the window and strip sources is expected to report 0 missing.

**Playtest (human, repeatable):**
- Open SFX Forge with a 3-layer Impact recipe. Expected: tabs Sound / FX / Mod / Export over the centre, no right column, and the left column shows Variations, Seed, then Randomizer.
- Click FX, close and reopen the window → it opens on FX. Each page shows its content: the FX grid; Macros + LFO boxes with Routes (n) below; the Export form.
- Top bar: Recipe, New … `< Impact >` `< preset >` save … dice, Mutate | undo, redo | play, stop, autoplay, meter, volume, `?`. Each icon is legible, and play and dice are drawn dark on orange.
- Click the category ▶ arrow → it moves to Whoosh, new variations are generated and play. Ctrl+Z restores the previous sound and the stepper shows the old category.
- Hover the Pitch knob on a strip → the status bar shows **Pitch** Transpose in semitones. Hover the curve editor → **Mouse** plus the mouse help. Move onto empty background → the last status message returns.
- Press the dice → the "Randomize: kept 8 of …" message shows at once, even with the mouse still over the dice.
- Meter as volume fader:
  - Drag sideways on the meter → the thin line in its middle moves (orange on hover). Preview playback gets quieter or louder, and the status bar shows "Preview volume … dB".
  - Shift-drag moves it 10× finer. Double-click resets it to 80%.
  - A plain click without dragging only clears the clip light, and the volume doesn't jump.
  - Reopen the window → the volume is kept.
- Left panel (FUI-D15):
  - From top to bottom: Randomize / Mutate, Harmony, Variation / Physics, Candidates, Variations grid, Seed. The top bar has no Randomize or Mutate.
  - Click `«` left of the Sound tab → the panel hides and the button shows `»`. Reopen the window → it is still hidden. Press R → it still randomizes. Click `»` → the panel returns.
- Autoplay icon toggles orange. Edits auto-play only when it is on.
- Sound page: in each strip, VOICE, AMP and FILTER are the same height, and the knobs are centred vertically in the shorter boxes.
- Resize the window to its 900px minimum: Oscillator and Noise strips stay one row with no clipping. The top bar has no overlapping controls.

### Verification Results
- 2026-10-05, agent: Core and Editor compiled with Unity 6000.6's Roslyn. 0 errors, no Forge warnings. The hint-name scan found 53 names used and 0 missing.
- 2026-10-05, agent: after the meter/volume merge (FUI-D14), the Editor assembly recompiled with 0 errors.
- 2026-10-05, agent: after the left-panel reorder, the collapse button and the move of Randomize/Mutate (FUI-D15), the Editor assembly recompiled with 0 errors.
- 2026-10-05, user screenshots (Impact, 3 layers): about 1000px wide with the panel open and collapsed, and about 830px wide with it collapsed and open. Widths are inferred from the 52px knob pitch at 2× scale.
  - **Pass:**
    - Page tabs and the Sound page.
    - Top bar layout, the icons and the meter's volume line.
    - `«`/`»` collapse.
    - Randomizer above Variations.
    - Hover hints (Mouse, Pan) and the idle status text.
    - VOICE/AMP/FILTER equal height.
    - Strips are one row at about 1000px, and at about 830px with the panel collapsed.
  - **Bugs found and fixed (USS only):**
    - The selected curve tab was unreadable: `.forge-tab` overrode `.forge-selected`'s background, a bug from before phase 2. Added `.forge-tab.forge-selected`.
    - The curve bar's `?` was clipped at about 830px. The bar now wraps.
  - **Strips at about 830px with the panel open wrap under the display.** Docked windows ignore `minSize`, so this is below the 900 minimum. But the arithmetic left only about 2px of headroom at 900, so for about 26px more:
    - Display basis/min went from 200 to 176.
    - The source-type stepper went from 116 to 96, and the variant min from 90 to 76.
  - **Not shown yet:** FX/Mod/Export pages, category stepping + undo, meter drag, autoplay toggle, and the same layout at exactly 900px.
- 2026-10-05, user: "all works". The remaining playtest steps pass, including the FX/Mod/Export pages, category stepping + undo, meter drag, autoplay and the 900px one-row check.

### Phase Summary
- **Pages:** the centre column is split into Sound / FX / Mod / Export tabs (FUI-D11), stored in `[SerializeField] Page _page`. The right column is gone; its FX sections sit in a wrapping grid on the FX page (placeholder for phase 3), and macros, LFO and routes are on the Mod page. Export is its own page.
- **Left panel (FUI-D15):** Randomize/Mutate, Randomizer settings, then Variations and Seed. The `«`/`»` button in the tab bar hides it (`_leftCollapsed`).
- **Top bar:** Recipe + New, a centred category stepper + preset stepper + save, undo/redo, transport icons, and the meter that doubles as the preview volume fader (FUI-D14). Icons are drawn by `IconElement` (FUI-D9).
- **Category stepper** is unbound and drives `SelectCategory`; `RefreshSelectors` pushes the value back.
- **Hover hints:** `ForgeHints` registry; the status bar shows the hovered control's name and its `ForgeHelp` text (FUI-D13). `SetStatus` overrides a hint until the pointer moves to another element (FUI-D10). Permanent hint labels are gone.
- **Layout:** the strip's VOICE/AMP/FILTER boxes are equal height, and the display basis is 176px so Oscillator/Noise strips stay one row at 900px.
- **USS fixes found in playtest:** `.forge-tab.forge-selected` (unreadable selected curve tab) and a wrapping curve bar.
- **Assets:** window state changed (`_page`, `_leftCollapsed` added; `_routesExpanded`, `_exportExpanded` removed). No recipe, preset, scene or prefab changes. Nothing is committed yet.

## Phase 3: FX modules with visualisations
Status: Complete
Depends on: Phase 2

- [x] `ParamBoxElement`: the title becomes a pill container (`__pill`) holding the label, so a box can carry an optional power light (`AddPower()`), styled like the strip's enable light. Clicking the title text toggles it. Strip and Mod-page boxes look the same as before.
- [x] FX page: each effect is a `ParamBoxElement` module with a power light, stacked top to bottom in chain order (FUI-O1, decided). Each module is one row: graph on the left, controls on the right.
  - A disabled module collapses to its title row (`forge-fx--off`, driven from the recipe in `RefreshSelectors`, so undo and randomize keep it right).
  - Distortion's mode dropdown becomes a stepper.
  - The old 230px grid and the `forge-fx__enable` toggles go.
- [x] `FxGraphElement` (`Editor/Forge/UI/FxGraphElement.cs`): a small `Painter2D` graph with a bright trace (filled under it for time graphs), a dim reference trace and an optional horizontal marker. Zero-GC refills through spans.
- [x] `FxGraphBuilder` (`Editor/Forge/FxGraphBuilder.cs`): fills the graphs by running the real Core FX code on short synthetic signals at 4 kHz (FUI-D16). Native scratch buffers are allocated once and disposed in `OnDisable`.
  - Distortion: transfer curve, `lerp(x, Distortion.Shape(x·drive), mix)`, over a dim y = x.
  - Delay: impulse response of `StereoDelay` over the recipe length: the dry spike, then the taps.
  - Reverb: `Reverb` impulse response over the recipe length, wet only, in dB (60 dB range).
  - Transient: a synthetic hit (1 ms attack, 60 ms decay) through `TransientShaper`; the input envelope dim, the shaped one bright.
  - Limiter: a quiet signal with a +6 dB burst through `Limiter`, in dB; input dim, output bright, the ceiling as the marker. The dip after the burst shows Release.
- [x] The window refreshes the graphs from `RenderNow` (already throttled) while the FX page is showing, and on switching to it.
- [x] Update `ForgeHelp.Fx` (power light, graphs, chain order) and the FX section of `Documentation~/SfxForge.md`.

### Affected Assets
- Code: `Editor/Forge/SfxForgeWindow.cs`, `Editor/Forge/UI/ParamBoxElement.cs`, `Editor/Forge/ForgeHelp.cs`, new `Editor/Forge/UI/FxGraphElement.cs` and `Editor/Forge/FxGraphBuilder.cs` (+ `.meta`, which Unity generates on import).
- Styles: `Editor/Forge/SfxForgeWindow.uss` (box pill, power light, FX modules, graph).
- Docs: `Documentation~/SfxForge.md`.
- No Core/DSP change: the graphs call the existing public `Distortion`, `StereoDelay`, `Reverb`, `TransientShaper`, `Limiter` and `FxParams.DelayFramesFor`. No recipe, preset, scene or prefab changes.
- Revert path: still uncommitted on top of `2627a11` together with phases 1–2; `git checkout 2627a11 -- Editor/Forge` drops all three.

### Verification Plan
**Agent-runnable:**
- Compile Core → Editor with Unity's Roslyn (the "forge-compile-check" memory note). Expected: 0 errors.
- Hint-name scan (as in phase 2). Expected: 0 missing.

**Playtest (human, repeatable):**
- Open SFX Forge with a 3-layer Impact recipe, go to FX. Expected: five modules stacked Transient, Distortion, Delay, Reverb, Limiter, each with a pill title on its top edge and a light in the pill.
- Click a module's light, or its title text → it turns on/off. Off: the module collapses to its title row and the light is grey. Ctrl+Z restores it. Randomize (with FX unlocked) → the collapsed state follows the new settings.
- Strips (Sound page) and Macros/LFO (Mod page) boxes look unchanged.
- Distortion on: Tanh shows an S-curve; raising Drive steepens it. Foldback with high Drive zig-zags. Mix 0 → the curve becomes the dim diagonal. The mode stepper's `< >` flips Tanh/Foldback.
- Delay on: a spike at the left, then evenly spaced taps; shorter Time → closer taps; higher Feedback → more taps; lowering the Length knob on the Sound page shows fewer taps.
- Reverb on: a falling slope; bigger Size → slower fall.
- Transient on: Attack +100% → the bright start rises above the dim input; −100% → it sits below. Sustain moves the tail the same way.
- Limiter on: the bright line is flat at the ceiling marker during the burst; lowering Ceiling moves both; longer Release → a longer dip after the burst.
- Dragging an FX knob updates its graph while dragging, with no visible stutter.
- Resize to 900px: modules don't clip, and the controls wrap under the graph if needed.

### Verification Results
- 2026-10-05, agent: Core and Editor compiled with Unity 6000.6's Roslyn. 0 errors, no Forge warnings. Hint names are unchanged (the five effect names and Lock, all in `ForgeHelp.Fx`).
- 2026-10-05, user: "all works". The Phase 3 playtest passes: module order and pills, power light/title toggle with collapse, undo and randomize, all five graphs reacting to their knobs, live updates while dragging, and the 900px layout.

### Phase Summary
- **Modules:** `ParamBoxElement` titles are now a pill (`__pill`) with an optional power light (`AddPower()`); clicking the light or the title text toggles the effect. The FX page stacks Transient, Distortion, Delay, Reverb, Limiter in chain order (FUI-O1), one row each: graph left, controls right. Off modules collapse to their title row (`forge-fx--off`), driven from the recipe in `RefreshSelectors`, so undo and randomize stay in step. Distortion's mode is a stepper.
- **Graphs:** `FxGraphElement` draws a bright trace (filled for time graphs), a dim reference and an optional marker with `Painter2D`, refilled through spans. `FxGraphBuilder` runs the real Core FX code on synthetic signals at 4 kHz (FUI-D16); its native scratch buffers live for the window and are disposed in `OnDisable`. Graphs refresh from the throttled `RenderNow` while the FX page shows, and on switching to it.
- **Docs:** `ForgeHelp.Fx` and the FX section of `Documentation~/SfxForge.md` cover the power light, graphs and chain order.
- **Assets:** no recipe, preset, scene or prefab changes; no Core/DSP change. Still uncommitted on top of `2627a11` with phases 1–2.

## Phase 4: Drag-to-modulate with knob arcs
Status: Complete
Depends on: Phase 2

- [x] Source bar (`ModSourceBarElement`) under the page tabs, visible on every page: one drag chip per `ModSource` (Size, Energy, Tone, Motion, LFO, Env, Rnd), each in its source colour (FUI-D17).
  - Dragging shows a ghost label that follows the pointer, highlights every knob that can take a route, and marks the hovered knob as accepted or refused. The status bar says what a drop would do, or why it is refused.
- [x] Knobs carry an optional `ModTarget` and layer:
  - Strip: Pitch, Cutoff, Level, Pan, Decay, Reso, with the strip's layer index.
  - Global: Length, Transient Attack, Drive, Delay Mix, Reverb Mix, LFO Rate.
  - `LfoDepth` has no knob and is reachable only from the matrix.
- [x] A drop adds a route at 25% of `ModTargets.MaxAmount` with undo:
  - On a layer knob, the route covers that layer; Alt+drop covers all layers (FUI-D18).
  - Refused: LFO/Env on a target that isn't evaluated at control rate, and a source/target/layer combination that already exists.
- [x] Knob arcs: each active route draws its range on an inner ring in the source colour, `base ± amount` (log knobs `base · 2^±amount`; Env is unipolar), with a dot at the `+amount` end.
- [x] Knob chips (`ModChipElement`) overlay the dial's bottom gap, one per route that reaches the knob, so knob size and strip layout never change (FUI-D19).
  - Drag a chip vertically to set the amount (Shift for fine; one undo step per drag). Double-click zeroes it.
  - Right-click: Enabled, This layer / All layers, Remove.
  - An all-layers route on a layer knob shows a hollow chip. A disabled or unsupported route shows a dim chip with no arc.
- [x] Matrix: `ModRouteElement` becomes one row (FUI-D20): source-colour swatch, power light, source stepper, `→`, target stepper, layer stepper, a bipolar amount bar (`ModAmountElement`) in the source colour, and `×`.
- [x] `ForgeModulation` controller: source colours/names, drop validation, route edits, and refreshing arcs and chips on every recipe change, undo and strip rebuild.
- [x] Update `ForgeHelp.Modulation` (source bar, drop, chips, matrix) and the modulation section of `Documentation~/SfxForge.md`.

### Affected Assets
- Code: `Editor/Forge/SfxForgeWindow.cs`, `Editor/Forge/UI/KnobElement.cs`, `Editor/Forge/UI/LayerStripElement.cs`, `Editor/Forge/UI/ModRouteElement.cs`, `Editor/Forge/ForgeHelp.cs`. New: `Editor/Forge/ForgeModulation.cs`, `Editor/Forge/UI/ModSourceBarElement.cs`, `Editor/Forge/UI/ModChipElement.cs`, `Editor/Forge/UI/ModAmountElement.cs` (+ `.meta`, which Unity generates).
- Styles: `Editor/Forge/SfxForgeWindow.uss` (source bar, drop highlight, chips, matrix row, amount bar).
- Docs: `Documentation~/SfxForge.md`.
- No Core/DSP or recipe-format change: routes are still `ModRoute` entries in `SfxRecipe.Routes`.
- Revert path: uncommitted on top of `2627a11` with phases 1–3.

### Verification Plan
**Agent-runnable:**
- Compile Core → Editor with Unity's Roslyn (the "forge-compile-check" memory note). Expected: 0 errors.
- Hint-name scan. Expected: 0 missing.

**Playtest (human, repeatable):**
- Open SFX Forge with a 3-layer Impact recipe. The source bar shows seven coloured chips under the page tabs, on every page.
- Sound page: drag **Size** onto layer 2's Pitch.
  - While dragging, all target knobs are highlighted and Pitch shows accepted.
  - After the drop, layer 2's Pitch has a blue arc and one chip; layers 1 and 3 don't. A new matrix row reads Size → Pitch, Layer 2. Ctrl+Z removes it.
- Alt+drag **Tone** onto layer 1's Cutoff → arcs and hollow chips appear on every layer's Cutoff.
- Drag **LFO** onto Decay → refused, and the status bar explains why. Drag **Size** onto layer 2's Pitch again → refused as a duplicate.
- FX page: drag **Energy** onto Drive → a route with an arc and a chip.
- Drag a chip up and down → the arc widens or narrows, the matrix amount follows, and one Ctrl+Z restores it. Double-click zeroes it. Right-click → Remove deletes the route.
- Default routes: Size → Pitch (−5 st) shows an arc on every Pitch knob, with its dot on the lower side.
- Mod page: rows are one line each. Steppers change source, target and layer. Dragging the amount bar changes the amount, and the knob arcs follow. A row for an unsupported pair is dimmed.
- Strip layout at 900px is unchanged with chips present.

### Verification Results
- 2026-10-05, agent: Core, Runtime, Editor and Tests compiled with Unity 6000.6's Roslyn. 0 errors, no Forge warnings. Hint scan: 0 missing; the new names `Source bar` and `Chips` are in `ForgeHelp.Modulation`.
- Build note: `KnobElement`'s shared formatter is the static `FormatAs(KnobFormat, float)`, because the instance already has a `Format` property. The chips and the amount bar use it.
- 2026-10-05, user: "all works". The Phase 4 playtest passes: source bar on every page, drop highlight and refusals, layer and Alt all-layers drops, arcs and chips, chip drag/zero/menu with undo, one-line matrix rows, and the 900px layout.

### Phase Summary
- **Source bar** (`ModSourceBarElement`) sits between the page tabs and the pages. Each chip captures the pointer, and after a 3px move it shows a ghost label (added to the window root) and puts `forge-mod-dragging` on the root. That class highlights every knob with a `ModTarget`. `panel.Pick` finds the knob under the pointer; `ForgeModulation.CanDrop` decides accept or refuse and writes the status text.
- **`ForgeModulation`** is the single owner of the source colours/names, drop rules (FUI-D18), and route edits from chips (one undo group per drag). `Refresh()` queries every `KnobElement` with a `ModTarget` and rebuilds its arcs and chips. It runs from `RefreshSelectors`, `RebuildStrips` and route commits, so undo, randomize and layer edits keep it right.
- **`KnobElement`** has `ModTarget?`/`ModLayer`, a lazily created `__mods` chip row in the dial, change-detected arcs (`BeginModArcs`/`AddModArc`/`EndModArcs`, repainting only on a change), drop states, and the static `FormatAs(KnobFormat, float)`. Arcs use `base ± amount`, or `base · 2^±amount` on log knobs.
- **Matrix:** `ModRouteElement` is one row with steppers, a layer stepper and menu, and `ModAmountElement` (a bindable bipolar bar).
- **Assets:** no recipe-format or Core change; routes are plain `ModRoute` entries. Help (`Source bar`, `Chips`) and the docs are updated.
- **Playtest:** user, 2026-10-05: "all works".

## Decisions & Open Questions
- **FUI-D1 (decided):** No `specs/` folder in this repo; the user asked for a plan straight away, so the design lives in this plan's Context.
- **FUI-D2 (decided):** `StepperElement` wraps a real `EnumField` rather than implementing `INotifyValueChanged<Enum>` itself. Binding enum properties and the native dropdown list then work unchanged, and stepping just sets the inner field's value, so undo and change events behave exactly like the dropdowns do today.
- **FUI-D3 (decided):** Mini displays are scaled to their own peak so a quiet layer's shape is still readable; the Level knob shows the level. They show what the renderer produced, so a muted or soloed-out layer is flat. That matches what you hear.
- **FUI-D4 (decided):** No mouse-wheel stepping on steppers. The layer list scrolls with the wheel, and steppers under the cursor would change values by accident.
- **FUI-D5 (decided):** Compact mode is one window-wide toggle rather than per-strip collapse, to avoid storing per-layer UI state in the recipe.
- **FUI-D6 (decided):** The plan lives in `plans~/` because this repo is a UPM package; a plain `plans/` folder would be imported by Unity and get `.meta` files.
- **FUI-D7 (decided, during build):** A min/max envelope over the whole sound looks the same for sine, saw and square at audio rates, so it can't show what a layer *is*. Each strip therefore shows two pictures, like Vital's 2D display: a scope of two cycles at the loudest point, aligned to a zero crossing so it holds still, plus the envelope for timing. The scope window is clamped to 2–40 ms so noise still shows texture and sub tones fit.
- **FUI-D8 (decided, during build):** The menu button uses `…` (U+2026) rather than `⋯`. The editor font is known to have it, because the export folder button already uses it.
- **FUI-D9 (decided, phase 2):** Top-bar icons (play, stop, autoplay, undo, redo, dice, save) are drawn with `Painter2D` in `IconElement`. Their colour is `--icon-color`, which turns dark on `forge-primary` and on checked toggles. This avoids the editor font, which lacks most symbols (FUI-D8), and built-in editor icon names, which change between Unity versions.
- **FUI-D10 (decided, phase 2):** `SetStatus` always shows the message straight away, even over a hinted control. Otherwise the result of pressing the dice would be hidden behind the dice's own hint. Hovering a different element brings hints back.
- **FUI-D11 (decided, phase 2):** Page tabs head the centre column rather than spanning the window, because the left column (variations, randomizer) is the same on every page.
- **FUI-D12 (decided, phase 2):** The preset and category browsers use the stepper's `< >` arrows, not the `◀ ▶` the plan text names, so every stepper in the window looks the same.
- **FUI-D13 (decided, phase 2):** Hover hints read their text from the `ForgeHelp` items (`HelpTopic.TextOf`), so the `?` cards and the status bar can't drift apart. Tooltips that repeated help text were removed. The tooltips that show live data stay: thumbnail analysis and volume in dB.
- **FUI-D14 (decided, phase 2, user request):** The preview volume slider is merged into the meter.
  - The volume is drawn as a line in the 2px gap between the channel bars, ending in a full-height tick.
  - Dragging is relative, so grabbing the meter never makes the volume jump. A 2px threshold separates a drag from the click that clears the clip light.
  - The position→gain mapping (squared) and the EditorPrefs key are unchanged, so saved volumes carry over.
  - The dB value moved from the slider tooltip to the status bar while dragging.
- **FUI-D15 (decided, phase 2, user request):** Left panel changes.
  - Order: Randomize and Mutate (moved from the top bar), the Randomizer settings, then Variations and Seed.
  - The `«`/`»` button at the left end of the page-tab bar hides the whole panel. Its state is `[SerializeField] _leftCollapsed`.
  - The button lives in the tab bar, so it never hides itself.
  - R and M still work while the panel is hidden.
  - The dice icon was dropped from `IconElement`: the buttons are plain text, because it was no longer used.
- **FUI-O1 (decided, phase 3):** The FX chain order stays fixed. Reordering would change `FxChainJob` and the recipe format, both non-goals. The FX page stacks the modules top to bottom in the order they run, so the page itself shows the order. Reordering would be a separate feature.
- **FUI-D16 (decided, phase 3):** FX graphs come from running the real Core FX functions on short synthetic signals (an impulse, a hit, a burst) at 4 kHz, not from the render. The render only exposes the post-FX mix, and a graph of an effect's own response stays readable whatever the layers sound like. Using the Core code rather than re-deriving formulas keeps the graphs honest if the DSP changes. 4 kHz keeps a 4 s reverb response to about 16k frames.
- **FUI-D17 (decided, phase 4, user choice):** Mod sources live in a source bar under the page tabs on every page, because the macros (Mod page) and the knobs they target (Sound/FX pages) are never on screen together. Each source has a fixed colour used by its chip, arcs and matrix row.
- **FUI-D18 (decided, phase 4, user choice):** Dropping on a layer knob routes to that layer only; Alt+drop routes to all layers. All-layers routes show on every strip's knob as a hollow chip.
- **FUI-D19 (decided, phase 4, user choice):** Amounts are edited with Vital-style chips; the arc is display only, because one knob can carry several routes and grabbing "the arc" would be ambiguous. Chips sit in the dial's bottom gap so knobs keep their size.
- **FUI-D20 (decided, phase 4, user choice):** The matrix is one compact row per route rather than a sources × targets grid, which could not show per-layer routes.
- **FUI-O2 (decided 2026-10-05, deferred to a new plan):** The user asked for Vital features this plan rules out as DSP/recipe-format changes. They go into a separate `plans~/sfx-forge-sound-features.md`, written after Phase 4 is playtested and this plan is closed. All four were chosen:
  - A note keyboard after Length. It sets a recipe Root Note that transposes every layer, and plays it.
  - LFO 1–3, each with shape, rate, phase and retrigger/free.
  - Mod envelopes ENV 2/3 as drawn curves, plus Rnd modes: constant, sample-and-hold, Perlin.
  - Per-layer unison (voices, detune, spread) and start phase.
  - Today: Env is the layer's Amp curve, and Rnd is a per-route, per-layer constant from the Seed. Neither has settings.
