# SFX Forge Sound Features 2

Add four Vital-inspired features that give Forge sounds it can't make yet:
- a **Shepard tone** source, for risers and fallers that never end;
- **oscillator warp** (Sync first, then Bend, Squeeze, Pulse and Quantize);
- four more **distortion modes**: Hard Clip, Sine Fold, Bit Crush and Downsample;
- an **OTT-style 3-band compressor** FX.

These change the recipe format and the DSP. Old recipes and presets must still render bit-identical.

Written 2026-10-05 against `main` @ `b360d13`. Local ids are `FS2-n`. Follows `plans~/sfx-forge-sound-features.md` (FSF), which must be closed and archived first.

## Progress
| # | Phase | Status | Verified |
|---|-------|--------|----------|
| 1 | Shepard tone source | Complete | Tests verified by user; playtest pending |
| 2 | Oscillator warp | In progress — at Sync checkpoint | Unverified by user (agent: compile + hint scan) |
| 3 | Distortion modes | Complete | Unverified by user (agent: compile, hint scan, managed run of the new tests) |
| 4 | OTT compressor | Complete | Unverified by user (agent: compile, hint scan, managed run of the compressor test bodies) |

**Now:** Waiting on user:
1. Phase 2: re-run the EditMode suite (`WarpTests`, after the Formant floor fix) and do the Sync checkpoint playtest.
2. Phase 3: run the EditMode suite (the `FxTests` distortion tests, plus every existing suite unchanged) and the Phase 3 playtest.
3. Phase 4: run the EditMode suite (the new `FxTests` compressor tests, plus every existing suite unchanged) and the Phase 4 playtest.
4. Then, on Sync approval: implement Phase 2's remaining four warp modes (Bend, Squeeze, Pulse, Quantize) and its Docs item.

The Phase 1 playtest is also still unreported. Phase 4 was built before Phase 2 closed, at the user's request; `ModTarget.Warp = 13` was already appended, so `CompressorDepth = 14` is safe.

## Context
- **Specs:** this repo has no `specs/` folder, so the design is described below (as in FSF-D1).
- **Engine:** Unity 6000.6.0f1. Assemblies:
  - `DataKeeper.Forge.Core` (`Runtime/Forge` minus `Player/`): model, Burst render jobs, randomizer.
  - `DataKeeper.Forge.Runtime` (`Runtime/Forge/Player`): runtime clip rendering, which picks up every Core change for free.
  - `DataKeeper.Forge.Editor` (`Editor/Forge`).
  - `DataKeeper.Forge.Tests` (`Tests/Editor/Forge`).
- **Render path:**
  - `SfxRenderer.Render` runs `ModMatrix.Evaluate`, then `BuildLayers` fills `LayerRenderParams`.
  - `LayerRenderJob` (per layer, control rate 32 samples; mono path plus `RenderUnison`) → `MixJob` → `FxChainJob`.
  - `FxChainJob` runs a fixed order: Transient, Distortion, Delay, Reverb, Limiter.
  - Continuous mod sources (LFO 1–3, Env 1–3, moving Rnd) are a `float4` depth per slot over Pitch / Cutoff / Level / Pan, applied in `EvaluateControl`.
- **Surface:**
  - Window: `Editor/Forge/SfxForgeWindow.cs` + `.uss`. The FX page modules are built around `SfxForgeWindow.cs:742`.
  - Strips: `Editor/Forge/UI/LayerStripElement.cs` (SOURCE box at `:158`, UNISON box).
  - FX graphs: `Editor/Forge/FxGraphBuilder.cs`.
  - Modulation: `Editor/Forge/ForgeModulation.cs`, `Editor/Forge/UI/ModRouteElement.cs`.
  - Help and docs: `Editor/Forge/ForgeHelp.cs`, `Editor/Forge/ForgeHints.cs`, `Documentation~/SfxForge.md`.
