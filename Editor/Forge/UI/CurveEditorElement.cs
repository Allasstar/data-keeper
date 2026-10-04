using System;
using System.Collections.Generic;
using DataKeeper.Forge;
using DataKeeper.Forge.Dsp;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Edits a local copy of the breakpoints and reports every change through events; the owner
    // decides where the points go (one layer or all of them) and handles Undo.
    [UxmlElement]
    public partial class CurveEditorElement : VisualElement
    {
        public const string UssClassName = "forge-curve-editor";

        private const float Padding = 8f;
        private const float PointRadius = 4f;
        private const float HitRadius = 7f;
        private const float MinGap = 0.002f;
        private const int TimeGridDivisions = 16;
        private const int ValueGridDivisions = 16;
        private const float TensionDragScale = 0.01f;
        private const float FreehandEpsilon = 0.006f;

        private static readonly CustomStyleProperty<Color> CurveColorProperty = new("--curve-color");
        private static readonly CustomStyleProperty<Color> FillColorProperty = new("--curve-fill-color");
        private static readonly CustomStyleProperty<Color> GridColorProperty = new("--grid-color");
        private static readonly CustomStyleProperty<Color> ZeroColorProperty = new("--grid-zero-color");
        private static readonly CustomStyleProperty<Color> PointColorProperty = new("--point-color");
        private static readonly CustomStyleProperty<Color> SelectedColorProperty = new("--point-selected-color");
        private static readonly CustomStyleProperty<Color> StrokeColorProperty = new("--stroke-color");

        private enum DragMode
        {
            None,
            Point,
            Tension,
            Freehand,
        }

        private readonly List<Breakpoint> _points = new();
        private readonly Label _readout;

        private float _min;
        private float _max = 1f;
        private CurveUnit _unit;
        private int _selected = -1;
        private DragMode _drag;
        private int _dragSegment;
        private float _lastPointerY;
        private float[] _stroke;
        private int _strokeLastColumn;

        private Color _curveColor = new(1f, 0.57f, 0.19f);
        private Color _fillColor = new(1f, 0.57f, 0.19f, 0.12f);
        private Color _gridColor = new(1f, 1f, 1f, 0.06f);
        private Color _zeroColor = new(1f, 1f, 1f, 0.18f);
        private Color _pointColor = new(0.92f, 0.92f, 0.94f);
        private Color _selectedColor = new(1f, 0.57f, 0.19f);
        private Color _strokeColor = new(0.35f, 0.75f, 1f);

        public bool GridSnap { get; set; }
        public bool HarmonySnap { get; set; }
        public HarmonyMode Harmony { get; set; }
        public bool FreehandMode { get; set; }
        public bool IsEditing => _drag != DragMode.None;

        public event Action EditStarted;
        public event Action<List<Breakpoint>> Changed;
        public event Action EditFinished;

        public CurveEditorElement()
        {
            AddToClassList(UssClassName);
            focusable = true;
            generateVisualContent += Draw;

            _readout = new Label { pickingMode = PickingMode.Ignore };
            _readout.AddToClassList(UssClassName + "__readout");
            Add(_readout);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            RegisterCallback<PointerLeaveEvent>(_ => { if (_drag == DragMode.None) _readout.text = string.Empty; });
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // Ignored mid-drag so an external refresh never yanks a point out from under the cursor.
        public void SetCurve(IReadOnlyList<Breakpoint> points, float min, float max, CurveUnit unit, bool locked)
        {
            if (IsEditing) return;

            _points.Clear();
            for (var i = 0; i < points.Count; i++) _points.Add(points[i]);
            _min = min;
            _max = max;
            _unit = unit;
            if (_selected >= _points.Count) _selected = -1;

            EnableInClassList(UssClassName + "--locked", locked);
            MarkDirtyRepaint();
        }

        // ── Input ───────────────────────────────────────────────────────────────────

        private void OnPointerDown(PointerDownEvent evt)
        {
            var position = (Vector2)evt.localPosition;

            if (evt.button == 1)
            {
                var target = HitPoint(position);
                if (IsInterior(target)) DeletePoint(target);
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0) return;
            Focus();

            if (FreehandMode)
            {
                BeginFreehand(position);
                this.CapturePointer(evt.pointerId);
                evt.StopPropagation();
                return;
            }

            var hit = HitPoint(position);
            if (evt.clickCount == 2 && hit < 0)
            {
                AddPoint(position);
            }
            else if (hit >= 0)
            {
                _selected = hit;
                BeginDrag(DragMode.Point, evt.pointerId);
                ShowReadout(hit);
            }
            else if (evt.altKey)
            {
                _dragSegment = SegmentAt(FromLocal(position).x);
                if (_dragSegment >= 0)
                {
                    _lastPointerY = position.y;
                    BeginDrag(DragMode.Tension, evt.pointerId);
                }
            }
            else
            {
                _selected = -1;
            }

            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var position = (Vector2)evt.localPosition;

            switch (_drag)
            {
                case DragMode.Point:
                    MovePoint(_selected, position);
                    ShowReadout(_selected);
                    Changed?.Invoke(_points);
                    break;

                case DragMode.Tension:
                    BendSegment(_lastPointerY - position.y);
                    _lastPointerY = position.y;
                    _readout.text = $"tension {_points[_dragSegment].Tension:+0.00;-0.00;0.00}";
                    Changed?.Invoke(_points);
                    break;

                case DragMode.Freehand:
                    AddStroke(position);
                    break;

                default:
                    var hover = HitPoint(position);
                    if (hover >= 0) ShowReadout(hover);
                    else _readout.text = string.Empty;
                    return;
            }

            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId)) return;

            var finished = _drag;
            // Cleared before releasing so the capture-out handler does not finish twice.
            _drag = DragMode.None;
            this.ReleasePointer(evt.pointerId);

            if (finished == DragMode.Freehand) CommitStroke();
            if (finished != DragMode.None) EditFinished?.Invoke();

            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_drag == DragMode.None) return;

            _drag = DragMode.None;
            _stroke = null;
            EditFinished?.Invoke();
            MarkDirtyRepaint();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete && evt.keyCode != KeyCode.Backspace) return;
            if (!IsInterior(_selected)) return;

            DeletePoint(_selected);
            evt.StopPropagation();
        }

        private void BeginDrag(DragMode mode, int pointerId)
        {
            _drag = mode;
            EditStarted?.Invoke();
            this.CapturePointer(pointerId);
        }

        // ── Edits ───────────────────────────────────────────────────────────────────

        private void AddPoint(Vector2 position)
        {
            var location = FromLocal(position);
            var time = SnapTime(location.x);
            var value = SnapValue(location.y);

            var index = 0;
            while (index < _points.Count && _points[index].Time < time) index++;
            if (index == 0 || index >= _points.Count) return;
            if (time - _points[index - 1].Time < MinGap || _points[index].Time - time < MinGap) return;

            // Inheriting the segment's tension keeps the shape on both sides of the split similar.
            EditStarted?.Invoke();
            _points.Insert(index, new Breakpoint(time, value, _points[index - 1].Tension));
            _selected = index;
            Changed?.Invoke(_points);
            EditFinished?.Invoke();
        }

        private void DeletePoint(int index)
        {
            EditStarted?.Invoke();
            _points.RemoveAt(index);
            _selected = -1;
            Changed?.Invoke(_points);
            EditFinished?.Invoke();
            MarkDirtyRepaint();
        }

        // Endpoints stay pinned to time 0 and 1; only their value moves.
        private void MovePoint(int index, Vector2 position)
        {
            var location = FromLocal(position);
            var point = _points[index];
            point.Value = SnapValue(location.y);

            if (index == 0) point.Time = 0f;
            else if (index == _points.Count - 1) point.Time = 1f;
            else
            {
                var min = _points[index - 1].Time + MinGap;
                var max = _points[index + 1].Time - MinGap;
                point.Time = max < min ? min : Mathf.Clamp(SnapTime(location.x), min, max);
            }

            _points[index] = point;
        }

        // Dragging up always bulges the segment upward, whichever way it slopes.
        private void BendSegment(float deltaUp)
        {
            var start = _points[_dragSegment];
            var end = _points[_dragSegment + 1];
            var direction = end.Value < start.Value ? 1f : -1f;
            start.Tension = Mathf.Clamp(start.Tension + deltaUp * TensionDragScale * direction, -1f, 1f);
            _points[_dragSegment] = start;
        }

        private void BeginFreehand(Vector2 position)
        {
            var columns = Mathf.Max(2, (int)PlotRect.width + 1);
            _stroke = new float[columns];
            for (var i = 0; i < columns; i++) _stroke[i] = float.NaN;
            _strokeLastColumn = -1;

            _drag = DragMode.Freehand;
            EditStarted?.Invoke();
            AddStroke(position);
        }

        // One value per pixel column; gaps from fast mouse moves are filled by interpolation.
        private void AddStroke(Vector2 position)
        {
            var location = FromLocal(position);
            var value = SnapValue(location.y);
            var last = _stroke.Length - 1;
            var column = Mathf.Clamp(Mathf.RoundToInt(location.x * last), 0, last);

            if (_strokeLastColumn < 0 || _strokeLastColumn == column)
            {
                _stroke[column] = value;
            }
            else
            {
                var from = _stroke[_strokeLastColumn];
                var step = column > _strokeLastColumn ? 1 : -1;
                var span = Mathf.Abs(column - _strokeLastColumn);
                for (var i = 1; i <= span; i++)
                    _stroke[_strokeLastColumn + i * step] = SnapValue(Mathf.Lerp(from, value, i / (float)span));
            }

            _strokeLastColumn = column;
        }

        private void CommitStroke()
        {
            var samples = new List<Breakpoint>();
            var last = _stroke.Length - 1;
            for (var i = 0; i <= last; i++)
                if (!float.IsNaN(_stroke[i])) samples.Add(new Breakpoint(i / (float)last, _stroke[i]));
            _stroke = null;
            if (samples.Count < 2) return;

            var drawn = CurveSimplifier.Simplify(samples, FreehandEpsilon);
            var from = drawn[0].Time;
            var to = drawn[drawn.Count - 1].Time;

            var merged = new List<Breakpoint>();
            foreach (var point in _points)
                if (point.Time < from - MinGap || point.Time > to + MinGap) merged.Add(point);
            merged.AddRange(drawn);
            merged.Sort((a, b) => a.Time.CompareTo(b.Time));

            _points.Clear();
            foreach (var point in merged)
            {
                if (_points.Count > 0 && point.Time - _points[_points.Count - 1].Time < MinGap)
                    _points[_points.Count - 1] = point;
                else
                    _points.Add(point);
            }

            PinEndpoints();
            _selected = -1;
            Changed?.Invoke(_points);
        }

        private void PinEndpoints()
        {
            var first = _points[0];
            if (first.Time > 0f) _points.Insert(0, new Breakpoint(0f, first.Value));
            else _points[0] = new Breakpoint(0f, first.Value, first.Tension);

            var lastIndex = _points.Count - 1;
            var last = _points[lastIndex];
            if (last.Time < 1f) _points.Add(new Breakpoint(1f, last.Value));
            else _points[lastIndex] = new Breakpoint(1f, last.Value, last.Tension);
        }

        // ── Snapping ────────────────────────────────────────────────────────────────

        private float SnapTime(float t) =>
            GridSnap ? Mathf.Round(t * TimeGridDivisions) / TimeGridDivisions : t;

        private float SnapValue(float v)
        {
            v = Mathf.Clamp01(v);

            if (_unit == CurveUnit.Semitones && (HarmonySnap || GridSnap))
            {
                var semitones = Mathf.Lerp(_min, _max, v);
                semitones = HarmonySnap ? HarmonySets.Snap(semitones, Harmony) : Mathf.Round(semitones);
                return Mathf.Clamp01(Mathf.InverseLerp(_min, _max, semitones));
            }

            if (!GridSnap) return v;

            if (_unit == CurveUnit.Octaves)
            {
                var octaves = Mathf.Round(Mathf.Lerp(_min, _max, v) * 12f) / 12f;
                return Mathf.Clamp01(Mathf.InverseLerp(_min, _max, octaves));
            }

            return Mathf.Round(v * ValueGridDivisions) / ValueGridDivisions;
        }

        // ── Geometry ────────────────────────────────────────────────────────────────

        private Rect PlotRect
        {
            get
            {
                var rect = contentRect;
                return new Rect(rect.x + Padding, rect.y + Padding,
                    Mathf.Max(1f, rect.width - Padding * 2f), Mathf.Max(1f, rect.height - Padding * 2f));
            }
        }

        private Vector2 ToLocal(float time, float value)
        {
            var plot = PlotRect;
            return new Vector2(plot.x + time * plot.width, plot.y + (1f - value) * plot.height);
        }

        private Vector2 FromLocal(Vector2 position)
        {
            var plot = PlotRect;
            return new Vector2(
                Mathf.Clamp01((position.x - plot.x) / plot.width),
                Mathf.Clamp01(1f - (position.y - plot.y) / plot.height));
        }

        private int HitPoint(Vector2 position)
        {
            var best = -1;
            var bestDistance = HitRadius;
            for (var i = 0; i < _points.Count; i++)
            {
                var distance = Vector2.Distance(position, ToLocal(_points[i].Time, _points[i].Value));
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = i;
            }

            return best;
        }

        private int SegmentAt(float time)
        {
            for (var i = 0; i < _points.Count - 1; i++)
                if (time >= _points[i].Time && time < _points[i + 1].Time) return i;
            return -1;
        }

        private bool IsInterior(int index) => index > 0 && index < _points.Count - 1;

        // ── Drawing ─────────────────────────────────────────────────────────────────

        private void ShowReadout(int index)
        {
            var point = _points[index];
            _readout.text = $"t {point.Time:0.000}   {FormatValue(Mathf.Lerp(_min, _max, point.Value))}";
        }

        private string FormatValue(float value) => _unit switch
        {
            CurveUnit.Semitones => $"{value:+0.0;-0.0;0.0} st",
            CurveUnit.Octaves => $"{value:+0.00;-0.00;0.00} oct",
            CurveUnit.Pan => Mathf.Abs(value) < 0.005f ? "C" : value < 0f ? $"L{-value * 100f:0}" : $"R{value * 100f:0}",
            CurveUnit.Hertz => $"{value:0} Hz",
            _ => $"{value:0.00}",
        };

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(CurveColorProperty, out var curve)) _curveColor = curve;
            if (style.TryGetValue(FillColorProperty, out var fill)) _fillColor = fill;
            if (style.TryGetValue(GridColorProperty, out var grid)) _gridColor = grid;
            if (style.TryGetValue(ZeroColorProperty, out var zero)) _zeroColor = zero;
            if (style.TryGetValue(PointColorProperty, out var point)) _pointColor = point;
            if (style.TryGetValue(SelectedColorProperty, out var selected)) _selectedColor = selected;
            if (style.TryGetValue(StrokeColorProperty, out var stroke)) _strokeColor = stroke;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var plot = PlotRect;
            if (plot.width < 4f || plot.height < 4f) return;

            var painter = context.painter2D;
            DrawGrid(painter, plot);
            if (_points.Count >= 2) DrawCurve(painter, plot);
            if (_stroke != null) DrawStroke(painter);

            for (var i = 0; i < _points.Count; i++)
            {
                var selected = i == _selected;
                painter.fillColor = selected ? _selectedColor : _pointColor;
                painter.BeginPath();
                painter.Arc(ToLocal(_points[i].Time, _points[i].Value), selected ? PointRadius + 1.5f : PointRadius,
                    Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }
        }

        private void DrawGrid(Painter2D painter, Rect plot)
        {
            painter.lineWidth = 1f;
            painter.strokeColor = _gridColor;
            painter.BeginPath();
            for (var i = 0; i <= 8; i++)
            {
                var x = plot.x + plot.width * i / 8f;
                painter.MoveTo(new Vector2(x, plot.yMin));
                painter.LineTo(new Vector2(x, plot.yMax));
            }

            var step = _unit switch
            {
                CurveUnit.Semitones => 12f,
                CurveUnit.Octaves => 1f,
                CurveUnit.Pan => 0.5f,
                _ => (_max - _min) / 4f,
            };

            if (step > 0f && _max > _min)
            {
                for (var value = Mathf.Ceil(_min / step) * step; value <= _max + 1e-4f; value += step)
                {
                    var y = ToLocal(0f, Mathf.InverseLerp(_min, _max, value)).y;
                    painter.MoveTo(new Vector2(plot.xMin, y));
                    painter.LineTo(new Vector2(plot.xMax, y));
                }
            }

            painter.Stroke();

            if (_min < 0f && _max > 0f)
            {
                var y = ToLocal(0f, Mathf.InverseLerp(_min, _max, 0f)).y;
                painter.strokeColor = _zeroColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(plot.xMin, y));
                painter.LineTo(new Vector2(plot.xMax, y));
                painter.Stroke();
            }
        }

        private void DrawCurve(Painter2D painter, Rect plot)
        {
            var columns = Mathf.Max(2, (int)plot.width);

            if (_unit == CurveUnit.Gain)
            {
                painter.fillColor = _fillColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(plot.xMin, plot.yMax));
                for (var x = 0; x <= columns; x++)
                {
                    var t = x / (float)columns;
                    painter.LineTo(ToLocal(t, CurveEvaluator.Evaluate(_points, t)));
                }

                painter.LineTo(new Vector2(plot.xMax, plot.yMax));
                painter.ClosePath();
                painter.Fill();
            }

            painter.lineWidth = 2f;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = _curveColor;
            painter.BeginPath();
            for (var x = 0; x <= columns; x++)
            {
                var t = x / (float)columns;
                var point = ToLocal(t, CurveEvaluator.Evaluate(_points, t));
                if (x == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }

            painter.Stroke();
        }

        private void DrawStroke(Painter2D painter)
        {
            painter.lineWidth = 2f;
            painter.strokeColor = _strokeColor;
            painter.BeginPath();

            var last = _stroke.Length - 1;
            var drawing = false;
            for (var i = 0; i <= last; i++)
            {
                if (float.IsNaN(_stroke[i]))
                {
                    drawing = false;
                    continue;
                }

                var point = ToLocal(i / (float)last, _stroke[i]);
                if (drawing) painter.LineTo(point);
                else painter.MoveTo(point);
                drawing = true;
            }

            painter.Stroke();
        }
    }
}
