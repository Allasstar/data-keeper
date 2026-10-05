# SFX Forge Sound Features

Add the Vital-style sound features the UI overhaul left out (FUI-O2 in `plans~/archive/sfx-forge-ui-overhaul.md`):
- a root note set from a piano keyboard;
- unison and oscillator start phase per layer;
- three LFOs;
- two drawn mod envelopes;
- Rnd modes that can move over time.

These change the recipe format and the DSP. Old recipes and presets must still render bit-identical.

Written 2026-10-05 against `main` @ `3485b3d`. Local ids are `FSF-n`.

## Progress
| # | Phase | Status | Verified |
|---|-------|--------|----------|
| 1 | Root note and keyboard | Complete | 2026-10-05 |
| 2 | Unison and start phase | Complete | 2026-10-05 |
| 3 | Modulation engine: LFO 1–3, ENV 2/3, Rnd modes | Complete | 2026-10-05 |
| 4 | Mod page source panels and source bar | Complete | 2026-10-05 |

**Now:** All phases are complete. Waiting on the user: run the EditMode suite once more for the FSF-O3 hash fix (`ModulationTests.StaticRandom_IsNotEvenlySteppedAcrossSeeds`, plus the existing Rnd/unison tests), then commit. After that, close the plan and move it to `plans~/archive/`.

## Context
- **Specs:** this repo has no `specs/` folder, so the design is described below (FSF-D1, same as FUI-D1).
- **Engine:** Unity 6000.6.0f1. Assemblies:
  - `DataKeeper.Forge.Core` (`Runtime/Forge` minus `Player/`): model, Burst render jobs, randomizer.
  - `DataKeeper.Forge.Runtime` (`Runtime/Forge/Player`): runtime clip rendering, which picks up every Core change for free.
  - `DataKeeper.Forge.Editor` (`Editor/Forge`).
  - `DataKeeper.Forge.Tests` (`Tests/Editor/Forge`).
- **Render path:**
  - `SfxRenderer.Render` runs `ModMatrix.Evaluate`, then `BuildLayers` fills `LayerRenderParams`.
  - `LayerRenderJob` (per layer, control rate 32 samples) → `MixJob` → `FxChainJob`.
  - Static mod sources become fixed offsets before rendering. Continuous ones (LFO, Env) become per-layer `float4` depths over Pitch / Cutoff / Level / Pan, applied in `EvaluateControl`.
- **Surface:** `Editor/Forge/SfxForgeWindow.cs` + `.uss`, `Editor/Forge/UI/LayerStripElement.cs`, `Editor/Forge/ForgeModulation.cs`, `Editor/Forge/UI/ModSourceBarElement.cs`, `Editor/Forge/ForgeHelp.cs`, `Documentation~/SfxForge.md`.
- **Design (carried here, no spec):**
  - *Root note:*
    - `SfxRecipe.RootNote` is a MIDI note, default 69 (A4). Every layer is transposed by `RootNote − 69` semitones.
    - Layer Pitch stays relative, so 69 changes nothing.
    - A piano strip on the Sound page's globals bar, after Length, spans C2–B6. Clicking a key sets the root (with undo) and plays it. The root key is lit, and its name (e.g. `A4`) shows beside the strip.
  - *Unison and phase (per layer):*
    - `Layer.Unison`: Voices 1–8, Detune 0–100 cents (total width), Spread 0–1 (stereo).
    - `Layer.Phase`: Start 0–1, plus a Random toggle (random per voice, from the layer seed).
    - Applies to Oscillator, Wavetable and FM. Voices are detuned evenly across ±Detune/2 and panned alternately across ±Spread, with gain 1/√Voices.
    - The strip gets a **UNISON** box (Voices, Detune, Spread, Phase, Rnd) for those three source types.
  - *LFO 1–3 (recipe-level):*
    - `Lfo` stays as LFO 1, so old data keeps working. `Lfo2` and `Lfo3` are added.
    - Each LFO has Shape, Rate, Phase 0–1, and Mode: Retrigger (time from the layer's start, as today) or Free (time from the sound's start).
    - The `LfoRate`/`LfoDepth` targets still act on LFO 1 only.
  - *ENV 2/3:* recipe-level drawn curves (0..1) spanning the whole sound, shared by all layers (FSF-D3). Env 1 stays each layer's Amp curve.
  - *Rnd modes:* `SfxRecipe.Random`, with Mode and Rate.
    - **Constant** is today's behaviour: one value per route and layer from the Seed, reaching every target.
    - **Sample & Hold** steps to a new value at Rate.
    - **Smooth** is value noise at Rate.
    - In the two moving modes Rnd becomes a continuous source, so it only reaches Pitch / Cutoff / Level / Pan, like LFOs.
  - *Continuous sources:* LFO 1–3, Env 1–3, and Rnd in a moving mode. All of them reach only the control-rate targets.
  - *Source order:* the `ModSource` enum gains `Lfo2 = 7, Lfo3 = 8, Env2 = 9, Env3 = 10`, appended so serialized routes keep their meaning. The source bar shows them in a logical order: macros, LFOs, Envs, Rnd.
