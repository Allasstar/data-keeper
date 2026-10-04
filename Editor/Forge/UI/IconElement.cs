using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    public enum ForgeIcon
    {
        Play,
        Stop,
        Autoplay,
        Undo,
        Redo,
        Save,
    }

    // Drawn rather than a font glyph or a built-in editor icon: the editor font lacks most
    // symbols, and icon names change between Unity versions.
    public class IconElement : VisualElement
    {
        public const string UssClassName = "forge-icon";

        private const float Size = 14f;
        private const float Grid = 16f;

        private static readonly CustomStyleProperty<Color> ColorProperty = new("--icon-color");

        private readonly ForgeIcon _icon;
        private Color _color = new(0.84f, 0.85f, 0.86f);
        private Vector2 _origin;
        private float _scale;

        public IconElement(ForgeIcon icon)
        {
            _icon = icon;
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.customStyle.TryGetValue(ColorProperty, out var color)) _color = color;
                MarkDirtyRepaint();
            });
        }

        private Vector2 P(float x, float y) => _origin + new Vector2(x, y) * _scale;

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            _scale = Size / Grid;
            _origin = rect.center - new Vector2(Size, Size) * 0.5f;

            var painter = context.painter2D;
            painter.fillColor = _color;
            painter.strokeColor = _color;
            painter.lineWidth = 1.5f;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            switch (_icon)
            {
                case ForgeIcon.Play:
                    Triangle(painter, P(4f, 2f), P(14f, 8f), P(4f, 14f));
                    break;
                case ForgeIcon.Stop:
                    Rectangle(painter, 3f, 3f, 13f, 13f);
                    painter.Fill();
                    break;
                case ForgeIcon.Autoplay:
                    painter.BeginPath();
                    painter.Arc(P(8f, 8f), 6f * _scale, Angle.Degrees(-60f), Angle.Degrees(230f));
                    painter.Stroke();
                    Triangle(painter, P(9.5f, 0.5f), P(13.5f, 3.5f), P(9f, 5.5f));
                    Triangle(painter, P(6.5f, 5f), P(11f, 8f), P(6.5f, 11f));
                    break;
                case ForgeIcon.Undo:
                    Hook(painter, 1f);
                    break;
                case ForgeIcon.Redo:
                    Hook(painter, -1f);
                    break;
                case ForgeIcon.Save:
                    Rectangle(painter, 2f, 2f, 14f, 14f);
                    painter.Stroke();
                    Rectangle(painter, 5f, 2f, 11f, 6f);
                    painter.Fill();
                    Rectangle(painter, 5f, 10f, 11f, 14f);
                    painter.Stroke();
                    break;
            }
        }

        // An arch over the top with the arrowhead on one end; direction 1 points left (undo).
        private void Hook(Painter2D painter, float direction)
        {
            var center = P(8f, 10f);
            var radius = 5f * _scale;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(180f), Angle.Degrees(360f));
            painter.Stroke();

            var tip = 8f - 5f * direction;
            Triangle(painter, P(tip - 3f, 9f), P(tip + 3f, 9f), P(tip, 13.5f));
        }

        private void Rectangle(Painter2D painter, float left, float top, float right, float bottom)
        {
            painter.BeginPath();
            painter.MoveTo(P(left, top));
            painter.LineTo(P(right, top));
            painter.LineTo(P(right, bottom));
            painter.LineTo(P(left, bottom));
            painter.ClosePath();
        }

        private static void Triangle(Painter2D painter, Vector2 a, Vector2 b, Vector2 c)
        {
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.LineTo(c);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
