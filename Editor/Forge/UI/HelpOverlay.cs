using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Covers the whole window while open, so a click anywhere outside the card closes it
    // without also pressing whatever lies underneath.
    public class HelpOverlay : VisualElement
    {
        public const string UssClassName = "forge-help";

        private const float Margin = 8f;
        private const float AnchorGap = 4f;

        private readonly VisualElement _card;
        private readonly Label _title;
        private readonly Label _summary;
        private readonly VisualElement _items;

        private Rect _anchor;

        public bool IsOpen { get; private set; }

        public HelpOverlay()
        {
            AddToClassList(UssClassName);
            style.display = DisplayStyle.None;
            RegisterCallback<PointerDownEvent>(evt =>
            {
                Hide();
                evt.StopPropagation();
            });
            RegisterCallback<GeometryChangedEvent>(_ => Place());

            _card = new VisualElement();
            _card.AddToClassList(UssClassName + "__card");
            _card.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            _card.RegisterCallback<GeometryChangedEvent>(_ => Place());
            Add(_card);

            var header = new VisualElement();
            header.AddToClassList(UssClassName + "__header");
            _title = new Label();
            _title.AddToClassList(UssClassName + "__title");
            header.Add(_title);
            var close = new Button(Hide) { text = "×", focusable = false };
            close.AddToClassList(UssClassName + "__close");
            header.Add(close);
            _card.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList(UssClassName + "__scroll");
            _summary = new Label();
            _summary.AddToClassList(UssClassName + "__summary");
            scroll.Add(_summary);
            _items = new VisualElement();
            scroll.Add(_items);
            _card.Add(scroll);
        }

        public void Show(HelpTopic topic, VisualElement anchor)
        {
            _anchor = parent.WorldToLocal(anchor.worldBound);
            _title.text = topic.Title;
            _summary.text = topic.Summary;

            _items.Clear();
            foreach (var (name, text) in topic.Items)
            {
                var row = new VisualElement();
                row.AddToClassList(UssClassName + "__item");
                var nameLabel = new Label(name);
                nameLabel.AddToClassList(UssClassName + "__name");
                row.Add(nameLabel);
                var textLabel = new Label(text);
                textLabel.AddToClassList(UssClassName + "__text");
                row.Add(textLabel);
                _items.Add(row);
            }

            IsOpen = true;
            style.display = DisplayStyle.Flex;
            BringToFront();
            Place();
        }

        public void Hide()
        {
            IsOpen = false;
            style.display = DisplayStyle.None;
        }

        // Opens below the button, then slides left or up as needed to stay inside the window.
        private void Place()
        {
            if (!IsOpen) return;

            var bounds = layout.size;
            var card = _card.layout.size;
            if (float.IsNaN(bounds.x) || float.IsNaN(card.x)) return;

            _card.style.maxHeight = Mathf.Max(0f, bounds.y - 2f * Margin);
            var left = Mathf.Max(Margin, Mathf.Min(_anchor.x, bounds.x - Margin - card.x));
            var top = Mathf.Max(Margin, Mathf.Min(_anchor.yMax + AnchorGap, bounds.y - Margin - card.y));
            _card.style.left = left;
            _card.style.top = top;
        }
    }
}