- **Success criteria:**
  - *Measurable:*
    - All four Forge assemblies compile, and the EditMode suite is green.
    - `DeterminismTests` and `ModulationTests.DefaultRoutes_AtNeutralMacrosRenderBitIdenticalToNoRoutes` still pass unchanged.
    - New tests prove that default values render bit-identical to before every feature.
  - *Feel:*
    - Clicking piano keys plays the sound up and down the keyboard like an instrument.
    - 5-voice unison on a saw layer sounds wide and thick.
    - An LFO 2 or ENV 2 route visibly moves the knob arc's target and audibly shapes the sound.
    - The window stays responsive while dragging knobs on a 6-layer recipe with unison.
- **Non-goals:**
  - Tempo sync. SFX have no tempo.
  - Drawn LFO shapes.
  - Continuous modulation of FX or global targets (Drive, mixes, Length…).
  - Unison on Noise, Sample or Granular sources.
  - MIDI input, and polyphonic playback.
  - Randomizer templates that generate unison (FSF-D4).

## For Future Agents
The plan file is the source of truth; the conversation is not.

**Resuming:** read **Progress** and **Now**, then only the current phase. `grep -n "^## \|^\*\*Now:"` gives you the map.

**Working:** tick `- [x]` items as they are done. When a phase completes:
1. Set its **Progress** row and update **Now**.
2. Run the **Verification Plan** and record the outcome in **Verification Results**.
3. Write the **Phase Summary**.

Never rewrite a completed summary; append a correction. Log decisions and open questions in **Decisions & Open Questions**.

**Bit-identical rule:** every new field's default must leave the render unchanged. Guard new math so the default path skips it, as `EvaluateControl` already does for `mod.z == 0`, and prove it with a test in the same phase.

**Compile check without focusing Unity:** see the memory note "forge-compile-check". The script is `forge_check.py`, which reuses Unity's Bee `.rsp` files with the bundled Roslyn. The user runs the EditMode tests in Unity.

## Phase 1: Root note and keyboard
Status: Complete

- [x] `SfxRecipe.RootNote` (int, default 69, clamped 24–108). `SfxRenderer.BuildLayers` adds `RootNote − 69` to each layer's `Pitch` only when it is non-zero.
- [x] `ForgePresets.Load` resets the recipe to a fresh instance's values before `FromJsonOverwrite`. Otherwise fields missing from old preset JSON (`RootNote` now, more in later phases) keep the current recipe's values, and an old preset would load differently depending on what was open (FSF-D2).
- [x] `PianoElement` (`Editor/Forge/UI/PianoElement.cs`): C2–B6 drawn with `Painter2D`, the root key in the accent colour, and a `NoteClicked(int)` event. Placed in the Sound page globals bar after Length, with a note-name label.
- [x] The window sets `RootNote` through the serialized object (undo) and plays the sound.
- [x] Help: `ForgeHelp.Waveform` gains a "Root note" item; the hint goes on the piano. Docs: a root-note paragraph.
- [x] Tests: `RootNote = 69` renders bit-identical to a recipe without the field set. `RootNote = 81` renders the same as every layer's Pitch +12.

### Affected Assets
- Code: `Runtime/Forge/Model/SfxRecipe.cs`, `Runtime/Forge/Render/SfxRenderer.cs`, `Editor/Forge/ForgePresets.cs`, `Editor/Forge/SfxForgeWindow.cs` + `.uss`, new `Editor/Forge/UI/PianoElement.cs`, `Editor/Forge/ForgeHelp.cs`, `Documentation~/SfxForge.md`, a new `Tests/Editor/Forge/RootNoteTests.cs`.
- Data: `SfxRecipe` assets gain `RootNote` (it defaults to 69 on load). Presets in `Assets/Forge Presets` and `Runtime/Forge/Templates/*.json` are unchanged and must still load.
- Revert path: `git checkout 3485b3d -- .` in the package.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: all four assemblies, 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- Open the Impact preset. The piano shows with A4 lit and "A4" beside it.
- Click C5 → the sound plays higher, C5 is lit, and Ctrl+Z returns to A4.
- Click keys from left to right → the pitch rises step by step. Noise-only layers stay unpitched.
- Load another preset, then the Impact preset again → it plays at A4, not at the last key clicked.
- At 900px the globals bar doesn't clip, and the piano stays clickable.

### Verification Results
- 2026-10-05, agent: all four Forge assemblies compiled with Unity 6000.6's Roslyn. 0 errors. Hint scan: 0 missing (the new name `Root note` is in `ForgeHelp.Waveform`).
- Build notes:
  - The transpose is added as `layer.Pitch + mod.Pitch + transpose`. Adding `0f` is exact, so A4 needs no guard branch.
  - `ForgePresets.Load` captures the asset name *before* the defaults reset, because the reset overwrites `m_Name` too.
  - Sliding across keys raises one root change and one play per key reached. Each key is its own undo step.
- 2026-10-05, user: "all works". The EditMode suite is green (including `RootNoteTests`), and the Phase 1 playtest passes: A4 lit on open, C5 plays higher, Ctrl+Z returns to A4, presets reload at A4, and the 900px globals bar fits.