- **Design (carried here, no spec):**
  - *Shepard tone (`SourceType.Shepard = 6`):*
    - A stack of octave-spaced sines under a bell-shaped (Gaussian in octaves) amplitude window centred on the layer pitch.
    - Every partial glides at **Rate** octaves per second, between −4 and +4. Rate 0 is a static octave stack.
    - When a partial leaves the window at one edge, a new one fades in at the other, so the sweep never ends.
    - **Width** (0–1) sets the window width in octaves. **Partials** (4–10) sets how many octaves are stacked.
    - Partials at or above 0.45 × sample rate fade to zero. Pitch curve, pitch routes and root note move the window centre.
    - Shepard is not "tonal" in the `IsTonal` sense, so it gets no unison and the randomizer's harmony transpose skips it.
  - *Warp (`Layer.Warp`, Oscillator and Wavetable sources):*
    - Mode is Off, Sync, Bend, Squeeze, Pulse or Quantize. Amount runs 0–1.
    - The phase each voice reads is remapped before the wave is read:
      - **Sync:** a slave phase runs at 2^(4 × Amount) times the voice phase and resets when the voice phase wraps. Up to 4 octaves of sync: the laser zap.
      - **Bend:** `p' = 1 − (1 − p)^(1 + 7a)` pushes the cycle toward its start.
      - **Squeeze:** symmetric power curve toward the middle of the cycle.
      - **Pulse:** the wave plays in the first `1 − 0.95a` of the cycle and holds its end value for the rest.
      - **Quantize:** the phase steps through `2^(8 − 6a)` steps per cycle, from 256 down to 4. Lo-fi, and it aliases on purpose.
    - Warp is stored on `Layer` beside Unison, not in `SourceSettings`, so Randomize and Mutate keep it (as FSF-D4 does for Unison).
    - Warp is a new mod target, `ModTarget.Warp`. It is per layer and *continuous*, so LFO, Env and moving Rnd can sweep it. An Env 2 ramp on Warp in Sync mode is the classic sync sweep.
  - *Distortion modes:* appended to `DistortionMode`: `HardClip = 2`, `SineFold = 3`, `BitCrush = 4`, `Downsample = 5`.
    - HardClip and SineFold go through the existing 2× oversampled path.
    - BitCrush and Downsample run at the base rate, because aliasing is the point.
    - Drive stays the single amount knob, stored in `DriveDb`, so the `Drive` mod target works in every mode. Bit Crush maps 0–36 dB to 16–2 bits. Downsample maps it to a hold factor of `DbToLinear(DriveDb)`, from 1× to 63×.
  - *Compressor (OTT style):* a new FX module, running after Distortion and before Delay.
    - A 3-band Linkwitz-Riley crossover at 88 Hz and 2.5 kHz (OTT's split).
    - Each band gets downward compression above a threshold and upward compression below it, up to a capped boost.
    - Controls: **Depth** (dry/wet, 0–1), **Time** (scales attack and release), **Upward** and **Downward** (0–1 each), and **Gain** (output, ±12 dB).
    - Nothing below −70 dBFS is boosted, so silence stays silent and analysis (effective length, tail cut-off) keeps working.
    - `CompressorDepth` is a new global, static mod target.
- **Success criteria:**
  - *Measurable:*
    - All four Forge assemblies compile, the EditMode suite is green, and the hint scan reports 0 missing.
    - `DeterminismTests`, `ModulationTests.DefaultRoutes_AtNeutralMacrosRenderBitIdenticalToNoRoutes` and every existing FX test pass unchanged.
    - Each phase adds a test proving its defaults render bit-identical to `b360d13`.
  - *Feel:*
    - A Shepard layer at Rate +1 sounds like it rises forever, with no audible loop point or click.
    - A saw layer with Sync and an Env 2 ramp on Warp is an obvious laser zap.
    - Bit Crush and Downsample sound clearly 8-bit.
    - The compressor at Depth 1 makes a reverb-tailed impact denser and louder in the tail, without pumping noise in the silence after it.
    - Dragging knobs on a 6-layer recipe stays responsive.
- **Non-goals:**
  - Warp on FM, Noise, Sample, Granular or Shepard sources.
  - Formant warp, and cross-layer FM/RM (the job-graph change in the earlier recommendation).
  - A drawn Warp curve in the curve editor. Env 1–3 routes cover that.
  - Unison on Shepard. A Shepard Rate mod target.
  - Reordering the FX chain (FUI-O1 stays decided).
  - Compressor band gains, band split controls and sidechain.
  - Templates or the randomizer generating Shepard, Warp or the Compressor (as FSF-D4).

## For Future Agents
The plan file is the source of truth; the conversation is not.

**Resuming:** read **Progress** and **Now**, then only the current phase. `grep -n "^## \|^\*\*Now:"` gives you the map.

**Working:** tick `- [x]` items as they are done. When a phase completes:
1. Set its **Progress** row and update **Now**.
2. Run the **Verification Plan** and record the outcome in **Verification Results**.
3. Write the **Phase Summary**.

Never rewrite a completed summary; append a correction. Log decisions and open questions in **Decisions & Open Questions**.

**Bit-identical rule:** every new field's default must leave the render unchanged. Guard new math so the default path skips it, the way `EvaluateControl` already does for `mod.z == 0`, and prove it with a test in the same phase. A zero-valued new struct must be a safe value too: old recipe assets and the randomizer's `GenerateLayer` (`SfxRandomizer.cs:152`) both produce one.

**Enum rule:** append, never reorder. `SourceType`, `DistortionMode` and `ModTarget` are serialized as ints.

**Compile check without focusing Unity:** see the memory note "forge-compile-check". The script is `forge_check.py`, which reuses Unity's Bee `.rsp` files with the bundled Roslyn. The user runs the EditMode tests in Unity.

## Phase 1: Shepard tone source
Status: Complete (EditMode tests verified by user; playtest pending)

- [x] Model: `SourceType.Shepard = 6`, and a `ShepardSettings` struct on `SourceSettings`:
  - fields `RateOctaves` (−4..4), `Width` (0..1) and `Partials` (4..10);
  - range constants beside the other settings;
  - a `Default` of Rate +1, Width 0.5, Partials 8, used by `SourceSettings.Default` and by `GenerateLayer`.
  - Zero values must render: Partials 0 clamps to 4, Width 0 to the minimum width, and Rate 0 is static.
- [x] `LayerRenderParams` gains `ShepardRate`, `ShepardWidthOctaves` and `ShepardPartials`. `BuildLayers` packs them clamped.
- [x] DSP: a new `Runtime/Forge/Dsp/ShepardOscillator.cs`:
  - one phase accumulator per partial in a `FixedList64Bytes<float>`, plus a running octave shift;
  - frequency of partial k: `centre × 2^(k − Partials/2 + shift)`;
  - when `shift` wraps past 1 (or below 0), rotate the phases one slot so each partial keeps a continuous phase;
  - amplitude: a Gaussian in octaves from the centre, zero at or above 0.45 × sample rate, normalised so a full stack peaks near 1.
- [x] `LayerRenderJob` mono path: a `SourceType.Shepard` case that takes the interpolated phase increment as the window centre.
  - `IsTonal` stays false, so `SetVoices` packs one voice and `RenderUnison` never sees Shepard.
- [x] Strip:
  - the source stepper lists Shepard;
  - the variant stepper is hidden for it;
  - the SOURCE box shows a Shepard group (Rate as a bipolar knob in oct/s, Width, Partials as an integer knob), lockable under `LayerParam.Source`.
- [x] Help: a `ForgeHelp.Layers` "Shepard" item, with the hint on the group. Docs: a row in the source table and a short paragraph.
- [x] Tests (`Tests/Editor/Forge/ShepardTests.cs`):
  - Every existing recipe renders unchanged. Covered by `DeterminismTests`; add one Shepard-free recipe hash check if none exists.
  - Rate 0: the magnitude spectrum peaks only at octaves of the centre.
  - Rate +1 over 3 s: the magnitude spectra of 100 ms windows 1 s apart correlate above 0.95. One octave of travel returns to the same spectrum.
  - No click: the largest sample-to-sample step stays under a bound across a wrap.
  - A high centre pitch puts no energy above 0.45 × sample rate.
  - Zero-valued `ShepardSettings` renders non-silent and finite.

### Affected Assets
- Code:
  - `Runtime/Forge/Model/ForgeEnums.cs`, `SourceSettings.cs`
  - `Runtime/Forge/Render/LayerRenderParams.cs`, `LayerRenderJob.cs`, `SfxRenderer.cs`
  - new `Runtime/Forge/Dsp/ShepardOscillator.cs`
  - `Runtime/Forge/Randomizer/SfxRandomizer.cs` (Default in `GenerateLayer` only)
  - `Editor/Forge/UI/LayerStripElement.cs`, `Editor/Forge/ForgeHelp.cs`, `Documentation~/SfxForge.md`
  - new `Tests/Editor/Forge/ShepardTests.cs`
- Data: `SourceSettings` gains a `Shepard` struct. Recipes, presets in `Assets/Forge Presets` and `Runtime/Forge/Templates/*.json` are unchanged and must still load.
- Revert path: `git checkout b360d13 -- .` in the package.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: all four assemblies, 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- New recipe. Set layer 1 to Shepard (defaults), Length 4000 ms, Decay 4000 ms, and play → a smooth endless rise with no audible loop point.
- Set Rate to −1 → it falls forever. Set Rate to 0 → a static organ-like octave chord.
- Click piano keys C3, then C5 → the window centre moves, so the tone gets darker or brighter.
- Width at 0 → a narrow, nearly pure tone. Width at 1 → a broad, full one.
- Randomize with the Source lock off → generated layers never pick Shepard. Switching a generated layer to Shepard sounds right straight away.

### Verification Results
- 2026-10-05, agent: `forge_check.py` compiled Core (53 files), Runtime, Editor and Tests (21 files) with Unity's Roslyn: 0 errors, no Forge warnings. Unity was not focused, so Burst and the EditMode suite did not run.
- 2026-10-05, agent: hint scan: 66 `ForgeHints.Set` calls, 0 missing; `Layers` "Shepard" resolves. The scanner's one "unresolved" line (`SfxForgeWindow.cs:279`) is a regex miss on a nested call; its item "Play / Stop  (Space)" exists.
- 2026-10-05, agent: a float64 numpy prototype of the same algorithm passed every `ShepardTests` threshold with margin: octave energy at Rate 0 is 0.99998; 1 s and 2 s correlations are 0.99999 for Rate ±1, and half an octave gives −0.005; the largest step across ±4 oct/s wraps is 0.009 against a bound of 0.058; energy above 0.45 × SR is 1e-15; peaks with 10 partials are 1.00 / 0.99 / 1.10 at Width 0 / 0.5 / 1.
- Pending: the user runs the EditMode suite and the playtest above.
- 2026-10-05, user: all EditMode tests pass (including ShepardTests). Playtest not yet reported.

### Phase Summary
Shepard is `SourceType.Shepard = 6`, with `ShepardSettings` (Rate, Width, Partials) on `SourceSettings`. Its `Default` (Rate +1, Width 0.5, 8 partials) feeds `SourceSettings.Default` and `GenerateLayer`. `BuildLayers` clamps the settings into `ShepardRate`, `ShepardWidthOctaves` (σ) and `ShepardPartials`, so zero values play as a static 4-partial stack at the narrowest width.

`Dsp/ShepardOscillator` keeps one phase per partial in a `FixedList64Bytes<float>` and a running octave shift. It rotates the phases one slot when the shift wraps.
- Window: Gaussian, lowered by its edge value (FS2-D7).
- Normalisation: by window power, times 0.8 (FS2-D8).
- Nyquist: a fade from 0.35 to 0.45 × SR (FS2-D9).

The mono path has a `Shepard` case that reads the interpolated phase increment as the window centre. `IsTonal` is unchanged, so Shepard gets one voice, no unison and no harmony transpose.

The strip's SOURCE box has a Shepard group: Rate is a bipolar knob with a new `KnobFormat.OctavesPerSecond`, then Width, then Partials, an int knob bound by hand like Voices. All three lock under `LayerParam.Source`. The variant stepper stays hidden.

Help gains a `Layers` "Shepard" item, and the docs a source-table row and a paragraph. `ShepardTests` has 10 tests. A true cross-commit hash against `b360d13` was not possible (FS2-D10, FS2-O3).

Files: `ForgeEnums.cs`, `SourceSettings.cs`, `LayerRenderParams.cs`, `LayerRenderJob.cs`, `SfxRenderer.cs`, `SfxRandomizer.cs`, new `Dsp/ShepardOscillator.cs` (+ `.meta`), `KnobElement.cs`, `LayerStripElement.cs`, `ForgeHelp.cs`, `SfxForge.md`, new `Tests/Editor/Forge/ShepardTests.cs` (+ `.meta`). Not committed: the user tests in Unity first.

## Phase 2: Oscillator warp
Status: In progress — at Sync checkpoint
Depends on: nothing (it touches the modulation engine, so do it before Phase 4 adds `CompressorDepth`)

**Sync first:** finish and listen to the Sync items, then ask the user before starting the other four modes. Sync on its own is a shippable stop.

- [x] Model:
  - `WarpMode` enum (`Off = 0, Sync = 1, Bend = 2, Squeeze = 3, Pulse = 4, Quantize = 5`).
  - A `WarpSettings` struct (`Mode`, `Amount`) as `Layer.Warp`.
  - `ModTarget.Warp = 13`.
- [x] `ModTargets.IsPerLayer` and `IsContinuous(ModTarget)` become explicit sets instead of `<= Resonance` / `<= Pan`, and gain Warp. `MaxAmount(Warp)` is 1. Check every caller: `ModMatrix`, `ForgeModulation.cs:79/128/155/170/206`, `ModRouteElement.cs:100`.
- [x] Modulation engine: a fifth lane for Warp.
  - `LayerModulation` gains a static `Warp` offset, plus one `float` warp depth per continuous slot beside each `float4` (e.g. `LfoWarpDepth`).
  - `LayerRenderParams` carries the same.
  - `ModMatrix` routes Warp into that lane (FS2-D3).
  - `EvaluateControl` sums the warp lane in its own accumulator after the four existing lanes, skipping zero depths, so the `float4` order and guards are unchanged. `ControlFrame` gains `WarpAmount = saturate(Amount + static + lane)`, interpolated per sample like the others.
- [x] Band-limited tables for warped oscillators: an internal "Classic" table set (sine, saw, square, triangle) built with the existing mip machinery and appended after the 5 public banks, so `WavetableBank` is unchanged (FS2-D2).
  - `PrepareSources` builds the wavetables when any Oscillator layer has Warp on.
- [x] Sync:
  - a `Warp.cs` helper in `Runtime/Forge/Dsp`;
  - slave phase = `frac(phase × 2^(4a))`, read from the table at the mip level for `increment × ratio`;
  - a PolyBLEP step correction at each voice-phase wrap, sized by the slave value jump;
  - wired into the mono path and `RenderUnison` for Oscillator (Classic tables) and Wavetable (its own bank).
  - Warp Off must take today's code untouched: branch once per layer, not per sample.
  - Done as one `RenderWarp` loop for every voice count (FS2-D12), with the slave capped at SR/4 (FS2-D13).
- [x] Strip: a **WARP** box (Mode stepper, Amount knob with `ModTarget = ModTarget.Warp`), shown for Oscillator and Wavetable and lockable under `LayerParam.Source`. Help: a `ForgeHelp.Layers` "Warp" item.
  - The stepper offers Off and Sync only, from `LayerStripElement.WarpModes` (FS2-D14). Append the other modes there when they exist.
- [x] Randomizer: `CarryLockedParams` copies `Warp` like Unison (FS2-D1).
- [ ] **Checkpoint:** user playtest of Sync (below). Ask before continuing.
- [ ] Bend, Squeeze, Pulse, Quantize:
  - phase maps in `Warp.cs`;
  - mip level from the map's largest slope (`1 + 7a` for Bend and Squeeze, `1 / (1 − 0.95a)` for Pulse);
  - Quantize reads at the base mip and aliases on purpose.
  - also: let `SfxRenderer.WarpModeOf` pass the new modes, append them to `LayerStripElement.WarpModes` (or switch back to a bound `StepperElement` once all six are offered), and switch on the mode in `LayerRenderJob.RenderWarp`, which only calls `WarpOscillator.NextSync` today.
- [ ] Docs: a Warp paragraph under Layers. Add Warp to the targets list and the continuous targets sentence in "Macros and modulation".
- [ ] Tests (`Tests/Editor/Forge/WarpTests.cs`). The Sync tests are written (14 test methods); still to add: the Pulse centroid case, the Quantize levels test, and the other modes in the finite/peak test:
  - Warp Off renders bit-identical, both mono and 3-voice unison.
  - Zero depths on every warp lane render bit-identical.
  - Sync with Amount 0 on a Wavetable layer matches Warp Off within 1e-5.
  - The spectral centroid (via `SfxAnalyzer`) rises with Sync Amount, and with Pulse Amount.
  - An Env 2 ramp routed to Warp gives a later window a higher centroid than an earlier one.
  - A macro route to Warp acts as a static offset.
  - Quantize at Amount 1 on a slow sine gives at most 4 distinct levels per cycle.
  - Every mode at Amount 1 renders finite, with peak ≤ 1.5.
  - Randomize keeps `Warp`.

### Affected Assets
- Code:
  - `Runtime/Forge/Model/ForgeEnums.cs`, `Layer.cs`, `ModRoute.cs` (`ModTargets`)
  - `Runtime/Forge/Render/ModMatrix.cs`, `LayerRenderParams.cs`, `LayerRenderJob.cs`, `SfxRenderer.cs`
  - `Runtime/Forge/Dsp/Wavetables.cs`, new `Runtime/Forge/Dsp/Warp.cs`
  - `Runtime/Forge/Randomizer/SfxRandomizer.cs`
  - `Editor/Forge/UI/LayerStripElement.cs`, `Editor/Forge/ForgeModulation.cs`, `Editor/Forge/UI/ModRouteElement.cs`, `Editor/Forge/ForgeHelp.cs`
  - `Documentation~/SfxForge.md`
  - new `Tests/Editor/Forge/WarpTests.cs`
- Data: `Layer` gains `Warp`. Serialized `ModRoute.Target` ints are unchanged. No preset or template change.
- Also touched so far: `WarpSettings` lives in `SourceSettings.cs` beside `UnisonSettings`; `Editor/Forge/SfxForgeWindow.uss` gains the WARP stepper rules (`.forge-strip__stepper--warp`, `.forge-stepper__label`); new `Runtime/Forge/Dsp/Warp.cs.meta` and `Tests/Editor/Forge/WarpTests.cs.meta`.
- Revert path: the Phase 1 commit.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: all four assemblies, 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- *Sync checkpoint:*
  1. New recipe, one saw Oscillator layer, 600 ms.
  2. Set Warp to Sync and sweep Amount → the tone gets brighter and more nasal, with no crackle at low pitch.
  3. Set Env 2 to a 1→0 ramp and drag Env 2 onto the Warp knob → a laser "pew" that falls in timbre. The knob shows the Env 2 arc.
  4. Add an LFO 1 route to Warp → a wobbling sync, audible at 6 Hz.
- *All modes:* on the same layer, step through Bend, Squeeze, Pulse and Quantize at Amount 0.7 → each is clearly distinct, and Quantize sounds lo-fi.
- *Wavetable:* set the layer to Wavetable Metallic with Sync → it works the same way.
- *Randomize:* Randomize with Sync set → layer 1 keeps Warp. Ctrl+Z restores everything.

### Verification Results
- 2026-10-05, agent, at the Sync checkpoint: `forge_check.py` compiled Core (54 files), Runtime (2), Editor (30) and Tests (22) with Unity's Roslyn: 0 errors, no Forge warnings. Unity was not focused, so Burst and the EditMode suite did not run.
- 2026-10-05, agent: hint scan: 67 `ForgeHints.Set` calls, 0 missing; `Layers` "Warp" resolves. The one "unresolved" line is the known regex miss in `SfxForgeWindow.cs` (Play).
- 2026-10-05, agent: a numpy prototype of `WarpOscillator.NextSync` on the same Classic tables passed the `WarpTests` thresholds with margin:
  - centroid at Amount 0 / 0.5 / 1: sine at 110 Hz 119 / 461 / 1805 Hz; saw at 110 Hz 2592 / 3420 / 4836 Hz (test asks ×1.15 per step);
  - an Env 2 0→1 ramp on a 110 Hz saw: first quarter 3773 Hz, last quarter 5866 Hz (test asks ×1.2);
  - peak over −48..+48 st, Amount 0.5–1, the four Classic waves and approximated Pulse, Harmonic and Formant banks: ≤ 1.27 with the SR/4 cap. Without the cap a square reached 1.67, which is why FS2-D13 exists.
- Build notes: Warp Off branches once per layer in `LayerRenderJob.Execute`, and `EvaluateControl` skips the whole warp lane unless the layer is warped, so no old recipe runs new math. The public wavetable banks are built by the same arithmetic, moved into `WriteLevels`.
- Pending: the user runs the EditMode suite (`WarpTests`, plus the existing suites unchanged) and the Sync checkpoint playtest above.
- 2026-10-05, user: `SyncAtFullAmount_OnWavetableIsFiniteAndBounded(Formant, 0)` and `(Formant, 30)` failed the non-silent floor: peak 0.0228 against > 0.05.
  - The DSP is correct. At those pitches the slave reads mip level 8, which keeps only harmonic 1. The Formant bank at position 0.7 peaks near harmonic 23, and its fundamental is the `0.3/√h` floor, about 0.023 after full-band peak normalisation.
  - Both pitches land on the same level (pitch 30 through the SR/4 cap), so the peaks match.
  - The test floor is now 0.01, with a why-comment. Re-run pending.

### Phase Summary
_(write when the phase completes)_

## Phase 3: Distortion modes
Status: Complete (unverified by user)

- [x] Model: append `HardClip = 2`, `SineFold = 3`, `BitCrush = 4`, `Downsample = 5` to `DistortionMode`.
- [x] `Distortion.Shape` gains HardClip (`clamp(x, −1, 1)`) and SineFold (`sin(x × π/2)`). Both run in the existing oversampled `Process`.
- [x] Base-rate path for BitCrush and Downsample:
  - `Distortion.Process` branches before the oversampling loop for these two modes;
  - BitCrush: quantize to `2^bits` levels, with bits = `lerp(16, 2, DriveDb / 36)` from `FxParams`;
  - Downsample: sample-and-hold every `round(DbToLinear(DriveDb))` frames, per channel;
  - both then blend by Mix.
  - `FxParams` keeps `DistortionDrive` linear. Pass `DriveDb` (or the derived bits and hold) beside it so Tanh and Foldback stay bit-identical.
- [x] UI:
  - the distortion stepper lists the new modes;
  - the Drive knob's label and readout follow the mode (Drive dB, Bits, Hold ×N), while the stored value stays `DriveDb`;
  - `FxGraphBuilder.FillDistortion` draws Downsample as a sine before (grey) and after (orange), since it has no static curve. The other modes keep the shaping curve; Bit Crush draws as a staircase.
- [x] Help: update `ForgeHelp` "Distortion". Docs: the FX table row and the mode list.
- [x] Tests (add to `Tests/Editor/Forge/FxTests.cs`):
  - Tanh and Foldback outputs are unchanged. The existing tests cover this; add a hash check if they compare loosely.
  - HardClip peak ≤ 1.1 (halfband ringing allowance).
  - SineFold is odd-symmetric.
  - BitCrush at 36 dB gives at most 4 distinct output levels (Mix 1).
  - Downsample at 18 dB holds runs of 8 equal frames.
  - Every mode at Drive 0 and Mix 1 renders finite.

### Affected Assets
- Code: `Runtime/Forge/Model/ForgeEnums.cs`, `Runtime/Forge/Dsp/Fx/Distortion.cs`, `FxParams.cs`, `Editor/Forge/SfxForgeWindow.cs`, `Editor/Forge/FxGraphBuilder.cs`, `Editor/Forge/ForgeHelp.cs`, `Documentation~/SfxForge.md`, `Tests/Editor/Forge/FxTests.cs`.
  - Also touched: `Runtime/Forge/Render/FxChainJob.cs` (passes the dB), `Editor/Forge/UI/KnobElement.cs` (`KnobFormat.Bits`, `KnobFormat.Hold`), `Editor/Forge/SfxForgeWindow.uss` (mode stepper 96 → 108 px so "Downsample" fits).
- Data: none new. Recipes that use mode 0 or 1 are unchanged.
- Revert path: the Phase 2 commit.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: all four assemblies, 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- Load the Laser preset, open the FX page and enable Distortion.
- Step through the modes at Drive 18 dB:
  - Hard Clip is buzzier than Tanh;
  - Sine Fold is hollow and metallic;
  - Bit Crush sounds 8-bit, and its graph is a staircase;
  - Downsample is gritty and aliased, and its graph shows the held steps.
- On Bit Crush, the knob reads in bits. Drag the Energy macro → bits drop, because the `Drive` target works.
- Switch back to Tanh → it sounds exactly as before.

### Verification Results
- 2026-10-05, agent: `forge_check.py` compiled Core (54 files), Runtime (2), Editor (30) and Tests (22) with Unity's Roslyn: 0 errors, no Forge warnings. Unity was not focused, so Burst and the EditMode suite did not run.
- 2026-10-05, agent: hint scan: 67 `ForgeHints.Set` calls, 0 missing. The one "unresolved" line is the known regex miss in `SfxForgeWindow.cs` (Play).
- 2026-10-05, agent: the real `Distortion.cs`, `AudioMath.cs`, `ForgeEnums.cs` and `FxChain.cs` were compiled into a net8 exe with a `NativeArray` shim, and the new test bodies were run managed (float32, as the EditMode tests run, not Burst). All passed:
  - max drive (36 dB) on noise, every mode: peak ≤ 1.4403 against the 1.45 bound (HardClip 1.4403, Tanh 1.4400, SineFold 1.3900, Foldback 1.2474, BitCrush 1, Downsample 0.9998);
  - Drive 0 / Mix 1, every mode: finite, peak ≤ 1.25;
  - HardClip, 0.9 sine at 220 Hz, 18 dB: peak 1.0086 (test asks ≤ 1.1 and > 0.95);
  - SineFold: shape and `Process` symmetry error exactly 0 (test allows 1e-6); `Shape(3)` = −1;
  - BitCrush at 36 dB: 3 levels (−1, 0, 1); test asks ≤ 4;
  - Downsample at 18 dB: hold 8, every run of 8 frames equals its first input frame exactly;
  - Drive 0: BitCrush error 1.526e-5 (half a 16-bit step; test allows 2e-5), Downsample exact;
  - Tanh and Foldback: bit-identical to the verbatim pre-FS2 reference in the test.
  - Bits by dB 0..36: 16 16 15 15 14 14 14 13 13 12 12 12 11 11 11 10 10 9 9 9 8 8 7 7 7 6 6 6 5 5 4 4 4 3 3 2 2. Hold: ×1 up to 3 dB, ×2 at 4 dB, ×8 at 18 dB, ×63 at 36 dB.
- Build notes:
  - Tanh and Foldback run the same float operations as before: `Shape` became a switch whose Tanh and Foldback arms are the old expressions, and `DistortionDrive` is computed from the same clamp, now in `FxParams.DriveDbOf`. The two crush branches return before the oversampling loop.
  - The existing `Distortion_MaxDriveStaysBounded([Values] mode)` now covers all six modes at 36 dB.
- Pending: the user runs the EditMode suite and the Phase 3 playtest above.

### Phase Summary
Done on 2026-10-05; the user has not yet run the tests or the playtest.

- `DistortionMode` gained `HardClip = 2`, `SineFold = 3`, `BitCrush = 4` and `Downsample = 5`, appended.
- Hard Clip (`clamp`) and Sine Fold (`sin(x·π/2)`) go through the existing 2× path. Bit Crush and Downsample branch off before it at the base rate (FS2-D6), then blend by Mix with a form that is exact at Mix 1 (FS2-D19).
- `Distortion.Process` now takes `driveDb` beside the linear drive. `FxParams.DistortionDriveDb` carries it (FS2-D18). Bits and hold come from `Distortion.BitsFor` and `HoldFor`, which the DSP, the knob readout and the graph all use.
- Bit Crush uses whole bits, 16 down to 2, on symmetric levels around an exact 0 (FS2-D17).
- FX page:
  - the stepper lists all six modes;
  - the Drive knob reads Drive (dB), Bits ("9 bits") or Hold ("×8") by mode, and stays bound to `Fx.Distortion.DriveDb` with `ModTarget.Drive` (FS2-D21);
  - the graph draws Bit Crush as a staircase and Downsample as a test wave before and after (FS2-D22).
- `ForgeHelp` "Distortion" and the docs (FX table row, plus a new paragraph on the modes) describe the modes and the relabelled knob.
- New `FxTests`:
  - Tanh and Foldback bit-identical to a verbatim copy of the original algorithm (FS2-D23);
  - HardClip ringing bound on a low note (FS2-D20);
  - SineFold odd symmetry and fold-back;
  - BitCrush at most 4 levels at 36 dB;
  - Downsample runs of 8 at 18 dB;
  - every mode finite at Drive 0;
  - Bit Crush and Downsample near-clean at Drive 0.
- Next: the user verifies Phase 3, and Phase 2 resumes after its checkpoint. Phase 4 waits for Phase 2 (`ModTarget` layout).

## Phase 4: OTT compressor
Status: Complete (unverified by user)
Depends on: Phase 2 (`ModTarget` layout)

- [x] Model: a `CompressorSettings` struct on `FxChain`:
  - fields `Enabled`, `Depth`, `Time`, `Upward`, `Downward`, `GainDb`, with range constants;
  - initializer Depth 1, Time 0.5, Upward 1, Downward 1, Gain 0, Enabled false.
  - Append `ModTarget.CompressorDepth = 14`, global and static.
- [x] DSP: a new `Runtime/Forge/Dsp/Fx/Compressor.cs`:
  - a Linkwitz-Riley 4th-order 3-band split at 88 Hz and 2.5 kHz;
  - a per-band envelope follower whose attack and release scale with Time;
  - a downward ratio above the band threshold, and an upward ratio below it with a capped boost (+24 dB);
  - no upward gain below −70 dBFS;
  - bands sum, then output Gain, then `lerp(dry, wet, Depth)`. Depth 0 is exactly dry.
  - Scratch memory is sized in `PrepareFx`.
- [x] `FxParams` and `FxChainJob`: compressor fields, run after Distortion and before Delay, skipped when disabled. `ModMatrix.AddGlobal` handles `CompressorDepth`.
- [x] FX page:
  - a **COMPRESSOR** module between Distortion and Delay, with knobs Depth (`ModTarget.CompressorDepth`), Time, Upward, Downward and Gain;
  - a graph in `FxGraphBuilder`: the static transfer curve per band setting, dB in across and dB out up, with the grey diagonal as unity, like the distortion graph.
- [x] Randomizer: `RandomizeFx` leaves the Compressor untouched (FS2-D4).
- [x] Help: a `ForgeHelp` "Compressor" item. Docs:
  - the FX chain order sentence (now 6 effects);
  - a FX table row;
  - the `CompressorDepth` target in the global targets list.
- [x] Tests (add to `Tests/Editor/Forge/FxTests.cs`):
  - A default `FxChain` renders bit-identical (Compressor off).
  - Depth 0 with the compressor on is bit-identical to off.
  - Depth 1 with Upward 0 and Downward 0: the magnitude response is flat within ±0.1 dB (the crossover sums to allpass).
  - Downward only: a −6 dBFS sine comes out quieter.
  - Upward only: a −40 dBFS sine comes out louder.
  - Digital silence in gives silence out. A −80 dBFS signal is not boosted.
  - Deterministic across two renders.

### Affected Assets
- Code:
  - `Runtime/Forge/Model/FxChain.cs`, `ForgeEnums.cs`, `ModRoute.cs` (`ModTargets.MaxAmount`, the sets)
  - `Runtime/Forge/Dsp/Fx/FxParams.cs`, new `Runtime/Forge/Dsp/Fx/Compressor.cs`
  - `Runtime/Forge/Render/FxChainJob.cs`, `SfxRenderer.cs` (`PrepareFx`), `ModMatrix.cs`
  - `Runtime/Forge/Randomizer/SfxRandomizer.cs` (only if `RandomizeFx` needs an explicit skip)
  - `Editor/Forge/SfxForgeWindow.cs` + `.uss`, `Editor/Forge/FxGraphBuilder.cs`, `Editor/Forge/ForgeHelp.cs`
  - `Documentation~/SfxForge.md`, `Tests/Editor/Forge/FxTests.cs`
- Data: `FxChain` gains `Compressor`. Old recipes and presets load it disabled.
- Revert path: the Phase 3 commit.

### Verification Plan
**Agent-runnable:**
- `forge_check.py`: all four assemblies, 0 errors. Hint scan: 0 missing.

**Playtest (human, repeatable):**
- Load the Explosion preset with Reverb on, and enable Compressor at the defaults → a denser, louder tail. After the tail the waveform view shows silence, not a noise floor. The analysis readouts show no new warnings.
- Depth 0 → identical to off, A/B by toggling the light.
- Upward 0 → only peaks are tamed. Downward 0 → only quiet detail comes up.
- The graph changes shape with Upward and Downward.
- The FX page at 900 px fits 6 modules without clipping.

### Verification Results
- 2026-10-05, agent: `forge_check.py` compiled Core (55 files), Runtime (2), Editor (30) and Tests (22) with Unity's Roslyn: 0 errors, no Forge warnings. Unity was not focused, so Burst and the EditMode suite did not run.
- 2026-10-05, agent: hint scan: 67 `ForgeHints.Set` calls, 0 missing; the one "unresolved" line is the known Play regex miss. The FX modules pass their hint name through `FxModule`'s parameter, which the scan cannot follow, so a separate check matched all six names (Transient, Distortion, Compressor, Delay, Reverb, Limiter) against `ForgeHelp.Fx`: 0 missing.
- 2026-10-05, agent: the real `Compressor.cs` and `AudioMath.cs` were compiled into a net8 exe with a `NativeArray` shim, and the process-level test bodies were run managed (float32, not Burst). All passed:
  - Depth 1, Upward 0, Downward 0, sine at 0.5: worst level change 0.0014 dB at 48 kHz over 20 Hz–20 kHz (the test checks 30, 88, 200, 1000, 2500, 6000 and 16000 Hz against ±0.1 dB). Also 0.0057 dB at 44.1 kHz, 0.021 dB at 192 kHz, 0.00003 dB at 8 kHz.
  - Downward only, −6 dBFS sine at 1 kHz: −3.91 dB (test asks < −2). Upward only, −40 dBFS: +7.54 dB (test asks > +3).
  - Digital silence at +12 dB Gain: every sample exactly 0. A −80 dBFS sine at the defaults: −0.00001 dB (test asks ≤ +0.1).
  - Depth 0 with +12 dB Gain: bit-exact. Two runs on the same noise: bit-identical.
  - Full-scale noise at +12 dB Gain: peak 4.36 at Time 0 and 5.33 at Time 1 (test bound 8).
- 2026-10-05, agent, observations from the same run (not tests):
  - A 0.9 sine burst out of silence peaks at 1.002 at the defaults: no onset spike.
  - Noise decaying at 90 dB/s, in/out RMS per 100 ms: −8/−15, −17/−22, −26/−26, −36/−30, −44/−35, −54/−43, −62/−47, −72/−56, −81/−80, −89/−89. The tail comes up, then drops back to the input below the floor (FS2-D27).
  - The same −6 dBFS Downward-only test at 60 Hz gives only −0.19 dB, and at 6 kHz −3.5 dB (FS2-O9).
  - About 18 ms per stereo second managed (.NET 8 JIT); Burst should be well under that.
- Build notes:
  - `SfxRenderer.PrepareFx` is unchanged: the compressor streams in one pass and keeps its state in locals (FS2-D28).
  - No `.uss` change: five small knobs (52 px each) and the graph fit the 720 px module, and "Downward" is no longer than Delay's "Feedback".
  - `SfxRandomizer` is unchanged: `RandomizeFx` and `MutateFx` assign the four other effects field by field and never replace the `FxChain`. A new test pins this.
  - The renderer tests (`Compressor_DefaultIsOffAndDisabledSettingsRenderBitIdentical`, `Compressor_DepthZeroRendersBitIdenticalToOff`, `Compressor_FullChainIsDeterministicAndChangesTheSound`), the randomizer test and the target test need Unity; they were compiled but not run.
- Pending: the user runs the EditMode suite and the Phase 4 playtest above.

### Phase Summary
Done on 2026-10-05; the user has not yet run the tests or the playtest.

- `FxChain.Compressor` (`CompressorSettings`: Enabled, Depth, Time, Upward, Downward, GainDb, `MaxGainDb` 12) starts disabled at Depth 1, Time 0.5, Upward 1, Downward 1, Gain 0. A zero struct (old assets, the randomizer) is also disabled.
- `ModTarget.CompressorDepth = 14`, appended. It is global and static: `ModTargets.IsPerLayer`/`IsContinuous` leave it out, `MaxAmount` is 1, `ModMatrix.AddGlobal` adds it to `GlobalModulation.CompressorDepth`, and `FxParams` saturates Depth + route. The route rows show it as a percentage.
- New `Runtime/Forge/Dsp/Fx/Compressor.cs`:
  - LR4 split at 88 Hz and 2.5 kHz (two Butterworth biquads per filter). The low band also runs the 2.5 kHz allpass, so the untouched bands sum to an allpass of the input.
  - One shared static curve for all bands (FS2-D24): 6:1 down above −24 dB, 4:1 up below −30 dB capped at +24 dB, +10 dB makeup with Downward. Boost and makeup fade in over −70..−60 dB, so nothing below −70 dB is lifted.
  - Detection (FS2-D26): stereo-linked instant peak per band with release; the attack slews the gain. Times by band at Time 0.5: attack 40/20/10 ms, release 280/200/130 ms. Time scales them ×0.25 to ×4. The floor reads a separate 50 ms detector (FS2-D27).
  - Depth blends each band from unity to its gain, so the dry is the band sum (FS2-D25). Depth 0 returns before touching the buffer.
- `FxChainJob` runs it after Distortion and before Delay, only when enabled. No scratch memory is needed (FS2-D28).
- FX page: a COMPRESSOR module between Distortion and Delay with Depth (`ModTarget.CompressorDepth`), Time, Upward, Downward and Gain (±12 dB, bipolar). Its graph is the static curve, −80..0 dB on both axes, with Depth and Gain applied as the DSP applies them and the grey diagonal as unity (FS2-D29).
- `ForgeHelp.Fx` gained a "Compressor" item, which is also the module's hint. Docs: the chain sentence lists six effects, a FX table row, a paragraph on the module, and Compressor Depth in the global targets.
- New `FxTests` (FS2-D30): max settings bounded; neutral bands flat within ±0.1 dB at seven frequencies; Downward quieter; Upward louder; silence and −80 dBFS untouched; Depth 0 bit-exact; the target is global, static and moves Depth; disabled settings and Depth 0 render bit-identical to off; deterministic with the compressor on; Randomize and Mutate leave it alone.
- Next: the user verifies Phases 2–4; Phase 2 resumes after its checkpoint.

## Decisions & Open Questions
- **FS2-D1 (decided):** Warp lives on `Layer` beside Unison, not inside `SourceSettings`, and Randomize/Mutate carry it like Unison. A Sync laser should survive a Randomize; it is the user's voicing, as with FSF-D4.
- **FS2-D2 (decided):** Warped oscillators read band-limited tables instead of PolyBLEP, because phase warping moves the edges PolyBLEP corrects. The internal Classic set is appended after the public banks, so the `WavetableBank` enum and its stepper are unchanged.
- **FS2-D3 (decided):** Warp gets a separate fifth lane rather than widening `float4` to `float4x2`/`float8`. The four existing lanes keep their exact summation order, which is what keeps every old recipe bit-identical.
- **FS2-D4 (decided):** The randomizer never touches the Compressor, like Unison and Warp. It is a mix decision. `LockFx` already exists for the other effects.
- **FS2-D5 (decided):** The Compressor sits after Distortion and before Delay. Boosting reverb tails upward lifts their noise and makes the release pump. After distortion, it evens out the drive before the space effects.
- **FS2-D6 (decided):** BitCrush and Downsample skip oversampling. Their aliasing is the sound; the halfband filter would smooth it away.
- **FS2-D7 (decided):** The Shepard window is a Gaussian in octaves, lowered by its own value at the stack edges (±Partials/2) and rescaled to peak at 1. Partials leave and enter at exactly zero, so the slot rotation cannot click. Width maps 0–1 linearly to σ 0.35–3 octaves. A new partial enters with phase 0; any phase works at zero level.
- **FS2-D8 (decided):** The stack is normalised by the window's power (1/√Σa²), not its sum. Loudness then stays constant through every hand-over: summing gave a 1.2–2.7 dB throb at the glide rate for narrow widths, and up to 8 dB less level at wide ones. Octave stacks crest about 2 dB above a sine, so a fixed 0.8 brings the full-stack peak back near 1.
- **FS2-D9 (decided):** Partials fade linearly between phase increments 0.35 and 0.45, and are silent at or above 0.45. Normalisation uses the window before this fade, so a high centre loses its top partials instead of pumping up the rest. The centre itself is still capped at SR/4 by `EvaluateControl`.
- **FS2-D10 (decided):** The bit-identical proof for Phase 1 is `ShepardTests.ShepardSettingsOnOtherSources_RenderBitIdentical`: non-default and zero `ShepardSettings` on Oscillator, Wavetable, FM (3-voice) and Noise layers leave the output bit-identical. The existing Unison, Modulation and Determinism suites also stay unchanged. A golden hash against `b360d13` needs a Burst render, which the agent cannot run (see FS2-O3).
- **FS2-D11 (decided):** `Partials` is an `int`, like `Unison.Voices`, so its knob is bound by hand through a shared `SetInt` helper, which replaces `SetVoices`. `KnobFormat.OctavesPerSecond` is appended to the editor-only `KnobFormat` enum, which is never serialized.
- **FS2-D12 (decided):** Warped layers render in their own `RenderWarp` loop for every voice count. It sums voices to stereo before two filters, like `RenderUnison`; a single voice gets gain √2·cos(π/4) ≈ 1, so Sync at Amount 0 on a mono Wavetable matches Warp Off within float rounding. `Execute` picks it once per layer before the mono/unison split, so Off layers run today's two loops unchanged and no per-sample warp branch exists.
- **FS2-D13 (decided):** The sync slave's phase increment is capped at 0.25 (SR/4) by lowering the ratio for high notes. The top mip level is empty and a faster slave would alias, which pushed a synced square to a 1.67 peak in the prototype. With the cap, Amount 1 (16×) brightens fully up to about 750 Hz at 48 kHz and less above it.
- **FS2-D14 (decided):** The WARP Mode stepper is hand-built (arrows plus a menu button, as the route rows' layer stepper), because an `EnumField` cannot hide the modes that are not implemented yet. It offers only Off and Sync for now. `SfxRenderer.WarpModeOf` plays any other stored mode, and any source other than Oscillator or Wavetable, as Off.
- **FS2-D15 (decided):** The Classic tables hold the Fourier series of `PolyBlepOscillator`'s own shapes (saw `2t − 1`, square high then low, triangle peaking at 0) at their true amplitude, not peak-normalised like the public banks. A warped saw then starts as loud as the PolyBLEP saw (within 0.25 dB, tested) and resets at the same point of its cycle.
- **FS2-D16 (decided):** The help strings that list the continuous targets (Modulation topic: Env 2/3, Rnd mode, Greyed out) and `ForgeModulation.UnsupportedReason` now include Warp, since a route to Warp works from the first checkpoint. `Documentation~/SfxForge.md` waits for the Docs item after the checkpoint.
- **FS2-D17 (decided):** Bit Crush uses whole bits, `round(lerp(16, 2, DriveDb/36))`, on symmetric mid-tread levels: `round(x·s)/s` with s = 2^(bits−1) − 1, clamped to ±1. Two bits is therefore 3 levels (−1, 0, 1).
  - Fractional bits made the level count unstable: float round-off at 2 bits could give 5 levels.
  - Two's-complement levels (4 at 2 bits) drop the positive peak to 0.5 and add DC (−0.06 on full-scale noise).
  - Silence stays exactly 0, so analysis and tail cut-off still work.
  - Drive is a static global target, so whole-bit steps never zipper during a render.
- **FS2-D18 (decided):** `Distortion.Process` takes `driveDb` beside the linear `drive`, carried by the new `FxParams.DistortionDriveDb`. Bits and hold are derived inside by `Distortion.BitsFor` and `HoldFor`, which the knob readout and the graph also call, so all three agree. `DistortionDrive` keeps its exact expression through `FxParams.DriveDbOf`.
- **FS2-D19 (decided):** The crush modes blend as `wet·mix + dry·(1 − mix)`, not `math.lerp`. Lerp's `x + (y − x)` can land an ulp off `y` at Mix 1, which would smear the levels and the held runs. The oversampled modes keep `math.lerp`, which keeps Tanh and Foldback bit-identical.
- **FS2-D20 (decided):** The plan's "HardClip peak ≤ 1.1" only holds for a low note at moderate drive.
  - A numpy prototype of the 2× path gave 1.009 for a 0.9 sine at 220 Hz and 18 dB.
  - At 1 kHz and 36 dB it gave 1.125, and Tanh gives the same 1.125. High notes ring up to the 1.44 tap-sum bound.
  - So the test uses 220 Hz at 18 dB. The extremes are covered by the existing max-drive test (< 1.45), which now runs all six modes.
- **FS2-D21 (decided):** The Drive knob's label and readout follow the mode through the new editor-only `KnobFormat.Bits` and `KnobFormat.Hold` (never serialized, as FS2-D11). `RefreshSelectors` re-applies them on every recipe change, undo and recipe switch. Route rows for the Drive target still show dB, because a route adds dB to the stored value.
- **FS2-D22 (decided):** The Downsample graph runs the real `Process` on one cycle of a cosine. Starting at the peak keeps the steps visible even at ×63, where a sine starting at 0 would look flat. Bit Crush stays a transfer curve through `Distortion.Crush`, so it draws as a staircase from about 6 bits down. The mode stepper is now 108 px wide (was 96) so "Downsample" fits.
- **FS2-D23 (decided):** The Phase 3 bit-identical proof is `FxTests.Distortion_TanhAndFoldbackMatchTheOriginalAlgorithm`. It compares, bit for bit, against a verbatim copy of the `b360d13` `Process` kept in the test file. It runs managed; the Burst path executes the same float operations but is not compared (see FS2-O3).
- **FS2-D24 (decided):** The compressor has one static curve, shared by the three bands. OTT tunes each band separately, but band controls are a non-goal, and one curve keeps the graph honest.
  - Down: above −24 dB at 6:1. Downward scales the slope, up to 5/6 dB of cut per dB.
  - Up: below −30 dB at 4:1. Upward scales the slope, up to 0.75 dB of boost per dB, capped at +24 dB.
  - Makeup: +10 dB × Downward, so Upward 0 and Downward 0 is exactly unity (the flat test relies on it).
  - Floor: boost and makeup fade in linearly over −70..−60 dB of band level. Nothing below −70 dB is lifted, and there is no step at −70 for a signal to flutter across.
  - At the defaults: 0 dB → −10, −24 → −14, −40 → −22.5, −60 → −27.5, −65 → −48, −70 and below unchanged.
- **FS2-D25 (decided):** Depth blends each band as `lerp(1, gain × Gain, Depth)`, so the dry side is the band sum (an allpass of the input), not the raw input. Mixing the raw input with phase-shifted bands would notch the crossovers at partial Depth; at 50% the crossover allpass is near −180° around 88 Hz and would cancel there. Depth 0 returns before any filtering, so it is bit-exact dry.
- **FS2-D26 (decided):** Detection is per band and stereo-linked (`max(|L|, |R|)`): an instant-attack peak detector with a per-band release. The attack is a one-pole slew on the gain in dB.
  - Why: an attack on the detector would briefly apply the full upward boost to a loud onset out of silence, while the envelope climbs through the quiet range. Slewing the gain starts every onset at unity. The managed run's 0.9 burst peaked at 1.002.
  - Times at Time 0.5, low/mid/high: attack 40/20/10 ms, release 280/200/130 ms (slower lows, faster highs, as in OTT). Time maps 0–1 to ×0.25..×4 (`exp2(−2..+2)`), so higher is slower.
- **FS2-D27 (decided):** The −70 dB floor reads its own fast detector (50 ms release, not scaled by Time).
  - The main detector falls only 31–67 dB/s (low to high band), so a tail decaying faster kept its boost after dropping under the floor.
  - On noise decaying at 90 dB/s, the last 200 ms came out at −55/−60 dB RMS instead of −81/−89. With the fast floor detector they come out at −80/−89.
- **FS2-D28 (decided):** No scratch memory. The compressor runs in one streaming pass, and its nine stereo biquads, envelopes and gains are locals, so `SfxRenderer.PrepareFx` is unchanged. The plan item "scratch memory is sized in `PrepareFx`" turned out to be unnecessary.
- **FS2-D29 (decided):** The graph draws `Compressor.GainDb`, the function the DSP uses, with Depth and Gain applied exactly as the DSP applies them. Both axes run −80..0 dB, so the grey corner-to-corner diagonal is unity; output above 0 dB clamps at the top. Time is not shown, because the curve is static. The floor shows as the steep rise between −70 and −60 dB.
- **FS2-D30 (decided):** The Phase 4 bit-identical proofs:
  - `Compressor_DefaultIsOffAndDisabledSettingsRenderBitIdentical`: a zero `CompressorSettings` against non-default disabled ones, through the renderer, plus the default being off;
  - `Compressor_DepthZeroRendersBitIdenticalToOff`: through the renderer;
  - `Compressor_DepthZeroLeavesTheBufferUntouched`: process level, runnable managed.
  - The flat test compares RMS over whole cycles (even frequencies in a half-second window), which measures magnitude only, since the allpass moves the phase.
- **FS2-O8 (open):** Is the default output level right? Sustained material near 0 dBFS settles about 10 dB lower at the defaults, since the +10 dB makeup does not fully restore the 6:1 cut. Transients pass during the attack, and the tail comes up. Raise `MakeupDb` if the module reads as "quieter" in the playtest; the Limiter after it catches any overshoot.
- **FS2-O9 (open):** Near a crossover, a loud tone leaks into the quieter neighbouring band, which gets that band's makeup and upward boost. A −6 dBFS 60 Hz sine with Downward only loses 0.2 dB, against 3.9 dB at 1 kHz. This is inherent to multiband upward compression with makeup. Listen on bass-heavy impacts; if it pumps or sounds uneven, link the bands' gains partly, or move the low split down.
- **FS2-O6 (open):** Is 2 bits (3 levels) too harsh for the top of the Drive range? If the playtest says so, stop at 3 bits (7 levels).
- **FS2-O7 (open):** Downsample's hold is `DbToLinear(DriveDb)`, so the first 3 dB of the knob are all ×1, and most of the travel is ×10 and above. Keep it, or map the hold to the knob more evenly?
- **FS2-O4 (open):** Is the SR/4 cap (FS2-D13) acceptable on high laser pitches, or should Amount 1 keep rising there and accept aliasing? Listen in the checkpoint playtest with a layer above 1 kHz.
- **FS2-O5 (open):** The Warp Amount knob greys out when Mode is Off, like Cutoff with the filter off. Keep it, or leave it live so routes can be set up before choosing a mode?
- **FS2-O3 (open):** Should the suite carry a golden output hash per phase? It would have to be captured in Unity, either at `b360d13` or now, since bit-identity holds. The user would paste the value, and later phases would check against it.
- **FS2-O1 (open):** Should the Laser template eventually generate Sync? That would be a later, separate change (non-goal here). Decide after the Phase 2 playtest.
- **FS2-O2 (open):** Compressor band gains (OTT's L/M/H knobs). Add them if the playtest shows the module needs tonal control. The FX page width is the constraint.
