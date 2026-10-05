using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // C2–B6. Pressing or sliding across keys raises NoteClicked once per key reached.
    public class PianoElement : VisualElement
    {
        public const string UssClassName = "forge-piano";
        public const int FirstNote = 36;
        public const int LastNote = 95;

        private const float BlackWidthFactor = 0.6f;
        private const float BlackHeightFactor = 0.6f;

        private static readonly string[] PitchNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static readonly int[] WhiteIndexInOctave = { 0, -1, 1, -1, 2, 3, -1, 4, -1, 5, -1, 6 };
        private static readonly int WhiteCount = (LastNote - FirstNote + 1) / 12 * 7;

        private static readonly Color WhiteColor = new(0.8f, 0.81f, 0.83f);
        private static readonly Color WhiteHoverColor = new(0.92f, 0.93f, 0.95f);
        private static readonly Color BlackColor = new(0.1f, 0.1f, 0.11f);
        private static readonly Color BlackHoverColor = new(0.26f, 0.27f, 0.3f);
        private static readonly Color RootColor = new(1f, 0.57f, 0.19f);

        private int _rootNote = -1;
        private int _hoverNote = -1;
        private int _pressedNote = -1;

        public event Action<int> NoteClicked;

        public int RootNote
        {
            get => _rootNote;
            set
            {
                if (value == _rootNote) return;
                _rootNote = value;
                MarkDirtyRepaint();
            }
        }

        public PianoElement()
        {
            AddToClassList(UssClassName);
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => _pressedNote = -1);
            RegisterCallback<PointerLeaveEvent>(_ => SetHover(-1));
        }

        public static string NoteName(int note) => PitchNames[note % 12] + (note / 12 - 1);

        private static bool IsBlack(int note) => WhiteIndexInOctave[note % 12] < 0;

        private static int WhiteIndex(int note) =>
            (note - FirstNote) / 12 * 7 + WhiteIndexInOctave[note % 12];

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            var note = NoteAt(evt.localPosition);
            if (note < 0) return;

            _pressedNote = note;
            this.CapturePointer(evt.pointerId);
            NoteClicked?.Invoke(note);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var note = NoteAt(evt.localPosition);
            SetHover(note);
            if (_pressedNote < 0 || !this.HasPointerCapture(evt.pointerId) || note < 0 || note == _pressedNote) return;

            _pressedNote = note;
            NoteClicked?.Invoke(note);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            _pressedNote = -1;
            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void SetHover(int note)
        {
            if (note == _hoverNote) return;
            _hoverNote = note;
            MarkDirtyRepaint();
        }

        // Black keys first: they sit on top of the white keys.
        private int NoteAt(Vector2 position)
        {
            var rect = contentRect;
            if (!rect.Contains(position)) return -1;

            var whiteWidth = rect.width / WhiteCount;
            if (position.y < rect.height * BlackHeightFactor)
            {
                for (var note = FirstNote; note <= LastNote; note++)
                {
                    if (IsBlack(note) && BlackRect(note, whiteWidth, rect.height).Contains(position)) return note;
                }
            }

            var white = Mathf.Clamp((int)(position.x / whiteWidth), 0, WhiteCount - 1);
            var octave = white / 7;
            var degree = white % 7;
            for (var pitchClass = 0; pitchClass < 12; pitchClass++)
            {
                if (WhiteIndexInOctave[pitchClass] == degree) return FirstNote + octave * 12 + pitchClass;
            }

            return -1;
        }

        private static Rect BlackRect(int note, float whiteWidth, float height)
        {
            var width = whiteWidth * BlackWidthFactor;
            var center = (WhiteIndex(note - 1) + 1) * whiteWidth;
            return new Rect(center - width * 0.5f, 0f, width, height * BlackHeightFactor);
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < WhiteCount * 3f || rect.height < 8f) return;

            var painter = context.painter2D;
            var whiteWidth = rect.width / WhiteCount;
            for (var note = FirstNote; note <= LastNote; note++)
            {
                if (IsBlack(note)) continue;
                var x = WhiteIndex(note) * whiteWidth;
                FillRect(painter, new Rect(x + 0.5f, 0f, whiteWidth - 1f, rect.height), KeyColor(note, WhiteColor, WhiteHoverColor));
            }

            for (var note = FirstNote; note <= LastNote; note++)
            {
                if (IsBlack(note)) FillRect(painter, BlackRect(note, whiteWidth, rect.height), KeyColor(note, BlackColor, BlackHoverColor));
            }
        }

        private Color KeyColor(int note, Color normal, Color hover) =>
            note == _rootNote ? RootColor : note == _hoverNote ? hover : normal;

        private static void FillRect(Painter2D painter, Rect rect, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
