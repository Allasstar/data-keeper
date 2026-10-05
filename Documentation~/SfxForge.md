# SFX Forge

`Tools > Windows > SFX Forge` — a procedural sound-effect generator. Recipes are assets
(`Create > DataKeeper > Forge > SFX Recipe`) that render offline with Burst into 48 kHz stereo;
export them as WAV files or render them at runtime.

Rendering is deterministic: the same recipe and seed give bit-identical output on the same
platform.

## Window

| Area | What it holds |
|---|---|
| Top bar | Recipe, New, `< category >`, `< preset >` with save, Undo/Redo, Play/Stop/Autoplay, output meter (drag it sideways to set the preview volume, shown as the thin middle line; double-click resets) |
| Left | Randomize and Mutate, randomizer settings (harmony, variation, physics, candidates), the 8-variation grid (hover a thumbnail for its loudness, brightness and length), seed. `«` at the left of the page tabs hides the panel, `»` brings it back |
| Sound page | Length, root-note keyboard, waveform with trim handles and analysis readouts, curve editor (Pitch/Filter/Amp/Pan), layer strips |
| FX page | The FX chain, one module per effect (see below) |
| Mod page | Macros, the source panels (LFO 1–3, ENV 1–3, RND 1–3) and modulation routes |
| Export page | WAV export settings |
| Status bar | The last message, or the name and a line of help for the control under the mouse |

The window remembers the last page. Hotkeys: `Space` play/stop, `R` randomize, `M` mutate.
Right-click a knob or stepper to lock it against the randomizer. Each panel has a `?` button that
explains the panel and every control in it; click outside the card or press `Esc` to close it.

**Root note.** The keyboard next to Length (C2–B6) sets the recipe's `RootNote`, a MIDI note
that defaults to A4 (69). Every layer is transposed by its distance from A4, so each layer's
Pitch stays relative to the root. Clicking a key, or sliding across the keys, plays the sound
at that note. Runtime renders use the root note too.

## Layers and sources

Up to 6 layers, each with a source, band, pitch, filter, level, pan, start offset and voice
length (Decay), plus Pitch/Cutoff/Amp/Pan curves across the voice.

Each layer strip has the same layout:

- **Side tab:** on/off light, layer number, band colour. It turns red when the layer is locked.
- **Header:** name, band, Mute, Solo, and `…` (Duplicate, Remove, Lock Layer, Lock Curves, Clear Parameter Locks; right-clicking the header or tab opens the same menu).
- **Display:** `< >` steppers for the source type and its variant (wave, noise colour, wavetable bank, or the clip field), above two pictures of the layer's own render before mixing and FX. The left picture shows two cycles of the wave at its loudest point. The right one shows the whole layer over time, scaled to its own peak. A flat picture means the layer is silent.
- **Boxes:** **VOICE** (Pitch, Offset, Decay), **AMP** (Level, Pan), **FILTER** (type, Cutoff, Reso), **SOURCE** (the FM, Wavetable, Sample or Granular controls; hidden for Oscillator and Noise), and **UNISON** (Voices, Detune, Spread, Phase, Rnd; Oscillator, Wavetable and FM only).

`Compact` in the Layers header hides the boxes on every strip.

| Source | Notes |
|---|---|
| Oscillator | Sine, saw, square, triangle; PolyBLEP anti-aliased |
| Noise | White, pink, brown |
| Wavetable | 5 built-in banks, morphing position, mip-mapped |
| FM | 2-operator; ratio, index, index follows the amp curve |
| Sample | AudioClip, start, reverse, linear or cubic; pitch transposes the clip |
| Granular | Uses the Sample clip settings, plus grain size, density, spray and per-grain detune. The read head moves in real time, so pitch does not change duration |

**Unison and start phase** (Oscillator, Wavetable and FM):