### Phase Summary
- **Model:** `SfxRecipe.RootNote` (MIDI, default `DefaultRootNote = 69`, clamped `MinRootNote` 24 to `MaxRootNote` 108). `SfxRenderer.BuildLayers` adds `RootNote − 69` to every layer's Pitch. A4 is exact, so there is no guard branch.
- **Presets:** `ForgePresets.Load` resets the recipe to a fresh instance's JSON before the overwrite (FSF-D2). It keeps the asset name, which it reads before the reset.
- **UI:** `PianoElement` (C2–B6, `Painter2D`, root key in the accent colour, hover, slide to play) and a note-name label in the Sound page globals bar. `SetRootNote` writes through the serialized object, then calls `Play()`. `RefreshSelectors` pushes the root to the piano.
- **Help/docs:** a `Root note` item in `ForgeHelp.Waveform`, and a root-note paragraph in `Documentation~/SfxForge.md`.
- **Tests:** `Tests/Editor/Forge/RootNoteTests.cs`.
- **Assets:** recipes gain `RootNote`. No preset or template change.

## Phase 2: Unison and start phase
Status: Complete
Depends on: Phase 1 (only for the preset-load reset)

- [x] Model: a `UnisonSettings` struct (Voices = 1, DetuneCents, Spread) and a `PhaseSettings` struct (Start, Random) on `Layer`, with range constants beside the other settings.
- [x] `LayerRenderParams` gains the voice count, the per-voice detune ratios and pan gains (precomputed in `BuildLayers`), and the start phase. With Random on, per-voice phases come from the layer seed.
- [x] `LayerRenderJob`:
  - Voices = 1 keeps today's single-oscillator path untouched.
  - Voices > 1 runs a voice loop over `FixedList` oscillator states for Oscillator, Wavetable and FM, and sums to stereo before the filter.
  - The filter becomes per channel only in that case, so the mono path stays bit-identical.
- [x] Oscillators start at `Start` (or a random phase per voice) instead of 0. Start = 0 with Random off must skip that code.
- [x] Strip: a **UNISON** box (Voices as a 1–8 integer knob, Detune, Spread, Phase, plus an Rnd toggle), shown for Oscillator, Wavetable and FM. Its controls are lockable under `LayerParam.Source`.
- [x] Randomizer: generated layers inherit Unison and Phase from the layer at the same index (FSF-D4).
- [x] Help (`ForgeHelp.Layers` "Unison") and docs.
- [x] Tests:
  - Voices 1 / Start 0 is bit-identical.
  - 2 voices with 0 detune and 0 spread equal 1 voice × √2 (gain law).
  - Spread > 0 makes L ≠ R.
  - Start 0.25 on a sine starts at the peak.
  - Randomize keeps Voices.

### Affected Assets
- Code: `Runtime/Forge/Model/Layer.cs`, `SourceSettings.cs` (or a new `UnisonSettings.cs`), `Runtime/Forge/Render/LayerRenderParams.cs`, `LayerRenderJob.cs`, `SfxRenderer.cs`, `Runtime/Forge/Randomizer/SfxRandomizer.cs`, `Editor/Forge/UI/LayerStripElement.cs`, `Editor/Forge/UI/KnobElement.cs` (`WholeNumbers`, and the `Integer`/`Cents`/`Degrees` formats), `.uss`, help and docs, a new `Tests/Editor/Forge/UnisonTests.cs`.
- Data: `Layer` gains two serialized structs, which default to off.
- Revert path: the Phase 1 commit.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- Make a 1-layer Oscillator saw recipe and set Voices 5, Detune 30, Spread 1. It sounds wide and chorused, and the waveform is wider in stereo.
- Voices back to 1 → identical to before (A/B with autoplay).
- Phase 0.25 on a sine with a short Amp attack → no click at the start. Rnd on → each render seed sounds slightly different.
- Randomize with a 5-voice layer 1 → layer 1 keeps 5 voices.
- 6 layers × 8 voices at 2 s: note the Render readout (record it here). Knob drags stay smooth.
- Strip layout at 900px: record whether Oscillator strips with UNISON still fit in one row.

### Verification Results
- 2026-10-05, agent: all four Forge assemblies compiled with Unity 6000.6's Roslyn (`forge_check.py`), 0 errors. Hint scan: 55 `ForgeHints.Set`/`Hint` calls checked against their topics, 0 missing (the new name `Unison` is in `ForgeHelp.Layers`). EditMode tests not run (needs the user).
- Build notes:
  - `LayerRenderJob.Execute` branches once per layer: `Voices > 1` goes to a separate `RenderUnison` loop, so the mono path is the old code plus start-phase initialisers. Those assign `VoicePhases[0]`, which is exactly `0f` for Start 0 / Rnd off, the same as the old default-constructed oscillators, so no guard branch is needed.
  - Per-voice data lives in `LayerRenderParams` as `FixedList64Bytes<float>` ratios and phases and a `FixedList128Bytes<float2>` of gains (1/√N level and spread pan folded together), filled by `SfxRenderer.SetVoices`. Oscillator states are `FixedList`s local to the job.
  - Non-tonal sources always pack one voice, so Noise/Sample/Granular ignore unison and phase.
  - `UnisonTests`: explicit defaults, `Voices = 0` (old data) and 1 voice with Detune/Spread set are all bit-identical to an untouched recipe; Noise ignores unison; 2 identical voices = 1 voice × √2 (Oscillator, Wavetable, FM, through a resonant low-pass); Spread makes L ≠ R; Start 0.25 on a sine starts at its peak (Start 0 at zero); Rnd phase follows the seed; Randomize and Mutate keep Unison and Phase.
