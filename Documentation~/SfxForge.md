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
| Sound page | Length, waveform with trim handles and analysis readouts, curve editor (Pitch/Filter/Amp/Pan), layer strips |
| FX page | The FX chain, one module per effect (see below) |
| Mod page | Macros, LFO and modulation routes |
| Export page | WAV export settings |
| Status bar | The last message, or the name and a line of help for the control under the mouse |

The window remembers the last page. Hotkeys: `Space` play/stop, `R` randomize, `M` mutate.
Right-click a knob or stepper to lock it against the randomizer. Each panel has a `?` button that
explains the panel and every control in it; click outside the card or press `Esc` to close it.

## Layers and sources

Up to 6 layers, each with a source, band, pitch, filter, level, pan, start offset and voice
length (Decay), plus Pitch/Cutoff/Amp/Pan curves across the voice.

Each layer strip has the same layout:

- **Side tab:** on/off light, layer number, band colour. It turns red when the layer is locked.
- **Header:** name, band, Mute, Solo, and `…` (Duplicate, Remove, Lock Layer, Lock Curves, Clear Parameter Locks; right-clicking the header or tab opens the same menu).
- **Display:** `< >` steppers for the source type and its variant (wave, noise colour, wavetable bank, or the clip field), above two pictures of the layer's own render before mixing and FX. The left picture shows two cycles of the wave at its loudest point. The right one shows the whole layer over time, scaled to its own peak. A flat picture means the layer is silent.
- **Boxes:** **VOICE** (Pitch, Offset, Decay), **AMP** (Level, Pan), **FILTER** (type, Cutoff, Reso), and **SOURCE** (the FM, Wavetable, Sample or Granular controls; hidden for Oscillator and Noise).

`Compact` in the Layers header hides the boxes on every strip.

| Source | Notes |
|---|---|
| Oscillator | Sine, saw, square, triangle; PolyBLEP anti-aliased |
| Noise | White, pink, brown |
| Wavetable | 5 built-in banks, morphing position, mip-mapped |
| FM | 2-operator; ratio, index, index follows the amp curve |
| Sample | AudioClip, start, reverse, linear or cubic; pitch transposes the clip |
| Granular | Uses the Sample clip settings, plus grain size, density, spray and per-grain detune. The read head moves in real time, so pitch does not change duration |

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

| Sources | Targets |
|---|---|
| Size, Energy, Tone, Motion (bipolar around 0.5) | Per layer: Pitch, Cutoff, Level, Pan, Decay, Resonance |
| Random (bipolar, from the render seed) | Global: Length, Drive, Reverb Mix, Delay Mix, Transient Attack, LFO Rate, LFO Depth |
| LFO (one per recipe, shape and rate) | |
| Envelope (the layer's amp curve, 0..1) | |

LFO and Envelope are evaluated every 32 samples, so they only reach Pitch, Cutoff, Level and Pan;
other combinations are greyed out in the route list. New recipes start with default routes that
follow the design rules: Size lowers pitch and lengthens the sound, Energy adds attack, drive and
brightness, Tone tilts brightness, Motion scales the LFO and adds delay. A route can target all
layers or one.

**Drag to modulate.** The source bar under the page tabs holds one coloured chip per source and
is there on every page. Drag a chip onto a highlighted knob to add a route at a quarter of the
target's range:

- On a layer knob the route moves that layer only; hold Alt while dropping to move every layer.
- LFO and Env are refused on targets they can't reach, and an existing source/target/layer
  combination is refused as a duplicate. The status bar says why.

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
