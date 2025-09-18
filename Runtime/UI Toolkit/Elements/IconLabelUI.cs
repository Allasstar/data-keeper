using Unity.Properties;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit.Elements
{
    [UxmlElement]
    public partial class IconLabelUI : BindableElement
    {
        private readonly Image _icon;
        private readonly Label _text;
        
        private VectorImage _iconVectorImage;
        private string _labelText = "Default Text";
        
        [UxmlAttribute("icon-vector-image"), CreateProperty]
        public VectorImage iconVectorImage
        {
            get => _iconVectorImage;
            set
            {
                _iconVectorImage = value;
                _icon.SetVectorImage(value);
            }
        }
        
        [UxmlAttribute("label-text"), CreateProperty]
        public string labelText
        {
            get => _labelText;
            set
            {
                _labelText = value;
                _text.SetText(value);
            }
        }

        public IconLabelUI()
        {
            _icon = new Image()
                .SetMargin(10)
                .SetChildOf(this);

            _text = new Label()
                .SetMarginRight(10)
                .SetText(labelText)
                .SetChildOf(this);

            this.SetFlexDirection(FlexDirection.Row)
                .SetJustifyContent(Justify.Center)
                .SetAlignItems(Align.Center);
        }
    }
}