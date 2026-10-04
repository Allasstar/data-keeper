using DataKeeper.Forge.Analysis;
using DataKeeper.Forge.Dsp;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Stereo peak meter on a dB scale with peak hold and a latching clip light; click to clear.
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

        private static readonly CustomStyleProperty<Color> TrackColorProperty = new("--meter-track-color");
        private static readonly CustomStyleProperty<Color> LevelColorProperty = new("--meter-level-color");
        private static readonly CustomStyleProperty<Color> WarnColorProperty = new("--meter-warn-color");
        private static readonly CustomStyleProperty<Color> ClipColorProperty = new("--meter-clip-color");

        private readonly float[] _level = new float[2];
        private readonly float[] _hold = new float[2];
        private readonly double[] _holdTime = new double[2];
        private bool _clipped;
        private double _lastUpdate;

        private Color _trackColor = new(0.06f, 0.06f, 0.07f);
        private Color _levelColor = new(0.35f, 0.8f, 0.45f);
        private Color _warnColor = new(0.95f, 0.78f, 0.25f);
        private Color _clipColor = new(0.95f, 0.3f, 0.3f);

        public MeterElement()
        {
            AddToClassList(UssClassName);
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<PointerDownEvent>(_ =>
            {
                _clipped = false;
                MarkDirtyRepaint();
            });
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
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var painter = context.painter2D;
            var barWidth = rect.width - ClipLightWidth - ChannelGap;
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