- **Voices** (1–8) stacks copies of the source, each at 1/√Voices gain.
- **Detune** (0–100 cents) is the total width: voices are spread evenly across ±Detune/2. FM detunes the modulator with its carrier, so each voice keeps the same timbre.
- **Spread** (0–1) pans voices alternately left and right, up to ±Spread. The layer's Pan still applies on top.
- **Phase** (0–360°) sets where every voice's wave starts. **Rnd** gives each voice its own start instead, hashed from the layer seed, so each render seed sounds slightly different.
- More than one voice sums to stereo before the filter, which then runs per channel. One voice with Phase 0 and Rnd off renders exactly as before.
- Randomize and Mutate keep each layer's unison and phase (by layer index); the templates never generate them.

Sample and granular clips are read with `AudioClip.GetData`, which only returns data for
uncompressed clips or clips set to Decompress On Load.

## FX

The FX page stacks the effects in the order they run on the mix of all layers: Transient,
Distortion, Delay, Reverb, Limiter. The order is fixed. Each module has an on/off light in its
title (clicking the name works too); an effect that is off folds down to its title. The graph
on the left of each module shows what the effect does with its current settings, using a test
signal rather than your sound:

| Effect | Graph |
|---|---|
| Transient | A test hit before (grey) and after (orange) the shaper |
| Distortion | The shaping curve: input across, output up. The grey diagonal is the clean signal |
| Delay | A click and its echoes over the length of the sound |
| Reverb | The tail of a click fading over the length of the sound, 60 dB top to bottom |
| Limiter | A quiet signal with a loud burst, in dB. The red line is the ceiling; the dip after the burst is the release |

## Randomizer

Changing the category in the top bar randomizes straight away. Randomize builds a new sound from the category template (`Runtime/Forge/Templates/*.json`):
harmony-aware pitches, band separation, physics coupling and perturbed template curves. Mutate
nudges the current sound by the Variation amount. Both render a batch of candidates (Candidates,
default 24), analyse each, reject silent, clipping, DC-offset, cut-off and outlier results, and
keep the best 8 in the grid.

## Macros and modulation

Four macros — Size, Energy, Tone, Motion — sit at 0.5, which is neutral. They drive parameters
through **routes**: source → target × amount, with the amount in the target's unit.

| Source | Signal | Reaches |
|---|---|---|
| Size, Energy, Tone, Motion | Bipolar around 0.5, fixed for the render | Every target |
| LFO 1, LFO 2, LFO 3 | Recipe-level, −1..1. Shape (sine, saw, square, triangle), Rate 0.05–40 Hz, Phase 0–1 (where the cycle starts), Mode: **Retrigger** restarts at each layer's start, **Free** runs from the sound's start | Pitch, Cutoff, Level, Pan |
| Env 1 | Each layer's amp curve, 0..1 | Pitch, Cutoff, Level, Pan |
| Env 2, Env 3 | Recipe-level drawn curves, 0..1, spanning the whole sound and shared by every layer. Flat at 0 by default | Pitch, Cutoff, Level, Pan |
| Rnd 1, Rnd 2, Rnd 3 | Bipolar, from the render seed; each has its own Mode and Rate and its own values. Mode: **Constant** gives one fixed value per route and layer; **Sample & Hold** jumps to a new value at Rate; **Smooth** glides between those values at Rate (0.1–40 Hz). The moving modes give each layer its own signal on the sound's timeline | Constant: every target. Moving modes: Pitch, Cutoff, Level, Pan |

Targets are per layer (Pitch, Cutoff, Level, Pan, Decay, Resonance) or global (Length, Drive,
Reverb Mix, Delay Mix, Transient Attack, LFO Rate, LFO Depth). LFO Rate and LFO Depth act on
LFO 1 only.

The LFOs, the envelopes and moving Rnd are evaluated every 32 samples, so they only reach Pitch,
Cutoff, Level and Pan; other combinations are greyed out in the route list. Defaults leave the
sound unchanged: LFO Phase 0 in Retrigger, Rnd 1–3 in Constant, and Env 2/3 do nothing until routed.
New recipes start with default routes that
follow the design rules: Size lowers pitch and lengthens the sound, Energy adds attack, drive and
brightness, Tone tilts brightness, Motion scales LFO 1 and adds delay. A route can target all
layers or one.

