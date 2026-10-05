using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Values run 0..1 bottom to top, or -1..1 on both axes when Bipolar (a transfer curve).
    // Callers write into the spans and then call MarkDirtyRepaint.
    [UxmlElement]
    public partial class FxGraphElement : VisualElement
    {
        public const string UssClassName = "forge-fx-graph";
        public const int Capacity = 128;

        private static readonly CustomStyleProperty<Color> TraceColorProperty = new("--trace-color");
        private static readonly CustomStyleProperty<Color> FillColorProperty = new("--fill-color");
        private static readonly CustomStyleProperty<Color> ReferenceColorProperty = new("--reference-color");
        private static readonly CustomStyleProperty<Color> MarkerColorProperty = new("--marker-color");
        private static readonly CustomStyleProperty<Color> GridColorProperty = new("--grid-color");

        private readonly float[] _trace = new float[Capacity];
        private readonly float[] _reference = new float[Capacity];
        private int _traceCount;
        private int _referenceCount;

        private Color _traceColor = new(1f, 0.57f, 0.19f);
        private Color _fillColor = new(1f, 0.57f, 0.19f, 0.18f);
        private Color _referenceColor = new(1f, 1f, 1f, 0.25f);
        private Color _markerColor = new(0.9f, 0.35f, 0.35f);
        private Color _gridColor = new(1f, 1f, 1f, 0.08f);
        private bool _traceColorFromCode;

        public bool Bipolar { get; set; }

        public float Marker { get; set; } = float.NaN;

        // A vertical line, 0..1 left to right whether or not the graph is Bipolar.
        public float MarkerX { get; set; } = float.NaN;

        public FxGraphElement()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public Span<float> Trace(int count)
        {
            _traceCount = Mathf.Clamp(count, 0, Capacity);
            return _trace.AsSpan(0, _traceCount);
        }

        // Wins over the stylesheet, for graphs coloured by what they show (a mod source).
        public void SetTraceColor(Color color)
        {
            _traceColorFromCode = true;
            _traceColor = color;
            _fillColor = new Color(color.r, color.g, color.b, 0.18f);
            MarkDirtyRepaint();
        }

        public Span<float> Reference(int count)
        {
            _referenceCount = Mathf.Clamp(count, 0, Capacity);
            return _reference.AsSpan(0, _referenceCount);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (!_traceColorFromCode && evt.customStyle.TryGetValue(TraceColorProperty, out var trace)) _traceColor = trace;
            if (!_traceColorFromCode && evt.customStyle.TryGetValue(FillColorProperty, out var fill)) _fillColor = fill;
            if (evt.customStyle.TryGetValue(ReferenceColorProperty, out var reference)) _referenceColor = reference;
            if (evt.customStyle.TryGetValue(MarkerColorProperty, out var marker)) _markerColor = marker;
            if (evt.customStyle.TryGetValue(GridColorProperty, out var grid)) _gridColor = grid;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 2f || rect.height < 2f) return;

            var painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;

            if (Bipolar)
            {
                Line(painter, rect, _gridColor, 1f, new Vector2(-1f, 0f), new Vector2(1f, 0f));
                Line(painter, rect, _gridColor, 1f, new Vector2(0f, -1f), new Vector2(0f, 1f));
            }

            if (!float.IsNaN(Marker))
                Line(painter, rect, _markerColor, 1f, new Vector2(Bipolar ? -1f : 0f, Marker), new Vector2(1f, Marker));
            if (!float.IsNaN(MarkerX))
            {
                var x = Bipolar ? MarkerX * 2f - 1f : MarkerX;
                Line(painter, rect, _markerColor, 1f, new Vector2(x, Bipolar ? -1f : 0f), new Vector2(x, 1f));
            }

            if (_referenceCount > 1) Stroke(painter, rect, _reference, _referenceCount, _referenceColor, 1f);
            if (_traceCount < 2) return;

            if (!Bipolar)
            {
                painter.fillColor = _fillColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, rect.height));
                for (var i = 0; i < _traceCount; i++) painter.LineTo(Point(rect, i, _traceCount, _trace[i]));
                painter.LineTo(new Vector2(rect.width, rect.height));
                painter.ClosePath();
                painter.Fill();
            }

            Stroke(painter, rect, _trace, _traceCount, _traceColor, 1.5f);
        }

        private void Stroke(Painter2D painter, Rect rect, float[] values, int count, Color color, float width)
        {
            painter.lineWidth = width;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.MoveTo(Point(rect, 0, count, values[0]));
            for (var i = 1; i < count; i++) painter.LineTo(Point(rect, i, count, values[i]));
            painter.Stroke();
        }

        private void Line(Painter2D painter, Rect rect, Color color, float width, Vector2 from, Vector2 to)
        {
            painter.lineWidth = width;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.MoveTo(ToPixels(rect, from));
            painter.LineTo(ToPixels(rect, to));
            painter.Stroke();
        }

        private Vector2 Point(Rect rect, int index, int count, float value)
        {
            var t = (float)index / (count - 1);
            return ToPixels(rect, new Vector2(Bipolar ? t * 2f - 1f : t, value));
        }

        // Keeps a 1px inset so a full-scale trace isn't cut in half by the edge.
        private Vector2 ToPixels(Rect rect, Vector2 value)
        {
            var x = Bipolar ? (value.x + 1f) * 0.5f : value.x;
            var y = Bipolar ? (value.y + 1f) * 0.5f : value.y;
            return new Vector2(x * rect.width, 1f + (1f - Mathf.Clamp01(y)) * (rect.height - 2f));
        }
    }
}
