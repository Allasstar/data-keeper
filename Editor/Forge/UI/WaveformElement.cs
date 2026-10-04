using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    [UxmlElement]
    public partial class WaveformElement : VisualElement
    {
        public const string UssClassName = "forge-waveform";

        private const int BucketCapacity = 4096;
        private const float HandleGrabWidth = 6f;
        private const float MinTrimSpan = 0.01f;
        private const float WaveHeadroom = 0.95f;

        private static readonly CustomStyleProperty<Color> WaveColorProperty = new("--wave-color");
        private static readonly CustomStyleProperty<Color> CenterColorProperty = new("--wave-center-color");
        private static readonly CustomStyleProperty<Color> PlayheadColorProperty = new("--playhead-color");
        private static readonly CustomStyleProperty<Color> TrimColorProperty = new("--trim-color");
        private static readonly CustomStyleProperty<Color> TrimShadeColorProperty = new("--trim-shade-color");

        private enum Handle
        {
            None,
            Start,
            End,
        }

        private readonly float[] _min = new float[BucketCapacity];
        private readonly float[] _max = new float[BucketCapacity];
        private int _bucketCount;

        private float _playhead = -1f;
        private float _trimStart;
        private float _trimEnd = 1f;
        private Handle _dragHandle;

        private Color _waveColor = new(1f, 0.57f, 0.19f);
        private Color _centerColor = new(1f, 1f, 1f, 0.12f);
        private Color _playheadColor = Color.white;
        private Color _trimColor = new(0.35f, 0.75f, 1f);
        private Color _trimShadeColor = new(0f, 0f, 0f, 0.55f);

        public event Action<float, float> TrimChanged;

        public bool ShowTrim { get; set; } = true;

        public float TrimStart => _trimStart;
        public float TrimEnd => _trimEnd;

        public float Playhead
        {
            get => _playhead;
            set
            {
                if (_playhead == value) return;
                _playhead = value;
                MarkDirtyRepaint();
            }
        }

        public WaveformElement()
        {
            AddToClassList(UssClassName);
            generateVisualContent += Draw;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => _dragHandle = Handle.None);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public void SetTrim(float start, float end)
        {
            _trimStart = Mathf.Clamp(start, 0f, 1f - MinTrimSpan);
            _trimEnd = Mathf.Clamp(end, _trimStart + MinTrimSpan, 1f);
            MarkDirtyRepaint();
        }

        public void SetSamples(NativeArray<float> interleaved, int channels)
        {
            var frames = interleaved.Length / channels;
            _bucketCount = Mathf.Min(BucketCapacity, frames);

            for (var bucket = 0; bucket < _bucketCount; bucket++)
            {
                var first = (int)((long)bucket * frames / _bucketCount);
                var last = (int)((long)(bucket + 1) * frames / _bucketCount);
                var lo = float.MaxValue;
                var hi = float.MinValue;

                for (var frame = first; frame < last; frame++)
                {
                    for (var channel = 0; channel < channels; channel++)
                    {
                        var sample = interleaved[frame * channels + channel];
                        if (sample < lo) lo = sample;
                        if (sample > hi) hi = sample;
                    }
                }

                _min[bucket] = lo;
                _max[bucket] = hi;
            }

            MarkDirtyRepaint();
        }

        public void ClearSamples()
        {
            _bucketCount = 0;
            MarkDirtyRepaint();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(WaveColorProperty, out var wave)) _waveColor = wave;
            if (style.TryGetValue(CenterColorProperty, out var center)) _centerColor = center;
            if (style.TryGetValue(PlayheadColorProperty, out var playhead)) _playheadColor = playhead;
            if (style.TryGetValue(TrimColorProperty, out var trim)) _trimColor = trim;
            if (style.TryGetValue(TrimShadeColorProperty, out var shade)) _trimShadeColor = shade;
            MarkDirtyRepaint();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !ShowTrim) return;

            if (evt.clickCount == 2)
            {
                SetTrim(0f, 1f);
                TrimChanged?.Invoke(_trimStart, _trimEnd);
                evt.StopPropagation();
                return;
            }

            _dragHandle = HitHandle(evt.localPosition.x);
            if (_dragHandle == Handle.None) return;

            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_dragHandle == Handle.None || !this.HasPointerCapture(evt.pointerId)) return;

            var position = Mathf.Clamp01(evt.localPosition.x / Mathf.Max(1f, contentRect.width));
            if (_dragHandle == Handle.Start) SetTrim(Mathf.Min(position, _trimEnd - MinTrimSpan), _trimEnd);
            else SetTrim(_trimStart, Mathf.Max(position, _trimStart + MinTrimSpan));
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            this.ReleasePointer(evt.pointerId);
            _dragHandle = Handle.None;
            TrimChanged?.Invoke(_trimStart, _trimEnd);
            evt.StopPropagation();
        }

        private Handle HitHandle(float x)
        {
            var width = contentRect.width;
            var startDistance = Mathf.Abs(x - _trimStart * width);
            var endDistance = Mathf.Abs(x - _trimEnd * width);
            if (Mathf.Min(startDistance, endDistance) > HandleGrabWidth) return Handle.None;
            return startDistance <= endDistance ? Handle.Start : Handle.End;
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var width = rect.width;
            var height = rect.height;
            if (width < 2f || height < 2f) return;

            var painter = context.painter2D;
            var mid = height * 0.5f;
            var scale = mid * WaveHeadroom;

            painter.lineWidth = 1f;
            painter.strokeColor = _centerColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, mid));
            painter.LineTo(new Vector2(width, mid));
            painter.Stroke();

            if (_bucketCount > 0) DrawWave(painter, width, mid, scale);

            if (ShowTrim) DrawTrim(painter, width, height);

            if (_playhead >= 0f)
            {
                var x = _playhead * width;
                painter.lineWidth = 1.5f;
                painter.strokeColor = _playheadColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
        }

        private void DrawTrim(Painter2D painter, float width, float height)
        {
            var startX = _trimStart * width;
            var endX = _trimEnd * width;
            painter.fillColor = _trimShadeColor;
            if (startX > 0f) FillRect(painter, 0f, startX, height);
            if (endX < width) FillRect(painter, endX, width, height);

            DrawHandle(painter, startX, height, 1f);
            DrawHandle(painter, endX, height, -1f);
        }

        // One closed polygon: the max envelope left to right, then the min envelope back.
        private void DrawWave(Painter2D painter, float width, float mid, float scale)
        {
            var columns = Mathf.Max(1, (int)width);

            painter.fillColor = _waveColor;
            painter.BeginPath();

            for (var x = 0; x < columns; x++)
            {
                ColumnRange(x, columns, out _, out var hi);
                var point = new Vector2(x + 0.5f, mid - Mathf.Clamp(hi, -1f, 1f) * scale);
                if (x == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }

            for (var x = columns - 1; x >= 0; x--)
            {
                ColumnRange(x, columns, out var lo, out var hi);
                var top = mid - Mathf.Clamp(hi, -1f, 1f) * scale;
                var bottom = mid - Mathf.Clamp(lo, -1f, 1f) * scale;
                // At least 1 px thick so silent stretches still draw a line.
                painter.LineTo(new Vector2(x + 0.5f, Mathf.Max(bottom, top + 1f)));
            }

            painter.ClosePath();
            painter.Fill();
        }

        private void ColumnRange(int column, int columns, out float lo, out float hi)
        {
            var first = (int)((long)column * _bucketCount / columns);
            var last = Mathf.Max(first + 1, (int)((long)(column + 1) * _bucketCount / columns));
            last = Mathf.Min(last, _bucketCount);

            lo = float.MaxValue;
            hi = float.MinValue;
            for (var bucket = first; bucket < last; bucket++)
            {
                if (_min[bucket] < lo) lo = _min[bucket];
                if (_max[bucket] > hi) hi = _max[bucket];
            }
        }

        private void DrawHandle(Painter2D painter, float x, float height, float tabDirection)
        {
            painter.lineWidth = 1.5f;
            painter.strokeColor = _trimColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0f));
            painter.LineTo(new Vector2(x, height));
            painter.Stroke();

            painter.fillColor = _trimColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0f));
            painter.LineTo(new Vector2(x + 7f * tabDirection, 0f));
            painter.LineTo(new Vector2(x, 9f));
            painter.ClosePath();
            painter.Fill();
        }

        private static void FillRect(Painter2D painter, float x0, float x1, float height)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(x0, 0f));
            painter.LineTo(new Vector2(x1, 0f));
            painter.LineTo(new Vector2(x1, height));
            painter.LineTo(new Vector2(x0, height));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
