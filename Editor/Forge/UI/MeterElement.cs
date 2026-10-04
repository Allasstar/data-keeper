using System;
using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Stereo peak meter on a dB scale with peak hold and a latching clip light; click to clear.
    // Doubles as the preview volume fader: drag sideways to set it, double-click to reset.
    [UxmlElement]
    public partial class MeterElement : VisualElement
    {
        public const string UssClassName = "forge-meter";

        private const float FloorDb = -60f;
        private const float WarnDb = -6f;
        private const float ChannelGap = 2f;
        private const float ClipLightWidth = 6f;
        private const double HoldSeconds = 1.2;
        private const float FallDbPerSecond = 30f;
        private const float DragThreshold = 2f;
        private const float FineFactor = 0.1f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty = new("--meter-track-color");
        private static readonly CustomStyleProperty<Color> LevelColorProperty = new("--meter-level-color");
        private static readonly CustomStyleProperty<Color> WarnColorProperty = new("--meter-warn-color");
        private static readonly CustomStyleProperty<Color> ClipColorProperty = new("--meter-clip-color");
        private static readonly CustomStyleProperty<Color> VolumeColorProperty = new("--meter-volume-color");

        private readonly float[] _level = new float[2];
        private readonly float[] _hold = new float[2];
        private readonly double[] _holdTime = new double[2];
        private bool _clipped;
        private double _lastUpdate;
        private float _volume = 1f;
        private float _pressX;
        private float _lastX;
        private bool _dragging;

        private Color _trackColor = new(0.06f, 0.06f, 0.07f);
        private Color _levelColor = new(0.35f, 0.8f, 0.45f);
        private Color _warnColor = new(0.95f, 0.78f, 0.25f);
        private Color _clipColor = new(0.95f, 0.3f, 0.3f);
        private Color _volumeColor = new(0.92f, 0.92f, 0.94f, 0.7f);

        public float DefaultVolume { get; set; } = 1f;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Mathf.Clamp01(value);
                MarkDirtyRepaint();
            }
        }

        public event Action<float> VolumeChanged;

        public MeterElement()
        {
            AddToClassList(UssClassName);
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => _dragging = false);
            ResetLevels(0.0);
        }

        // Levels are linear peaks over the last update interval.
        public void SetLevels(float left, float right, double now)
        {
            var elapsed = (float)(now - _lastUpdate);
            _lastUpdate = now;
            UpdateChannel(0, left, now, elapsed);
            UpdateChannel(1, right, now, elapsed);
            _clipped |= left >= SfxAnalysis.ClipThreshold || right >= SfxAnalysis.ClipThreshold;
            MarkDirtyRepaint();
        }

        public void ResetLevels(double now)
        {
            _lastUpdate = now;
            for (var c = 0; c < 2; c++)
            {
                _level[c] = FloorDb;
                _hold[c] = FloorDb;
            }

            MarkDirtyRepaint();
        }

        private float BarWidth => contentRect.width - ClipLightWidth - ChannelGap;

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            if (evt.clickCount == 2)
            {
                SetVolume(DefaultVolume);
            }
            else
            {
                _pressX = _lastX = evt.localPosition.x;
                _dragging = false;
                this.CapturePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        // Relative drag, so grabbing the meter anywhere never makes the volume jump.
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            var x = evt.localPosition.x;
            if (!_dragging && Mathf.Abs(x - _pressX) < DragThreshold) return;

            _dragging = true;
            var width = BarWidth;
            if (width > 0f) SetVolume(_volume + (x - _lastX) / width * (evt.shiftKey ? FineFactor : 1f));
            _lastX = x;
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            this.ReleasePointer(evt.pointerId);
            if (!_dragging)
            {
                _clipped = false;
                MarkDirtyRepaint();
            }

            _dragging = false;
            evt.StopPropagation();
        }

        private void SetVolume(float volume)
        {
            Volume = volume;
            VolumeChanged?.Invoke(_volume);
        }

        private void UpdateChannel(int channel, float peak, double now, float elapsed)
        {
            var db = Mathf.Max(AudioMath.LinearToDb(peak), FloorDb);

            // Instant attack, steady fall, so short transients stay readable.
            _level[channel] = Mathf.Max(db, _level[channel] - FallDbPerSecond * elapsed);

            if (db >= _hold[channel] || now - _holdTime[channel] > HoldSeconds)
            {
                _hold[channel] = db;
                _holdTime[channel] = now;
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(TrackColorProperty, out var track)) _trackColor = track;
            if (style.TryGetValue(LevelColorProperty, out var level)) _levelColor = level;
            if (style.TryGetValue(WarnColorProperty, out var warn)) _warnColor = warn;
            if (style.TryGetValue(ClipColorProperty, out var clip)) _clipColor = clip;
            if (style.TryGetValue(VolumeColorProperty, out var volume)) _volumeColor = volume;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var painter = context.painter2D;
            var barWidth = BarWidth;
            var barHeight = (rect.height - ChannelGap) * 0.5f;
            if (barWidth <= 0f || barHeight <= 0f) return;

            var warnX = Position(WarnDb) * barWidth;
            for (var c = 0; c < 2; c++)
            {
                var y = c * (barHeight + ChannelGap);
                Fill(painter, new Rect(0f, y, barWidth, barHeight), _trackColor);

                var levelX = Position(_level[c]) * barWidth;
                Fill(painter, new Rect(0f, y, Mathf.Min(levelX, warnX), barHeight), _levelColor);
                if (levelX > warnX) Fill(painter, new Rect(warnX, y, levelX - warnX, barHeight), _warnColor);

                if (_hold[c] > FloorDb)
                {
                    var holdX = Mathf.Clamp(Position(_hold[c]) * barWidth, 1f, barWidth);
                    Fill(painter, new Rect(holdX - 1f, y, 1f, barHeight), _hold[c] > WarnDb ? _warnColor : _levelColor);
                }
            }

            // In the gap between the channels, ending in a full-height tick so a low volume still reads.
            var volumeX = _volume * barWidth;
            Fill(painter, new Rect(0f, barHeight, volumeX, ChannelGap), _volumeColor);
            Fill(painter, new Rect(Mathf.Clamp(volumeX - 1f, 0f, barWidth - 1f), 0f, 1f, rect.height), _volumeColor);

            Fill(painter, new Rect(barWidth + ChannelGap, 0f, ClipLightWidth, rect.height), _clipped ? _clipColor : _trackColor);
        }

        private static float Position(float db) => Mathf.Clamp01((db - FloorDb) / -FloorDb);

        private static void Fill(Painter2D painter, Rect rect, Color color)
        {
            if (rect.width <= 0f) return;
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
