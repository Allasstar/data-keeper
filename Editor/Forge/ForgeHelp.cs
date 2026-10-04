namespace DataKeeper.Editor.Forge
{
    public sealed class HelpTopic
    {
        public readonly string Title;
        public readonly string Summary;
        public readonly (string Name, string Text)[] Items;

        public HelpTopic(string title, string summary, params (string, string)[] items)
        {
            Title = title;
            Summary = summary;
            Items = items;
        }
    }

    public static class ForgeHelp
    {
        public static readonly HelpTopic TopBar = new("Toolbar",
            "Pick the recipe to edit, browse presets, generate new sounds and listen. Every edit re-renders the sound instantly.",
            ("Recipe", "The SFX Recipe asset being edited. Edits are saved into it and can be undone."),
            ("New", "Creates a new recipe asset and opens it."),
            ("◀  ▶", "Load the previous or next preset in the preset folder."),
            ("Preset name", "Lists the presets in the preset folder. Pick one to load it into the current recipe (undoable), or choose another preset folder."),
            ("Save Preset", "Saves the current recipe as a JSON preset in the preset folder."),
            ("Randomize  (R)", "Builds new sounds from the category template and fills the Variations grid with the best 8."),
            ("Mutate  (M)", "Like Randomize, but nudges the current sound by the Variation amount instead of starting over."),
            ("Undo / Redo", "Step through recipe edits, same as Ctrl+Z / Ctrl+Y."),
            ("Meter", "Preview output level, left and right. The light on the right turns red when the sound clips; click the meter to reset it."),
            ("Vol", "Preview volume only. It does not change the sound, the meter or exported files."),
            ("Auto", "Plays the sound automatically shortly after each edit."),
            ("Play / Stop  (Space)", "Plays the trimmed part of the sound."));

        public static readonly HelpTopic Category = new("Category",
            "The kind of sound to make. Clicking a category sets it on the recipe and immediately randomizes new sounds from that category's template: its layers, pitch ranges, curves and FX.",
            ("Impact", "Hits, thuds and punches."),
            ("Whoosh", "Swings, swishes and fly-bys."),
            ("Magic", "Spells, shimmers and sparkles."),
            ("UI Click", "Short interface clicks and ticks."),
            ("Pickup", "Coins, items and power-ups."),
            ("Laser", "Zaps, beams and shots."),
            ("Explosion", "Blasts with a long tail."),
            ("Footstep", "Steps on different surfaces."));

        public static readonly HelpTopic Variations = new("Variations",
            "The best 8 results of the last Randomize or Mutate. Each batch renders more candidates than it keeps (Candidates in the Randomizer panel), analyses them, throws away silent, clipping or broken ones and keeps the 8 best.",
            ("Thumbnail", "Click to load that variation into the recipe and play it (undoable). Hover to see its loudness, brightness and length."),
            ("Seed", "The render seed. It drives noise, grain scatter and Random modulation routes, so another seed gives a slightly different take on the same recipe."));

        public static readonly HelpTopic Export = new("Export",
            "Writes the current sound as WAV files into the project. File 1 is the recipe as designed; with Count above 1 the others are filtered mutations seeded from the recipe seed, so exporting again writes the same files. The waveform trim is applied.",
            ("Folder  …", "Destination folder inside Assets. … opens a folder picker. Missing folders are created."),
            ("Name", "File name template. Tokens: {category}, {name} (recipe name), {n} (file number), {seed}."),
            ("Count", "How many files to write, 1 to 64."),
            ("Format", "Pcm16 or Pcm24 integer WAV, or Float32."),
            ("Channels", "Auto writes mono when left and right are identical, stereo otherwise. Mono and Stereo force it."),
            ("Normalize", "None keeps the level. Peak scales the sound to the dBTP target. Loudness scales it to the LUFS target, but never past the dBTP ceiling."),
            ("LUFS / dBTP", "Targets for Normalize: perceived loudness and true peak."),
            ("Trim", "Cuts the silent tail (below -60 dB) off the end."),
            ("Fade ms", "Short fade-out at the end so the file never ends with a click."),
            ("Export", "Writes the files. Asks before overwriting. Imported clips are set to Decompress On Load."));

        public static readonly HelpTopic Waveform = new("Length, waveform and analysis",
            "The rendered sound and measurements of it, updated after every edit.",
            ("Length knob", "Total render length. Layers still sounding at this point are cut off. Right-click to lock it against the randomizer."),
            ("Waveform", "The rendered sound. Drag the handles at the edges to trim what plays and what is exported; double-click to reset the trim."),
            ("Length", "Effective length (until the sound falls 60 dB below its peak) / render length."),
            ("Peak", "True peak in dBTP. Above 0 the sound may clip on playback."),
            ("Loudness", "Integrated loudness in LUFS: how loud the sound feels, not its peak."),
            ("Centroid", "Spectral centroid, a measure of brightness. Higher is brighter."),
            ("Crest", "Peak-to-average ratio in dB. High is punchy and transient, low is dense or squashed."),
            ("Render", "How long the last render took."),
            ("Warnings", "Clipping, DC offset, or Tail cut off when the sound is still audible at the end: raise Length or shorten the layers' Decay."));

        public static readonly HelpTopic Curves = new("Curves",
            "Shape how a parameter changes over a layer's voice, from its start to the end of its Decay. Edits go to the selected layer (click a layer strip to select it), or to every layer with Link Layers.",
            ("Pitch", "Pitch bend in semitones, added to the layer's Pitch."),
            ("Filter", "Moves the filter cutoff over time. Needs the layer's filter to be on."),
            ("Amp", "Volume envelope: attack, decay and tail of the layer."),
            ("Pan", "Moves the layer between left and right."),
            ("Link Layers", "Edits apply to all layers at once."),
            ("Draw", "Freehand: drag to draw the curve, it is simplified into points when you let go."),
            ("Grid", "Snaps points to a time grid and to round values (whole semitones on Pitch)."),
            ("Harmony", "Snaps Pitch points to the intervals of the Randomizer's Harmony mode."),
            ("Lock", "Keeps this curve when randomizing or mutating."),
            ("Reset", "Restores the default curve."),
            ("Mouse", "Double-click adds a point, drag moves it, right-click or Delete removes it, Alt-drag bends a segment."));

        public static readonly HelpTopic Layers = new("Layers",
            "A sound is up to 6 layers playing together, each with its own source, filter and envelope. Click a layer to edit its curves above. Right-click a knob or dropdown to lock it against the randomizer; right-click a layer header to lock the whole layer or its curves.",
            ("Toggle, name", "Turns the layer on or off; the name is just a label."),
            ("Band", "The frequency range the layer covers: Sub (lows), Body (mids), Click (2-6 kHz attack) or Air (highs). The randomizer keeps the layer's pitch and filter inside it."),
            ("L  M  S", "Lock (the randomizer keeps the whole layer), Mute, Solo."),
            ("Dup  ×", "Duplicate or remove the layer."),
            ("Source", "Oscillator (Wave: sine, saw, square, triangle), Noise (Color: white, pink, brown), Wavetable, FM, Sample or Granular."),
            ("Filter", "Off, LowPass, HighPass, BandPass or Notch. Cutoff sets its frequency, Reso its resonance."),
            ("FM", "Ratio: modulator frequency relative to the tone. Index: brightness and metallic bite. Index Env: how much the index follows the Amp curve."),
            ("Wavetable", "Bank picks the set of waves, Position morphs through it."),
            ("Sample", "Clip to play, Start offset into it, Rev plays it backwards, Interp (Cubic is smoother). The clip must be uncompressed or set to Decompress On Load."),
            ("Granular", "Plays the clip as many tiny overlapping grains. Grain: grain length. Density: grains per second. Spray: random scatter of where grains read. Pitch Rnd: random detune per grain."),
            ("Pitch", "Transpose in semitones."),
            ("Decay", "How long the layer's voice lasts; its curves stretch across it."),
            ("Level / Pan", "Volume in dB and stereo position."),
            ("Offset", "Delay before the layer starts."));

        public static readonly HelpTopic Modulation = new("Macros and modulation",
            "Macros are four big knobs that move many parameters at once through routes. At 50% they change nothing; turning them up or down pushes their targets in opposite directions.",
            ("Size", "By default: lower pitch, longer and roomier."),
            ("Energy", "By default: sharper attack, more drive, brighter."),
            ("Tone", "By default: brighter or darker."),
            ("Motion", "By default: deeper LFO wobble and more delay."),
            ("LFO", "A repeating wobble with a shape and Rate. It only does something when a route uses it."),
            ("Routes", "Each route is Source → Target × Amount, with the amount in the target's unit (semitones, octaves, dB...). The layer dropdown picks all layers or one."),
            ("Sources", "The four macros, LFO, Envelope (the layer's Amp curve) and Random (changes with the seed)."),
            ("+ Route / Defaults", "Add a route, or restore the default routes."),
            ("Greyed out", "LFO and Envelope can only move Pitch, Cutoff, Level and Pan; other combinations have no effect."));

        public static readonly HelpTopic Randomizer = new("Randomizer",
            "Controls how Randomize and Mutate make new sounds. Anything locked (right-click) is kept as it is.",
            ("Harmony", "Pitch relationship between layers: Unison, Fifths, Major, Minor or Dissonant."),
            ("Variation", "How far Mutate moves away from the current sound."),
            ("Physics", "How strictly parameters follow one shared physical character. High keeps sounds believable (bigger means lower and longer); low lets every parameter vary on its own for wilder results."),
            ("Candidates", "How many sounds each batch renders before the best 8 are kept. More gives better picks but takes longer."));

        public static readonly HelpTopic Fx = new("FX",
            "Effects on the mix of all layers, applied from top to bottom. Click an effect's name to turn it on or off.",
            ("Lock", "The randomizer keeps the FX settings as they are."),
            ("Transient", "Attack boosts or softens the start of the sound, Sustain the body after it."),
            ("Distortion", "Tanh is smooth saturation, Foldback is harsher. Drive sets the amount, Mix blends it with the clean sound."),
            ("Delay", "Echoes. Time between echoes, Feedback for how many repeats, Mix for their level; Ping-Pong bounces them left and right."),
            ("Reverb", "Room ambience. Size of the space, Damping darkens the tail, Mix sets its level."),
            ("Limiter", "Keeps peaks under the Ceiling. Release sets how quickly it lets go."));
    }
}
