using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // A short line trace of a few cycles, so the wave's shape is visible rather than its envelope.
    [UxmlElement]
    public partial class ScopeElement : VisualElement
    {
        public const string UssClassName = "forge-scope";

        private const int PointCapacity = 256;
        private const float Headroom = 0.85f;
        private const int Cycles = 2;
        private const float MinWindowMs = 2f;
        private const float MaxWindowMs = 40f;

        private static readonly CustomStyleProperty<Color> TraceColorProperty = new("--trace-color");
        private static readonly CustomStyleProperty<Color> CenterColorProperty = new("--wave-center-color");

        private readonly float[] _points = new float[PointCapacity];
        private int _pointCount;

        private Color _traceColor = new(1f, 0.57f, 0.19f);
        private Color _centerColor = new(1f, 1f, 1f, 0.12f);

        public ScopeElement()
        {
            AddToClassList(UssClassName);
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // Starts at the loudest frame, aligned to a rising zero crossing so the trace holds still
        // between renders, and spans a fixed number of cycles whatever the pitch. The window is
        // clamped so noise (crossings every few samples) still shows texture and sub tones fit.
        public void SetCycles(NativeArray<float> interleaved, int channels, int firstFrame, int lastFrame, int sampleRate)
        {
            if (lastFrame - firstFrame < 2)
            {
                ClearTrace();
                return;
            }

            var loudest = firstFrame;
            var peak = 0f;
            for (var frame = firstFrame; frame < lastFrame; frame++)
            {
                var level = Mathf.Abs(Mono(interleaved, channels, frame));
                if (level <= peak) continue;
                peak = level;
                loudest = frame;
            }

            var minFrames = Mathf.Max(2, (int)(MinWindowMs * sampleRate / 1000f));
            var maxFrames = (int)(MaxWindowMs * sampleRate / 1000f);
            var start = NextRisingCrossing(interleaved, channels, loudest + 1, Mathf.Min(lastFrame, loudest + maxFrames));
            if (start < 0) start = loudest;

            var end = start;
            for (var cycle = 0; cycle < Cycles; cycle++)
            {
                var next = NextRisingCrossing(interleaved, channels, end + 1, Mathf.Min(lastFrame, start + maxFrames));
                if (next < 0) break;
                end = next;
            }

            var frameCount = Mathf.Clamp(end - start, minFrames, maxFrames);
            start = Mathf.Max(firstFrame, Mathf.Min(start, lastFrame - frameCount));
            SetTrace(interleaved, channels, start, Mathf.Min(frameCount, lastFrame - start));
        }

        private static int NextRisingCrossing(NativeArray<float> interleaved, int channels, int from, int to)
        {
            for (var frame = from; frame < to; frame++)
                if (Mono(interleaved, channels, frame - 1) < 0f && Mono(interleaved, channels, frame) >= 0f)
                    return frame;
            return -1;
        }

        private static float Mono(NativeArray<float> interleaved, int channels, int frame)
        {
            var sum = 0f;
            for (var channel = 0; channel < channels; channel++) sum += interleaved[frame * channels + channel];
            return sum;
        }

        private void SetTrace(NativeArray<float> interleaved, int channels, int firstFrame, int frameCount)
        {
            _pointCount = Mathf.Min(PointCapacity, frameCount);
            var peak = 0f;

            for (var point = 0; point < _pointCount; point++)
            {
                _points[point] = Mono(interleaved, channels, firstFrame + (int)((long)point * frameCount / _pointCount));
                peak = Mathf.Max(peak, Mathf.Abs(_points[point]));
            }

            if (peak > 0f)
                for (var point = 0; point < _pointCount; point++) _points[point] /= peak;

            MarkDirtyRepaint();
        }

        public void ClearTrace()
        {
            _pointCount = 0;
            MarkDirtyRepaint();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(TraceColorProperty, out var trace)) _traceColor = trace;
            if (evt.customStyle.TryGetValue(CenterColorProperty, out var center)) _centerColor = center;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var width = rect.width;
            var height = rect.height;
            if (width < 2f || height < 2f) return;

            var painter = context.painter2D;
            var mid = height * 0.5f;

            painter.lineWidth = 1f;
            painter.strokeColor = _centerColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, mid));
            painter.LineTo(new Vector2(width, mid));
            painter.Stroke();

            if (_pointCount < 2) return;

            var scale = mid * Headroom;
            var step = width / (_pointCount - 1);
            painter.lineWidth = 1.5f;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = _traceColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, mid - _points[0] * scale));
            for (var point = 1; point < _pointCount; point++)
                painter.LineTo(new Vector2(point * step, mid - _points[point] * scale));
            painter.Stroke();
        }
    }
}