- Playtest to record: the 6 layers × 8 voices render time (FSF-O1) and whether Oscillator strips with UNISON fit one row at 900px. UNISON adds four 52px knobs plus a 36px Rnd toggle (~250px); an FM strip shows SOURCE and UNISON together.
- 2026-10-05, user: "all works". The EditMode suite is green (including `UnisonTests`), and the Phase 2 playtest passes.

### Phase Summary
- **Model:** `UnisonSettings` (Voices 1–8, `DetuneCents` 0–100, `Spread`) and `PhaseSettings` (`Start`, `Random`) live in `SourceSettings.cs`, on `Layer.Unison` / `Layer.Phase`. Voices 0 from old data is clamped to 1.
- **Render:** `SfxRenderer.SetVoices` precomputes per-voice detune ratios, start phases and pan gains into `FixedList` fields on `LayerRenderParams`. Non-tonal sources always get 1 voice.
- **Job:** `LayerRenderJob` branches once per layer. Voices > 1 runs `RenderUnison` (stereo sum, two filter states); the mono path is unchanged apart from setting the start phase (exactly `0f` by default).
- **Decisions:** FM detunes its modulator with its carrier (FSF-D8). The pan law is `ConstantPowerPan(pos × Spread) × √2/√N` (FSF-D9). Rnd phase replaces Start (FSF-D10).
- **UI:** a UNISON box on Oscillator/Wavetable/FM strips. `KnobElement.WholeNumbers` and new formats `Integer`, `Cents`, `Degrees`. The Voices int is written by hand from the knob's change event (FSF-D7). Phase is disabled while Rnd is on.
- **Randomizer:** `CarryLockedParams` always copies Unison and Phase (FSF-D4, FSF-D11).
- **Tests:** `Tests/Editor/Forge/UnisonTests.cs`.
- **Docs and help:** the `Unison` item in `ForgeHelp.Layers`, and the docs section.
- **Assets:** `Layer` gains two serialized structs. No preset or template change. Uncommitted.

## Phase 3: Modulation engine: LFO 1–3, ENV 2/3, Rnd modes
Status: Complete
Depends on: Phase 1

- [x] Model:
  - `LfoSettings` gains Phase and `LfoMode` (Retrigger, Free).
  - `SfxRecipe` gains `Lfo2`, `Lfo3`, `Env2` and `Env3` (a `Curve` with gain unit, defaulting to flat 0), and `Random` (a `RandomSettings` with Mode and RateHz).
  - `ModSource` gains `Lfo2`, `Lfo3`, `Env2` and `Env3`, appended.
- [x] `ModTargets.IsContinuous(ModSource, RandomMode)` replaces the source-only overload. Rnd counts as continuous in a moving mode. Callers updated: `ModMatrix`, `ForgeModulation`, `ModRouteElement`.
- [x] `ModMatrix`: `LayerModulation` holds one `float4` depth per continuous slot (LFO 1–3, Env 1–3, Rnd) instead of `LfoDepth`/`EnvelopeDepth`. Constant Rnd stays a static offset, exactly as today.
- [x] `LayerRenderJob.EvaluateControl` sums `depth × value` per slot, and skips any slot whose depth is zero:
  - LFO time uses layer-relative or absolute seconds, depending on Mode, plus Phase.
  - Env 2/3 are evaluated at absolute sound time through the existing breakpoint buffer.
  - Rnd S&H and Smooth are hashed per layer from the seed at Rate.
- [x] `SfxRenderer` copies Env 2/3 into the breakpoint buffer, and packs the LFO and Rnd settings into the params.
- [x] Tests:
  - All new defaults are bit-identical, and the existing `ModulationTests` stay green.
  - An LFO 2 route renders the same as the same settings on LFO 1.
  - Free and Retrigger differ for a layer with a start offset.
  - An Env 2 ramp on Pitch rises over the whole sound.
  - Rnd S&H on Level holds steps of 1/Rate.
  - Rnd Smooth has no jumps above a bound.
  - Constant Rnd matches today's `Evaluate_RandomSourceFollowsTheSeed`.

### Affected Assets
- Code: `Runtime/Forge/Model/ModRoute.cs`, `ForgeEnums.cs`, `SfxRecipe.cs`, a new `RandomSettings` (in `ModRoute.cs` next to `LfoSettings`), `Runtime/Forge/Render/ModMatrix.cs`, `LayerRenderParams.cs`, `LayerRenderJob.cs`, `SfxRenderer.cs`, `Editor/Forge/ForgeModulation.cs`, `Editor/Forge/UI/ModRouteElement.cs`, `Tests/Editor/Forge/ModulationTests.cs` (new cases only).
- Data: recipes gain `Lfo2`, `Lfo3`, `Env2`, `Env3` and `Random`. The serialized ints of existing `ModRoute.Source` values are unchanged.
- Revert path: the Phase 2 commit.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: 0 errors.

**Playtest (human, repeatable):**
- The user runs the EditMode suite: all green, including the unchanged determinism and modulation tests.
- Every built-in preset sounds the same as before (A/B with autoplay against a render exported before the phase).