**Source panels.** Below the macros, a column of tabs (LFO 1, LFO 2, LFO 3, ENV 1, ENV 2, ENV 3,
RND 1, RND 2, RND 3), each edged in its source's colour, opens one panel at a time. The window remembers the
last tab.

| Tab | Panel |
|---|---|
| LFO 1–3 | A picture of two cycles of the shape, with a line where the LFO starts (Phase), plus Shape, Mode (Retrigger or Free), Rate and Phase (0–360°). LFO 1's Rate is also a drop target, for the LFO Rate route target |
| ENV 1 | A note that Env 1 is each layer's Amp curve, and a button that opens the Sound page's curve editor on Amp |
| ENV 2, ENV 3 | A curve editor for the recipe's envelope (0..1 across the whole sound), with Draw, Grid and Reset. A drag is one undo step |
| RND 1–3 | Mode (Constant, Sample & Hold, Smooth) and, for the moving modes, Rate and a picture of layer 1's signal over the sound for the current seed. Constant shows a line of text instead, since its values don't move |

**Drag to modulate.** The source bar under the page tabs holds one coloured chip per source and
is there on every page, in the order macros, LFOs, envelopes, Rnd. A narrow window wraps it
between those groups. Drag a chip onto a highlighted knob to add a route at a quarter of the
target's range; click an LFO, Env or Rnd chip without dragging to open its tab on the Mod page:

- On a layer knob the route moves that layer only; hold Alt while dropping to move every layer.
- The LFOs, the envelopes and moving Rnd are refused on targets they can't reach, and an existing source/target/layer
  combination is refused as a duplicate. The status bar says why.
- Switching an Rnd to Sample & Hold or Smooth dims its existing routes on other targets (rows and
  chips, with no arc); switching back to Constant brings them back.

Every route that reaches a knob draws an arc for its range in the source's colour, with a dot
at the `+amount` end, and a small chip in the dial's bottom gap. Drag a chip vertically to set
the amount (Shift for fine), double-click it to zero it, or right-click it to switch the route
off, change its layers or remove it. A hollow chip is an all-layers route. LFO Depth has no
knob, so it is routed from the matrix only.

The **Routes** matrix on the Mod page shows one row per route: colour, light, source, target,
layer and a bipolar amount bar (drag it; double-click zeroes it).

## Analysis and export

Readouts under the waveform: effective length (to -60 dB below peak), true peak (dBTP), loudness
(BS.1770 gated LUFS), spectral centroid, crest factor, and warnings for clipping, DC offset or a
tail cut off by the length.

The Export page writes `Count` WAV files into an `Assets` folder, named by a template
(`sfx_{category}_{name}_{n}` by default; tokens `{category}`, `{name}`, `{n}`, `{seed}`). File 1
is the recipe as designed, the rest are filtered mutations seeded from the recipe seed, so
re-exporting writes identical files. Options: 16/24-bit PCM or 32-bit float; Auto/Mono/Stereo
(Auto writes mono when both sides match); normalize to a true-peak target or a loudness target
(capped by the peak target); trim trailing silence; fade-out. Imported clips are set to
Decompress On Load.

## Presets

The save icon writes the recipe as JSON into a preset folder (`Assets/Forge Presets` by default);
`< >` step through that folder and clicking the name lists it. Loading a preset is undoable. Clip
references are stored as asset GUIDs, so presets are editor-only; at runtime use the recipe asset.

## Runtime

Assembly `DataKeeper.Forge.Runtime`:

```csharp
AudioClip clip = SfxClipRenderer.Render(recipe, seed, variation);
```

`variation` 0 renders the recipe as designed (the seed still drives noise, grains and Random
routes); above 0 it first mutates the recipe by that amount. For many clips, keep an
`SfxClipRenderer` instance and call `RenderClip`, then `Dispose` it. Clips are mono when both
channels match.

`SfxVariationPool` (component) pre-renders `Pool Size` variations on `Awake` and plays them from a
shuffle bag — every variation once per round, never the same one twice in a row:

```csharp
pool.Play();               // PlayOneShot on the pool's AudioSource
AudioClip next = pool.Next();
```
