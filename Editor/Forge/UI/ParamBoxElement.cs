using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    [UxmlElement]
    public partial class ParamBoxElement : VisualElement
    {
        public const string UssClassName = "forge-box";

        private readonly VisualElement _pill;
        private readonly Label _title;
        private readonly VisualElement _content;

        public override VisualElement contentContainer => _content;

        public Toggle Power { get; private set; }

        [UxmlAttribute]
        public string Title
        {
            get => _title.text;
            set => _title.text = value;
        }

        public ParamBoxElement() : this(null)
        {
        }

        public ParamBoxElement(string title)
        {
            AddToClassList(UssClassName);

            // Absolutely positioned so the pill can sit on the border without a negative margin,
            // which would make the layout measure the box shorter than its content.
            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.AddToClassList(UssClassName + "__header");
            hierarchy.Add(header);

            _pill = new VisualElement();
            _pill.AddToClassList(UssClassName + "__pill");
            header.Add(_pill);

            _title = new Label(title);
            _title.AddToClassList(UssClassName + "__title");
            _pill.Add(_title);

            _content = new VisualElement();
            _content.AddToClassList(UssClassName + "__content");
            hierarchy.Add(_content);
        }

        public Toggle AddPower()
        {
            // Not focusable, so the window's Space hotkey is never swallowed as a toggle press.
            Power = new Toggle { focusable = false };
            Power.AddToClassList("forge-power");
            _pill.Insert(0, Power);
            _pill.AddToClassList(UssClassName + "__pill--power");
            _title.RegisterCallback<ClickEvent>(_ => Power.value = !Power.value);
            return Power;
        }
    }
}
