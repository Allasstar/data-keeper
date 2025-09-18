using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    public static class UTKFactory
    {
        public static VisualElement CreateVisualElement(UTKStyleClass style = null)
        {
            var element = new VisualElement();
            style?.ApplyTo(element);
            return element;
        }

        public static Label CreateLabel(string text, UTKStyleClass style = null)
        {
            var label = new Label(text);
            style?.ApplyTo(label);
            return label;
        }

        public static Button CreateButton(string text, EventCallback<ClickEvent> onClick = null, UTKStyleClass style = null)
        {
            var button = new Button { text = text };
            if (onClick != null)
                button.clicked += () => onClick.Invoke(null);
            style?.ApplyTo(button);
            return button;
        }

        public static TextField CreateTextField(string label = null, string value = "", UTKStyleClass style = null)
        {
            var textField = new TextField(label) { value = value };
            style?.ApplyTo(textField);
            return textField;
        }

        public static Toggle CreateToggle(string label = null, bool initialState = false, UTKStyleClass style = null)
        {
            var toggle = new Toggle(label) { value = initialState };
            style?.ApplyTo(toggle);
            return toggle;
        }

        public static Foldout CreateFoldout(string title, bool value = true, UTKStyleClass style = null)
        {
            var foldout = new Foldout { text = title, value = value };
            style?.ApplyTo(foldout);
            return foldout;
        }

        public static VisualElement CreateLabeledBox(string labelText, string contentText, UTKStyleClass style = null)
        {
            var container = CreateVisualElement(style);
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingBottom = 4;

            container.Add(CreateLabel(labelText, UTKStyleClass.LabelTitle));
            container.Add(CreateLabel(contentText));

            return container;
        }
    }
}