### Verification Results
- 2026-10-05, agent: all four Forge assemblies compiled with Unity 6000.6's Roslyn (`forge_check.py`), 0 errors. EditMode tests not run (needs the user). The Rnd hash values and the Smooth slope bound were checked outside Unity, with a small console build against `Unity.Mathematics`. For seeds 1, 5 and 42, the largest per-sample Smooth step at 4 Hz was 2.13e-4, against a bound of 2.5e-4.
- Build notes:
  - The new tests are in `Tests/Editor/Forge/ModSourcesTests.cs`; `ModulationTests.cs` is untouched. Level and pitch measurements read `SfxRenderer.LayerOutput(0)`, so FX defaults cannot skew them.
  - Editor (minimal, as scoped):
    - `ForgeModulation` has 11 source colours and names, `IsSupported(source, target, randomMode)`, `UnsupportedReason` (Rnd's text names its mode and points at Constant), and `IsUnipolar` (arcs for Env 1–3).
    - `ModRouteElement` reads `Random.Mode` from `_route.serializedObject`. It only re-evaluates when its own route changes, so a row's greyed-out state can lag a Rnd mode change until the list rebuilds. Phase 4 adds the Mode control and should refresh the rows then.
  - Docs: the modulation sources table in `Documentation~/SfxForge.md` now covers LFO 1–3 (Phase, Mode), Env 1–3 and the Rnd modes. It says the new settings are edited in the recipe Inspector until Phase 4.
- 2026-10-05, user: "all works". The EditMode suite is green (unchanged `DeterminismTests`/`ModulationTests`, and every `ModSourcesTests` case), and the presets A/B the same as before.

### Phase Summary
- **Model:**
  - `ModSource` adds `Lfo2`, `Lfo3`, `Env2`, `Env3` (7–10, appended).
  - New `LfoMode` and `RandomMode` enums.
  - `LfoSettings` gains `Phase` and `Mode`.
  - New `RandomSettings` (Mode, `RateHz` 0.1–40, default 4).
  - `SfxRecipe` gains `Lfo2`, `Lfo3`, `Env2`, `Env3` (`Curve.DefaultModEnvelope()`, flat 0) and `Random`.
  - `ModTargets.IsContinuous(ModSource, RandomMode)` replaces the source-only overload.
- **Engine:**
  - Seven named `float4` depth slots on `LayerModulation`. LFO 1 and Env 1 keep the names `LfoDepth`/`EnvelopeDepth` (FSF-D12).
  - `EvaluateControl` skips any zero slot, and the unison path uses the same evaluation.
  - LFO `cycles = time × Rate + Phase`; Free adds `StartSeconds` (FSF-D13).
  - Env 2/3 run on the sound's timeline (FSF-D14).
  - Moving Rnd is one signal per layer through `ModMatrix.RandomStep`/`RandomSignal`, with an avalanche mix on top of `math.hash` (FSF-D15).
- **Editor (minimal):** `ForgeModulation` has 11 source colours and names (FSF-D16), `IsSupported(source, target, randomMode)`, `UnsupportedReason` and `IsUnipolar`. `ModRouteElement` reads `Random.Mode`.
- **Tests:** `Tests/Editor/Forge/ModSourcesTests.cs` (21 cases).
- **Docs:** the sources table is updated.
- **Still open:** FSF-O3 (the linear hash in Phase 2's `RandomPhase` and Constant Rnd's `StaticValue`) waits for the user's decision.
- **Assets:** recipes gain `Lfo2`, `Lfo3`, `Env2`, `Env3` and `Random`. Uncommitted.

## Phase 4: Mod page source panels and source bar
Status: Complete
Depends on: Phase 3

- [x] Mod page, like Vital's left column: a vertical tab list **LFO 1, LFO 2, LFO 3, ENV 1, ENV 2, ENV 3, RND** beside a panel showing the selected source. Macros stay above.
  - **LFO:** a shape picture (`FxGraphElement` filled from `ModMatrix.Lfo`, with the phase marker), plus Shape stepper, Rate, Phase and Mode stepper.
  - **ENV 1:** explains that it is each layer's Amp curve, with a button that opens the Sound page Curves on Amp.
  - **ENV 2/3:** a `CurveEditorElement` bound to the recipe curve (same undo grouping as the layer curves).
  - **RND:** Mode stepper, Rate (hidden for Constant), and a picture of the random signal for the current seed.
- [x] `ForgeModulation`: colours and names for the 11 sources. Refusal text and chip dimming follow the Rnd mode. The source bar orders its chips macros → LFOs → Envs → Rnd and wraps at narrow widths.
- [x] Knob arcs for Env 2/3 are unipolar (like Env 1). Arcs and chips for moving-Rnd routes on non-continuous targets are dimmed.
- [x] Help: `ForgeHelp.Modulation` items for each panel, and the "Greyed out" text updated. Docs: a modulation sources table.
- [x] Tests: none new (UI). The hint scan covers the help names.

### Affected Assets
- Code: `Editor/Forge/SfxForgeWindow.cs` + `.uss`, `Editor/Forge/ForgeModulation.cs`, `Editor/Forge/UI/ModSourceBarElement.cs`, possibly a new `Editor/Forge/UI/ModSourcePanelElement.cs`, `Editor/Forge/ForgeHelp.cs`, `Documentation~/SfxForge.md`.
- Window state: a `[SerializeField]` for the selected source tab.
- Revert path: the Phase 3 commit.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- Mod page: click through the seven tabs. Each panel shows its controls, and the LFO picture follows Shape/Phase changes live.
- Set LFO 2 to Square at 6 Hz, then drag LFO 2 from the source bar onto layer 1's Level → it pulses. Switch Mode to Free on a layer with Offset 200 ms → the pulse lines up with the sound start, not the layer start.
- Draw a rising ENV 2, then drag Env 2 onto Cutoff → the sound opens up over its whole length. Ctrl+Z undoes the curve edit in one step.
- RND: set Smooth at 3 Hz and drag Rnd onto Pan → the sound drifts. Switch to Constant → the route still works, and Rnd can again reach Decay and Drive. In Smooth, dropping on Decay is refused with a reason.
- The source bar shows 11 chips in the right order, and wraps cleanly at 900px with the left panel open.

### Verification Results
- 2026-10-05, agent: all four Forge assemblies compiled with Unity 6000.6's Roslyn (`forge_check.py`), 0 errors, no new warnings. Hint scan: 65 literal `ForgeHints.Set`/`Hint` names checked against their topics, plus the names passed through variables (`LFO`, `ENV 1`, `ENV 2 / 3`, `RND` in `ForgeHelp.Modulation`; `Draw`, `Grid` in `ForgeHelp.Curves`), 0 missing. EditMode tests not run (needs the user; no test changed).
- Build notes:
  - New files: `Editor/Forge/UI/LfoPanelElement.cs` and `RandomPanelElement.cs` (+ `.meta`). The ENV 1 and ENV 2/3 panels live in the window, because they drive the page switch and the curve undo.
  - Arcs and chips checked in code, unchanged: `ForgeModulation.RefreshKnob` draws an arc only for an enabled route with `IsSupported(source, target, Random.Mode)`, passes `IsUnipolar` (Env 1–3), and dims the chip otherwise. `Refresh` runs from `RefreshSelectors` on every recipe change, so a Rnd mode switch re-dims chips without extra code.
  - The old `_lfoShape`/`_lfoRate` fields and the `.forge-lfo__shape` rule are gone; their controls now live in the LFO 1 panel.
  - Width budget at 900px with the left panel open: the centre column is about 660px. The sources row is capped at 640px like the routes: 64px of tabs, and the LFO panel needs about 360px (a graph of at least 120px, 110px of steppers, two 58px knobs). The source bar's chips add up to about 650px, so it may just fit on one line; if not, it wraps before the Env or Rnd group.
- Playtest to record: the 900px layout of the Mod page and the source bar; whether the LFO picture and the RND picture follow knob drags smoothly.
- 2026-10-05, user: "all works". The Phase 4 playtest passes.

### Phase Summary
- **Mod page:** Macros, then source tabs (LFO 1–3, ENV 1–3, RND) with one panel per tab, then Routes. The selected tab is `[SerializeField] _modTab` (FSF-D18).
  - New `LfoPanelElement` (shape picture with a phase marker, Shape/Rate/Phase/Mode) and `RandomPanelElement` (Mode, Rate, signal picture; text for Constant) (FSF-D19).
  - ENV 1 is a note plus a button that opens the Amp curve. ENV 2/3 share one `CurveEditorElement`, with its own Draw/Grid/Reset and one undo step per drag (FSF-D20).
  - LFO 1's Rate keeps `ModTarget.LfoRate`. It is only a drop target while its tab is open.
- **Source bar:** chips are grouped macros → LFOs → Envs → Rnd and wrap only between groups. A click without drag selects the source's tab (FSF-D21).
- **Matrix:** friendly names via `[InspectorName]` on `ModSource`/`RandomMode` (FSF-D17). The arrows step in the bar's order. Rows refresh on every recipe change, so Rnd-mode greying is immediate (FSF-D22).
- **Shared UI:** `StepperElement` gains a step order and a `value` getter. `FxGraphElement` gains `MarkerX` and `SetTraceColor`.
- **Help/docs:** Modulation items for every panel, and the docs' source-panels table.
- **Assets:** window state `_modTab`, `_envDrawMode`, `_envGridSnap`. No recipe-format change. Uncommitted.

## Decisions & Open Questions
- **FSF-D1 (decided):** No `specs/` folder; the design lives in this plan's Context, as in the UI-overhaul plan.
- **FSF-D2 (decided):** Loading a preset resets the recipe to defaults first. `EditorJsonUtility.FromJsonOverwrite` leaves fields missing from the JSON as they were, which would make old presets load differently once new fields exist.
- **FSF-D3 (decided, user choice):** ENV 2/3 are recipe-level curves over the whole sound, shared by all layers. Per-layer envelopes would mean 12 more curves to manage. Env 1 stays the layer's Amp curve.
- **FSF-D4 (decided, user choice):** Randomize and Mutate keep each layer's unison and phase (inherited by layer index). Unison is the user's setting, not part of the generated sound, so templates gain no unison ranges.
- **FSF-D5 (decided):** `ModSource` values are appended, never renumbered, so routes saved in recipes and presets keep their meaning. Display order is a UI concern.
- **FSF-D6 (decided):** `Lfo` stays the field for LFO 1, and `Lfo2`/`Lfo3` are separate fields rather than a list. Renaming or moving `Lfo` would drop every saved LFO setting, and a fixed three needs no list UI.
- **FSF-D7 (decided):** `UnisonSettings.Voices` is an `int`. The Voices knob is a `KnobElement` with a new `WholeNumbers` flag. The flag rounds the value and accumulates the drag internally, because rounding every small step would cancel the drag. The strip writes the int property by hand from the knob's `ChangeEvent<float>`, rather than relying on float-to-int binding. `UpdateState` pushes the value back, showing old-data 0 as 1.
- **FSF-D8 (decided):** FM detunes the modulator with its carrier (the job passes the voice's scaled increment, and the ratio is unchanged), so every voice keeps the same timbre. Start phase sets the modulator to `frac(Start × Ratio)`, so Start time-shifts the whole FM waveform instead of changing its timbre.
- **FSF-D9 (decided):** Spread pan law: each voice gets `ConstantPowerPan(position × Spread) × √2 / √N`, so a centred voice matches the mono path at 1/√N. The layer Pan still applies after the per-channel filters. Positions are evenly spaced from −1 to +1. Even voices take the left slots and odd voices the right, each from the outside in, so neighbouring detunes land on opposite sides. 2 identical voices at Spread 1 still give L = R; only detune or Rnd phase makes them differ.
- **FSF-D10 (decided):** Rnd phase *replaces* Start rather than adding to it (a uniform random offset makes Start meaningless). The strip disables the Phase knob while Rnd is on. Per-voice phases are `math.hash(uint3(layerSeed, voice, salt))`, and they apply to one voice too, so Rnd also varies single-voice layers per seed.
- **FSF-D11 (decided):** Detune and Spread default to 0 for new layers, as for old data, so a new layer and an old one behave the same when Voices is raised. Unison and Phase knobs are lockable under `LayerParam.Source` as asked, but Randomize and Mutate never change them anyway (FSF-D4), so the lock is cosmetic for now.
- **FSF-O1 (open):** The performance budget for unison. Phase 2 records the render time for 6 layers × 8 voices at 2 s. If knob drags stutter, cap Voices lower or render the strip mini displays less often.
- **FSF-D12 (decided):** Depth slots.
  - `LayerModulation` and `LayerRenderParams` hold seven named `float4` depths: `LfoDepth` (LFO 1), `Lfo2Depth`, `Lfo3Depth`, `EnvelopeDepth` (Env 1), `Env2Depth`, `Env3Depth` and `RandomDepth`.
  - LFO 1 and Env 1 keep their old field names, so `ModulationTests.Evaluate_MotionScalesLfoDepthAndContinuousSourcesSkipStaticTargets` stays untouched.
  - `EvaluateControl` skips a slot unless `math.any(depth != 0)`. LFO 1 is *assigned* first and Env 1 *added* next. That is the old `Lfo × l + Env × e` order, so a recipe with both routed keeps its exact sum.
  - When a slot is skipped, the only possible change is the sign of a zero term. Every consumer (`exp2`, the `mod.z == 0` test, the pan clamp) treats ±0 the same.
  - The LFO settings travel as an `LfoParams` struct (Shape, RateHz, Phase, Mode), one per LFO.
- **FSF-D13 (decided):** LFO time.
  - `cycles = time × Rate + Phase`. For Retrigger, time is the layer-relative seconds. For Free, it is those plus `LayerRenderParams.StartSeconds`.
  - `StartSeconds = StartFrame / SampleRate`, computed in `BuildLayers`, so it is the clamped, frame-rounded start the layer actually renders at.
  - With Phase 0 and Retrigger, this is the old `seconds × rate` expression plus `0f`, which is exact. Phase is clamped to 0–1.
  - `PackLfo` applies the `LfoRate` octaves to LFO 1 only. LFO 2/3 get `exp2(0) = 1`, so a route on LFO 2 with LFO 1's settings renders identically.
- **FSF-D14 (decided):** Env 2/3 are copied into the breakpoint buffer once per render, before the layer curves. They are evaluated at `(StartFrame + t × VoiceFrames) / FrameCount`, the sound's own 0..1 timeline. The default curve is `Curve.DefaultModEnvelope()`: Gain unit, Min 0, Max 1, points (0, 0) and (1, 0).
- **FSF-D15 (decided):** Rnd moving modes.
  - `RandomSettings` has Mode (default Constant) and RateHz (default 4, range 0.1–40 Hz).
  - In SampleHold and Smooth, every Rnd route on a layer shares one signal, as with an LFO. The signal is seeded by the layer seed and runs on the sound's timeline: `steps = (seconds + StartSeconds) × Rate`.
  - Constant keeps today's per-route `StaticValue` hash untouched.
  - `ModMatrix.RandomStep(seed, step)` is `lowbias32(math.hash(uint3(seed, step, salt))) >> 8`, scaled to −1..1. The extra avalanche is needed because `math.hash(uint3)` is a linear combination of its inputs. Without it, consecutive steps formed an arithmetic sequence (seed 1 gave 0.183, 0.070, −0.043, …).
  - Smooth is value noise: `lerp(v[n], v[n+1], smoothstep(frac))`. It is continuous, and its slope is at most 1.5 × |Δv| × Rate ≤ 3 × Rate per second.
- **FSF-D16 (decided):** Source names, indexed by the enum value: Size, Energy, Tone, Motion, **LFO 1**, **Env 1**, Rnd, LFO 2, LFO 3, Env 2, Env 3.
  - New colours: LFO 2 orange, LFO 3 lime, Env 2 deep teal, Env 3 pale lavender. Phase 4 may retune them, and it reorders the chips.
  - Until Phase 4, the route matrix's source stepper still shows the nicified enum names (`Lfo`, `Envelope`, `Lfo 2`, …).
- **FSF-O2 (open):** Should moving sources also reach Resonance? This cost is reasoned, not measured.
  - `ResonanceToK` is one multiply-add. Computing `FilterK` per control block and lerping it per sample like `FilterG` costs little next to the filter itself.
  - The real cost is structural. Every depth slot is a full `float4` (Pitch, Cutoff, Level, Pan), so Resonance needs a fifth component in all seven slots, or a second `float4`/`float` per slot.
  - `ControlFrame` also needs a K field, and `IsContinuous(ModTarget)` can no longer be `target <= Pan`, because Decay sits between Pan and Resonance.
  - Still deferred; Phase 4 does not need it.
- **FSF-O3 (resolved 2026-10-05 by FSF-D23; found in Phase 3):** `math.hash(uint3)` is linear, so existing hashes that step one input over consecutive integers are poorly mixed.
  - Phase 2's `SfxRenderer.RandomPhase`: per-voice phases come out evenly stepped, not random.
  - Constant Rnd's `StaticValue`: consecutive layers, routes or seeds differ by a fixed amount mod 1.
  - Fixing either changes existing renders (Rnd phase, Constant Rnd), so it needs the user's call. Phase 3's moving Rnd already uses the avalanche mix.
- **FSF-D17 (decided, Phase 4):** Source names and order in the route rows (supersedes FSF-D16's last point).
  - `ModSource` values carry `[InspectorName]` ("LFO 1", "Env 1", "Rnd", "LFO 2"…), and `RandomMode.SampleHold` shows as "Sample & Hold". `EnumField` reads these through `EnumDataUtility`, so route rows, the Rnd Mode stepper and the recipe Inspector all show them, with no change to `StepperElement`'s binding. The attributes must match `ForgeModulation.SourceNames`; there is a comment on the enum saying so.
  - `StepperElement` takes an optional step order. Route rows step sources in `ForgeModulation.SourceOrder` (macros → LFO 1–3 → Env 1–3 → Rnd), which the source bar also uses. The click-open dropdown still lists them in enum order.
- **FSF-D18 (decided, Phase 4):** Tab layout.
  - The selected tab is `[SerializeField] ModSource _modTab` (default `Lfo`). Anything that isn't a tab falls back to LFO 1.
  - Tabs are 60px buttons with a 3px left edge in the source colour, and the selected one has accent text.
  - ENV 2 and ENV 3 share one panel and one `CurveEditorElement`; its title and the curve it edits follow the tab.
  - There are three `LfoPanelElement`s, and only LFO 1's Rate keeps `ModTarget.LfoRate`. That knob is a drop target only while the LFO 1 tab is open (before, it was always visible on the Mod page).
  - The row is capped at 640px like the routes. Panels have a minimum height equal to the tab column, so the routes don't jump when switching tabs.
- **FSF-D19 (decided, Phase 4):** Source pictures.
  - **LFO:** two cycles of the shape from phase 0 on a bipolar graph. The grid gives a zero line and the boundary between the cycles. A vertical marker (new `FxGraphElement.MarkerX`) sits at Phase / 2. Rate doesn't change the picture.
  - **RND:** layer 1's moving signal (seed `hash(uint2(Seed, 0))`, as in `SfxRenderer`) across the sound length, with Rate clamped as the renderer clamps it. Constant hides the graph and the Rate knob, and shows a line of text instead, since nothing moves.
  - Traces use the source colour (new `FxGraphElement.SetTraceColor`, which wins over the USS).
  - Live updates: a panel redraws from its own controls on their change events, so dragging is immediate. `RefreshSelectors` redraws every panel from the recipe, which covers undo and preset loads. Neither allocates.
- **FSF-D20 (decided, Phase 4):** The ENV 2/3 editor has its own Draw and Grid toggles (`_envDrawMode`, `_envGridSnap`), separate from the Sound page's, plus Reset. The curve's `Locked` flag is ignored, because the randomizer never touches Env 2/3. Undo reuses `OnCurveEditStarted`/`_curveUndoGroup`, so a drag is one "Edit Envelope" step.
- **FSF-D21 (decided, Phase 4):** Source bar.
  - Chips sit in one row container per group (macros, LFOs, Envs, Rnd), so a narrow window wraps only between groups.
  - Clicking a chip without dragging selects that source's Mod page tab, if it has one. It does not switch pages: a click on another page only shows the drag hint, as before.
- **FSF-D22 (decided, Phase 4):** Route rows now refresh from `RefreshSelectors`, through `RefreshModPanels`, so a Rnd Mode change updates their greyed-out state at once. If the row count no longer matches the recipe (a variation with other routes), the routes are rebuilt instead of refreshing rows bound to stale array elements.
- **FSF-D23 (decided 2026-10-05, user: "fix both hashes"):** `ModMatrix.Avalanche` (lowbias32) is now public, and wraps the `math.hash` in both Constant Rnd's `StaticValue` and Phase 2's `SfxRenderer.RandomPhase`.
  - This deliberately changes renders: any recipe with an Rnd route, or with unison Rnd phase on, sounds different from before. Default routes don't use Rnd, so unrouted recipes are unaffected.
  - The `math.hash(uint2)` seeds for the randomizer, variations and layer noise were left alone. They feed `Unity.Mathematics.Random` or noise generators, which do their own mixing.
  - Regression test: `ModulationTests.StaticRandom_IsNotEvenlySteppedAcrossSeeds`. Offline, seeds 1–4 stepped by exactly −0.3973 before the fix.

