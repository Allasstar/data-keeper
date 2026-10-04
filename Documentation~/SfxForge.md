# SFX Forge

`Tools > Windows > SFX Forge` — a procedural sound-effect generator. Recipes are assets
(`Create > DataKeeper > Forge > SFX Recipe`) that render offline with Burst into 48 kHz stereo;
export them as WAV files or render them at runtime.

Rendering is deterministic: the same recipe and seed give bit-identical output on the same
platform.

## Window

| Area | What it holds |
|---|---|
| Top bar | Recipe, New, preset browser (◀ name ▶, Save Preset), Randomize, Mutate, Undo/Redo, output meter, Play/Stop |
| Left | Categories, the 8-variation grid (hover a thumbnail for its loudness, brightness and length), seed, Export foldout |
| Center | Length, waveform with trim handles and analysis readouts, curve editor (Pitch/Filter/Amp/Pan), layer strips |
| Right | Macros, LFO and modulation routes, randomizer settings, FX chain |

Hotkeys: `Space` play/stop, `R` randomize, `M` mutate. Right-click a knob or dropdown to lock it
against the randomizer.

## Layers and sources

Up to 6 layers, each with a source, band, pitch, filter, level, pan, start offset and voice
length (Decay), plus Pitch/Cutoff/Amp/Pan curves across the voice.

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

## Randomizer

Randomize builds a new sound from the category template (`Runtime/Forge/Templates/*.json`):
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

## Analysis and export

Readouts under the waveform: effective length (to -60 dB below peak), true peak (dBTP), loudness
(BS.1770 gated LUFS), spectral centroid, crest factor, and warnings for clipping, DC offset or a
tail cut off by the length.

The Export foldout writes `Count` WAV files into an `Assets` folder, named by a template
(`sfx_{category}_{name}_{n}` by default; tokens `{category}`, `{name}`, `{n}`, `{seed}`). File 1
is the recipe as designed, the rest are filtered mutations seeded from the recipe seed, so
re-exporting writes identical files. Options: 16/24-bit PCM or 32-bit float; Auto/Mono/Stereo
(Auto writes mono when both sides match); normalize to a true-peak target or a loudness target
(capped by the peak target); trim trailing silence; fade-out. Imported clips are set to
Decompress On Load.

## Presets

Save Preset writes the recipe as JSON into a preset folder (`Assets/Forge Presets` by default);
◀ ▶ step through that folder and the name button lists it. Loading a preset is undoable. Clip
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
