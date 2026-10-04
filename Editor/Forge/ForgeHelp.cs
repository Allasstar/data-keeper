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

        public string TextOf(string name)
        {
            foreach (var (itemName, text) in Items)
                if (itemName == name) return text;
            return null;
        }
    }

    public static class ForgeHelp
    {
        public static readonly HelpTopic TopBar = new("Toolbar",
            "Pick the recipe to edit, browse presets, generate new sounds and listen. Every edit re-renders the sound instantly. Hover any control to see what it does in the status bar at the bottom.",
            ("Recipe", "The SFX Recipe asset being edited. Edits are saved into it and can be undone."),
            ("New", "Creates a new recipe asset and opens it."),
            ("Category", "The kind of sound: Impact, Whoosh, Magic, UI Click, Pickup, Laser, Explosion or Footstep. Changing it randomizes new sounds from that category's template."),
            ("Preset", "The arrows load the previous or next preset in the preset folder. Click the name to list them, load one (undoable) or choose another preset folder."),
            ("Save Preset", "Saves the current recipe as a JSON preset in the preset folder."),
            ("Undo / Redo", "Step through recipe edits, same as Ctrl+Z / Ctrl+Y."),
            ("Play / Stop  (Space)", "Plays the trimmed part of the sound, or stops it."),
            ("Auto", "Plays the sound automatically shortly after each edit."),
            ("Meter", "Output level, left and right; the light on the right turns red on clipping, click to clear it. Drag sideways to set the preview volume (the thin middle line), Shift for fine, double-click to reset. Volume only affects what you hear, not the sound, the meter or exported files."));

        public static readonly HelpTopic Variations = new("Variations",
            "The best 8 results of the last Randomize or Mutate. Each batch renders more candidates than it keeps (Candidates, above), analyses them, throws away silent, clipping or broken ones and keeps the 8 best.",
            ("Thumbnail", "Click to load that variation into the recipe and play it (undoable). Hover to see its loudness, brightness and length."),
            ("Seed", "The render seed. It drives noise, grain scatter and Random modulation routes, so another seed gives a slightly different take on the same recipe."),
            ("«  »", "Hides or shows the Randomizer and Variations panel, for more room on the pages."));

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
            "A sound is up to 6 layers playing together, each with its own source, filter and envelope. Click a layer to edit its curves above. Right-click a knob or stepper to lock it against the randomizer.",
            ("Side tab", "The light turns the layer on or off. The coloured edge shows the band; the tab turns red when the whole layer is locked. Right-click it for the layer menu."),
            ("Name, band", "The name is just a label. Band is the frequency range the layer covers: Sub (lows), Body (mids), Click (2-6 kHz attack) or Air (highs). The randomizer keeps the layer's pitch and filter inside it."),
            ("M  S", "Mute, Solo."),
            ("…", "Duplicate, Remove, Lock Layer (the randomizer keeps the whole layer), Lock Curves, Clear Parameter Locks. Right-clicking the header opens the same menu."),
            ("<  >", "Steppers: the arrows step through the values and wrap around; click the name for the full list."),
            ("Pictures", "Left: two cycles of the wave at the layer's loudest point. Right: the whole layer over time, scaled to its own peak. Flat means the layer is silent: off, muted, soloed out or missing its clip."),
            ("Compact", "Hides the knob boxes on every layer so more layers fit."),
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
            ("Source bar", "Drag a source onto any highlighted knob to add a route. On a layer knob it moves that layer only; hold Alt while dropping to move every layer."),
            ("Chips", "Each route on a knob shows a chip and a coloured arc for its range, with a dot at the + end. Drag a chip up or down to set the amount (Shift for fine), double-click to zero it, right-click to switch it off, change its layers or remove it. A hollow chip is a route for all layers."),
            ("Routes", "Each row is Source → Target × Amount, with the amount in the target's unit (semitones, octaves, dB...). Drag the bar to set the amount; the layer stepper picks all layers or one."),
            ("Sources", "The four macros, LFO, Env (the layer's Amp curve) and Rnd (changes with the seed). Each has its own colour on chips, arcs and rows."),
            ("+ Route / Defaults", "Add a route, or restore the default routes."),
            ("Greyed out", "LFO and Envelope can only move Pitch, Cutoff, Level and Pan; other combinations have no effect."));

        public static readonly HelpTopic Randomizer = new("Randomizer",
            "Makes new sounds and controls how. Anything locked (right-click) is kept as it is.",
            ("Randomize  (R)", "Builds new sounds from the category template and fills the Variations grid with the best 8."),
            ("Mutate  (M)", "Like Randomize, but nudges the current sound by the Variation amount instead of starting over."),
            ("Harmony", "Pitch relationship between layers: Unison, Fifths, Major, Minor or Dissonant."),
            ("Variation", "How far Mutate moves away from the current sound."),
            ("Physics", "How strictly parameters follow one shared physical character. High keeps sounds believable (bigger means lower and longer); low lets every parameter vary on its own for wilder results."),
            ("Candidates", "How many sounds each batch renders before the best 8 are kept. More gives better picks but takes longer."));

        public static readonly HelpTopic Fx = new("FX",
            "Effects on the mix of all layers, run from top to bottom in a fixed order. The light in a title turns the effect on or off (so does clicking its name); an effect that is off folds down to its title. Each graph shows what the effect does to a test signal with the current settings.",
            ("Lock", "The randomizer keeps the FX settings as they are."),
            ("Transient", "Attack boosts or softens the start of the sound, Sustain the body after it. The graph shows a test hit before (grey) and after (orange)."),
            ("Distortion", "Tanh is smooth saturation, Foldback is harsher. Drive sets the amount, Mix blends it with the clean sound. The graph is the shaping curve, input across and output up."),
            ("Delay", "Echoes. Time between echoes, Feedback for how many repeats, Mix for their level; Ping-Pong bounces them left and right. The graph shows a click and its echoes over the length of the sound."),
            ("Reverb", "Room ambience. Size of the space, Damping darkens the tail, Mix sets its level. The graph shows the tail fading over the length of the sound; a slower fall is a longer tail."),
            ("Limiter", "Keeps peaks under the Ceiling (the red line). Release sets how quickly it lets go, seen as the dip after the loud burst in the graph."));
    }
}
